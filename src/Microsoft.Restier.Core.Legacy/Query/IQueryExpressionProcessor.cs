// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq.Expressions;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents a service that processes a query expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Query expression processing converts an expression node into a
    /// different expression node according to API logic such as a
    /// restricting filter on top of some composable API data.
    /// </para>
    /// <para>
    /// Processing is the third step that occurs when visiting a query
    /// expression after its children have been visited, so it occurs during
    /// upward traversal of the query expression and after inspection and
    /// expansion. Since processing fundamentally alters the query expression,
    /// the resulting expression is recursively processed to ensure that all
    /// appropriate normalization, inspection, expansion, processing and
    /// sourcing occurs.
    /// </para>
    /// </remarks>
    public interface IQueryExpressionProcessor
    {
        /// <summary>
        /// Processes an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <returns>
        /// A processed expression of the same type as the visited node, or
        /// if processing did not apply, the visited node.
        /// </returns>
        Expression Process(QueryExpressionContext context);
    }
}
