// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;

namespace Microsoft.Data.Domain.Query
{
    /// <summary>
    /// Represents the default query handler.
    /// </summary>
    public class DefaultQueryHandler : IQueryHandler
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
        public async Task<QueryResult> QueryAsync(
            QueryContext context,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");

            // STEP 1: pre-filter
            var filters = context.GetHookPoints<IQueryFilter>();
            foreach (var filter in filters.Reverse())
            {
                await filter.FilterRequestAsync(context, cancellationToken);
                if (context.Result != null)
                {
                    return context.Result;
                }
            }

            // STEP 2: process query expression
            var expression = context.Request.Expression;
            var visitor = new QueryExpressionVisitor(context);
            expression = visitor.Visit(expression);

            // STEP 3: execute query
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
                var task = method.Invoke(executor, new object[] {
                    context, query, cancellationToken
                }) as Task<QueryResult>;
                result = await task;
            }
            else
            {
                var method = typeof(IQueryExecutor)
                    .GetMethod("ExecuteSingleAsync")
                    .MakeGenericMethod(expression.Type);
                var task = method.Invoke(executor, new object[] {
                    context, visitor.BaseQuery, expression, cancellationToken
                }) as Task<QueryResult>;
                result = await task;
            }
            if (result != null)
            {
                result.ResultsSource = visitor.EntitySet;
            }
            context.Result = result;

            // STEP 4: post-filter
            foreach (var filter in filters)
            {
                await filter.FilterResultAsync(context, cancellationToken);
            }

            return context.Result;
        }

        private class QueryExpressionVisitor : ExpressionVisitor
        {
            private readonly QueryExpressionContext _context;
            private readonly IDictionary<Expression, Expression> _processed;
            private IEnumerable<IQueryExpressionNormalizer> _normalizers;
            private IEnumerable<IQueryExpressionInspector> _inspectors;
            private IEnumerable<IQueryExpressionExpander> _expanders;
            private IEnumerable<IQueryExpressionFilter> _filters;
            private IQueryExpressionSourcer _sourcer;

            public QueryExpressionVisitor(QueryContext context)
            {
                this._context = new QueryExpressionContext(context);
                this._processed = new Dictionary<Expression, Expression>();
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
                this._context.PushVisitedNode(visited);

                // If visited node has already been processed,
                // skip normalization, inspection and filtering
                // and simply replace with the processed node
                if (this._processed.ContainsKey(visited))
                {
                    node = this._processed[visited];
                }
                else
                {
                    // Normalize visited node
                    node = this.Normalize(visited);
                    if (node != visited)
                    {
                        // Update the visited node
                        visited = node;
                        this._context.ReplaceVisitedNode(visited);
                    }

                    // Only visit the visited node's children if
                    // the visited node represents domain data
                    if (!(this._context.ModelReference is DomainDataReference))
                    {
                        // Visit visited node's children
                        node = base.Visit(visited);
                    }

                    // Inspect the visited node
                    this.Inspect();

                    // Try to expand the visited node
                    // if it represents domain data
                    if (this._context.ModelReference is DomainDataReference)
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
                    this._context.ModelReference is DomainDataReference)
                {
                    node = this.Source(node);
                }

                // TODO: IQueryExpressionTranslator - for when the domain
                // types are different from the data source proxy types

                this._context.PopVisitedNode();

                if (this._context.VisitedNode != null)
                {
                    this.EntitySet = this._context.ModelReference != null ?
                        this._context.ModelReference.EntitySet : null;
                }

                return node;
            }

            private Expression Normalize(Expression visited)
            {
                var normalizers = this._normalizers ??
                    (this._normalizers = this._context.QueryContext
                        .GetHookPoints<IQueryExpressionNormalizer>().Reverse());
                foreach (var normalizer in normalizers)
                {
                    var normalized = normalizer.Normalize(this._context);
                    if (normalized != null && normalized != visited)
                    {
                        if (!visited.Type.IsAssignableFrom(normalized.Type))
                        {
                            // Normalizer cannot change expression type
                            // TODO: error message
                            throw new InvalidOperationException();
                        }
                        return normalized;
                    }
                }
                return visited;
            }

            private void Inspect()
            {
                var inspectors = this._inspectors ??
                    (this._inspectors = this._context.QueryContext
                        .GetHookPoints<IQueryExpressionInspector>().Reverse());
                if (inspectors.Any(i => !i.Inspect(this._context)))
                {
                    // TODO: error message
                    throw new InvalidOperationException("Inspection failed.");
                }
            }

            private Expression Expand(Expression visited)
            {
                var expanders = this._expanders ??
                    (this._expanders = this._context.QueryContext
                        .GetHookPoints<IQueryExpressionExpander>().Reverse());
                foreach (var expander in expanders)
                {
                    var expanded = expander.Expand(this._context);
                    var callback = this._context.AfterNestedVisitCallback;
                    this._context.AfterNestedVisitCallback = null;
                    if (expanded != null && expanded != visited)
                    {
                        if (!visited.Type.IsAssignableFrom(expanded.Type))
                        {
                            // Expander cannot change expression type
                            // TODO: error message
                            throw new InvalidOperationException();
                        }
                        this._context.PushVisitedNode(null);
                        expanded = this.Visit(expanded);
                        this._context.PopVisitedNode();
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
                var filters = this._filters ??
                    (this._filters = this._context.QueryContext
                        .GetHookPoints<IQueryExpressionFilter>());
                foreach (var filter in filters)
                {
                    var filtered = filter.Filter(this._context);
                    var callback = this._context.AfterNestedVisitCallback;
                    this._context.AfterNestedVisitCallback = null;
                    if (filtered != null && filtered != visited)
                    {
                        if (!visited.Type.IsAssignableFrom(filtered.Type))
                        {
                            // Filter cannot change expression type
                            // TODO: error message
                            throw new InvalidOperationException();
                        }
                        this._processed.Add(visited, processed);
                        this._context.PushVisitedNode(null);
                        try
                        {
                            processed = this.Visit(filtered);
                        }
                        finally
                        {
                            this._context.PopVisitedNode();
                            this._processed.Remove(visited);
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
                var sourcer = this._sourcer ??
                    (this._sourcer = this._context.QueryContext
                        .GetHookPoint<IQueryExpressionSourcer>());
                if (sourcer == null)
                {
                    // Missing sourcer
                    throw new NotSupportedException();
                }
                node = sourcer.Source(this._context, this.BaseQuery != null);
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
