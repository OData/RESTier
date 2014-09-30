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
    /// Represents a hook point that sources a query expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Query expression sourcing converts an expression that identifies
    /// domain data in a normalized manner to an equivalent representation
    /// in terms of the underlying data source proxy.
    /// </para>
    /// <para>
    /// Sourcing is the last step that occurs when processing a query
    /// expression, and only happens on expressions that represent domain
    /// data that cannot be expanded into any more primitive of an expression.
    /// </para>
    /// <para>
    /// This is a singleton hook point that should be
    /// implemented by an underlying data provider.
    /// </para>
    /// </remarks>
    public interface IQueryExpressionSourcer
    {
        /// <summary>
        /// Sources an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <param name="embedded">
        /// Indicates if the sourcing is occurring on an embedded node.
        /// </param>
        /// <returns>
        /// A data source expression that represents the visited node.
        /// </returns>
        /// <remarks>
        /// <para>
        /// When <paramref name="embedded"/> is <c>false</c>, this method
        /// should produce a constant expression whose value is a queryable
        /// object produced by calling into the underlying data source proxy.
        /// </para>
        /// <para>
        /// When <paramref name="embedded"/> is <c>true</c>, this method should
        /// return an expression that represents the domain data identified by
        /// the visited node in terms of the underlying data source proxy.
        /// </para>
        /// <para>
        /// Consider an example where the data source API has a method to get
        /// a query over customers, accessed through "data.GetCustomers()".
        /// When <paramref name="embedded"/> is false, this method should call
        /// that method and return a constant expression containing the query.
        /// When <paramref name="embedded"/> is true, this method should build
        /// a call expression to "GetCustomers" where the object to which it
        /// applies is a constant expression whose value is the data object.
        /// </para>
        /// </remarks>
        Expression Source(QueryExpressionContext context, bool embedded);
    }
}
