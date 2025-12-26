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
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Restier.Tests.Core.Query
{

    /// <summary>
    /// Unit tests for the <see cref="DefaultQueryExecutor"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class DefaultQueryExecutorTests
    {
        private readonly DefaultQueryExecutor testClass;
        private readonly ServiceProviderMock serviceProviderFixture;
        private readonly IQueryable<Test> queryable = new List<Test>()
        {
            new Test() { Name = "The" },
            new Test() { Name = "Quick" },
            new Test() { Name = "Brown" },
            new Test() { Name = "Fox" },
        }.AsQueryable();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultQueryExecutorTests"/> class.
        /// </summary>
        public DefaultQueryExecutorTests()
        {
            serviceProviderFixture = new ServiceProviderMock();
            testClass = new DefaultQueryExecutor();
        }

        /// <summary>
        /// Tests that a new instance can be constructed.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new DefaultQueryExecutor();
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can call ExecuteQueryAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallExecuteQueryAsync()
        {
            var context = new QueryContext(
                new TestApi(serviceProviderFixture.ServiceProvider.Object),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))));
            var cancellationToken = CancellationToken.None;
            var result = await testClass.ExecuteQueryAsync(
                context,
                queryable,
                cancellationToken);
            result.Should().NotBeNull();
            result.Results.Should().BeEquivalentTo(queryable);
        }

        /// <summary>
        /// Cannot call ExecuteQueryAsync with a null context.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallExecuteQueryAsyncWithNullContext()
        {
            Func<Task> act = () =>
                testClass.ExecuteQueryAsync(
                    default(QueryContext),
                    queryable,
                    CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call ExecuteQueryAsync with a null context.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallExecuteQueryAsyncWithNullQuery()
        {
            var context = new QueryContext(
                new TestApi(serviceProviderFixture.ServiceProvider.Object),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))));
            Func<Task> act = () => testClass.ExecuteQueryAsync(
                    context,
                    default(IQueryable<Test>),
                    CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Can call ExecuteExpressionAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallExecuteExpressionAsync()
        {
            var context = new QueryContext(
                new TestApi(serviceProviderFixture.ServiceProvider.Object),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))));
            var queryProviderMock = new Mock<IQueryProvider>();
            queryProviderMock
                .Setup(x => x.Execute(It.IsAny<Expression>()))
                .Returns<Expression>(ex => Expression.Lambda<Func<IQueryable<Test>>>(ex).Compile()());
            var expression = Expression.Constant(queryable);
            var cancellationToken = CancellationToken.None;
            var result = await testClass.ExecuteExpressionAsync<Test>(
                context,
                queryProviderMock.Object,
                expression,
                cancellationToken);
            result.Should().NotBeNull();
            ((IEnumerable<object>)result.Results).First().Should().Be(queryable);
        }

        /// <summary>
        /// Cannot call ExpressionAsync with a null query provider.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallExecuteExpressionAsyncWithNullQueryProvider()
        {
            var context = new QueryContext(
                new TestApi(serviceProviderFixture.ServiceProvider.Object),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))));
            var expression = Expression.Constant(queryable);

            Func<Task> act = () =>
                testClass.ExecuteExpressionAsync<Test>(
                    context,
                    default(IQueryProvider),
                    expression,
                    CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call ExecuteExpressionAsync with a null expression.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallExecuteExpressionAsyncWithNullExpression()
        {
            var context = new QueryContext(
                new TestApi(serviceProviderFixture.ServiceProvider.Object),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))));
            var queryProviderMock = new Mock<IQueryProvider>();
            queryProviderMock
                .Setup(x => x.Execute(It.IsAny<Expression>()))
                .Returns<Expression>(ex => Expression.Lambda<Func<IQueryable<Test>>>(ex).Compile()());
            Func<Task> act = () =>
                testClass.ExecuteExpressionAsync<Test>(
                    context,
                    queryProviderMock.Object,
                    default(Expression),
                    CancellationToken.None);
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