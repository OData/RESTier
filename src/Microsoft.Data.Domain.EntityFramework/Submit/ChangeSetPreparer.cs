// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Domain.Query;
using Microsoft.Data.Domain.Submit;

namespace Microsoft.Data.Domain.EntityFramework.Submit
{
    public class ChangeSetPreparer : IChangeSetPreparer
    {
        private ChangeSetPreparer()
        {
        }

        public static readonly ChangeSetPreparer
            Instance = new ChangeSetPreparer();

        public async Task PrepareAsync(
            SubmitContext context,
            CancellationToken cancellationToken)
        {
            DbContext dbContext = context.DomainContext.GetProperty<DbContext>("DbContext");

            foreach (var entry in context.ChangeSet.Entries.OfType<DataModificationEntry>())
            {
                object strongTypedDbSet = dbContext.GetType().GetProperty(entry.EntitySetName).GetValue(dbContext);
                Type entityType = strongTypedDbSet.GetType().GetGenericArguments()[0];
                DbSet set = dbContext.Set(entityType);

                object entity;

                if (entry.IsNew)
                {
                    entity = set.Create();

                    ChangeSetPreparer.SetValues(entity, entityType, entry.LocalValues);

                    set.Add(entity);
                }
                else if (entry.IsDelete)
                {
                    entity = await ChangeSetPreparer.FindEntity(context, entry, cancellationToken);
                    set.Remove(entity);
                }
                else if (entry.IsUpdate)
                {
                    entity = await ChangeSetPreparer.FindEntity(context, entry, cancellationToken);

                    DbEntityEntry dbEntry = dbContext.Entry(entity);
                    ChangeSetPreparer.SetValues(dbEntry, entry, entityType);
                }
                else
                {
                    throw new NotSupportedException("A DataModificationEntry must be either New, Update or Delete.");
                }

                entry.Entity = entity;
            }
        }

        private static async Task<object> FindEntity(SubmitContext context, DataModificationEntry entry, CancellationToken cancellationToken)
        {
            IQueryable query = Domain.Source(context.DomainContext, entry.EntitySetName);
            query = entry.ApplyTo(query);

            QueryResult result = await Domain.QueryAsync(context.DomainContext, new QueryRequest(query), cancellationToken);

            object entity = result.Results.SingleOrDefault();
            if (entity == null)
            {
                // TODO: there are 2 cases where the entity is not found:
                // 1) it doesn't exist
                // 2) concurrency checks have failed
                // we should account for both - I can see 3 options:
                // a. always return "PreConditionFailed" result - this is the canonical behavior of WebAPI OData (see http://blogs.msdn.com/b/webdev/archive/2014/03/13/getting-started-with-asp-net-web-api-2-2-for-odata-v4-0.aspx)
                //  - this makes sense because if someone deleted the record, then you still have a concurrency error
                // b. possibly doing a 2nd query with just the keys to see if the record still exists
                // c. only query with the keys, and then set the DbEntityEntry's OriginalValues to the ETag values, letting the save fail if there are concurrency errors

                //throw new EntityNotFoundException
                throw new InvalidOperationException("Could not find the specified resource.");
            }

            return entity;
        }

        private static void SetValues(DbEntityEntry dbEntry, DataModificationEntry entry, Type entityType)
        {
            if (entry.IsFullReplaceUpdate)
            {
                // The algorithm for a "FullReplaceUpdate" is taken from WCF DS ObjectContextServiceProvider.ResetResource, and is as follows:
                // Create a new, blank instance of the entity.  Copy over the key values, and set any updated values from the client on the new instance.
                // Then apply all the properties of the new instance to the instance to be updated.  This will set any unspecified
                // properties to their default value.

                object newInstance = Activator.CreateInstance(entityType);

                ChangeSetPreparer.SetValues(newInstance, entityType, entry.EntityKey);
                ChangeSetPreparer.SetValues(newInstance, entityType, entry.LocalValues);

                dbEntry.CurrentValues.SetValues(newInstance);
            }
            else
            {
                foreach (KeyValuePair<string, object> propertyPair in entry.LocalValues)
                {
                    DbPropertyEntry propertyEntry = dbEntry.Property(propertyPair.Key);
                    propertyEntry.CurrentValue = propertyPair.Value;
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
