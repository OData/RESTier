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
    internal class DefaultQueryExecutor : IQueryExecutor
    {
        /// <inheritdoc/>
        public Task<QueryResult> ExecuteQueryAsync<TElement>(
            QueryContext context,
            IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");
            var result = new QueryResult(query.ToList());
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
    }
}
