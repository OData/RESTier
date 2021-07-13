// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if !EFCore
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#endif
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
#if EFCore
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Restier.Core.Query;

#if EFCore
using IAsyncQueryProvider = Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider;
#endif

#if EFCore
namespace Microsoft.Restier.EntityFrameworkCore
#else
namespace Microsoft.Restier.EntityFramework
#endif
{
    /// <summary>
    /// Represents a query executor that uses Entity Framework methods.
    /// This class only executes queries against EF provider, it'll
    /// delegate other queries to inner IQueryExecutor.
    /// </summary>
    internal class EFQueryExecutor : IQueryExecutor
    {
        /// <summary>
        /// Gets or sets the Inner IQueryExecutor.
        /// </summary>
        public IQueryExecutor Inner { get; set; }

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
        public async Task<QueryResult> ExecuteQueryAsync<TElement>(
            QueryContext context,
            IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
#if EFCore
            if (query.Provider is IAsyncQueryProvider)
#else
            if (query.Provider is IDbAsyncQueryProvider)
#endif
            {
                return new QueryResult(await query.ToArrayAsync(cancellationToken).ConfigureAwait(false));
            }

            return await Inner.ExecuteQueryAsync(context, query, cancellationToken).ConfigureAwait(false);
        }

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
        /// A query provider to execute expression.
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
        public async Task<QueryResult> ExecuteExpressionAsync<TResult>(
            QueryContext context,
            IQueryProvider queryProvider,
            Expression expression,
            CancellationToken cancellationToken)
        {
#if EFCore
            var provider = queryProvider as IAsyncQueryProvider;
#else
            var provider = queryProvider as IDbAsyncQueryProvider;
#endif
            if (provider != null)
            {
#if EFCore
                var result = await provider.ExecuteAsync<Task<TResult>>(expression, cancellationToken).ConfigureAwait(false);
                return new QueryResult(new TResult[] { result });
#else
                var result = await provider.ExecuteAsync<TResult>(expression, cancellationToken).ConfigureAwait(false);
                return new QueryResult(new TResult[] { result });
#endif
            }

            return await Inner.ExecuteExpressionAsync<TResult>(context, queryProvider, expression, cancellationToken).ConfigureAwait(false);
        }
    }
}
