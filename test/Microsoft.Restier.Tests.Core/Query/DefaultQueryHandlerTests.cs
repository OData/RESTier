// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.Core.Query
{
    /// <summary>
    /// Unit tests for the <see cref="DefaultQueryHandler"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class DefaultQueryHandlerTests
    {
        private readonly IChainOfResponsibilityFactory<IQueryExpressionSourcer> sourcerFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExpressionSourcer>>();
        private readonly IChainOfResponsibilityFactory<IQueryExecutor> executorFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExecutor>>();
        private readonly IChainOfResponsibilityFactory<IModelMapper> modelMapperFactory = Substitute.For< IChainOfResponsibilityFactory<IModelMapper>>();
        private readonly IQueryExpressionAuthorizer authorizer = Substitute.For<IQueryExpressionAuthorizer>();
        private readonly IChainOfResponsibilityFactory<IQueryExpressionAuthorizer> authorizerFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExpressionAuthorizer>>();
        private readonly IQueryExpressionExpander expander = Substitute.For<IQueryExpressionExpander>();
        private readonly IChainOfResponsibilityFactory<IQueryExpressionExpander> expanderFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExpressionExpander>>();
        private readonly IChainOfResponsibilityFactory<IQueryExpressionProcessor> processorFactory = Substitute.For<IChainOfResponsibilityFactory<IQueryExpressionProcessor>>();

        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;

        private readonly IQueryable<Test> queryable = new List<Test>()
        {
            new Test() { Name = "The" },
            new Test() { Name = "Quick" },
            new Test() { Name = "Brown" },
            new Test() { Name = "Fox" },
        }.AsQueryable();

        private IQueryExecutor executor = Substitute.For<IQueryExecutor>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultQueryHandlerTests"/> class.
        /// </summary>
        public DefaultQueryHandlerTests()
        {
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();
            authorizerFactory.Create().Returns(authorizer);
            authorizer.Authorize(Arg.Any<QueryExpressionContext>()).Returns(true);
            sourcerFactory.Create().Returns(Substitute.For<IQueryExpressionSourcer>());
            executorFactory.Create().Returns(executor);
            expanderFactory.Create().Returns(expander);
            modelMapperFactory.Create().Returns(Substitute.For<IModelMapper>());
        }

        /// <summary>
        /// Can construct instance of the <see cref="DefaultQueryHandler"/> class.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new DefaultQueryHandler(
                sourcerFactory,
                executorFactory,
                modelMapperFactory,
                authorizerFactory,
                expanderFactory,
                processorFactory);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with a null sourcer.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullSourcer()
        {
            sourcerFactory.Create().Returns(default(IQueryExpressionSourcer));
            Action act = () => new DefaultQueryHandler(
                sourcerFactory,
                executorFactory,
                modelMapperFactory,
                authorizerFactory,
                expanderFactory,
                processorFactory);
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Cannot construct with a null executor.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullExecutor()
        {
            executorFactory.Create().Returns(default(IQueryExecutor));
            Action act = () => new DefaultQueryHandler(
                sourcerFactory,
                executorFactory,
                modelMapperFactory,
                authorizerFactory,
                expanderFactory,
                processorFactory);
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Cannot construct with a null model mapper.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullModelMapper()
        {
            modelMapperFactory.Create().Returns(default(IModelMapper));
            Action act = () => new DefaultQueryHandler(
                sourcerFactory,
                executorFactory,
                modelMapperFactory,
                authorizerFactory,
                expanderFactory,
                processorFactory);
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Can call QueryAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallQueryAsync()
        {
            var instance = new DefaultQueryHandler(
                sourcerFactory,
                executorFactory,
                modelMapperFactory,
                authorizerFactory,
                expanderFactory,
                processorFactory);

            var model = Substitute.For<IEdmModel>();
            var entityContainer = Substitute.For<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItem = Substitute.For<IEdmEntityContainerElement>();
            entityContainerElementItem.Name.Returns("Tests");
            list.Add(entityContainerElementItem);

            model.EntityContainer.Returns(entityContainer);
            entityContainer.Elements.Returns(list);

            executor
                .ExecuteQueryAsync<Test>(
                    Arg.Any<QueryContext>(),
                    Arg.Any<IQueryable<Test>>(),
                    Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var queryable = callInfo.ArgAt<IQueryable<Test>>(1);
                    return Task.FromResult(new QueryResult(queryable.ToList()));
                });

            var queryContext = new QueryContext(
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))))
            {
                Model = model,
            };

            var cancellationToken = CancellationToken.None;
            var result = await instance.QueryAsync(queryContext, cancellationToken);
            result.Results.Should().BeEquivalentTo(queryable);
        }

        /// <summary>
        /// Can call QueryAsync with count option.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallQueryAsyncWithCount()
        {
            var instance = new DefaultQueryHandler(
                sourcerFactory,
                executorFactory,
                modelMapperFactory,
                authorizerFactory,
                expanderFactory,
                processorFactory);

            var model = Substitute.For<IEdmModel>();
            var entityContainer = Substitute.For<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItem = Substitute.For<IEdmEntityContainerElement>();
            entityContainerElementItem.Name.Returns("Tests");
            list.Add(entityContainerElementItem);

            model.EntityContainer.Returns(entityContainer);
            entityContainer.Elements.Returns(list);

            executor.ExecuteExpressionAsync<long>(
                Arg.Any<QueryContext>(),
                Arg.Any<IQueryProvider>(),
                Arg.Any<Expression>(),
                Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var expression = callInfo.ArgAt<Expression>(2);
                    return Task.FromResult(new QueryResult(new[] { Expression.Lambda<Func<long>>(expression, null).Compile()() }));
                });

            var queryContext = new QueryContext(
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable)))
                {
                    ShouldReturnCount = true,
                })
            {
                Model = model,
            };

            var cancellationToken = CancellationToken.None;
            var result = await instance.QueryAsync(queryContext, cancellationToken);
            result.Results.Should().BeEquivalentTo(new[] { queryable.LongCount() });
        }

        // TODO: More tests.

        /// <summary>
        /// Cannot call QueryAsync with a null context.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallQueryAsyncWithNullContext()
        {
            var instance = new DefaultQueryHandler(
                sourcerFactory,
                executorFactory,
                modelMapperFactory,
                authorizerFactory,
                expanderFactory,
                processorFactory);

            Func<Task> act = () => instance.QueryAsync(default(QueryContext), CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        private class TestApi : ApiBase
        {
            public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class Test
        {
            public string Name { get; set; }
        }
    }
}