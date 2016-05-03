// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents a service that executes a query.
    /// </summary>
    /// <remarks>
    /// Data provider implemented IQueryExecutor should only handle queries against the specific
    /// provider, and delegates all other queries to inner IQueryExecutor.
    /// </remarks>
    public interface IQueryExecutor
    {
        /// <summary>
        /// Asynchronously executes a query and produces a query result.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the query.
        /// </typeparam>
        /// <param name="context">
        /// The query context.
        /// </param>
        /// <param name="query">
        /// A composed query.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a query result.
        /// </returns>
        Task<QueryResult> ExecuteQueryAsync<TElement>(
            QueryContext context,
            IQueryable<TElement> query,
            CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously executes a singleton
        /// query and produces a query result.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the singleton query result.
        /// </typeparam>
        /// <param name="context">
        /// The query context.
        /// </param>
        /// <param name="queryProvider">
        /// A query provider to execute the expression.
        /// </param>
        /// <param name="expression">
        /// An expression to be composed on the base query.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a query result.
        /// </returns>
        Task<QueryResult> ExecuteExpressionAsync<TResult>(
            QueryContext context,
            IQueryProvider queryProvider,
            Expression expression,
            CancellationToken cancellationToken);
    }
}
