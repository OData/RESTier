// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Domain.Query;
using QueryContext = Microsoft.Data.Domain.Query.QueryContext;

namespace Microsoft.Data.Domain.EntityFramework.Query
{
    /// <summary>
    /// Represents a query executor that uses Entity Framework methods.
    /// </summary>
    public class QueryExecutor : IQueryExecutor
    {
        private QueryExecutor()
        {
        }

        /// <summary>
        /// Gets the single instance of this query executor.
        /// </summary>
        public static readonly QueryExecutor Instance = new QueryExecutor();

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
            QueryContext context, IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
            long? totalCount = null;
            if (context.Request.IncludeTotalCount == true)
            {
                var countQuery = QueryExecutor.StripPagingOperators(query);
                totalCount = await countQuery.LongCountAsync(cancellationToken);
            }
            return new QueryResult(
                await query.ToArrayAsync(cancellationToken),
                totalCount);
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
            IQueryable query, Expression expression,
            CancellationToken cancellationToken)
        {
            var provider = query.Provider as IDbAsyncQueryProvider;
            var result = await provider.ExecuteAsync<TResult>(
                expression, cancellationToken);
            return new QueryResult(new TResult[] { result });
        }

        private static IQueryable<TElement> StripPagingOperators<TElement>(
            IQueryable<TElement> query)
        {
            var expression = query.Expression;
            expression = QueryExecutor.StripQueryMethod(expression, "Take");
            expression = QueryExecutor.StripQueryMethod(expression, "Skip");
            if (expression != query.Expression)
            {
                query = query.Provider.CreateQuery<TElement>(expression);
            }
            return query;
        }

        private static Expression StripQueryMethod(
            Expression expression, string methodName)
        {
            var methodCall = expression as MethodCallExpression;
            if (methodCall != null &&
                methodCall.Method.DeclaringType == typeof(Queryable) &&
                methodCall.Method.Name.Equals(methodName, StringComparison.Ordinal))
            {
                expression = methodCall.Arguments[0];
            }
            return expression;
        }
    }
}
