// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.AspNet.Query
{
    /// <summary>
    /// 
    /// </summary>
    internal class RestierQueryExecutor : IQueryExecutor
    {

        /// <summary>
        /// 
        /// </summary>
        public IQueryExecutor Inner { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="context"></param>
        /// <param name="query"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="context"></param>
        /// <param name="queryProvider"></param>
        /// <param name="expression"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<QueryResult> ExecuteExpressionAsync<TResult>( QueryContext context, IQueryProvider queryProvider, Expression expression, CancellationToken cancellationToken)
            => Inner.ExecuteExpressionAsync<TResult>(context, queryProvider, expression, cancellationToken);
    }
}
