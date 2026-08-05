// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
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
    /// Unit tests for the <see cref="DefaultQueryExecutor"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class DefaultQueryExecutorTests
    {
        private readonly DefaultQueryExecutor testClass;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultQueryExecutorTests"/> class.
        /// </summary>
        public DefaultQueryExecutorTests()
        {
            testClass = new DefaultQueryExecutor();
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();
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
                new TestApi(model, queryHandler, submitHandler),
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
        /// Verifies that ExecuteQueryAsync returns the IQueryable without materializing it.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ExecuteQueryAsync_ReturnsDeferredQueryable()
        {
            var context = new QueryContext(
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))));

            var result = await testClass.ExecuteQueryAsync(
                context,
                queryable,
                CancellationToken.None);

            result.Results.Should().BeSameAs(queryable);
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
                new TestApi(model, queryHandler, submitHandler),
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
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))));

            var queryProvider = Substitute.For<IQueryProvider>();
            queryProvider.Execute(Arg.Any<Expression>())
                .Returns(callInfo => Expression.Lambda<Func<IQueryable<Test>>>(callInfo.Arg<Expression>()).Compile()());

            var expression = Expression.Constant(queryable);
            var cancellationToken = CancellationToken.None;

            var result = await testClass.ExecuteExpressionAsync<Test>(
                context,
                queryProvider,
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
                new TestApi(model, queryHandler, submitHandler),
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
                new TestApi(model, queryHandler, submitHandler),
                new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))));

            var queryProvider = Substitute.For<IQueryProvider>();
            queryProvider.Execute(Arg.Any<Expression>())
                .Returns(callInfo => Expression.Lambda<Func<IQueryable<Test>>>(callInfo.Arg<Expression>()).Compile()());

            Func<Task> act = () =>
                testClass.ExecuteExpressionAsync<Test>(
                    context,
                    queryProvider,
                    default(Expression),
                    CancellationToken.None);

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