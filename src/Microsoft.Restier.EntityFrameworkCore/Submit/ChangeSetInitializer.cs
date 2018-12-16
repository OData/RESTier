// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Providers.EntityFramework.Properties;

namespace Microsoft.Restier.Providers.EntityFramework
{
    /// <summary>
    /// To prepare changed entries for the given <see cref="ChangeSet"/>.
    /// For this class we cannot reuse EF6 ChangeSetPreparer code, since many types used here have their type name or
    /// member name changed.
    /// </summary>
    public class ChangeSetInitializer : IChangeSetInitializer
    {
        private static MethodInfo prepareEntryGeneric = typeof(ChangeSetInitializer)
            .GetMethod("PrepareEntry", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Asynchronously prepare the <see cref="ChangeSet"/>.
        /// </summary>
        /// <param name="context">The submit context class used for preparation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that represents this asynchronous operation.</returns>
        public async Task InitializeAsync(
            SubmitContext context,
            CancellationToken cancellationToken)
        {
            DbContext dbContext = context.GetApiService<DbContext>();

            foreach (var entry in context.ChangeSet.Entries.OfType<DataModificationItem>())
            {
                object strongTypedDbSet = dbContext.GetType().GetProperty(entry.ResourceSetName).GetValue(dbContext);
                Type entityType = strongTypedDbSet.GetType().GetGenericArguments()[0];

                // This means request resource is sub type of resource type
                if (entry.ActualResourceType != null && entityType != entry.ActualResourceType)
                {
                    entityType = entry.ActualResourceType;
                }

                MethodInfo prepareEntryMethod = prepareEntryGeneric.MakeGenericMethod(entityType);

                var task = (Task)prepareEntryMethod.Invoke(
                    obj: this,
                    parameters: new[] { context, dbContext, entry, strongTypedDbSet, cancellationToken });
                await task;
            }
        }

        /// <summary>
        /// Convert a Edm type value to Resource Framework supported value type
        /// </summary>
        /// <param name="type">The type of the property defined in CLR class</param>
        /// <param name="value">The value from OData deserializer and in type of Edm</param>
        /// <returns>The converted value object</returns>
        public virtual object ConvertToEfValue(Type type, object value)
        {
            // string[EdmType = Enum] => System.Enum
            if (TypeHelper.IsEnum(type))
            {
                return Enum.Parse(TypeHelper.GetUnderlyingTypeOrSelf(type), (string)value);
            }

            // Edm.Date => System.DateTime[SqlType = Date]
            if (value is Date)
            {
                var dateValue = (Date)value;
                return (DateTime)dateValue;
            }

            // System.DateTimeOffset => System.DateTime[SqlType = DateTime or DateTime2]
            if (value is DateTimeOffset && TypeHelper.IsDateTime(type))
            {
                var dateTimeOffsetValue = (DateTimeOffset)value;
                return dateTimeOffsetValue.DateTime;
            }

            // Edm.TimeOfDay => System.TimeSpan[SqlType = Time]
            if (value is TimeOfDay && TypeHelper.IsTimeSpan(type))
            {
                var timeOfDayValue = (TimeOfDay)value;
                return (TimeSpan)timeOfDayValue;
            }

            // In case key is long type, when put an resource, key value will be from key parsing which is type of int
            if (value is int && type == typeof(long))
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }

            return value;
        }

        private static async Task<object> FindResource(
            SubmitContext context,
            DataModificationItem item,
            CancellationToken cancellationToken)
        {
            var apiBase = context.GetApiService<ApiBase>();
            IQueryable query = apiBase.GetQueryableSource(item.ResourceSetName);
            query = item.ApplyTo(query);

            QueryResult result = await apiBase.QueryAsync(new QueryRequest(query), cancellationToken);

            object resource = result.Results.SingleOrDefault();
            if (resource == null)
            {
                throw new ResourceNotFoundException(Resources.ResourceNotFound);
            }

            // This means no If-Match or If-None-Match header
            if (item.OriginalValues == null || item.OriginalValues.Count == 0)
            {
                return resource;
            }

            resource = item.ValidateEtag(result.Results.AsQueryable());
            return resource;
        }

