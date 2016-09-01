// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Spatial;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Spatial;

namespace Microsoft.Restier.Providers.EntityFramework
{
    /// <summary>
    /// To prepare changed entries for the given <see cref="ChangeSet"/>.
    /// </summary>
    public class ChangeSetInitializer : IChangeSetInitializer
    {
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
                Type resourceType = strongTypedDbSet.GetType().GetGenericArguments()[0];

                // This means request resource is sub type of resource type
                if (entry.ActualResourceType != null && resourceType != entry.ActualResourceType)
                {
                    // Set type to derived type
                    resourceType = entry.ActualResourceType;
                }

                DbSet set = dbContext.Set(resourceType);

                object resource;

                if (entry.DataModificationItemAction == DataModificationItemAction.Insert)
                {
                    resource = set.Create();
                    SetValues(resource, resourceType, entry.LocalValues);
                    set.Add(resource);
                }
                else if (entry.DataModificationItemAction == DataModificationItemAction.Remove)
                {
                    resource = await FindResource(context, entry, cancellationToken);
                    set.Remove(resource);
                }
                else if (entry.DataModificationItemAction == DataModificationItemAction.Update)
                {
                    resource = await FindResource(context, entry, cancellationToken);

                    DbEntityEntry dbEntry = dbContext.Entry(resource);
                    SetValues(dbEntry, entry, resourceType);
                }
                else
                {
                    throw new NotSupportedException(Resources.DataModificationMustBeCUD);
                }

                entry.Resource = resource;
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

            if (type == typeof(DbGeography))
            {
                var point = value as GeographyPoint;
                if (point != null)
                {
                    return point.ToDbGeography();
                }

                var s = value as GeographyLineString;
                if (s != null)
                {
                    return s.ToDbGeography();
                }
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

        private void SetValues(DbEntityEntry dbEntry, DataModificationItem item, Type resourceType)
        {
            if (item.IsFullReplaceUpdateRequest)
            {
                // The algorithm for a "FullReplaceUpdate" is taken from ObjectContextServiceProvider.ResetResource
                // in WCF DS, and works as follows:
                //  - Create a new, blank instance of the entity.
                //  - Copy over the key values and set any updated values from the client on the new instance.
                //  - Then apply all the properties of the new instance to the instance to be updated.
                //    This will set any unspecified properties to their default value.
                object newInstance = Activator.CreateInstance(resourceType);

                SetValues(newInstance, resourceType, item.ResourceKey);
                SetValues(newInstance, resourceType, item.LocalValues);

                dbEntry.CurrentValues.SetValues(newInstance);
            }
            else
            {
                foreach (KeyValuePair<string, object> propertyPair in item.LocalValues)
                {
                    DbPropertyEntry propertyEntry = dbEntry.Property(propertyPair.Key);
                    object value = propertyPair.Value;
                    if (value == null)
                    {
                        // If the property value is null, we set null in the item too.
                        propertyEntry.CurrentValue = null;
                        continue;
                    }

                    Type type = null;
                    if (propertyEntry.CurrentValue != null)
                    {
                        type = propertyEntry.CurrentValue.GetType();
                    }
                    else
                    {
                        // If property does not have value now, will get property type from model
                        var propertyInfo = dbEntry.Entity.GetType().GetProperty(propertyPair.Key);
                        type = propertyInfo.PropertyType;
                    }

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

                        value = propertyEntry.CurrentValue;
                        SetValues(value, type, dic);
                    }

                    propertyEntry.CurrentValue = ConvertToEfValue(type, value);
                }
            }
        }

        private void SetValues(object instance, Type type, IReadOnlyDictionary<string, object> values)
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

                    // TODO GithubIssue #508
                    value = Activator.CreateInstance(propertyInfo.PropertyType);
                    SetValues(value, propertyInfo.PropertyType, dic);
                }

                propertyInfo.SetValue(instance, value);
            }
        }
    }
}
