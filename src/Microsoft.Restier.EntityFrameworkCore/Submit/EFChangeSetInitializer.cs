// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
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
        private static readonly MethodInfo HandleMethod = typeof(EFChangeSetInitializer).GetMethod("HandleEntitySet", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly Microsoft.Restier.Core.Spatial.ISpatialTypeConverter[] spatialConverters;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFChangeSetInitializer"/> class.
        /// </summary>
        public EFChangeSetInitializer()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EFChangeSetInitializer"/> class
        /// with the specified spatial type converters.
        /// </summary>
        /// <param name="spatialConverters">The registered spatial type converters, or null for none.</param>
        public EFChangeSetInitializer(System.Collections.Generic.IEnumerable<Microsoft.Restier.Core.Spatial.ISpatialTypeConverter> spatialConverters)
        {
            this.spatialConverters = spatialConverters?.ToArray() ?? System.Array.Empty<Microsoft.Restier.Core.Spatial.ISpatialTypeConverter>();
        }

        /// <summary>
        /// Asynchronously prepare the <see cref="ChangeSet"/>.
        /// </summary>
        /// <param name="context">The submit context class used for preparation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that represents this asynchronous operation.</returns>
        public async override Task InitializeAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Api is not IEntityFrameworkApi frameworkApi)
            {
                // Not an EF Api.
                return;
            }

            var dbContext = frameworkApi.DbContext;

            // Phase 1: Validate and resolve entity references (bind references) and relationship removals.
            // This runs before any entity materialization so invalid references fail atomically.
            foreach (var entry in context.ChangeSet.Entries.OfType<DataModificationItem>())
            {
                if (entry.NavigationBindings.Count > 0)
                {
                    foreach (var binding in entry.NavigationBindings)
                    {
                        foreach (var bindRef in binding.Value)
                        {
                            bindRef.ResolvedEntity = await ResolveBindReference(context, bindRef, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }

                foreach (var removal in entry.RelationshipRemovals)
                {
                    var bindRef = new BindReference
                    {
                        ResourceSetName = removal.ResourceSetName,
                        ResourceKey = removal.ResourceKey,
                    };
                    try
                    {
                        removal.ResolvedEntity = await ResolveBindReference(context, bindRef, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (StatusCodeException)
                    {
                        // Entity no longer exists (concurrent deletion) — skip
                    }
                }
            }

            // Phase 2: Materialize entities and wire relationships.
            foreach (var entry in context.ChangeSet.Entries.OfType<DataModificationItem>())
            {
                var dbSetProperty = dbContext.GetType().GetProperty(entry.ResourceSetName);
                if (dbSetProperty is null)
                {
                    throw new InvalidOperationException(
                        $"The DbContext '{dbContext.GetType().Name}' does not have a property named '{entry.ResourceSetName}'. " +
                        $"Check that the entity set name matches a DbSet property on the context.");
                }

                var strongTypedDbSet = dbSetProperty.GetValue(dbContext);
                var resourceType = strongTypedDbSet.GetType().GetGenericArguments()[0];

                // This means request resource is sub type of resource type
                if (entry.ActualResourceType is not null && resourceType != entry.ActualResourceType)
                {
                    // Set type to derived type
                    resourceType = entry.ActualResourceType;
                }

                var typedMethodCall = HandleMethod.MakeGenericMethod(new Type[] { resourceType });
                var task = typedMethodCall.Invoke(this, new object[] { context, dbContext, entry, resourceType, cancellationToken }) as Task;
                await task.ConfigureAwait(false);

                // Wire parent-child relationships after materialization.
                if (entry.ParentItem?.Resource is not null && entry.Resource is not null)
                {
                    WireParentChildRelationship(entry);
                }

                // Wire bind references after materialization.
                if (entry.NavigationBindings.Count > 0 && entry.Resource is not null)
                {
                    WireBindReferences(entry);
                }

                // Process relationship removals after materialization.
                if (entry.RelationshipRemovals.Count > 0 && entry.Resource is not null)
                {
                    foreach (var removal in entry.RelationshipRemovals)
                    {
                        if (removal.ResolvedEntity is null)
                        {
                            continue;
                        }

                        if (removal.FkPropertyName is not null)
                        {
                            // Set FK to null directly on the child entity — most reliable approach
                            var fkPropInfo = removal.ResolvedEntity.GetType().GetProperty(removal.FkPropertyName);
                            if (fkPropInfo is not null)
                            {
                                // Check if the FK type is nullable — non-nullable FKs cannot be set to null
                                var fkType = fkPropInfo.PropertyType;
                                var isNullable = !fkType.IsValueType || Nullable.GetUnderlyingType(fkType) is not null;
                                if (!isNullable)
                                {
                                    throw new StatusCodeException(HttpStatusCode.BadRequest,
                                        $"Cannot unlink relationship via '{removal.FkPropertyName}': " +
                                        $"the foreign key property is required (non-nullable type {fkType.Name}).");
                                }

                                fkPropInfo.SetValue(removal.ResolvedEntity, null);
                            }
                        }
                        else if (removal.InverseNavigationPropertyName is not null)
                        {
                            // Clear inverse nav on child — EF infers FK null
                            SetNavigationProperty(removal.ResolvedEntity, removal.InverseNavigationPropertyName, null);
                        }
                        else
                        {
                            // Single nav on parent — set to null
                            var navPropInfo = entry.Resource.GetType().GetProperty(removal.NavigationPropertyName);
                            if (navPropInfo is not null)
                            {
                                navPropInfo.SetValue(entry.Resource, null);
                            }
                        }
                    }
                }
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
            // Use ignoreCase to support camelCase enum member names from EnableLowerCamelCase
            if (TypeHelper.IsEnum(type))
            {
                return Enum.Parse(TypeHelper.GetUnderlyingTypeOrSelf(type), (string)value, ignoreCase: true);
            }

            // Edm.Date => System.DateOnly
#pragma warning disable CS0618 // Date and TimeOfDay are obsolete but still used by OData
            if (value is Date dateValue && TypeHelper.IsDateOnly(type))
            {
                return new DateOnly(dateValue.Year, dateValue.Month, dateValue.Day);
            }

            // Edm.Date => System.DateTime[SqlType = Date]
            if (value is Date dateValueForDateTime)
            {
                return (DateTime)dateValueForDateTime;
            }

            // System.DateTimeOffset => System.DateTime[SqlType = DateTime or DateTime2]
            if (value is DateTimeOffset && TypeHelper.IsDateTime(type))
            {
                var dateTimeOffsetValue = (DateTimeOffset)value;
                return dateTimeOffsetValue.DateTime;
            }

            // Edm.TimeOfDay => System.TimeOnly
            if (value is TimeOfDay timeOfDayForTimeOnly && TypeHelper.IsTimeOnly(type))
            {
                return new TimeOnly(timeOfDayForTimeOnly.Hours, timeOfDayForTimeOnly.Minutes, timeOfDayForTimeOnly.Seconds, (int)timeOfDayForTimeOnly.Milliseconds);
            }

            // Edm.TimeOfDay => System.TimeSpan[SqlType = Time]
            if (value is TimeOfDay && TypeHelper.IsTimeSpan(type))
            {
                var timeOfDayValue = (TimeOfDay)value;
                return (TimeSpan)timeOfDayValue;
            }
#pragma warning restore CS0618

            // In case key is long type, when put an resource, key value will be from key parsing which is type of int
            if (value is int && type == typeof(long))
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }

            if (value is not null && IsNtsGeometryType(type))
            {
                for (var i = 0; i < spatialConverters.Length; i++)
                {
                    if (spatialConverters[i].CanConvert(type))
                    {
                        return spatialConverters[i].ToStorage(type, value);
                    }
                }
            }

            return value;
        }

        private static bool IsNtsGeometryType(Type type)
        {
            var t = type;
            while (t is not null && t != typeof(object))
            {
                if (t.FullName == "NetTopologySuite.Geometries.Geometry")
                {
                    return true;
                }

                t = t.BaseType;
            }

            return false;
        }

        private static async Task<object> FindResource(SubmitContext context, DataModificationItem item, CancellationToken cancellationToken)
        {
            var apiBase = context.Api;
            var query = apiBase.GetQueryableSource(item.ResourceSetName);
            query = item.ApplyTo(query);

            var result = await apiBase.QueryAsync(new QueryRequest(query), cancellationToken).ConfigureAwait(false);

            // Materialize preserving the entity element type so that ValidateEtag can build
            // typed expressions (Expression.Property requires the real entity type, not object).
            var elementType = query.ElementType;
            var toArray = ExpressionHelperMethods.EnumerableToArrayGeneric.MakeGenericMethod(elementType);
            var materialized = (Array)toArray.Invoke(null, new object[] { result.Results });

            var resource = materialized.Length == 1 ? materialized.GetValue(0) : null;
            if (resource is null)
            {
                if (materialized.Length > 1)
                {
                    throw new InvalidOperationException(Core.Resources.QueryShouldGetSingleRecord);
                }

                throw new StatusCodeException(HttpStatusCode.NotFound, Resources.ResourceNotFound);
            }

            // This means no If-Match or If-None-Match header
            if (item.OriginalValues is null || item.OriginalValues.Count == 0)
            {
                return resource;
            }

            var asQueryable = ExpressionHelperMethods.QueryableAsQueryableGeneric.MakeGenericMethod(elementType);
            resource = item.ValidateEtag((IQueryable)asQueryable.Invoke(null, new object[] { materialized }));
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

                SetValues(newInstance, resourceType, item.ResourceKey);
                SetValues(newInstance, resourceType, item.LocalValues);

                dbEntry.CurrentValues.SetValues(newInstance);
            }
            else
            {
                foreach (var propertyPair in item.LocalValues)
                {
                    var propertyEntry = dbEntry.Members.FirstOrDefault(m => m.Metadata.Name == propertyPair.Key);

                    if (propertyEntry != null)
                    {
                        var value = propertyPair.Value;
                        if (value is null)
                        {
                            // If the property value is null, we set null in the item too.
                            propertyEntry.CurrentValue = null;
                            continue;
                        }

                        Type type = null;
                        if (propertyEntry.CurrentValue is not null)
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
                            SetValues(value, type, dic);
                        }

                        propertyEntry.CurrentValue = ConvertToEfValue(type, value);
                    }
                }
            }
        }

        private void SetValues(object instance, Type type, IReadOnlyDictionary<string, object> values)
        {
            foreach (var propertyPair in values)
            {
                var value = propertyPair.Value;
                var propertyInfo = type.GetProperty(propertyPair.Key);
                if (value is null)
                {
                    // If the property value is null, we set null in the object too.
                    propertyInfo.SetValue(instance, null);
                    continue;
                }

                value = ConvertToEfValue(propertyInfo.PropertyType, value);
                if (value is not null && !propertyInfo.PropertyType.IsInstanceOfType(value))
                {
                    if (!(value is IReadOnlyDictionary<string, object> dic))
                    {
                        propertyInfo.SetValue(instance, value);
                        return;

                        // throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Resources.UnsupportedPropertyType, propertyPair.Key));
                    }

                    // TODO GithubIssue #508
                    value = Activator.CreateInstance(propertyInfo.PropertyType);
                    SetValues(value, propertyInfo.PropertyType, dic);
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

                SetValues(resource, resourceType, entry.LocalValues);
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
                SetValues(dbEntry, entry, resourceType);
            }
            else
            {
                throw new NotSupportedException(Resources.DataModificationMustBeCUD);
            }

            entry.Resource = resource;
        }

        private static async Task<object> ResolveBindReference(SubmitContext context, BindReference bindRef, CancellationToken cancellationToken)
        {
            var apiBase = context.Api;
            var query = apiBase.GetQueryableSource(bindRef.ResourceSetName);
            var elementType = query.ElementType;
            var param = Expression.Parameter(elementType);
            Expression where = null;

            foreach (var keyPair in bindRef.ResourceKey)
            {
                var property = Expression.Property(param, keyPair.Key);
                var value = keyPair.Value;
                if (value.GetType() != property.Type)
                {
                    value = Convert.ChangeType(value, property.Type, CultureInfo.InvariantCulture);
                }

                var equal = Expression.Equal(property, Expression.Constant(value, property.Type));
                where = where is null ? equal : Expression.AndAlso(where, equal);
            }

            var whereLambda = Expression.Lambda(where, param);
            query = ExpressionHelpers.Where(query, whereLambda, elementType);

            var result = await apiBase.QueryAsync(new QueryRequest(query), cancellationToken).ConfigureAwait(false);
            var toArray = ExpressionHelperMethods.EnumerableToArrayGeneric.MakeGenericMethod(elementType);
            var materialized = (Array)toArray.Invoke(null, new object[] { result.Results });

            if (materialized.Length == 0)
            {
                var keyDescription = string.Join(", ", bindRef.ResourceKey.Select(k => $"{k.Key}={k.Value}"));
                throw new StatusCodeException(HttpStatusCode.BadRequest,
                    $"Referenced entity '{bindRef.ResourceSetName}' with key ({keyDescription}) does not exist.");
            }

            return materialized.GetValue(0);
        }

        private void WireParentChildRelationship(DataModificationItem childEntry)
        {
            var parentResource = childEntry.ParentItem.Resource;
            var childResource = childEntry.Resource;
            var navPropName = childEntry.ParentNavigationPropertyName;

            var parentNavPropInfo = parentResource.GetType().GetProperty(navPropName);
            if (parentNavPropInfo is null)
            {
                return;
            }

            if (typeof(IEnumerable).IsAssignableFrom(parentNavPropInfo.PropertyType)
                && parentNavPropInfo.PropertyType != typeof(string))
            {
                AddToCollectionNavigationProperty(parentResource, navPropName, childResource);
            }
            else
            {
                SetNavigationProperty(parentResource, navPropName, childResource);
            }
        }

        private void WireBindReferences(DataModificationItem entry)
        {
            foreach (var binding in entry.NavigationBindings)
            {
                var navPropName = binding.Key;
                var navPropInfo = entry.Resource.GetType().GetProperty(navPropName);
                if (navPropInfo is null)
                {
                    continue;
                }

                if (typeof(IEnumerable).IsAssignableFrom(navPropInfo.PropertyType)
                    && navPropInfo.PropertyType != typeof(string))
                {
                    foreach (var bindRef in binding.Value)
                    {
                        if (bindRef.ResolvedEntity is not null)
                        {
                            AddToCollectionNavigationProperty(entry.Resource, navPropName, bindRef.ResolvedEntity);
                        }
                    }
                }
                else
                {
                    var bindRef = binding.Value.FirstOrDefault();
                    if (bindRef?.ResolvedEntity is not null)
                    {
                        SetNavigationProperty(entry.Resource, navPropName, bindRef.ResolvedEntity);
                    }
                }
            }
        }
    }
}