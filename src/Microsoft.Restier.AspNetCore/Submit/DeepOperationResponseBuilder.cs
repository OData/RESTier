// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.AspNetCore.Submit
{
    internal static class DeepOperationResponseBuilder
    {
        public static SelectExpandClause BuildSelectExpandClause(
            DataModificationItem rootItem,
            IEdmModel model,
            IEdmEntitySet entitySet)
        {
            // Only expand for NestedItems (inline deep insert entities).
            // NavigationBindings (@odata.bind) are relationship-only operations —
            // the bound entity wasn't included inline in the request, so the response
            // doesn't need to expand it per OData 4.01 response expansion rules.
            if (rootItem.NestedItems.Count == 0)
            {
                return null;
            }

            var entityType = entitySet.EntityType;
            var expandItems = new List<SelectItem>();

            var navPropNames = new HashSet<string>();
            foreach (var nested in rootItem.NestedItems)
            {
                if (nested.ParentNavigationPropertyName is not null)
                {
                    navPropNames.Add(nested.ParentNavigationPropertyName);
                }
            }

            foreach (var navPropName in navPropNames)
            {
                var edmNavProp = FindNavigationProperty(entityType, navPropName, model);
                if (edmNavProp is null)
                {
                    continue;
                }

                var navigationSource = entitySet.FindNavigationTarget(edmNavProp);

                // Default to an empty (but non-null) child clause.
                // SelectedPropertiesNode.Create throws a NullReferenceException when
                // the child SelectExpandClause passed to ExpandedNavigationSelectItem is null.
                SelectExpandClause childClause = new SelectExpandClause(Array.Empty<SelectItem>(), allSelected: true);
                var childItems = rootItem.NestedItems
                    .Where(n => n.ParentNavigationPropertyName == navPropName)
                    .ToList();

                if (childItems.Any(c => c.NestedItems.Count > 0 || c.NavigationBindings.Count > 0)
                    && navigationSource is IEdmEntitySet childEntitySet)
                {
                    var representativeChild = childItems.First(c => c.NestedItems.Count > 0 || c.NavigationBindings.Count > 0);
                    childClause = BuildSelectExpandClause(representativeChild, model, childEntitySet)
                        ?? new SelectExpandClause(Array.Empty<SelectItem>(), allSelected: true);
                }

                var segment = new NavigationPropertySegment(edmNavProp, navigationSource);
                var expandItem = new ExpandedNavigationSelectItem(
                    new ODataExpandPath(segment),
                    navigationSource,
                    childClause);

                expandItems.Add(expandItem);
            }

            if (expandItems.Count == 0)
            {
                return null;
            }

            return new SelectExpandClause(expandItems, allSelected: true);
        }

        private static IEdmNavigationProperty FindNavigationProperty(
            IEdmEntityType entityType,
            string clrPropertyName,
            IEdmModel model)
        {
            var prop = entityType.FindProperty(clrPropertyName) as IEdmNavigationProperty;
            if (prop is not null)
            {
                return prop;
            }

            foreach (var navProp in entityType.NavigationProperties())
            {
                if (string.Equals(navProp.Name, clrPropertyName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return navProp;
                }
            }

            return null;
        }
    }
}
