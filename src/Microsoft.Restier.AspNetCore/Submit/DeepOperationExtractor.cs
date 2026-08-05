// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.AspNetCore.Submit
{
    /// <summary>
    /// Walks an EdmEntityObject and extracts nested entities into a DataModificationItem tree.
    /// Entity references (@odata.bind in 4.0, @id in 4.01) are stored as NavigationBindings on the parent.
    /// </summary>
    internal class DeepOperationExtractor
    {
        private readonly IEdmModel model;
        private readonly ApiBase api;
        private readonly DeepOperationSettings settings;

        public DeepOperationExtractor(IEdmModel model, ApiBase api, DeepOperationSettings settings)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.api = api ?? throw new ArgumentNullException(nameof(api));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void ExtractNestedItems(
            Delta entity,
            IEdmStructuredType edmType,
            DataModificationItem parentItem,
            bool isCreation,
            int currentDepth = 0)
        {
            foreach (var propertyName in entity.GetChangedPropertyNames())
            {
                var edmProperty = edmType.FindProperty(propertyName);
                if (edmProperty is not IEdmNavigationProperty navProperty)
                {
                    continue; // Not a nav prop — already handled by CreatePropertyDictionary
                }

                var clrPropertyName = EdmClrPropertyMapper.GetClrPropertyName(edmProperty, model);

                if (!entity.TryGetPropertyValue(propertyName, out var value) || value is null)
                {
                    // Null nav prop — record for unlink handling
                    parentItem.NullNavigationProperties.Add(clrPropertyName);
                    continue;
                }

                var targetEntityType = navProperty.ToEntityType();
                var targetEntitySet = FindTargetEntitySet(navProperty);

                if (value is EdmEntityObject nestedEntity)
                {
                    ProcessSingleNestedEntity(
                        nestedEntity, targetEntityType, targetEntitySet,
                        clrPropertyName, parentItem, isCreation, currentDepth);
                }
                else if (value is IEnumerable collection && value is not string)
                {
                    foreach (var item in collection)
                    {
                        if (item is EdmEntityObject collectionEntity)
                        {
                            ProcessSingleNestedEntity(
                                collectionEntity, targetEntityType, targetEntitySet,
                                clrPropertyName, parentItem, isCreation, currentDepth);
                        }
                    }
                }
            }
        }

        private void ProcessSingleNestedEntity(
            EdmEntityObject nestedEntity,
            IEdmEntityType targetEntityType,
            string targetEntitySetName,
            string clrNavPropertyName,
            DataModificationItem parentItem,
            bool isCreation,
            int currentDepth)
        {
            if (IsEntityReference(nestedEntity, targetEntityType))
            {
                var bindRef = CreateBindReference(nestedEntity, targetEntityType, targetEntitySetName);
                if (!parentItem.NavigationBindings.TryGetValue(clrNavPropertyName, out var bindList))
                {
                    bindList = new List<BindReference>();
                    parentItem.NavigationBindings[clrNavPropertyName] = bindList;
                }

                bindList.Add(bindRef);
                return;
            }

            var childDepth = currentDepth + 1;

            // Reject if this child would exceed max depth
            if (settings.MaxDepth > 0 && childDepth > settings.MaxDepth)
            {
                throw new ODataException(
                    $"Deep operation exceeds maximum nesting depth of {settings.MaxDepth}.");
            }

            var actualEdmType = nestedEntity.ActualEdmType as IEdmStructuredType ?? targetEntityType;
            var clrType = actualEdmType.GetClrType(model);

            var extractedKeys = ExtractKeyValues(nestedEntity, targetEntityType);
            var creationLocalValues = nestedEntity.CreatePropertyDictionary(actualEdmType, api, isCreation: true);
            var updateLocalValues = nestedEntity.CreatePropertyDictionary(actualEdmType, api, isCreation: false);

            var childItem = new DataModificationItem(
                targetEntitySetName,
                targetEntityType.GetClrType(model),
                clrType,
                RestierEntitySetOperation.Insert, // Always Insert — classifier reclassifies in Task 5
                extractedKeys.Count > 0 ? extractedKeys : null,
                null,
                creationLocalValues)
            {
                ParentItem = parentItem,
                ParentNavigationPropertyName = clrNavPropertyName,
                UpdateLocalValues = updateLocalValues,
            };

            parentItem.NestedItems.Add(childItem);

            // Always recurse — the depth check above will reject grandchildren if needed
            ExtractNestedItems(nestedEntity, actualEdmType, childItem, isCreation, childDepth);
        }

        private static bool IsEntityReference(EdmEntityObject entity, IEdmEntityType entityType)
        {
            // When @odata.bind is used (OData 4.0), the OData framework resolves it to an
            // EdmEntityObject containing only the key properties extracted from the bind URL.
            // Detect this case: if the only changed properties are key properties, the entity
            // was created from a reference URL rather than an inline body.
            // Note: @odata.id (OData 4.01) is consumed by the deserializer and never appears
            // as a property value, so there is no TryGetPropertyValue check for it.
            var changedPropertyNames = new HashSet<string>(entity.GetChangedPropertyNames(), StringComparer.OrdinalIgnoreCase);
            if (changedPropertyNames.Count == 0)
            {
                return true;
            }

            if (entityType is not null)
            {
                var keyPropertyNames = new HashSet<string>(
                    entityType.Key().Select(k => k.Name),
                    StringComparer.OrdinalIgnoreCase);

                if (changedPropertyNames.IsSubsetOf(keyPropertyNames))
                {
                    return true;
                }
            }

            return false;
        }

        private BindReference CreateBindReference(
            EdmEntityObject entity,
            IEdmEntityType entityType,
            string entitySetName)
        {
            return new BindReference
            {
                ResourceSetName = entitySetName,
                ResourceKey = ExtractKeyValues(entity, entityType),
            };
        }

        private IReadOnlyDictionary<string, object> ExtractKeyValues(
            EdmEntityObject entity,
            IEdmEntityType entityType)
        {
            // Only extract keys that were explicitly provided in the payload (in the changed properties set).
            // TryGetPropertyValue returns default values (e.g. Guid.Empty) for unset properties,
            // which would incorrectly treat a keyless payload as having a key.
            var changedPropertyNames = new HashSet<string>(
                entity.GetChangedPropertyNames(), StringComparer.OrdinalIgnoreCase);
            var keys = new Dictionary<string, object>();
            foreach (var keyProperty in entityType.Key())
            {
                if (changedPropertyNames.Contains(keyProperty.Name)
                    && entity.TryGetPropertyValue(keyProperty.Name, out var value))
                {
                    var clrName = EdmClrPropertyMapper.GetClrPropertyName(keyProperty, model);
                    keys[clrName] = value;
                }
            }

            return keys;
        }

        private string FindTargetEntitySet(IEdmNavigationProperty navProperty)
        {
            var container = model.EntityContainer;
            if (container is not null)
            {
                // Primary: use explicit navigation bindings
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
                // Handles cases where FindNavigationTarget returns a phantom navigation source
                // (e.g., with the type name instead of the entity set name).
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
    }
}
