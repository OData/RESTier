// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Query;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.Core;

namespace Microsoft.Restier.Tests.AspNetCore.Query;

[TestClass]
public class RestierQueryExecutorTests
{
    [TestMethod]
    public async Task ExecuteQueryAsync_WhenIncludeTotalCountIsSet_DelegatesToInnerAndSetsTotalCount()
    {
        // Arrange
        var inner = Substitute.For<IQueryExecutor>();
        var executor = new RestierQueryExecutor { Inner = inner };
        var setTotalCountCalled = false;
        long? totalCountValue = null;

        var queryRequest = new QueryRequest(new TestQueryableSource())
        {
            IncludeTotalCount = true,
            SetTotalCount = count =>
            {
                setTotalCountCalled = true;
                totalCountValue = count;
            }
        };

        var context = new QueryContext(
            new TestApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>()),
            queryRequest);

        var query = new[] { new object(), new object(), new object() }.AsQueryable();
        var cancellationToken = new CancellationToken();
        var expectedCountResult = new QueryResult(new long[] { 3 }.AsQueryable());

        // Simulate the inner executor returning the expected result
        inner.ExecuteExpressionAsync<long>(Arg.Any<QueryContext>(), Arg.Any<IQueryProvider>(), Arg.Any<Expression>(), Arg.Any<CancellationToken>())
            .Returns(expectedCountResult);

        var expectedResult = new QueryResult(query);
        inner.ExecuteQueryAsync(context, query, cancellationToken)
            .Returns(Task.FromResult(expectedResult));

        // Act
        var result = await executor.ExecuteQueryAsync(context, query, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);
        await inner.Received(1).ExecuteQueryAsync(context, query, cancellationToken);

        // Since the counting logic is not implemented, SetTotalCount should not be called
        setTotalCountCalled.Should().BeTrue();
        totalCountValue.Should().Be(3);
    }

    [TestMethod]
    public async Task ExecuteExpressionAsync_DelegatesToInner()
    {
        // Arrange
        var inner = Substitute.For<IQueryExecutor>();
        var executor = new RestierQueryExecutor { Inner = inner };
        var context = new QueryContext(new TestApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>()), new QueryRequest(new TestQueryableSource()));
        var provider = Substitute.For<IQueryProvider>();
        var expression = Expression.Constant(42);
        var cancellationToken = new CancellationToken();
        var expectedResult = new QueryResult(new[] { 42 });

        inner.ExecuteExpressionAsync<int>(context, provider, expression, cancellationToken)
            .Returns(Task.FromResult(expectedResult));

        // Act
        var result = await executor.ExecuteExpressionAsync<int>(context, provider, expression, cancellationToken);

        // Assert
        result.Should().BeSameAs(expectedResult);
        await inner.Received(1).ExecuteExpressionAsync<int>(context, provider, expression, cancellationToken);
    }

    [TestMethod]
    public void Inner_CanBeSetAndGet()
    {
        // Arrange
        var inner = Substitute.For<IQueryExecutor>();
        var executor = new RestierQueryExecutor();

        // Act
        executor.Inner = inner;

        // Assert
        executor.Inner.Should().BeSameAs(inner);
    }

    // TestApi
    public class TestApi : ApiBase
    {
        public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }
    }

    internal class TestQueryableSource : QueryableSource
    {
        public TestQueryableSource() : base(new object[] { new object(), new object(), new object() }.AsQueryable().Expression)
        {
        }

        public override Type ElementType => typeof(int);
    }
}
