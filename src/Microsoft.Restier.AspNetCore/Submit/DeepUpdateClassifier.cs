// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.AspNetCore.Submit
{
    /// <summary>
    /// Classifies nested items in a deep update payload as Insert or Update,
    /// and generates RelationshipRemovals for omitted children (PUT) and null nav props.
    /// </summary>
    internal class DeepUpdateClassifier
    {
        private readonly ApiBase api;
        private readonly IEdmModel model;

        public DeepUpdateClassifier(ApiBase api, IEdmModel model)
        {
            Ensure.NotNull(api, nameof(api));
            Ensure.NotNull(model, nameof(model));
            this.api = api;
            this.model = model;
        }

        /// <summary>
        /// Classifies all nested items on the root item.
        /// </summary>
        public async Task ClassifyAsync(
            DataModificationItem rootItem,
            IEdmEntitySet entitySet,
            bool isFullReplace,
            CancellationToken cancellationToken)
        {
            var edmEntityType = entitySet.EntityType;

            // Split nested items by nav prop multiplicity
            var groups = rootItem.NestedItems
                .GroupBy(n => n.ParentNavigationPropertyName)
                .ToList();


            foreach (var group in groups)
            {
                var navPropName = group.Key;
                var edmNavProp = FindEdmNavigationProperty(edmEntityType, navPropName);
                if (edmNavProp is null)
                {
                    continue;
                }

                if (edmNavProp.TargetMultiplicity() == EdmMultiplicity.Many)
                {
                    await ClassifyCollectionNavProp(
                        rootItem, navPropName, group.ToList(),
                        edmNavProp, entitySet, isFullReplace, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await ClassifySingleNavProp(
                        rootItem, navPropName, group.First(),
                        edmNavProp, entitySet, cancellationToken).ConfigureAwait(false);
                }
            }

            // Handle NullNavigationProperties
            foreach (var nullNavProp in rootItem.NullNavigationProperties)
            {
                await HandleNullNavProp(rootItem, nullNavProp, edmEntityType, entitySet, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ClassifyCollectionNavProp(
            DataModificationItem rootItem,
            string navPropName,
            IList<DataModificationItem> nestedItems,
            IEdmNavigationProperty edmNavProp,
            IEdmEntitySet entitySet,
            bool isFullReplace,
            CancellationToken cancellationToken)
        {
            var targetEntitySetName = FindTargetEntitySetName(edmNavProp);

            // Find FK property name from referential constraint or convention
            var fkPropertyName = FindFkPropertyName(edmNavProp);


            // Classify each nested item
            foreach (var nestedItem in nestedItems)
            {
                if (nestedItem.ResourceKey is not null && nestedItem.ResourceKey.Count > 0)
                {
                    // Check if entity exists in db
                    var exists = await EntityExistsByKey(
                        targetEntitySetName, nestedItem.ResourceKey, cancellationToken).ConfigureAwait(false);

                    if (exists)
                    {
                        ReclassifyAsUpdate(nestedItem);
                    }
                    // else: leave as Insert
                }
                // else: no key provided, leave as Insert (server-generated key)
            }

            // For PUT: generate removals for omitted children
            if (isFullReplace && rootItem.ResourceKey is not null)
            {
                if (fkPropertyName is null)
                {
                    throw new StatusCodeException(HttpStatusCode.NotImplemented,
                        $"Deep update for navigation property '{navPropName}' is not supported: " +
                        $"no explicit foreign key property found. Cannot determine omitted children.");
                }
            }

            if (isFullReplace && fkPropertyName is not null && rootItem.ResourceKey is not null)
            {
                var payloadKeyStrings = new HashSet<string>();
                foreach (var nestedItem in nestedItems)
                {
                    if (nestedItem.ResourceKey is not null && nestedItem.ResourceKey.Count > 0)
                    {
                        payloadKeyStrings.Add(KeyToString(nestedItem.ResourceKey));
                    }
                }

                await GenerateRemovalsForOmittedChildren(
                    rootItem, navPropName, edmNavProp, targetEntitySetName,
                    fkPropertyName, payloadKeyStrings,
                    entitySet, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task GenerateRemovalsForOmittedChildren(
            DataModificationItem rootItem,
            string navPropName,
            IEdmNavigationProperty edmNavProp,
            string targetEntitySetName,
            string fkPropertyName,
            ISet<string> payloadKeyStrings,
            IEdmEntitySet entitySet,
            CancellationToken cancellationToken)
        {
            var targetEntityType = edmNavProp.ToEntityType();

            // Query all existing children for this parent
            var existingChildren = await QueryChildrenByFk(
                targetEntitySetName, fkPropertyName,
                rootItem.ResourceKey,
                cancellationToken).ConfigureAwait(false);


            var inverseNavPropName = GetInverseNavigationPropertyName(edmNavProp);

            foreach (var child in existingChildren)
            {
                var childKey = DefaultChangeSetInitializer.GetKeyValues(child, targetEntityType, model);
                var childKeyStr = KeyToString(childKey);

                if (!payloadKeyStrings.Contains(childKeyStr))
                {
                    // This child was omitted from the PUT payload
                    if (edmNavProp.ContainsTarget)
                    {
                        // Contained: generate a delete item
                        var deleteItem = new DataModificationItem(
                            targetEntitySetName,
                            targetEntityType.GetClrType(model),
                            null,
                            RestierEntitySetOperation.Delete,
                            childKey,
                            null,
                            null)
                        {
                            ParentItem = rootItem,
                            ParentNavigationPropertyName = navPropName,
                        };
                        rootItem.NestedItems.Add(deleteItem);
                    }
                    else
                    {
                        // Non-contained: add RelationshipRemoval
                        rootItem.RelationshipRemovals.Add(new RelationshipRemoval
                        {
                            NavigationPropertyName = navPropName,
                            InverseNavigationPropertyName = inverseNavPropName,
                            FkPropertyName = fkPropertyName,
                            ResourceSetName = targetEntitySetName,
                            ResourceKey = childKey,
                        });
                    }
                }
            }
        }

        private async Task ClassifySingleNavProp(
            DataModificationItem rootItem,
            string navPropName,
            DataModificationItem nestedItem,
            IEdmNavigationProperty edmNavProp,
            IEdmEntitySet entitySet,
            CancellationToken cancellationToken)
        {
            var targetEntitySetName = FindTargetEntitySetName(edmNavProp);
            var fkPropertyName = FindFkPropertyName(edmNavProp);

            if (nestedItem.ResourceKey is not null && nestedItem.ResourceKey.Count > 0)
            {
                // Has key — check if entity exists globally
                var exists = await EntityExistsByKey(
                    targetEntitySetName, nestedItem.ResourceKey, cancellationToken).ConfigureAwait(false);

                if (exists)
                {
                    ReclassifyAsUpdate(nestedItem);
                }

                // If the FK is on the root entity (dependent side), update the FK
                // to point to the new target entity. This handles both "same entity"
                // and "replace with different entity" cases, AND insert-with-client-key.
                if (fkPropertyName is not null)
                {
                    var targetKeyValue = nestedItem.ResourceKey.Values.First();
                    var updatedValues = new Dictionary<string, object>(rootItem.LocalValues ?? new Dictionary<string, object>())
                    {
                        [fkPropertyName] = targetKeyValue,
                    };
                    rootItem.LocalValues = updatedValues;
                }
            }
            else
            {
                // No key — new entity to Insert.
                // EF will handle the FK update automatically via nav prop assignment.
            }
        }

        private Task HandleNullNavProp(
            DataModificationItem rootItem,
            string nullNavPropName,
            IEdmEntityType edmEntityType,
            IEdmEntitySet entitySet,
            CancellationToken cancellationToken)
        {
            var edmNavProp = FindEdmNavigationProperty(edmEntityType, nullNavPropName);
            if (edmNavProp is null)
            {
                return Task.CompletedTask;
            }

            // For single nav props where the FK is on the root entity (dependent side),
            // the simplest unlink is to set the FK to null on the root entity's LocalValues.
            // For example: Book.Publisher = null → set Book.PublisherId = null.
            // We do NOT query the target entity set (Publisher) — the FK lives on the root (Book).
            if (edmNavProp.TargetMultiplicity() != EdmMultiplicity.Many)
            {
                var fkPropertyName = FindFkPropertyName(edmNavProp);

                if (fkPropertyName is null)
                {
                    throw new StatusCodeException(HttpStatusCode.NotImplemented,
                        $"Cannot unlink navigation property '{nullNavPropName}': no explicit foreign key property found.");
                }

                // Add FK null to root item's LocalValues
                var updatedValues = new Dictionary<string, object>(rootItem.LocalValues ?? new Dictionary<string, object>())
                {
                    [fkPropertyName] = null,
                };
                rootItem.LocalValues = updatedValues;
            }

            return Task.CompletedTask;
        }

        private static void ReclassifyAsUpdate(DataModificationItem item)
        {
            item.EntitySetOperation = RestierEntitySetOperation.Update;
            if (item.UpdateLocalValues is not null)
            {
                item.LocalValues = item.UpdateLocalValues;
            }
        }

        private async Task<bool> EntityExistsByKey(
            string entitySetName,
            IReadOnlyDictionary<string, object> resourceKey,
            CancellationToken cancellationToken)
        {
            var query = api.GetQueryableSource(entitySetName);
            var elementType = query.ElementType;
            var param = Expression.Parameter(elementType);
            Expression where = null;

            foreach (var keyPair in resourceKey)
            {
                var property = Expression.Property(param, keyPair.Key);
                var value = keyPair.Value;
                if (value is not null && value.GetType() != property.Type)
                {
                    value = Convert.ChangeType(value, property.Type, CultureInfo.InvariantCulture);
                }

                var equal = Expression.Equal(property, Expression.Constant(value, property.Type));
                where = where is null ? equal : Expression.AndAlso(where, equal);
            }

            if (where is null)
            {
                return false;
            }

            var whereLambda = Expression.Lambda(where, param);
            query = ExpressionHelpers.Where(query, whereLambda, elementType);

            var result = await api.QueryAsync(new QueryRequest(query), cancellationToken).ConfigureAwait(false);
            return result.Results.Cast<object>().Any();
        }

        private async Task<IList<object>> QueryChildrenByFk(
            string targetEntitySetName,
            string fkPropertyName,
            IReadOnlyDictionary<string, object> parentKey,
            CancellationToken cancellationToken)
        {
            var query = api.GetQueryableSource(targetEntitySetName);
            var elementType = query.ElementType;
            var param = Expression.Parameter(elementType);

            // Build FK filter: child.FkProperty == parentKey value
            // The FK value matches the parent's key value
            var parentKeyValue = parentKey.Values.First(); // Assume single-key parent for FK match
            var fkProperty = Expression.Property(param, fkPropertyName);

            // FK may be nullable — need to handle that
            var fkUnderlyingType = Nullable.GetUnderlyingType(fkProperty.Type) ?? fkProperty.Type;
            var convertedValue = Convert.ChangeType(parentKeyValue, fkUnderlyingType, CultureInfo.InvariantCulture);

            Expression fkValue = Expression.Constant(convertedValue, fkUnderlyingType);
            Expression fkExpr = fkProperty;

            // If FK is nullable, unwrap for comparison
            if (Nullable.GetUnderlyingType(fkProperty.Type) is not null)
            {
                fkExpr = Expression.Property(fkProperty, "Value");
                // Also add HasValue check
                var hasValue = Expression.Property(fkProperty, "HasValue");
                var equalExpr = Expression.Equal(fkExpr, fkValue);
                var combinedExpr = Expression.AndAlso(hasValue, equalExpr);
                var whereLambda = Expression.Lambda(combinedExpr, param);
                query = ExpressionHelpers.Where(query, whereLambda, elementType);
            }
            else
            {
                var equalExpr = Expression.Equal(fkExpr, fkValue);
                var whereLambda = Expression.Lambda(equalExpr, param);
                query = ExpressionHelpers.Where(query, whereLambda, elementType);
            }

            var result = await api.QueryAsync(new QueryRequest(query), cancellationToken).ConfigureAwait(false);
            return result.Results.Cast<object>().ToList();
        }

        private IEdmNavigationProperty FindEdmNavigationProperty(IEdmEntityType entityType, string clrNavPropName)
        {
            // Try direct name match first
            var prop = entityType.FindProperty(clrNavPropName) as IEdmNavigationProperty;
            if (prop is not null)
            {
                return prop;
            }

            // Try matching via CLR property name mapping
            foreach (var navProp in entityType.NavigationProperties())
            {
                var clrName = EdmClrPropertyMapper.GetClrPropertyName(navProp, model);
                if (string.Equals(clrName, clrNavPropName, StringComparison.Ordinal))
                {
                    return navProp;
                }
            }

            return null;
        }

        private string FindFkPropertyName(IEdmNavigationProperty edmNavProp)
        {
            // Try referential constraint first
            if (edmNavProp.ReferentialConstraint is not null)
            {
                foreach (var pair in edmNavProp.ReferentialConstraint.PropertyPairs)
                {
                    return EdmClrPropertyMapper.GetClrPropertyName(pair.DependentProperty, model);
                }
            }

            // Try the partner's referential constraint
            if (edmNavProp.Partner?.ReferentialConstraint is not null)
            {
                foreach (var pair in edmNavProp.Partner.ReferentialConstraint.PropertyPairs)
                {
                    return EdmClrPropertyMapper.GetClrPropertyName(pair.DependentProperty, model);
                }
            }

            var childType = edmNavProp.ToEntityType();

            // Fall back to convention: {PartnerNavName}Id on child type
            var partnerName = edmNavProp.Partner?.Name;
            if (partnerName is not null)
            {
                var fkConventionName = partnerName + "Id";
                var edmProp = childType.FindProperty(fkConventionName);
                if (edmProp is not null)
                {
                    return EdmClrPropertyMapper.GetClrPropertyName(edmProp, model);
                }
            }

            // Fall back to convention: {DeclaringTypeName}Id on child type
            // This handles the case where Partner is null but the declaring type name
            // matches the FK pattern (e.g., Publisher.Books -> Book.PublisherId)
            var declaringTypeName = edmNavProp.DeclaringType?.FullTypeName();
            if (declaringTypeName is not null)
            {
                // Extract the short name (after the last dot)
                var shortName = declaringTypeName;
                var lastDot = declaringTypeName.LastIndexOf('.');
                if (lastDot >= 0)
                {
                    shortName = declaringTypeName.Substring(lastDot + 1);
                }

                var fkByDeclTypeName = shortName + "Id";
                var edmProp = childType.FindProperty(fkByDeclTypeName);
                if (edmProp is not null)
                {
                    return EdmClrPropertyMapper.GetClrPropertyName(edmProp, model);
                }
            }

            return null;
        }

        private string GetInverseNavigationPropertyName(IEdmNavigationProperty edmNavProp)
        {
            if (edmNavProp.Partner is not null)
            {
                return EdmClrPropertyMapper.GetClrPropertyName(edmNavProp.Partner, model);
            }

            return null;
        }

        private string FindTargetEntitySetName(IEdmNavigationProperty navProperty)
        {
            var container = model.EntityContainer;
            if (container is not null)
            {
                foreach (var entitySet in container.EntitySets())
                {
                    var navigationTarget = entitySet.FindNavigationTarget(navProperty);
                    if (navigationTarget is not null
                        && container.FindEntitySet(navigationTarget.Name) is not null)
                    {
                        return navigationTarget.Name;
                    }
                }

                // Fallback: match entity set by target entity type name.
                var targetType = navProperty.ToEntityType();
                foreach (var entitySet in container.EntitySets())
                {
                    if (string.Equals(entitySet.EntityType.FullTypeName(), targetType.FullTypeName(), StringComparison.Ordinal)
                        || string.Equals(entitySet.EntityType.Name, targetType.Name, StringComparison.Ordinal))
                    {
                        return entitySet.Name;
                    }
                }
            }

            return navProperty.ToEntityType().Name;
        }

        private static string KeyToString(IReadOnlyDictionary<string, object> key)
        {
            return string.Join(",", key.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}"));
        }
    }
}
