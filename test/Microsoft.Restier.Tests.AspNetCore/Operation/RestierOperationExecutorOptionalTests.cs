// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
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

/// <summary>
/// Unit tests for the omitted-optional parameter path in <see cref="RestierOperationExecutor"/>.
/// The explicit-null path (Present = true, Value = null) requires a real HttpRequest for
/// ConvertValue and is covered instead by HTTP-level integration tests (Task 12).
/// </summary>
public class RestierOperationExecutorOptionalTests
{
    private readonly OptionalParamsApi _api = new();

    private RestierOperationExecutor CreateExecutor()
    {
        var authorizerFactory = Substitute.For<IChainOfResponsibilityFactory<IOperationAuthorizer>>();
        authorizerFactory.Create().Returns((IOperationAuthorizer)null);
        var filterFactory = Substitute.For<IChainOfResponsibilityFactory<IOperationFilter>>();
        filterFactory.Create().Returns((IOperationFilter)null);
        return new RestierOperationExecutor(authorizerFactory, filterFactory, new KeylessViewRegistry());
    }

    [Fact]
    public async Task OmittedCompilerDefault_PassesDeclaredDefault()
    {
        var executor = CreateExecutor();
        var ctx = BuildContext(
            api: _api,
            operationName: nameof(OptionalParamsApi.IntWithDefault),
            isFunction: true,
            delegateImpl: _ => (false, null));

        await executor.ExecuteOperationAsync(ctx, CancellationToken.None);

        _api.LastReceived.Should().Be(5);
    }

    [Fact]
    public async Task OmittedDefaultValueAttribute_PassesAttributeValue()
    {
        var executor = CreateExecutor();
        var ctx = BuildContext(
            api: _api,
            operationName: nameof(OptionalParamsApi.StringWithDefaultAttr),
            isFunction: true,
            delegateImpl: _ => (false, null));

        await executor.ExecuteOperationAsync(ctx, CancellationToken.None);

        _api.LastReceived.Should().Be("hello");
    }

    [Fact]
    public async Task OmittedNullableWithDefault_SubstitutesDefault()
    {
        var executor = CreateExecutor();
        var ctx = BuildContext(
            api: _api,
            operationName: nameof(OptionalParamsApi.NullableIntWithDefault),
            isFunction: true,
            delegateImpl: _ => (false, null));

        await executor.ExecuteOperationAsync(ctx, CancellationToken.None);

        _api.LastReceived.Should().Be(5);
    }

    private static RestierOperationContext BuildContext(
        ApiBase api,
        string operationName,
        bool isFunction,
        Func<string, (bool Present, object Value)> delegateImpl)
        => new(
            api,
            delegateImpl,
            operationName,
            isFunction,
            bindingParameterValue: null);

    public class OptionalParamsApi : ApiBase
    {
        public object LastReceived { get; private set; }

        public OptionalParamsApi()
            : base(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>())
        {
        }

        public int IntWithDefault(int p = 5)
        {
            LastReceived = p;
            return p;
        }

        public string StringWithDefaultAttr([System.ComponentModel.DefaultValue("hello")] string p)
        {
            LastReceived = p;
            return p;
        }

        // Used by Task 12 HTTP integration tests covering explicit-null parameter binding.
        public int? NullableInt(int? p)
        {
            LastReceived = p;
            return p;
        }

        public int? NullableIntWithDefault(int? p = 5)
        {
            LastReceived = p;
            return p;
        }
    }
}
