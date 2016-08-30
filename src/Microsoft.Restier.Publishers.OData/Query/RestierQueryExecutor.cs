// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Publishers.OData.Query
{
    internal class RestierQueryExecutor : IQueryExecutor
    {
        public IQueryExecutor Inner { get; set; }

        public async Task<QueryResult> ExecuteQueryAsync<TElement>(
            QueryContext context,
            IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
            var countOption = context.GetApiService<RestierQueryExecutorOptions>();
            if (countOption.IncludeTotalCount)
            {
                var countQuery = ExpressionHelpers.GetCountableQuery(query);
                var expression = ExpressionHelpers.Count(countQuery.Expression, countQuery.ElementType);
                var result
                    = await ExecuteExpressionAsync<long>(context, countQuery.Provider, expression, cancellationToken);
                var totalCount = result.Results.Cast<long>().Single();

                countOption.SetTotalCount(totalCount);
            }

            return await Inner.ExecuteQueryAsync<TElement>(context, query, cancellationToken);
        }

        public Task<QueryResult> ExecuteExpressionAsync<TResult>(
            QueryContext context,
            IQueryProvider queryProvider,
            Expression expression,
            CancellationToken cancellationToken)
        {
            return Inner.ExecuteExpressionAsync<TResult>(context, queryProvider, expression, cancellationToken);
        }
    }
}
