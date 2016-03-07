// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Properties;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents the default query handler.
    /// </summary>
    internal static class DefaultQueryHandler
    {
        /// <summary>
        /// Asynchronously executes the query flow.
        /// </summary>
        /// <param name="context">
        /// The query context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a query result.
        /// </returns>
        public static async Task<QueryResult> QueryAsync(
            QueryContext context,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");

            // process query expression
            var expression = context.Request.Expression;
            var visitor = new QueryExpressionVisitor(context);
            expression = visitor.Visit(expression);

            // get element type
            Type elementType = null;
            var queryType = expression.Type.FindGenericType(typeof(IQueryable<>));
            if (queryType != null)
            {
                elementType = queryType.GetGenericArguments()[0];
            }

            // append count expression if requested
            if (elementType != null && context.Request.ShouldReturnCount)
            {
                expression = ExpressionHelpers.Count(expression, elementType);
                elementType = null; // now return type is single int
            }

            // execute query
            QueryResult result;
            var executor = context.GetApiService<IQueryExecutor>();
            if (executor == null)
            {
                throw new NotSupportedException(Resources.QueryExecutorMissing);
            }

            if (elementType != null)
            {
                var query = visitor.BaseQuery.Provider.CreateQuery(expression);
                if (query is EnumerableQuery)
                {
                    // Use the default query executor to handle query created
                    // from IEnumerable<T> by calling query.ToList().
                    executor = DefaultQueryExecutor.Instance;
                }

                var method = typeof(IQueryExecutor)
                    .GetMethod("ExecuteQueryAsync")
                    .MakeGenericMethod(elementType);
                var parameters = new object[]
                {
                    context, query, cancellationToken
                };
                var task = method.Invoke(executor, parameters) as Task<QueryResult>;
                result = await task;
            }
            else
            {
                var method = typeof(IQueryExecutor)
                    .GetMethod("ExecuteSingleAsync")
                    .MakeGenericMethod(expression.Type);
                var parameters = new object[]
                {
                    context, visitor.BaseQuery, expression, cancellationToken
                };
                var task = method.Invoke(executor, parameters) as Task<QueryResult>;
                result = await task;
            }

            if (result != null)
            {
                result.ResultsSource = visitor.EntitySet;
            }

            return result;
        }

        private class QueryExpressionVisitor : ExpressionVisitor
        {
            private readonly QueryExpressionContext context;
            private readonly IDictionary<Expression, Expression> processedExpressions;
            private IQueryExpressionInspector inspector;
            private IQueryExpressionExpander expander;
            private IQueryExpressionFilter filter;
            private IQueryExpressionSourcer sourcer;

            public QueryExpressionVisitor(QueryContext context)
            {
                this.context = new QueryExpressionContext(context);
                this.processedExpressions = new Dictionary<Expression, Expression>();
            }

            public IQueryable BaseQuery { get; private set; }

            public IEdmEntitySet EntitySet { get; private set; }

            public override Expression Visit(Expression node)
            {
                if (node == null)
                {
                    return null;
                }

                // Initialize and push the visited node
                var visited = node;
                this.context.PushVisitedNode(visited);

                // If visited node has already been processed,
                // skip normalization, inspection and filtering
                // and simply replace with the processed node
                if (this.processedExpressions.ContainsKey(visited))
                {
                    node = this.processedExpressions[visited];
                }
                else
                {
                    // Only visit the visited node's children if
                    // the visited node represents API data
                    if (!(this.context.ModelReference is ApiDataReference))
                    {
                        // Visit visited node's children
                        node = base.Visit(visited);
                    }

                    // Inspect the visited node
                    this.Inspect();

                    // Try to expand the visited node
                    // if it represents API data
                    if (this.context.ModelReference is ApiDataReference)
                    {
                        node = this.Expand(visited);
                    }

                    // Filter the visited node
                    node = this.Filter(visited, node);
                }

                if (visited == node)
                {
                    if (this.context.ModelReference is ApiDataReference)
                    {
                        // If no processing occurred on the visited node
                        // and it represents API data, then it must be
                        // in its most primitive form, so source the node
                        node = this.Source(node);
                    }

                    if (this.BaseQuery == null)
                    {
                        // The very first time control reaches here, the
                        // visited node represents the original starting
                        // point for the entire composed query, and thus
                        // it should produce a non-embedded expression.
                        var constant = node as ConstantExpression;
                        if (constant == null)
                        {
                            throw new NotSupportedException(Resources.OriginalExpressionShouldBeConstant);
                        }

                        this.BaseQuery = constant.Value as IQueryable;
                        if (this.BaseQuery == null)
                        {
                            throw new NotSupportedException(Resources.OriginalExpressionShouldBeQueryable);
                        }

                        node = this.BaseQuery.Expression;
                    }
                }

                // TODO GitHubIssue#28 : Support transformation between API types and data source proxy types
                this.context.PopVisitedNode();

                if (this.context.VisitedNode != null)
                {
                    this.EntitySet = this.context.ModelReference != null ?
                        this.context.ModelReference.EntitySet : null;
                }

                return node;
            }

            private void Inspect()
            {
                if (this.inspector == null)
                {
                    this.inspector = this.context.QueryContext.GetApiService<IQueryExpressionInspector>();
                }

                if (this.inspector != null && !this.inspector.Inspect(this.context))
                {
                    throw new InvalidOperationException(Resources.InspectionFailed);
                }
            }

            private Expression Expand(Expression visited)
            {
                if (this.expander == null)
                {
                    this.expander = this.context.QueryContext
                        .GetApiService<IQueryExpressionExpander>();
                }

                if (expander == null)
                {
                    return visited;
                }

                var expanded = expander.Expand(this.context);
                var callback = this.context.AfterNestedVisitCallback;
                this.context.AfterNestedVisitCallback = null;
                if (expanded != null && expanded != visited)
                {
                    if (!visited.Type.IsAssignableFrom(expanded.Type))
                    {
                        throw new InvalidOperationException(
                            Resources.ExpanderCannotChangeExpressionType);
                    }

                    this.context.PushVisitedNode(null);
                    expanded = this.Visit(expanded);
                    this.context.PopVisitedNode();
                    if (callback != null)
                    {
                        callback();
                    }

                    return expanded;
                }

                return visited;
            }

            private Expression Filter(Expression visited, Expression processed)
            {
                if (this.filter == null)
                {
                    this.filter = this.context.QueryContext.GetApiService<IQueryExpressionFilter>();
                }

                if (this.filter != null)
                {
                    var filtered = filter.Filter(this.context);
                    var callback = this.context.AfterNestedVisitCallback;
                    this.context.AfterNestedVisitCallback = null;
                    if (filtered != null && filtered != visited)
                    {
                        if (!visited.Type.IsAssignableFrom(filtered.Type))
                        {
                            throw new InvalidOperationException(
                                Resources.FilterCannotChangeExpressionType);
                        }

                        this.processedExpressions.Add(visited, processed);
                        this.context.PushVisitedNode(null);
                        try
                        {
                            processed = this.Visit(filtered);
                        }
                        finally
                        {
                            this.context.PopVisitedNode();
                            this.processedExpressions.Remove(visited);
                        }

                        if (callback != null)
                        {
                            callback();
                        }
                    }
                }

                return processed;
            }

            private Expression Source(Expression node)
            {
                if (this.sourcer == null)
                {
                    this.sourcer = this.context.QueryContext
                        .GetApiService<IQueryExpressionSourcer>();
                }

                if (this.sourcer == null)
                {
                    // Missing sourcer
                    throw new NotSupportedException(Resources.QuerySourcerMissing);
                }

                node = this.sourcer.Source(this.context, this.BaseQuery != null);
                if (node == null)
                {
                    // Missing source expression
                    throw new NotSupportedException(Resources.SourceExpressionMissing);
                }

                return node;
            }
        }
    }
}
