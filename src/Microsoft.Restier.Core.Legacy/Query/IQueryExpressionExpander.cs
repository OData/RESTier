// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq.Expressions;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents a service that expands a query expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Query expression expansion converts an expression that represents
    /// normalized API data into an expression using more primitive nodes.
    /// </para>
    /// <para>
    /// Expansion is the second step that occurs when processing a query
    /// expression after its children have been visited, so it occurs during
    /// upward traversal of the query expression and after inspection. Since
    /// expansion fundamentally alters the query expression, the resulting
    /// expression is recursively processed to ensure that all appropriate
    /// normalization, inspection, expansion, filtering and sourcing occurs.
    /// </para>
    /// </remarks>
    public interface IQueryExpressionExpander
    {
        /// <summary>
        /// Expands an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <returns>
        /// An expanded expression of the same type as the visited node, or
        /// if expansion did not apply, the visited node or <c>null</c>.
        /// </returns>
        Expression Expand(QueryExpressionContext context);
    }
}
