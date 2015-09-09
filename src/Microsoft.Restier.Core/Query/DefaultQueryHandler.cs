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

            // execute query
            QueryResult result = null;
            var executor = context.GetHookPoint<IQueryExecutor>();
            if (executor == null)
            {
                throw new NotSupportedException();
            }

            var queryType = expression.Type
                .FindGenericType(typeof(IQueryable<>));
            if (queryType != null)
            {
                var query = visitor.BaseQuery.Provider.CreateQuery(expression);
                var method = typeof(IQueryExecutor)
                    .GetMethod("ExecuteQueryAsync")
                    .MakeGenericMethod(queryType.GetGenericArguments()[0]);
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

            context.Result = result;
            return context.Result;
        }

        private class QueryExpressionVisitor : ExpressionVisitor
        {
            private readonly QueryExpressionContext context;
            private readonly IDictionary<Expression, Expression> processedExpressions;
            private IEnumerable<IQueryExpressionNormalizer> normalizers;
            private IEnumerable<IQueryExpressionInspector> inspectors;
            private IEnumerable<IQueryExpressionExpander> expanders;
            private IEnumerable<IQueryExpressionFilter> filters;
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
                    // Normalize visited node
                    node = this.Normalize(visited);
                    if (node != visited)
                    {
                        // Update the visited node
                        visited = node;
                        this.context.ReplaceVisitedNode(visited);
                    }

                    // Only visit the visited node's children if
                    // the visited node represents domain data
                    if (!(this.context.ModelReference is DomainDataReference))
                    {
                        // Visit visited node's children
                        node = base.Visit(visited);
                    }

                    // Inspect the visited node
                    this.Inspect();

                    // Try to expand the visited node
                    // if it represents domain data
                    if (this.context.ModelReference is DomainDataReference)
                    {
                        node = this.Expand(visited);
                    }

                    // Filter the visited node
                    node = this.Filter(visited, node);
                }

                // If no processing occurred on the visited node
                // and it represents domain data, then it must be
                // in its most primitive form, so source the node
                if (visited == node &&
                    this.context.ModelReference is DomainDataReference)
                {
                    node = this.Source(node);
                }

                // TODO GitHubIssue#28 : Support transformation between domain types and data source proxy types
                this.context.PopVisitedNode();

                if (this.context.VisitedNode != null)
                {
                    this.EntitySet = this.context.ModelReference != null ?
                        this.context.ModelReference.EntitySet : null;
                }

                return node;
            }

            private Expression Normalize(Expression visited)
            {
                if (this.normalizers == null)
                {
                    this.normalizers = this.context.QueryContext
                        .GetHookPoints<IQueryExpressionNormalizer>().Reverse();
                }

                foreach (var normalizer in this.normalizers)
                {
                    var normalized = normalizer.Normalize(this.context);
                    if (normalized != null && normalized != visited)
                    {
                        if (!visited.Type.IsAssignableFrom(normalized.Type))
                        {
                            // Normalizer cannot change expression type
                            // TODO GitHubIssue#24 : error message
                            throw new InvalidOperationException();
                        }

                        return normalized;
                    }
                }

                return visited;
            }

            private void Inspect()
            {
                if (this.inspectors == null)
                {
                    this.inspectors = this.context.QueryContext
                        .GetHookPoints<IQueryExpressionInspector>().Reverse();
                }

                if (this.inspectors.Any(i => !i.Inspect(this.context)))
                {
                    throw new InvalidOperationException(Resources.InspectionFailed);
                }
            }

            private Expression Expand(Expression visited)
            {
                if (this.expanders == null)
                {
                    this.expanders = this.context.QueryContext
                        .GetHookPoints<IQueryExpressionExpander>().Reverse();
                }

                foreach (var expander in this.expanders)
                {
                    var expanded = expander.Expand(this.context);
                    var callback = this.context.AfterNestedVisitCallback;
                    this.context.AfterNestedVisitCallback = null;
                    if (expanded != null && expanded != visited)
                    {
                        if (!visited.Type.IsAssignableFrom(expanded.Type))
                        {
                            // Expander cannot change expression type
                            // TODO GitHubIssue#24 : error message
                            throw new InvalidOperationException();
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
                }

                return visited;
            }

            private Expression Filter(Expression visited, Expression processed)
            {
                if (this.filters == null)
                {
                    this.filters = this.context.QueryContext
                        .GetHookPoints<IQueryExpressionFilter>();
                }

                foreach (var filter in this.filters)
                {
                    var filtered = filter.Filter(this.context);
                    var callback = this.context.AfterNestedVisitCallback;
                    this.context.AfterNestedVisitCallback = null;
                    if (filtered != null && filtered != visited)
                    {
                        if (!visited.Type.IsAssignableFrom(filtered.Type))
                        {
                            // Filter cannot change expression type
                            // TODO GitHubIssue#24 : error message
                            throw new InvalidOperationException();
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
                        .GetHookPoint<IQueryExpressionSourcer>();
                }

                if (this.sourcer == null)
                {
                    // Missing sourcer
                    throw new NotSupportedException();
                }

                node = this.sourcer.Source(this.context, this.BaseQuery != null);
                if (node == null)
                {
                    // Missing source expression
                    throw new NotSupportedException();
                }

                if (this.BaseQuery == null)
                {
                    // The very first time the sourcer is used, the
                    // visited node represents the original starting
                    // point for the entire composed query, and thus
                    // it should produce a non-embedded expression.
                    var constant = node as ConstantExpression;
                    if (constant == null)
                    {
                        throw new NotSupportedException();
                    }

                    this.BaseQuery = constant.Value as IQueryable;
                    if (this.BaseQuery == null)
                    {
                        throw new NotSupportedException();
                    }

                    node = this.BaseQuery.Expression;
                }

                return node;
            }
        }
    }
}
