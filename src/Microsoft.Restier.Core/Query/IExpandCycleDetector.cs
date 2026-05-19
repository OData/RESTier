// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Inspects a parsed OData <see cref="SelectExpandClause"/> to determine
    /// whether the expand graph contains a cycle.
    /// </summary>
    /// <remarks>
    /// A cycle exists when any <c>$expand</c> segment targets an entity type
    /// already present (directly or through inheritance) on the current
    /// expansion path. Both self-cycles (<c>Employee → Manager: Employee</c>)
    /// and cross-type cycles (<c>Department → Employees → Department</c>) are
    /// considered cycles.
    /// </remarks>
    public interface IExpandCycleDetector
    {
        /// <summary>
        /// Determines whether the supplied expand clause, rooted at
        /// <paramref name="rootType"/>, contains a cycle.
        /// </summary>
        /// <param name="rootType">The entity type of the queried set, used as
        /// the initial node of the expansion path. Required.</param>
        /// <param name="clause">The parsed select-and-expand clause. May be
        /// <c>null</c> (e.g. requests with no <c>$expand</c>) — in which case
        /// the method returns <c>false</c>.</param>
        /// <returns><c>true</c> if a cycle is detected, otherwise <c>false</c>.</returns>
        bool HasCycle(IEdmEntityType rootType, SelectExpandClause clause);
    }
}
