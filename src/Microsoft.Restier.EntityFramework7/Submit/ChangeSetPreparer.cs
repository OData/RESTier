// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.ChangeTracking;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFramework.Properties;

namespace Microsoft.Restier.EntityFramework.Submit
{
    /// <summary>
    /// This class convert OData entity changes (update/remove/create) to Entity Framework entity changes (in the form of StateEntry)
    /// For this class we cannot reuse EF6 ChangeSetPreparer code, since many types used here have their type name or member name changed.
    /// </summary>
    public class ChangeSetPreparer : IChangeSetPreparer
    {
        private ChangeSetPreparer()
        {
        }

        private static readonly ChangeSetPreparer instance = new ChangeSetPreparer();

        public static ChangeSetPreparer Instance { get { return instance; } }

        private static MethodInfo _prepareEntryGeneric = typeof(ChangeSetPreparer).
            GetMethod("PrepareEntry", BindingFlags.Static | BindingFlags.NonPublic);

        public async Task PrepareAsync(
            SubmitContext context,
            CancellationToken cancellationToken)
        {
            DbContext dbContext = context.ApiContext.GetProperty<DbContext>(DbApiConstants.DbContextKey);

            foreach (var entry in context.ChangeSet.Entries.OfType<DataModificationEntry>())
            {
                object strongTypedDbSet = dbContext.GetType().GetProperty(entry.EntitySetName).GetValue(dbContext);
                Type entityType = strongTypedDbSet.GetType().GetGenericArguments()[0];
                MethodInfo prepareEntryMethod = _prepareEntryGeneric.MakeGenericMethod(entityType);

                await (Task)prepareEntryMethod.Invoke(
                    obj: null,
                    parameters: new object[] { context, dbContext, entry, strongTypedDbSet, cancellationToken });
            }
        }

        private static async Task PrepareEntry<TEntity>(
            SubmitContext context, DbContext dbContext, DataModificationEntry entry, DbSet<TEntity> set, CancellationToken cancellationToken)
            where TEntity : class
        {
            Type entityType = typeof(TEntity);
            TEntity entity;

            if (entry.IsNew)
            {
                // TODO: See if Create method is in DbSet<> in future EF7 releases, as the one EF6 has.
                entity = (TEntity)Activator.CreateInstance(typeof(TEntity));

                ChangeSetPreparer.SetValues(entity, entityType, entry.LocalValues);
                set.Add(entity);
            }
            else if (entry.IsDelete)
            {
                entity = (TEntity)await ChangeSetPreparer.FindEntity(context, entry, cancellationToken);
                set.Remove(entity);
            }
            else if (entry.IsUpdate)
            {
                entity = (TEntity)await ChangeSetPreparer.FindEntity(context, entry, cancellationToken);

                var dbEntry = dbContext.Update(entity);
                ChangeSetPreparer.SetValues(dbEntry, entry, entityType);
            }
            else
            {
                throw new NotSupportedException(Resources.DataModificationMustBeCUD);
            }

            entry.Entity = entity;
        }

        private static async Task<object> FindEntity(SubmitContext context, DataModificationEntry entry, CancellationToken cancellationToken)
        {
            IQueryable query = Api.Source(context.ApiContext, entry.EntitySetName);
            query = entry.ApplyTo(query);

            QueryResult result = await Api.QueryAsync(context.ApiContext, new QueryRequest(query), cancellationToken);

            object entity = result.Results.SingleOrDefault();
            if (entity == null)
            {
                // TODO GitHubIssue#38 : Handle the case when entity is resolved
                // there are 2 cases where the entity is not found:
                // 1) it doesn't exist
                // 2) concurrency checks have failed
                // we should account for both - I can see 3 options:
                // a. always return "PreConditionFailed" result - this is the canonical behavior of WebAPI OData (see http://blogs.msdn.com/b/webdev/archive/2014/03/13/getting-started-with-asp-net-web-api-2-2-for-odata-v4-0.aspx)
                //  - this makes sense because if someone deleted the record, then you still have a concurrency error
                // b. possibly doing a 2nd query with just the keys to see if the record still exists
                // c. only query with the keys, and then set the DbEntityEntry's OriginalValues to the ETag values, letting the save fail if there are concurrency errors

                //throw new EntityNotFoundException
                throw new InvalidOperationException(Resources.ResourceNotFound);
            }

            return entity;
        }

        private static void SetValues(EntityEntry dbEntry, DataModificationEntry entry, Type entityType)
        {
            //StateEntry stateEntry = ((IAccessor<InternalEntityEntry>) dbEntry.StateEntry;
            IEntityType edmType = dbEntry.Metadata;

            if (entry.IsFullReplaceUpdate)
            {
                // The algorithm for a "FullReplaceUpdate" is taken from WCF DS ObjectContextServiceProvider.ResetResource, and is as follows:
                // Create a new, blank instance of the entity.  Copy over the key values, and set any updated values from the client on the new instance.
                // Then apply all the properties of the new instance to the instance to be updated.  This will set any unspecified
                // properties to their default value.

                object newInstance = Activator.CreateInstance(entityType);

                ChangeSetPreparer.SetValues(newInstance, entityType, entry.EntityKey);
                ChangeSetPreparer.SetValues(newInstance, entityType, entry.LocalValues);

                foreach (var property in edmType.GetProperties())
                {
                    object val;
                    if (!entry.LocalValues.TryGetValue(property.Name, out val))
                    {
                        PropertyInfo propertyInfo = entityType.GetProperty(property.Name);
                        val = propertyInfo.GetValue(newInstance);
                    }
                    //stateEntry[property] = val;
                    dbEntry.Property(property.Name).CurrentValue = val;
                }
            }
            else
            {
                // For some properties like DateTimeOffset, the backing EF property could be of a different type like DateTime, so we can't just
                // copy every property pair in DataModificationEntry to EF StateEntry, instead we let the entity type to do the conversion, by
                // first setting the EDM property (in DataModificationEntry) to a entity instance, then getting the EF mapped property from the
                // entity instance and set to StateEntry.

                object instance = null;

                foreach (var property in edmType.GetProperties())
                {
                    object val;

                    var edmPropName = (string)property["EdmPropertyName"];
                    if (edmPropName != null && entry.LocalValues.TryGetValue(edmPropName, out val))
                    {
                        if (instance == null)
                        {
                            instance = Activator.CreateInstance(entityType);
                        }
                        PropertyInfo edmPropInfo = entityType.GetProperty(edmPropName);
                        edmPropInfo.SetValue(instance, val);

                        PropertyInfo propertyInfo = entityType.GetProperty(property.Name);
                        val = propertyInfo.GetValue(instance);
                    }
                    else if (!entry.LocalValues.TryGetValue(property.Name, out val))
                    {
                        continue;
                    }
                    //stateEntry[property] = val;
                    dbEntry.Property(property.Name).CurrentValue = val;
                }
            }
        }

        private static void SetValues(object instance, Type type, IReadOnlyDictionary<string, object> values)
        {
            foreach (KeyValuePair<string, object> propertyPair in values)
            {
                PropertyInfo propertyInfo = type.GetProperty(propertyPair.Key);
                propertyInfo.SetValue(instance, propertyPair.Value);
            }
        }
    }
}
