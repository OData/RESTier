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
    /// Represents a hook point that normalizes a query expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Query expression normalization converts an expression that identifies
    /// domain data in a non-normalized manner to its equivalent normalized
    /// representation, which is a method call to one of the methods defined
    /// on the <see cref="DomainData"/> class.
    /// </para>
    /// <para>
    /// Normalization is the first step that occurs when processing a query
    /// expression and ensures that arbitrary expressions identifying domain
    /// data, such as "myData.Customers", are converted to a representation
    /// over which query expression visitors can reason in a general manner.
    /// </para>
    /// <para>
    /// This is a multi-cast hook point whose instances
    /// are used in the reverse order of registration.
    /// </para>
    /// </remarks>
    public interface IQueryExpressionNormalizer
    {
        /// <summary>
        /// Normalizes an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <returns>
        /// A normalized expression of the same type as the visited node, or
        /// if normalization did not apply, the visited node or <c>null</c>.
        /// </returns>
        Expression Normalize(QueryExpressionContext context);
    }
}
