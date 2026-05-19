// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
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
        LibraryWithViewsApi.OnFilteringBooksByPublisherCallCount = 0;

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
    public async Task Get_KeylessView_DoesNotInvokeOnFilteringConvention()
    {
        // v1 limitation pin: convention hooks do NOT fire on keyless-view function imports.
        // When the convention-processor follow-up lands, flip this test to assert the call count > 0.
        LibraryWithViewsApi.OnFilteringBooksByPublisherCallCount = 0;

        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            HttpMethod.Get,
            resource: "/BooksByPublisher()?$filter=PublisherId eq 'Publisher1'",
            serviceCollection: ConfigureServices);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LibraryWithViewsApi.OnFilteringBooksByPublisherCallCount.Should().Be(0,
            because: "v1 does not invoke OnFiltering<View> for keyless-view function imports; see Follow-up A");
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
}
