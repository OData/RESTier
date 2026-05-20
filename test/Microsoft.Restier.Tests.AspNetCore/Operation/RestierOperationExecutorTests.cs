// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Operation;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
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
        IOperationFilter filter = null,
        KeylessViewRegistry keylessViewRegistry = null)
    {
        var authorizerFactory = Substitute.For<IChainOfResponsibilityFactory<IOperationAuthorizer>>();
        authorizerFactory.Create().Returns(authorizer ?? _authorizer);
        var filterFactory = Substitute.For<IChainOfResponsibilityFactory<IOperationFilter>>();
        filterFactory.Create().Returns(filter ?? _filter);
        return new RestierOperationExecutor(authorizerFactory, filterFactory, keylessViewRegistry ?? new KeylessViewRegistry());
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
        var context = Substitute.For<OperationContext>(api, new Func<string, (bool Present, object Value)>(_ => (false, null)), "Test", true, null);
        Func<Task> act = async () => await executor.ExecuteOperationAsync(context, CancellationToken.None);
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task ExecuteOperationAsync_Should_Throw_If_Method_Not_Found()
    {
        var api = new DummyApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());
        var context = Substitute.For<RestierOperationContext>(api, new Func<string, (bool Present, object Value)>(_ => (false, null)), "NonExistentMethod", true, null);
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
                    new DummyApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>()), _ => (false, null), nameof(DummyApi.TestMethod), true, null);

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
            api, _ => (false, null), nameof(DummyApi.TestMethod), true, null);

        _authorizer.AuthorizeAsync(Arg.Any<OperationContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var executor = CreateExecutor(_authorizer, _filter);

        await executor.ExecuteOperationAsync(context, CancellationToken.None);

        await _filter.Received(1).OnOperationExecutingAsync(context, Arg.Any<CancellationToken>());
        await _filter.Received(1).OnOperationExecutedAsync(context, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteOperationAsync_KeylessView_Invokes_Filters_With_NonNull_ParameterValues()
    {
        // Regression test: the keyless-view dispatch path must initialise
        // RestierOperationContext.ParameterValues to a non-null array before invoking the
        // operation-filter pipeline, matching the invariant the normal method path maintains.
        // Custom IOperationFilter implementations can then read context.ParameterValues without
        // null-guarding (the built-in ConventionBasedOperationFilter happens to null-guard, but
        // that's not a contract third-party filters can rely on).
        var api = new DummyApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());
        var context = new RestierOperationContext(
            api, _ => (false, null), "MyKeylessView", isFunction: true, bindingParameterValue: null);

        _authorizer.AuthorizeAsync(Arg.Any<OperationContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var registry = new KeylessViewRegistry();
        registry.Register("MyKeylessView", typeof(string), _ => Enumerable.Empty<string>().AsQueryable());

        // Capture ParameterValues seen by the filter at the moment OnOperationExecutingAsync runs.
        System.Collections.Generic.ICollection<object> capturedParameterValues = null;
        await _filter.OnOperationExecutingAsync(Arg.Do<OperationContext>(c =>
            capturedParameterValues = ((RestierOperationContext)c).ParameterValues), Arg.Any<CancellationToken>());

        var executor = CreateExecutor(_authorizer, _filter, registry);

        await executor.ExecuteOperationAsync(context, CancellationToken.None);

        await _filter.Received(1).OnOperationExecutingAsync(context, Arg.Any<CancellationToken>());
        await _filter.Received(1).OnOperationExecutedAsync(context, Arg.Any<CancellationToken>());

        capturedParameterValues.Should().NotBeNull(
            because: "the keyless-view dispatch path must initialise ParameterValues before the filter pipeline runs");
        capturedParameterValues.Should().BeEmpty(
            because: "keyless-view function imports have no parameters");
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
