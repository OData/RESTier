// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Default <see cref="IExpandCycleDetector"/> — walks the expand tree
    /// depth-first and flags any segment whose target type shares an
    /// inheritance hierarchy with a type already on the current path.
    /// </summary>
    internal sealed class DefaultExpandCycleDetector : IExpandCycleDetector
    {
        /// <inheritdoc/>
        public bool HasCycle(IEdmEntityType rootType, SelectExpandClause clause)
        {
            Ensure.NotNull(rootType, nameof(rootType));

            if (clause is null)
            {
                return false;
            }

            var path = new List<IEdmEntityType> { rootType };
            return HasCycle(clause, path);
        }

        private static bool HasCycle(SelectExpandClause clause, List<IEdmEntityType> path)
        {
            foreach (var item in clause.SelectedItems)
            {
                IEdmType target;
                SelectExpandClause nested;

                if (item is ExpandedNavigationSelectItem expanded)
                {
                    target = expanded.PathToNavigationProperty.LastSegment.EdmType;
                    nested = expanded.SelectAndExpand;
                }
                else if (item is ExpandedReferenceSelectItem reference)
                {
                    target = reference.PathToNavigationProperty.LastSegment.EdmType;
                    nested = null;
                }
                else
                {
                    continue;
                }

                var targetEntity = ResolveEntityType(target);
                if (targetEntity is null)
                {
                    continue;
                }

                foreach (var onPath in path)
                {
                    if (SharesHierarchy(onPath, targetEntity))
                    {
                        return true;
                    }
                }

                path.Add(targetEntity);
                try
                {
                    if (nested is not null && HasCycle(nested, path))
                    {
                        return true;
                    }
                }
                finally
                {
                    path.RemoveAt(path.Count - 1);
                }
            }

            return false;
        }

        /// <summary>
        /// A navigation property's <see cref="IEdmType"/> may be the entity
        /// type itself or a <see cref="IEdmCollectionType"/> wrapping it.
        /// Reduce to the underlying entity type, returning <c>null</c> for
        /// non-entity targets (which should not arise from a valid
        /// navigation expand but are handled defensively).
        /// </summary>
        private static IEdmEntityType ResolveEntityType(IEdmType type)
        {
            if (type is IEdmCollectionType collection)
            {
                type = collection.ElementType.Definition;
            }

            return type as IEdmEntityType;
        }

        /// <summary>
        /// True when <paramref name="a"/> equals <paramref name="b"/> or one
        /// inherits from the other. Inheritance counts because EF's identity
        /// map keys on the base entity type — querying a derived type after
        /// the base (or vice versa) revisits the same identity space.
        /// </summary>
        private static bool SharesHierarchy(IEdmEntityType a, IEdmEntityType b)
        {
            return IsOrInheritsFrom(a, b) || IsOrInheritsFrom(b, a);
        }

        private static bool IsOrInheritsFrom(IEdmEntityType derived, IEdmEntityType maybeBase)
        {
            for (var current = derived; current is not null; current = current.BaseEntityType())
            {
                if (ReferenceEquals(current, maybeBase))
                {
                    return true;
                }

                if (string.Equals(current.FullName(), maybeBase.FullName(), StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
