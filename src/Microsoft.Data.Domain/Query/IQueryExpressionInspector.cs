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

namespace Microsoft.Data.Domain.Query
{
    /// <summary>
    /// Represents a hook point that inspects a query expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Query expression inspection evaluates an expression to determine
    /// if it is valid according to domain logic such as authorization rules.
    /// </para>
    /// <para>
    /// Inspection is the first step that occurs when processing a query
    /// expression after its children have been visited, so it occurs during
    /// upward traversal of the query expression. This ensures that inspection
    /// has a chance to take place before the node is altered in any way (with
    /// the exception of normalization of expressions identifying domain data).
    /// </para>
    /// <para>
    /// This is a multi-cast hook point whose instances
    /// are used in the reverse order of registration.
    /// </para>
    /// </remarks>
    public interface IQueryExpressionInspector
    {
        /// <summary>
        /// Inspects an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <returns>
        /// <c>true</c> if the inspection passed; otherwise, <c>false</c>.
        /// </returns>
        bool Inspect(QueryExpressionContext context);
    }
}
