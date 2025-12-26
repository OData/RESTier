// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Query;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.AspNetCore.Query
#else
namespace Microsoft.Restier.AspNet.Query
#endif
{
    /// <summary>
    /// Restier Query executor.
    /// </summary>
    internal class RestierQueryExecutor : IQueryExecutor
    {
        /// <summary>
        /// Gets or sets the inner Query executor.
        /// </summary>
        public IQueryExecutor Inner { get; set; }

        /// <summary>
        /// Executes a query asynchronously.
        /// </summary>
        /// <typeparam name="TElement">The type of the element.</typeparam>
        /// <param name="context">The Query context.</param>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<QueryResult> ExecuteQueryAsync<TElement>(QueryContext context, IQueryable<TElement> query, CancellationToken cancellationToken)
        {
            var countOption = context.GetApiService<RestierQueryExecutorOptions>();
            if (countOption.IncludeTotalCount)
            {
                var countQuery = ExpressionHelpers.GetCountableQuery(query);
                var expression = ExpressionHelpers.Count(countQuery.Expression, countQuery.ElementType);
                var result = await ExecuteExpressionAsync<long>(context, countQuery.Provider, expression, cancellationToken).ConfigureAwait(false);
                var totalCount = result.Results.Cast<long>().Single();

                countOption.SetTotalCount(totalCount);
            }

            return await Inner.ExecuteQueryAsync(context, query, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes an expression asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The resulting type.</typeparam>
        /// <param name="context">The Query context.</param>
        /// <param name="queryProvider">The query provider.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<QueryResult> ExecuteExpressionAsync<TResult>(QueryContext context, IQueryProvider queryProvider, Expression expression, CancellationToken cancellationToken)
            => Inner.ExecuteExpressionAsync<TResult>(context, queryProvider, expression, cancellationToken);
    }
}
