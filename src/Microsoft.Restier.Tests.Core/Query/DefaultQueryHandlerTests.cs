// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Restier.Tests.Core.Query
{
    /// <summary>
    /// Unit tests for the <see cref="DefaultQueryHandler"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class DefaultQueryHandlerTests
    {
        private readonly ServiceProviderMock serviceProviderFixture;

        private readonly IQueryable<Test> queryable = new List<Test>()
        {
            new Test() { Name = "The" },
            new Test() { Name = "Quick" },
            new Test() { Name = "Brown" },
            new Test() { Name = "Fox" },
        }.AsQueryable();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultQueryHandlerTests"/> class.
        /// </summary>
        public DefaultQueryHandlerTests()
        {
            serviceProviderFixture = new ServiceProviderMock();
        }

        private IQueryExpressionSourcer Sourcer
            => serviceProviderFixture.ServiceProvider.Object.GetRequiredService<IQueryExpressionSourcer>();

        private IQueryExpressionAuthorizer Authorizer
            => serviceProviderFixture.ServiceProvider.Object.GetRequiredService<IQueryExpressionAuthorizer>();

        private IQueryExpressionExpander Expander
            => serviceProviderFixture.ServiceProvider.Object.GetRequiredService<IQueryExpressionExpander>();

        private IQueryExpressionProcessor Processor
            => serviceProviderFixture.ServiceProvider.Object.GetRequiredService<IQueryExpressionProcessor>();

        /// <summary>
        /// Can construct instance of the <see cref="DefaultQueryHandler"/> class.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new DefaultQueryHandler(
                Sourcer,
                Authorizer,
                Expander,
                Processor);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct with a null sourcer.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullSourcer()
        {
            Action act = () => new DefaultQueryHandler(
                default(IQueryExpressionSourcer),
                Authorizer,
                Expander,
                Processor);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call QueryAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallQueryAsync()
        {
            var instance = new DefaultQueryHandler(
                Sourcer,
                Authorizer,
                Expander,
                Processor);

            var modelMock = new Mock<IEdmModel>();
            var entityContainerMock = new Mock<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItemMock = new Mock<IEdmEntityContainerElement>();
            entityContainerElementItemMock.Setup(x => x.Name).Returns("Tests");
            list.Add(entityContainerElementItemMock.Object);

            modelMock.Setup(x => x.EntityContainer).Returns(entityContainerMock.Object);
            entityContainerMock.Setup(x => x.Elements).Returns(list);

            serviceProviderFixture.QueryExecutor.Setup(x => x.ExecuteQueryAsync<Test>(
                It.IsAny<QueryContext>(),
                It.IsAny<IQueryable<Test>>(),
                It.IsAny<CancellationToken>())).Returns<QueryContext, IQueryable<Test>, CancellationToken>((q, iq, c)
                    => Task.FromResult(new QueryResult(iq.ToList())));

            var queryContext = new QueryContext(
                new TestApi(serviceProviderFixture.ServiceProvider.Object),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))))
            {
                Model = modelMock.Object,
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
                Sourcer,
                Authorizer,
                Expander,
                Processor);

            var modelMock = new Mock<IEdmModel>();
            var entityContainerMock = new Mock<IEdmEntityContainer>();
            var list = new List<IEdmEntityContainerElement>();
            var entityContainerElementItemMock = new Mock<IEdmEntityContainerElement>();
            entityContainerElementItemMock.Setup(x => x.Name).Returns("Tests");
            list.Add(entityContainerElementItemMock.Object);

            modelMock.Setup(x => x.EntityContainer).Returns(entityContainerMock.Object);
            entityContainerMock.Setup(x => x.Elements).Returns(list);

            serviceProviderFixture.QueryExecutor.Setup(x => x.ExecuteExpressionAsync<long>(
                It.IsAny<QueryContext>(),
                It.IsAny<IQueryProvider>(),
                It.IsAny<Expression>(),
                It.IsAny<CancellationToken>())).Returns<QueryContext, IQueryProvider, Expression, CancellationToken>(
                    (q, qp, e, c) => Task.FromResult(new QueryResult(new[] { Expression.Lambda<Func<long>>(e, null).Compile()() })));

            var queryContext = new QueryContext(
                new TestApi(serviceProviderFixture.ServiceProvider.Object),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable)))
                {
                    ShouldReturnCount = true,
                })
            {
                Model = modelMock.Object,
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
               Sourcer,
               Authorizer,
               Expander,
               Processor);

            Func<Task> act = () => instance.QueryAsync(default(QueryContext), CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        private class TestApi : ApiBase
        {
            public TestApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }

        private class Test
        {
            public string Name { get; set; }
        }
    }
}