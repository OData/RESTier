// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents the default query handler.
    /// </summary>
    internal static class DefaultQueryHandler
    {
        private const string ExpressionMethodNameOfWhere = "Where";
        private const string ExpressionMethodNameOfSelect = "Select";
        private const string ExpressionMethodNameOfSelectMany = "SelectMany";

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
                var method = typeof(IQueryExecutor)
                    .GetMethod("ExecuteQueryAsync")
                    .MakeGenericMethod(elementType);
                var parameters = new object[]
                {
                    context, query, cancellationToken
                };
                var task = method.Invoke(executor, parameters) as Task<QueryResult>;
                result = await task;

                await CheckSubExpressionResult(
                    context, cancellationToken, result.Results, visitor, executor, expression);
            }
            else
            {
                var method = typeof(IQueryExecutor)
                    .GetMethod("ExecuteExpressionAsync")
                    .MakeGenericMethod(expression.Type);
                var parameters = new object[]
                {
                    context, visitor.BaseQuery.Provider, expression, cancellationToken
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

        private static async Task CheckSubExpressionResult(
            QueryContext context,
            CancellationToken cancellationToken,
            IEnumerable enumerableResult,
            QueryExpressionVisitor visitor,
            IQueryExecutor executor,
            Expression expression)
        {
            if (enumerableResult.GetEnumerator().MoveNext())
            {
                // If there is some result, will not have additional processing
                return;
            }

            var methodCallExpression = expression as MethodCallExpression;

            // This will remove unneeded statement which includes $expand, $select,$top,$skip,$orderby
            methodCallExpression = methodCallExpression.RemoveUnneededStatement();
            if (methodCallExpression == null || methodCallExpression.Arguments.Count != 2)
            {
                return;
            }

            if (methodCallExpression.Method.Name == ExpressionMethodNameOfWhere)
            {
                // Throw exception if key as last where statement, or remove $filter where statement
                methodCallExpression = CheckWhereCondition(methodCallExpression);
                if (methodCallExpression == null || methodCallExpression.Arguments.Count != 2)
                {
                    return;
                }

                // Call without $filter where statement and with Key where statement
                if (methodCallExpression.Method.Name == ExpressionMethodNameOfWhere)
                {
                    // The last where from $filter is removed and run with key where statement
                    await ExecuteSubExpression(context, cancellationToken, visitor, executor, methodCallExpression);
                    return;
                }
            }

            if (methodCallExpression.Method.Name != ExpressionMethodNameOfSelect
                && methodCallExpression.Method.Name != ExpressionMethodNameOfSelectMany)
            {
                // If last statement is not select property, will no further checking
                return;
            }

            var subExpression = methodCallExpression.Arguments[0];

            // Remove appended statement like Where(Param_0 => (Param_0.Prop != null)) if there is one
            subExpression = subExpression.RemoveAppendWhereStatement();

            await ExecuteSubExpression(context, cancellationToken, visitor, executor, subExpression);
        }

        private static async Task ExecuteSubExpression(
            QueryContext context,
            CancellationToken cancellationToken,
            QueryExpressionVisitor visitor,
            IQueryExecutor executor,
            Expression expression)
        {
            // get element type
            Type elementType = null;
            var queryType = expression.Type.FindGenericType(typeof(IQueryable<>));
            if (queryType != null)
            {
                elementType = queryType.GetGenericArguments()[0];
            }

            var query = visitor.BaseQuery.Provider.CreateQuery(expression);
            var method = typeof(IQueryExecutor)
                .GetMethod("ExecuteQueryAsync")
                .MakeGenericMethod(elementType);
            var parameters = new object[]
            {
                context, query, cancellationToken
            };
            var task = method.Invoke(executor, parameters) as Task<QueryResult>;
            var result = await task;

            var any = result.Results.Cast<object>().Any();
            if (!any)
            {
                // Which means previous expression does not have result, and should throw ResourceNotFoundException.
                throw new ResourceNotFoundException(Resources.ResourceNotFound);
            }
        }

        private static MethodCallExpression CheckWhereCondition(MethodCallExpression methodCallExpression)
        {
            // This means a select for expand is appended, will remove it for resource existing check
            var lastWhere = methodCallExpression.Arguments[1] as UnaryExpression;
            var lambdaExpression = lastWhere.Operand as LambdaExpression;
            if (lambdaExpression == null)
            {
                return null;
            }

            var binaryExpression = lambdaExpression.Body as BinaryExpression;
            if (binaryExpression == null)
            {
                return null;
            }

            // Key segment will have ConstantExpression but $filter will not have ConstantExpression
            var rightExpression = binaryExpression.Right as ConstantExpression;
            if (rightExpression != null && rightExpression.Value != null)
            {
                // This means where statement is key segment but not for $filter
                throw new ResourceNotFoundException(Resources.ResourceNotFound);
            }

            return methodCallExpression.Arguments[0] as MethodCallExpression;
        }

        private class QueryExpressionVisitor : ExpressionVisitor
        {
            private readonly QueryExpressionContext context;
            private readonly IDictionary<Expression, Expression> processedExpressions;
            private IQueryExpressionAuthorizer authorizer;
            private IQueryExpressionExpander expander;
            private IQueryExpressionProcessor processor;
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
                    if (!(this.context.ModelReference is DataSourceStubModelReference))
                    {
                        // Visit visited node's children
                        node = base.Visit(visited);
                    }

                    // Inspect the visited node
                    this.Inspect();

                    // Try to expand the visited node
                    // if it represents API data
                    if (this.context.ModelReference is DataSourceStubModelReference)
                    {
                        node = this.Expand(visited);
                    }

                    // Process the visited node
                    node = this.Process(visited, node);
                }

                if (visited == node)
                {
                    if (this.context.ModelReference is DataSourceStubModelReference)
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
                if (this.authorizer == null)
                {
                    this.authorizer = this.context.QueryContext.GetApiService<IQueryExpressionAuthorizer>();
                }

                if (this.authorizer != null && !this.authorizer.Authorize(this.context))
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

            private Expression Process(Expression visited, Expression processed)
            {
                if (this.processor == null)
                {
                    this.processor = this.context.QueryContext.GetApiService<IQueryExpressionProcessor>();
                }

                if (this.processor != null)
                {
                    var filtered = processor.Process(this.context);
                    var callback = this.context.AfterNestedVisitCallback;
                    this.context.AfterNestedVisitCallback = null;
                    if (filtered != null && filtered != visited)
                    {
                        if (!visited.Type.IsAssignableFrom(filtered.Type))
                        {
                            // In order to filter on the navigation properties,
                            // the type is changed from ICollection<> to IQueryable<>
                            var collectionType = visited.Type.FindGenericType(typeof(ICollection<>));
                            var queryableType = filtered.Type.FindGenericType(typeof(IQueryable<>));
                            if (collectionType == null || queryableType == null)
                            {
                                throw new InvalidOperationException(
                                    Resources.ProcessorCannotChangeExpressionType);
                            }

                            var queryableElementType = queryableType.GenericTypeArguments[0];
                            var collectionElementType = collectionType.GenericTypeArguments[0];
                            if (collectionElementType != queryableElementType
                                && !queryableElementType.IsAssignableFrom(collectionElementType))
                            {
                                throw new InvalidOperationException(
                                    Resources.ProcessorCannotChangeExpressionType);
                            }
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

                node = this.sourcer.ReplaceQueryableSource(this.context, this.BaseQuery != null);
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