        private async Task PrepareEntry<TEntity>(
            SubmitContext context,
            DbContext dbContext,
            DataModificationItem entry,
            DbSet<TEntity> set,
            CancellationToken cancellationToken) where TEntity : class
        {
            Type entityType = typeof(TEntity);
            TEntity entity;

            if (entry.DataModificationItemAction == DataModificationItemAction.Insert)
            {
                // TODO: See if Create method is in DbSet<> in future EF7 releases, as the one EF6 has.
                entity = (TEntity)Activator.CreateInstance(typeof(TEntity));

                SetValues(entity, entityType, entry.LocalValues);
                set.Add(entity);
            }
            else if (entry.DataModificationItemAction == DataModificationItemAction.Remove)
            {
                entity = (TEntity)await ChangeSetInitializer.FindResource(context, entry, cancellationToken);
                set.Remove(entity);
            }
            else if (entry.DataModificationItemAction == DataModificationItemAction.Update)
            {
                if (entry.IsFullReplaceUpdateRequest)
                {
                    entity = (TEntity)CreateFullUpdateInstance(entry, entityType);
                    dbContext.Update(entity);
                }
                else
                {
                    entity = (TEntity)await ChangeSetInitializer.FindResource(context, entry, cancellationToken);

                    var dbEntry = dbContext.Attach(entity);
                    SetValues(dbEntry, entry);
                }
            }
            else
            {
                throw new NotSupportedException(Resources.DataModificationMustBeCUD);
            }

            entry.Resource = entity;
        }

        private object CreateFullUpdateInstance(DataModificationItem entry, Type entityType)
        {
            // The algorithm for a "FullReplaceUpdate" is taken from ObjectContextServiceProvider.ResetResource
            // in WCF DS, and works as follows:
            //  - Create a new, blank instance of the entity.
            //  - Copy over the key values and set any updated values from the client on the new instance.
            //  - Then apply all the properties of the new instance to the instance to be updated.
            //    This will set any unspecified properties to their default value.
            object newInstance = Activator.CreateInstance(entityType);

            SetValues(newInstance, entityType, entry.ResourceKey);
            SetValues(newInstance, entityType, entry.LocalValues);

            return newInstance;
        }

        private void SetValues(EntityEntry dbEntry, DataModificationItem entry)
        {
            foreach (KeyValuePair<string, object> propertyPair in entry.LocalValues)
            {
                PropertyEntry propertyEntry = dbEntry.Property(propertyPair.Key);
                object value = propertyPair.Value;
                if (value == null)
                {
                    // If the property value is null, we set null in the entry too.
                    propertyEntry.CurrentValue = null;
                    continue;
                }

                Type type = TypeHelper.GetUnderlyingTypeOrSelf(propertyEntry.Metadata.ClrType);
                value = ConvertToEfValue(type, value);
                if (value != null && !type.IsInstanceOfType(value))
                {
                    var dic = value as IReadOnlyDictionary<string, object>;
                    if (dic == null)
                    {
                        throw new NotSupportedException(string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.UnsupportedPropertyType,
                            propertyPair.Key));
                    }

                    value = Activator.CreateInstance(type);
                    SetValues(value, type, dic);
                }

                propertyEntry.CurrentValue = value;
            }
        }

        private void SetValues(object instance, Type instanceType, IReadOnlyDictionary<string, object> values)
        {
            foreach (KeyValuePair<string, object> propertyPair in values)
            {
                object value = propertyPair.Value;
                PropertyInfo propertyInfo = instanceType.GetProperty(propertyPair.Key);
                if (value == null)
                {
                    // If the property value is null, we set null in the object too.
                    propertyInfo.SetValue(instance, null);
                    continue;
                }

                Type type = TypeHelper.GetUnderlyingTypeOrSelf(propertyInfo.PropertyType);
                value = ConvertToEfValue(type, value);
                if (value != null && !type.IsInstanceOfType(value))
                {
                    var dic = value as IReadOnlyDictionary<string, object>;
                    if (dic == null)
                    {
                        throw new NotSupportedException(string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.UnsupportedPropertyType,
                            propertyPair.Key));
                    }

                    value = Activator.CreateInstance(type);
                    SetValues(value, type, dic);
                }

                propertyInfo.SetValue(instance, value);
            }
        }
    }
}
