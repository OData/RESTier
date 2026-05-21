// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Operation;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Operation;

public class RestierOperationExecutorTests
{
    private readonly IOperationAuthorizer _authorizer = Substitute.For<IOperationAuthorizer>();
    private readonly IOperationFilter _filter = Substitute.For<IOperationFilter>();

    private RestierOperationExecutor CreateExecutor(
        IOperationAuthorizer authorizer = null,
        IOperationFilter filter = null)
    {
        var authorizerFactory = Substitute.For<IChainOfResponsibilityFactory<IOperationAuthorizer>>();
        authorizerFactory.Create().Returns(authorizer ?? _authorizer);
        var filterFactory = Substitute.For<IChainOfResponsibilityFactory<IOperationFilter>>();
        filterFactory.Create().Returns(filter ?? _filter);
        return new RestierOperationExecutor(authorizerFactory, filterFactory);
    }

    [Fact]
    public void Constructor_Should_Set_Dependencies()
    {
        var executor = CreateExecutor();
        executor.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteOperationAsync_Should_Throw_If_Context_Is_Not_RestierOperationContext()
    {
        var api = new DummyApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());
        var executor = CreateExecutor();
        var context = Substitute.For<OperationContext>(api, new Func<string, object>(_ => null), "Test", true, null);
        Func<Task> act = async () => await executor.ExecuteOperationAsync(context, CancellationToken.None);
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task ExecuteOperationAsync_Should_Throw_If_Method_Not_Found()
    {
        var api = new DummyApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());
        var context = Substitute.For<RestierOperationContext>(api, new Func<string, object>(_ => null), "NonExistentMethod", true, null);
        var authorizer = Substitute.For<IOperationAuthorizer>();
        authorizer.AuthorizeAsync(Arg.Any<OperationContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        var executor = CreateExecutor(authorizer, null);

        Func<Task> act = async () => await executor.ExecuteOperationAsync(context, CancellationToken.None);
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task ExecuteOperationAsync_Should_Throw_If_Not_Authorized()
    {
        var api = new DummyApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());
        var method = typeof(DummyApi).GetMethod(nameof(DummyApi.TestMethod));
        var context = new RestierOperationContext(
                    new DummyApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>()), _ => null, nameof(DummyApi.TestMethod), true, null);

        var authorizer = Substitute.For<IOperationAuthorizer>();
        authorizer.AuthorizeAsync(Arg.Any<OperationContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        var executor = CreateExecutor(authorizer, _filter);

        Func<Task> act = async () => await executor.ExecuteOperationAsync(context, CancellationToken.None);
        await act.Should().ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task ExecuteOperationAsync_Should_Invoke_Filters()
    {
        var api = new DummyApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());
        var context = new RestierOperationContext(
            api, _ => null, nameof(DummyApi.TestMethod), true, null);

        _authorizer.AuthorizeAsync(Arg.Any<OperationContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var executor = CreateExecutor(_authorizer, _filter);

        await executor.ExecuteOperationAsync(context, CancellationToken.None);

        await _filter.Received(1).OnOperationExecutingAsync(context, Arg.Any<CancellationToken>());
        await _filter.Received(1).OnOperationExecutedAsync(context, Arg.Any<CancellationToken>());
    }

    // TestApi for testing reflection
    public class DummyApi : ApiBase
    {
        public DummyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }
        public int TestMethod() => 1;
    }
}
