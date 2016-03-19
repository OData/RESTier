// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if !EF7
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#endif
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
#if EF7
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
#endif
using Microsoft.Restier.Core.Query;
#if EF7
using IAsyncQueryProvider = Microsoft.Data.Entity.Query.IAsyncQueryProvider;
#endif

namespace Microsoft.Restier.EntityFramework.Query
{
    /// <summary>
    /// Represents a query executor that uses Entity Framework methods.
    /// </summary>
    internal class QueryExecutor : IQueryExecutor
    {
        public IQueryExecutor Inner { get; set; }

        /// <summary>
        /// Asynchronously executes a query and produces a query result.
        /// This class only executes queries against EF provider, it'll
        /// delegate other queries to inner IQueryExecutor.
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
#if EF7
            if (query.Provider is IAsyncQueryProvider)
#else
            if (query.Provider is IDbAsyncQueryProvider)
#endif
            {
                return new QueryResult(
                    await query.ToArrayAsync(cancellationToken));
            }

            return await Inner.ExecuteQueryAsync(context, query, cancellationToken);
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
        /// <param name="query">
        /// A base query.
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
        public async Task<QueryResult> ExecuteSingleAsync<TResult>(
            QueryContext context,
            IQueryable query,
            Expression expression,
            CancellationToken cancellationToken)
        {
#if EF7
            var provider = query.Provider as IAsyncQueryProvider;
#else
            var provider = query.Provider as IDbAsyncQueryProvider;
#endif
            if (provider != null)
            {
                var result = await provider.ExecuteAsync<TResult>(
                    expression, cancellationToken);
                return new QueryResult(new TResult[] { result });
            }

            return await Inner.ExecuteSingleAsync<TResult>(context, query, expression, cancellationToken);
        }
    }
}
