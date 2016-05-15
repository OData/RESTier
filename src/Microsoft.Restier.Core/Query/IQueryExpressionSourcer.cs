// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq.Expressions;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents a service that replace queryable source of an expression.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Query expression sourcing converts an expression that identifies
    /// API data in a normalized manner to an equivalent representation
    /// in terms of the underlying data source proxy.
    /// </para>
    /// <para>
    /// Sourcing is the last step that occurs when processing a query
    /// expression, and only happens on expressions that represent API
    /// data that cannot be expanded into any more primitive of an expression.
    /// </para>
    /// </remarks>
    public interface IQueryExpressionSourcer
    {
        /// <summary>
        /// Replace queryable source of an expression.
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
        /// return an expression that represents the API data identified by
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
        Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded);
    }
}
