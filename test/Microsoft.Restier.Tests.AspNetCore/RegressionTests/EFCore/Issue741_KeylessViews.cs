// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests.EFCore;

public class Issue741_KeylessViews
{
    private static Action<IServiceCollection> ConfigureServices => services =>
        services.AddEntityFrameworkServices<LibraryContext>();

    [Fact]
    public async Task Get_KeylessView_Returns200WithRows()
    {
        LibraryWithViewsApi.OnFilterBooksByPublisherCallCount = 0;

        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            HttpMethod.Get,
            resource: "/BooksByPublisher()",
            serviceCollection: ConfigureServices);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("\"value\"");
    }

    [Fact]
    public async Task Get_KeylessView_WithFilter_AppliesFilter()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            HttpMethod.Get,
            resource: "/BooksByPublisher()?$filter=PublisherId eq 'Publisher1'",
            serviceCollection: ConfigureServices);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("\"PublisherId\":\"Publisher1\"");
        body.Should().NotContain("\"PublisherId\":\"Publisher2\"");
    }

    [Fact]
    public async Task Get_KeylessView_InvokesOnFilterConvention()
    {
        LibraryWithViewsApi.OnFilterBooksByPublisherCallCount = 0;

        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            HttpMethod.Get,
            resource: "/BooksByPublisher()",
            serviceCollection: ConfigureServices);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LibraryWithViewsApi.OnFilterBooksByPublisherCallCount.Should().BeGreaterThan(0,
            because: "Follow-up A routes keyless-view function imports through the query pipeline so OnFilter<View> fires");
    }

    [Fact]
    public async Task Get_KeylessView_OnFilterFilterReachesResponse()
    {
        LibraryWithViewsApi.OnFilterBooksByPublisherCallCount = 0;

        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            HttpMethod.Get,
            resource: "/BooksByPublisher()",
            serviceCollection: ConfigureServices);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().NotContain("\"PublisherId\":\"Publisher3\"",
            because: "OnFilterBooksByPublisher filters out Publisher3 rows; the convention now fires through the query pipeline");
    }

    [Fact]
    public async Task Get_KeylessView_QueryAuthorizerFires()
    {
        CountingQueryExpressionAuthorizer.InvocationCount = 0;

        Action<IServiceCollection> configure = services =>
        {
            ConfigureServices(services);
            services.AddSingleton<IChainedService<IQueryExpressionAuthorizer>, CountingQueryExpressionAuthorizer>();
        };

        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            HttpMethod.Get,
            resource: "/BooksByPublisher()",
            serviceCollection: configure);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CountingQueryExpressionAuthorizer.InvocationCount.Should().BeGreaterThan(0,
            because: "Follow-up A routes keyless-view function imports through DefaultQueryHandler.QueryAsync so IQueryExpressionAuthorizer.Authorize fires");
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Write_KeylessView_Returns405(string verb)
    {
        // No payload — the 405 guard in RestierController fires on the function-import
        // segment before any body parsing happens. Passing a non-null payload trips up the
        // Breakdance helper's StringContent ctor (it reuses the OData Accept header as the
        // Content-Type media type, which the framework rejects).
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            new HttpMethod(verb),
            resource: "/BooksByPublisher()",
            serviceCollection: ConfigureServices);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    /// <summary>
    /// Counting <see cref="IQueryExpressionAuthorizer"/> probe used by
    /// <see cref="Get_KeylessView_QueryAuthorizerFires"/>. Implements
    /// <see cref="IChainedService{TService}"/> via the base interface declaration on
    /// <see cref="IQueryExpressionAuthorizer"/> so a single registration suffices.
    /// </summary>
    public sealed class CountingQueryExpressionAuthorizer : IQueryExpressionAuthorizer
    {
        public static int InvocationCount;

        public IQueryExpressionAuthorizer Inner { get; set; }

        public bool Authorize(QueryExpressionContext context)
        {
            Interlocked.Increment(ref InvocationCount);
            return Inner?.Authorize(context) ?? true;
        }
    }
}
