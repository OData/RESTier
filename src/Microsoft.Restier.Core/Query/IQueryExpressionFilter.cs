// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq.Expressions;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents a service that filters a query expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Query expression filtering converts an expression node into a
    /// different expression node according to API logic such as a
    /// restricting filter on top of some composable API data.
    /// </para>
    /// <para>
    /// Filtering is the third step that occurs when processing a query
    /// expression after its children have been visited, so it occurs during
    /// upward traversal of the query expression and after inspection and
    /// expansion. Since filtering fundamentally alters the query expression,
    /// the resulting expression is recursively processed to ensure that all
    /// appropriate normalization, inspection, expansion, filtering and
    /// sourcing occurs.
    /// </para>
    /// </remarks>
    public interface IQueryExpressionFilter
    {
        /// <summary>
        /// Filters an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <returns>
        /// A filtered expression of the same type as the visited node, or
        /// if filtering did not apply, the visited node.
        /// </returns>
        Expression Filter(QueryExpressionContext context);
    }
}
