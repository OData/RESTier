// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Linq.Expressions;

namespace Microsoft.Data.Domain.Query
{
    /// <summary>
    /// Represents a hook point that filters a query expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Query expression filtering converts an expression node into a
    /// different expression node according to domain logic such as a
    /// restricting filter on top of some composable domain data.
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
    /// <para>
    /// This is a multi-cast hook point whose instances
    /// are used in the original order of registration.
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
        /// if filtering did not apply, the visited node or <c>null</c>.
        /// </returns>
        Expression Filter(QueryExpressionContext context);
    }
}
