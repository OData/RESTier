// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.WebApi.Query
{
    internal class ODataQueryExecutor : IQueryExecutor
    {
        public IQueryExecutor Inner { get; set; }

        public async Task<QueryResult> ExecuteQueryAsync<TElement>(
            QueryContext context,
            IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
            var countOption = context.ApiContext.GetApiService<ODataQueryExecutorOptions>();
            if (countOption.IncludeTotalCount)
            {
                var countQuery = ExpressionHelpers.GetCountableQuery(query);
                var expression = ExpressionHelpers.Count(countQuery.Expression, countQuery.ElementType);
                var result = await ExecuteSingleAsync<long>(context, countQuery, expression, cancellationToken);
                var totalCount = result.Results.Cast<long>().Single();

                countOption.SetTotalCount(totalCount);
            }

            return await Inner.ExecuteQueryAsync<TElement>(context, query, cancellationToken);
        }

        public Task<QueryResult> ExecuteSingleAsync<TResult>(
            QueryContext context,
            IQueryable query,
            Expression expression,
            CancellationToken cancellationToken)
        {
            return Inner.ExecuteSingleAsync<TResult>(context, query, expression, cancellationToken);
        }
    }
}
