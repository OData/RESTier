// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFramework.Properties;

namespace Microsoft.Restier.EntityFramework.Submit
{
    /// <summary>
    /// To prepare changed entries for the given <see cref="ChangeSet"/>.
    /// </summary>
    internal class ChangeSetPreparer : IChangeSetPreparer
    {
        /// <summary>
        /// Asynchronously prepare the <see cref="ChangeSet"/>.
        /// </summary>
        /// <param name="context">The submit context class used for preparation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that represents this asynchronous operation.</returns>
        public async Task PrepareAsync(
            SubmitContext context,
            CancellationToken cancellationToken)
        {
            DbContext dbContext = context.ApiContext.GetApiService<DbContext>();

            foreach (var entry in context.ChangeSet.Entries.OfType<DataModificationEntry>())
            {
                object strongTypedDbSet = dbContext.GetType().GetProperty(entry.EntitySetName).GetValue(dbContext);
                Type entityType = strongTypedDbSet.GetType().GetGenericArguments()[0];
                DbSet set = dbContext.Set(entityType);

                object entity;

                if (entry.IsNew)
                {
                    entity = set.Create();

                    SetValues(entity, entityType, entry.LocalValues);

                    set.Add(entity);
                }
                else if (entry.IsDelete)
                {
                    entity = await FindEntity(context, entry, cancellationToken);
                    set.Remove(entity);
                }
                else if (entry.IsUpdate)
                {
                    entity = await FindEntity(context, entry, cancellationToken);

                    DbEntityEntry dbEntry = dbContext.Entry(entity);
                    SetValues(dbEntry, entry, entityType);
                }
                else
                {
                    throw new NotSupportedException(Resources.DataModificationMustBeCUD);
                }

                entry.Entity = entity;
            }
        }

        private static async Task<object> FindEntity(
            SubmitContext context,
            DataModificationEntry entry,
            CancellationToken cancellationToken)
        {
            IQueryable query = Api.Source(context.ApiContext, entry.EntitySetName);
            query = entry.ApplyTo(query);

            QueryResult result = await Api.QueryAsync(
                context.ApiContext,
                new QueryRequest(query),
                cancellationToken);

            object entity = result.Results.SingleOrDefault();
            if (entity == null)
            {
                // TODO GitHubIssue#38 : Handle the case when entity is resolved
                // there are 2 cases where the entity is not found:
                // 1) it doesn't exist
                // 2) concurrency checks have failed
                // we should account for both - I can see 3 options:
                // a. always return "PreConditionFailed" result
                //  - this is the canonical behavior of WebAPI OData, see the following post:
                //    "Getting started with ASP.NET Web API 2.2 for OData v4.0" on http://blogs.msdn.com/b/webdev/.
                //  - this makes sense because if someone deleted the record, then you still have a concurrency error
                // b. possibly doing a 2nd query with just the keys to see if the record still exists
                // c. only query with the keys, and then set the DbEntityEntry's OriginalValues to the ETag values,
                //    letting the save fail if there are concurrency errors

                ////throw new EntityNotFoundException
                throw new InvalidOperationException(Resources.ResourceNotFound);
            }

            return entity;
        }

        private static void SetValues(DbEntityEntry dbEntry, DataModificationEntry entry, Type entityType)
        {
            if (entry.IsFullReplaceUpdate)
            {
                // The algorithm for a "FullReplaceUpdate" is taken from ObjectContextServiceProvider.ResetResource
                // in WCF DS, and works as follows:
                //  - Create a new, blank instance of the entity.
                //  - Copy over the key values and set any updated values from the client on the new instance.
                //  - Then apply all the properties of the new instance to the instance to be updated.
                //    This will set any unspecified properties to their default value.
                object newInstance = Activator.CreateInstance(entityType);

                SetValues(newInstance, entityType, entry.EntityKey);
                SetValues(newInstance, entityType, entry.LocalValues);

                dbEntry.CurrentValues.SetValues(newInstance);
            }
            else
            {
                foreach (KeyValuePair<string, object> propertyPair in entry.LocalValues)
                {
                    DbPropertyEntry propertyEntry = dbEntry.Property(propertyPair.Key);
                    object value = propertyPair.Value;
                    if (value == null)
                    {
                        // If the property value is null, we set null in the entry too.
                        propertyEntry.CurrentValue = null;
                        continue;
                    }

                    Type type = propertyEntry.CurrentValue.GetType();
                    if (propertyEntry is DbComplexPropertyEntry)
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

                    propertyEntry.CurrentValue = ConvertToEfValue(type, value);
                }
            }
        }

        private static void SetValues(object instance, Type type, IReadOnlyDictionary<string, object> values)
        {
            foreach (KeyValuePair<string, object> propertyPair in values)
            {
                object value = propertyPair.Value;
                PropertyInfo propertyInfo = type.GetProperty(propertyPair.Key);
                if (value == null)
                {
                    // If the property value is null, we set null in the object too.
                    propertyInfo.SetValue(instance, null);
                    continue;
                }

                value = ConvertToEfValue(propertyInfo.PropertyType, value);
                if (value != null && !propertyInfo.PropertyType.IsInstanceOfType(value))
                {
                    var dic = value as IReadOnlyDictionary<string, object>;
                    if (dic == null)
                    {
                        throw new NotSupportedException(string.Format(
                            CultureInfo.InvariantCulture,
                            Resources.UnsupportedPropertyType,
                            propertyPair.Key));
                    }

                    value = Activator.CreateInstance(propertyInfo.PropertyType);
                    SetValues(value, propertyInfo.PropertyType, dic);
                }

                propertyInfo.SetValue(instance, value);
            }
        }

        private static object ConvertToEfValue(Type type, object value)
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

            return value;
        }
    }
}
