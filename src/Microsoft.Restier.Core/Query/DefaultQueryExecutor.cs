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
        /// <summary>
        /// Gets or sets the inner query executor.
        /// </summary>
        public IQueryExecutor Inner { get; set; }

        /// <inheritdoc/>
        public Task<QueryResult> ExecuteQueryAsync<TElement>(
            QueryContext context,
            IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            var result = new QueryResult(query);
            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public Task<QueryResult> ExecuteExpressionAsync<TResult>(
            QueryContext context,
            IQueryProvider queryProvider,
            Expression expression,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(queryProvider, nameof(queryProvider));
            return Task.FromResult(new QueryResult(new[] { queryProvider.Execute(expression) }));
        }
    }
}
