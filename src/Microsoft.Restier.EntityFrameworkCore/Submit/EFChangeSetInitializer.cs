// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.EntityFrameworkCore
{

    /// <summary>
    /// To prepare changed entries for the given <see cref="ChangeSet"/>.
    /// </summary>
    public class EFChangeSetInitializer : DefaultChangeSetInitializer
    {
        /// <summary>
        /// Asynchronously prepare the <see cref="ChangeSet"/>.
        /// </summary>
        /// <param name="context">The submit context class used for preparation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that represents this asynchronous operation.</returns>
        public async override Task InitializeAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var dbContext = context.GetApiService<DbContext>();

            var methodCall = this.GetType().GetMethod("HandleEntitySet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            foreach (var entry in context.ChangeSet.Entries.OfType<DataModificationItem>())
            {
                var strongTypedDbSet = dbContext.GetType().GetProperty(entry.ResourceSetName).GetValue(dbContext);
                var resourceType = strongTypedDbSet.GetType().GetGenericArguments()[0];

                // This means request resource is sub type of resource type
                if (entry.ActualResourceType != null && resourceType != entry.ActualResourceType)
                {
                    // Set type to derived type
                    resourceType = entry.ActualResourceType;
                }

                var task = methodCall.Invoke(this, new object[] { context, dbContext, entry, resourceType, cancellationToken }) as Task;
                await task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Convert a Edm type value to Resource Framework supported value type.
        /// </summary>
        /// <param name="type">The type of the property defined in CLR class.</param>
        /// <param name="value">The value from OData deserializer and in type of Edm.</param>
        /// <returns>The converted value object.</returns>
        public virtual object ConvertToEfValue(Type type, object value)
        {
            // string[EdmType = Enum] => System.Enum
            if (TypeHelper.IsEnum(type))
            {
                return Enum.Parse(TypeHelper.GetUnderlyingTypeOrSelf(type), (string)value);
            }

            // Edm.Date => System.DateTime[SqlType = Date]
            if (value is Date dateValue)
            {
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

#if !EF7
            // Todo: Restore geometry handling
            if (type == typeof(DbGeography))
            {
                if (value is GeographyPoint point)
                {
                    return point.ToDbGeography();
                }

                if (value is GeographyLineString s)
                {
                    return s.ToDbGeography();
                }
            }
#endif

            return value;
        }

        private static async Task<object> FindResource(SubmitContext context, DataModificationItem item, CancellationToken cancellationToken)
        {
            var apiBase = context.Api;
            var query = apiBase.GetQueryableSource(item.ResourceSetName);
            query = item.ApplyTo(query);

            var result = await apiBase.QueryAsync(new QueryRequest(query), cancellationToken).ConfigureAwait(false);

            var resource = result.Results.SingleOrDefault();
            if (resource == null)
            {
                throw new StatusCodeException(HttpStatusCode.NotFound, Resources.ResourceNotFound);
            }

            // This means no If-Match or If-None-Match header
            if (item.OriginalValues == null || item.OriginalValues.Count == 0)
            {
                return resource;
            }

            resource = item.ValidateEtag(result.Results.AsQueryable());
            return resource;
        }

        private void SetValues(EntityEntry dbEntry, DataModificationItem item, Type resourceType)
        {
            if (item.IsFullReplaceUpdateRequest)
            {
                // The algorithm for a "FullReplaceUpdate" is taken from ObjectContextServiceProvider.ResetResource
                // in WCF DS, and works as follows:
                //  - Create a new, blank instance of the entity.
                //  - Copy over the key values and set any updated values from the client on the new instance.
                //  - Then apply all the properties of the new instance to the instance to be updated.
                //    This will set any unspecified properties to their default value.
                var newInstance = Activator.CreateInstance(resourceType);

                this.SetValues(newInstance, resourceType, item.ResourceKey);
                this.SetValues(newInstance, resourceType, item.LocalValues);

                dbEntry.CurrentValues.SetValues(newInstance);
            }
            else
            {
                foreach (var propertyPair in item.LocalValues)
                {
                    var propertyEntry = dbEntry.Property(propertyPair.Key);
                    var value = propertyPair.Value;
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

                    // todo: complex property detection removed. Not sure whether IReadOnlyDictionary is enough.
                    if (value is IReadOnlyDictionary<string, object> dic)
                    {
                        value = propertyEntry.CurrentValue;
                        this.SetValues(value, type, dic);
                    }

                    propertyEntry.CurrentValue = this.ConvertToEfValue(type, value);
                }
            }
        }

        private void SetValues(object instance, Type type, IReadOnlyDictionary<string, object> values)
        {
            foreach (var propertyPair in values)
            {
                var value = propertyPair.Value;
                var propertyInfo = type.GetProperty(propertyPair.Key);
                if (value == null)
                {
                    // If the property value is null, we set null in the object too.
                    propertyInfo.SetValue(instance, null);
                    continue;
                }

                value = this.ConvertToEfValue(propertyInfo.PropertyType, value);
                if (value != null && !propertyInfo.PropertyType.IsInstanceOfType(value))
                {
                    if (!(value is IReadOnlyDictionary<string, object> dic))
                    {
                        propertyInfo.SetValue(instance, value);
                        return;

                        // throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Resources.UnsupportedPropertyType, propertyPair.Key));
                    }

                    // TODO GithubIssue #508
                    value = Activator.CreateInstance(propertyInfo.PropertyType);
                    this.SetValues(value, propertyInfo.PropertyType, dic);
                }

                propertyInfo.SetValue(instance, value);
            }
        }

        private async Task HandleEntitySet<TEntity>(SubmitContext context, DbContext dbContext, DataModificationItem entry, Type resourceType, CancellationToken cancellationToken)
            where TEntity : class, new()
        {
            var set = dbContext.Set<TEntity>();

            TEntity resource;

            if (entry.EntitySetOperation == RestierEntitySetOperation.Insert)
            {
                resource = new TEntity();

                this.SetValues(resource, resourceType, entry.LocalValues);
                set.Add(resource);
            }
            else if (entry.EntitySetOperation == RestierEntitySetOperation.Delete)
            {
                resource = (await FindResource(context, entry, cancellationToken).ConfigureAwait(false)) as TEntity;
                set.Remove(resource);
            }
            else if (entry.EntitySetOperation == RestierEntitySetOperation.Update)
            {
                resource = (await FindResource(context, entry, cancellationToken).ConfigureAwait(false)) as TEntity;

                var dbEntry = dbContext.Entry(resource);
                this.SetValues(dbEntry, entry, resourceType);
            }
            else
            {
                throw new NotSupportedException(Resources.DataModificationMustBeCUD);
            }

            entry.Resource = resource;
        }
    }
}