// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents the default query handler.
    /// </summary>
    internal class DefaultQueryHandler : IQueryHandler
    {
        private readonly IQueryExpressionAuthorizer authorizer;
        private readonly IQueryExpressionExpander expander;
        private readonly IQueryExpressionProcessor processor;
        private readonly IQueryExecutor executor;
        private readonly IQueryExpressionSourcer sourcer;
        private readonly IModelMapper mapper;

        /// <summary>
        /// Initializes a new instance of the DefaultQueryHandler class.
        /// </summary>
        /// <param name="sourcerFactory">The query expression sourcer factory to use.</param>
        /// <param name="executorFactory">The query executor factory to use.</param>
        /// <param name="mapperFactory">The model mapper factory to use.</param>
        /// <param name="authorizerFactory">The query expression authorizer factory to use.</param>
        /// <param name="expanderFactory">The query expression expander factory to use.</param>
        /// <param name="processorFactory">The query expression processor factory to use.</param>
        public DefaultQueryHandler(
            IChainOfResponsibilityFactory<IQueryExpressionSourcer> sourcerFactory,
            IChainOfResponsibilityFactory<IQueryExecutor> executorFactory,
            IChainOfResponsibilityFactory<IModelMapper> mapperFactory,
            IChainOfResponsibilityFactory<IQueryExpressionAuthorizer> authorizerFactory,
            IChainOfResponsibilityFactory<IQueryExpressionExpander> expanderFactory,
            IChainOfResponsibilityFactory<IQueryExpressionProcessor> processorFactory)
        {
            Ensure.NotNull(sourcerFactory, nameof(sourcerFactory));
            Ensure.NotNull(executorFactory, nameof(executorFactory));
            Ensure.NotNull(mapperFactory, nameof(mapperFactory));
            Ensure.NotNull(authorizerFactory, nameof(authorizerFactory));
            Ensure.NotNull(expanderFactory, nameof(expanderFactory));
            Ensure.NotNull(processorFactory, nameof(processorFactory));

            this.authorizer = authorizerFactory.Create();
            this.expander = expanderFactory.Create();
            this.processor = processorFactory.Create();
            this.executor = executorFactory.Create() ??
                           throw new InvalidOperationException("The IChainOfResponsibilityFactory for IQueryExecutor should return at least one implementation.");
            this.sourcer = sourcerFactory.Create() ??
                           throw new InvalidOperationException("The IChainOfResponsibilityFactory for IQueryExpressionSourcer should return at least one implementation.");
            this.mapper = mapperFactory.Create() ??
                          throw new InvalidOperationException("The IChainOfResponsibilityFactory for IModelMapper should return at least one implementation.");

        }

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
            Ensure.NotNull(context, nameof(context));

            // process query expression
            var expression = context.Request.Query.Expression;
            var visitor = new QueryExpressionVisitor(context, sourcer, authorizer, expander, processor);
            expression = visitor.Visit(expression);

            // get element type
            Type elementType = null;
            var queryType = expression.Type.FindGenericType(typeof(IQueryable<>));
            if (queryType is not null)
            {
                elementType = queryType.GetGenericArguments()[0];
            }

            // append count expression if requested
            if (elementType is not null && context.Request.ShouldReturnCount)
            {
                expression = ExpressionHelpers.Count(expression, elementType);
                elementType = null; // now return type is single int
            }

            // execute query
            QueryResult result;

            if (elementType is not null)
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
                result = await task.ConfigureAwait(false);
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
                result = await task.ConfigureAwait(false);
            }

            if (result is not null)
            {
                result.ResultsSource = visitor.EntitySet;
            }

            return result;
        }

        /// <summary>
        /// Ensures that the Element Type exists in the model.
        /// </summary>
        /// <param name="invocationContext">The model context to use.</param>
        /// <param name="namespaceName">The namespace of the element type. Can be null.</param>
        /// <param name="name">The name of the element type.</param>
        /// <returns>The element type.</returns>
        public Type EnsureElementType(InvocationContext invocationContext, string namespaceName, string name)
        {
            Type elementType;

            if (namespaceName is null)
            {
                mapper.TryGetRelevantType(invocationContext, name, out elementType);
            }
            else
            {
                mapper.TryGetRelevantType(invocationContext, namespaceName, name, out elementType);
            }

            if (elementType is null)
            {
                throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Resources.ElementTypeNotFound, name));
            }

            return elementType;
        }

        private class QueryExpressionVisitor : ExpressionVisitor
        {
            private readonly QueryExpressionContext context;
            private readonly IDictionary<Expression, Expression> processedExpressions;
            private readonly IQueryExpressionAuthorizer authorizer;
            private readonly IQueryExpressionExpander expander;
            private readonly IQueryExpressionProcessor processor;
            private readonly IQueryExpressionSourcer sourcer;

            public QueryExpressionVisitor(QueryContext context,
                IQueryExpressionSourcer sourcer,
                IQueryExpressionAuthorizer authorizer = null,
                IQueryExpressionExpander expander = null,
                IQueryExpressionProcessor processor = null)
            {
                Ensure.NotNull(sourcer, nameof(sourcer));

                this.context = new QueryExpressionContext(context);
                this.authorizer = authorizer;
                this.expander = expander;
                this.processor = processor;
                this.sourcer = sourcer;
                processedExpressions = new Dictionary<Expression, Expression>();
            }

            public IQueryable BaseQuery { get; private set; }

            public IEdmEntitySet EntitySet { get; private set; }

            public override Expression Visit(Expression node)
            {
                if (node is null)
                {
                    return null;
                }

                // Initialize and push the visited node
                var visited = node;
                context.PushVisitedNode(visited);

                // If visited node has already been processed,
                // skip normalization, inspection and filtering
                // and simply replace with the processed node
                if (processedExpressions.ContainsKey(visited))
                {
                    node = processedExpressions[visited];
                }
                else
                {
                    // Only visit the visited node's children if
                    // the visited node represents API data
                    if (!(context.ModelReference is DataSourceStubModelReference))
                    {
                        // Visit visited node's children
                        node = base.Visit(visited);
                    }

                    // Inspect the visited node
                    Inspect();

                    // Try to expand the visited node
                    // if it represents API data
                    if (context.ModelReference is DataSourceStubModelReference)
                    {
                        node = Expand(visited);
                    }

                    // Process the visited node
                    node = Process(visited, node);
                }

                if (visited == node)
                {
                    if (context.ModelReference is DataSourceStubModelReference)
                    {
                        // If no processing occurred on the visited node
                        // and it represents API data, then it must be
                        // in its most primitive form, so source the node
                        node = Source(node);
                    }

                    if (BaseQuery is null)
                    {
                        // The very first time control reaches here, the
                        // visited node represents the original starting
                        // point for the entire composed query, and thus
                        // it should produce a non-embedded expression.
                        if (!(node is ConstantExpression constant))
                        {
                            throw new NotSupportedException(Resources.OriginalExpressionShouldBeConstant);
                        }

                        BaseQuery = constant.Value as IQueryable;
                        if (BaseQuery is null)
                        {
                            throw new NotSupportedException(Resources.OriginalExpressionShouldBeQueryable);
                        }

                        node = BaseQuery.Expression;
                    }
                }

                // TODO GitHubIssue#28 : Support transformation between API types and data source proxy types
                context.PopVisitedNode();

                if (context.VisitedNode is not null)
                {
                    EntitySet = context.ModelReference?.EntitySet;
                }

                return node;
            }

            private void Inspect()
            {
                if (authorizer is not null && !authorizer.Authorize(context))
                {
                    throw new SecurityException("The current user does not have permission to query from the requested resource.");
                }
            }

            private Expression Expand(Expression visited)
            {
                if (expander is null)
                {
                    return visited;
                }

                var expanded = expander.Expand(context);
                var callback = context.AfterNestedVisitCallback;
                context.AfterNestedVisitCallback = null;
                if (expanded is not null && expanded != visited)
                {
                    if (!visited.Type.IsAssignableFrom(expanded.Type))
                    {
                        throw new InvalidOperationException(Resources.ExpanderCannotChangeExpressionType);
                    }

                    context.PushVisitedNode(null);
                    expanded = Visit(expanded);
                    context.PopVisitedNode();
                    if (callback is not null)
                    {
                        callback();
                    }

                    return expanded;
                }

                return visited;
            }

            private Expression Process(Expression visited, Expression processed)
            {
                if (processor is not null)
                {
                    var filtered = processor.Process(context);
                    var callback = context.AfterNestedVisitCallback;
                    context.AfterNestedVisitCallback = null;
                    if (filtered is not null && filtered != visited)
                    {
                        if (!visited.Type.IsAssignableFrom(filtered.Type))
                        {
                            // In order to filter on the navigation properties,
                            // the type is changed from ICollection<> to IQueryable<>
                            var collectionType = visited.Type.FindGenericType(typeof(ICollection<>));
                            var queryableType = filtered.Type.FindGenericType(typeof(IQueryable<>));
                            if (collectionType is null || queryableType is null)
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

                        processedExpressions.Add(visited, processed);
                        context.PushVisitedNode(null);
                        try
                        {
                            processed = Visit(filtered);
                        }
                        finally
                        {
                            context.PopVisitedNode();
                            processedExpressions.Remove(visited);
                        }

                        if (callback is not null)
                        {
                            callback();
                        }
                    }
                }

                return processed;
            }

            private Expression Source(Expression node)
            {
                node = sourcer.ReplaceQueryableSource(context, BaseQuery is not null);
                if (node is null)
                {
                    // Missing source expression
                    throw new NotSupportedException(Resources.SourceExpressionMissing);
                }

                return node;
            }
        }
    }
}
