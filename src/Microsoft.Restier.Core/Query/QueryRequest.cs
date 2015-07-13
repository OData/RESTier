// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents a query request.
    /// </summary>
    public class QueryRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryRequest" /> class with a composed query.
        /// </summary>
        /// <param name="query">
        /// A composed query that was derived from a queryable source.
        /// </param>
        /// <param name="includeTotalCount">
        /// Indicates if the total number of items should be retrieved
        /// when the result has been filtered using paging operators.
        /// </param>
        public QueryRequest(IQueryable query, bool? includeTotalCount = null)
        {
            Ensure.NotNull(query, "query");
            if (!(query is QueryableSource))
            {
                throw new NotSupportedException();
            }

            this.Expression = query.Expression;
            this.IncludeTotalCount = includeTotalCount;
        }

        /// <summary>
        /// Creates a new singular query request with
        /// a composed query and scalar expression.
        /// </summary>
        /// <typeparam name="TElement">
        /// The type of the elements in the query.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of the result.
        /// </typeparam>
        /// <param name="query">
        /// A composed query that was derived from a queryable source.
        /// </param>
        /// <param name="singularExpression">
        /// An expression that when composed on top of
        /// the composed query produces a singular result.
        /// </param>
        /// <param name="includeTotalCount">
        /// Indicates if the total number of items should be retrieved
        /// when the result has been filtered using paging operators.
        /// </param>
        /// <returns>
        /// The created instance of the <see cref="QueryRequest"/> class.
        /// </returns>
        public static QueryRequest Create<TElement, TResult>(
            IQueryable<TElement> query,
            Expression<Func<IQueryable<TElement>, TResult>> singularExpression,
            bool? includeTotalCount = null)
        {
            return QueryRequest.Create(query as IQueryable,
                singularExpression, includeTotalCount);
        }

        /// <summary>
        /// Creates a new singular query request with
        /// a composed query and scalar expression.
        /// </summary>
        /// <param name="query">
        /// A composed query that was derived from a queryable source.
        /// </param>
        /// <param name="singularExpression">
        /// An expression that when composed on top of
        /// the composed query produces a singular result.
        /// </param>
        /// <param name="includeTotalCount">
        /// Indicates if the total number of items should be retrieved
        /// when the result has been filtered using paging operators.
        /// </param>
        /// <returns>
        /// The created instance of the <see cref="QueryRequest"/> class.
        /// </returns>
        public static QueryRequest Create(
            IQueryable query,
            LambdaExpression singularExpression,
            bool? includeTotalCount = null)
        {
            Ensure.NotNull(query, "query");
            Ensure.NotNull(singularExpression, "singularExpression");
            var request = new QueryRequest(query, includeTotalCount);
            var rewriter = new SingularExpressionRewriter(
                query, singularExpression.Parameters[0]);
            request.Expression = rewriter.Visit(singularExpression.Body);
            return request;
        }

        private class SingularExpressionRewriter : ExpressionVisitor
        {
            private readonly IQueryable _query;
            private readonly ParameterExpression _parameter;

            public SingularExpressionRewriter(
                IQueryable query,
                ParameterExpression parameter)
            {
                this._query = query;
                this._parameter = parameter;
            }

            protected override Expression VisitParameter(
                ParameterExpression node)
            {
                if (node == this._parameter)
                {
                    return this._query.Expression;
                }

                return base.VisitParameter(node);
            }
        }

        /// <summary>
        /// Gets or sets the composed query expression.
        /// </summary>
        public Expression Expression { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the total
        /// number of items should be retrieved when the
        /// result has been filtered using paging operators.
        /// </summary>
        /// <remarks>
        /// Setting this to <c>true</c> may have a performance impact as
        /// the data provider may need to execute two independent queries.
        /// </remarks>
        public bool? IncludeTotalCount { get; set; }
    }
}
