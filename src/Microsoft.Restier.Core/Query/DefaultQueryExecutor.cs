// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Default implementation for <see cref="IQueryExecutor"/>
    /// </summary>
    public class DefaultQueryExecutor : IQueryExecutor
    {
        private const string MethodNameOfQueryTake = "Take";

        private const string MethodNameOfQuerySkip = "Skip";

        static DefaultQueryExecutor()
        {
            Instance = new DefaultQueryExecutor();
        }

        private DefaultQueryExecutor()
        {
        }

        /// <summary>
        /// Gets the singleton Instance for <see cref="DefaultQueryExecutor"/>
        /// </summary>
        public static DefaultQueryExecutor Instance { get; private set; }

        /// <summary>
        /// Remove paging methods for given IQueryable
        /// </summary>
        /// <typeparam name="TElement">The type parameter for IQueryable</typeparam>
        /// <param name="query">The input query.</param>
        /// <returns>The proceed query.</returns>
        public static IQueryable<TElement> StripPagingOperators<TElement>(
           IQueryable<TElement> query)
        {
            Ensure.NotNull(query, "query");
            var expression = query.Expression;
            expression = StripQueryMethod(expression, MethodNameOfQueryTake);
            expression = StripQueryMethod(expression, MethodNameOfQuerySkip);
            if (expression != query.Expression)
            {
                query = query.Provider.CreateQuery<TElement>(expression);
            }

            return query;
        }

        /// <inheritdoc/>
        public Task<QueryResult> ExecuteQueryAsync<TElement>(
            QueryContext context,
            IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");
            var result = new QueryResult(query.ToList());
            if (context.Request.IncludeTotalCount == true)
            {
                result.TotalCount = StripPagingOperators(query).Count();
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<QueryResult> ExecuteSingleAsync<TResult>(
            QueryContext context,
            IQueryable query,
            Expression expression,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(query, "query");
            return Task.FromResult(new QueryResult(new[] { query.Provider.Execute(expression) }));
        }

        private static Expression StripQueryMethod(Expression expression, string methodName)
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
