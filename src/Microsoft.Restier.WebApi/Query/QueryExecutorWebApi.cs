// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Extensions;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.WebApi.Query
{
    internal class QueryExecutorWebApi : IQueryExecutor
    {
        public IQueryExecutor Inner { get; set; }

        public async Task<QueryResult> ExecuteQueryAsync<TElement>(
            QueryContext context,
            IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
            var queryContext = context.ApiContext.GetApiService<WebApiContext>();
            if (queryContext.QueryIncludeTotalCount == true)
            {
                var countQuery = ExpressionHelpers.GetCountableQuery(query);
                var expression = countQuery.Expression;
                expression = ExpressionHelpers.Count(expression, countQuery.ElementType);
                var result = await ExecuteSingleAsync<long>(context, countQuery, expression, cancellationToken);

                queryContext.Request.ODataProperties().TotalCount = result.Results.Cast<long>().Single();
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
