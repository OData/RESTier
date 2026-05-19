// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

// Regression tests for the SelectExpandHelper in-memory projection on EF6: when a
// navigation property is null on a parent row (the Library seed adds "Sea of Rust"
// directly to Books with no Publisher), the OData-generated projection lambda used
// to NRE on any nested member access against the null nav. The helper now runs the
// lambda through a null-safe rewriter before compiling.
[Collection("LibraryApiEF6")]
public class OrphanExpandRepro : RestierTestBase<LibraryApi>
{
    private static Action<IServiceCollection> ConfigureServices =>
        services => services.AddEntityFrameworkServices<LibraryContext>();

    [Fact]
    public async Task Books_ExpandPublisher_OrphanSerializesWithNullPublisher()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books?$expand=Publisher",
            serviceCollection: ConfigureServices);
        var body = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
        body.Should().Contain("Sea of Rust", because: "the orphan row should still serialize");
        body.Should().Contain("\"Publisher\":null", because: "the orphan has no publisher; the expand slot should be null");
    }

    [Fact]
    public async Task Books_NestedExpandPublisherBooks_OrphanSerializesWithoutNRE()
    {
        // The nested case is the one that NRE'd: book.Publisher.Books dereferences a null Publisher
        // when the projection lambda is compiled and executed in-memory.
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books?$expand=Publisher($expand=Books)",
            serviceCollection: ConfigureServices);
        var body = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
        body.Should().Contain("Sea of Rust", because: "the orphan should still appear in the response");
    }

    [Fact]
    public async Task Books_FilterToOrphanOnly_ExpandPublisher_DoesNotNRE()
    {
        // Reduce to just the orphan to make sure null-nav handling is the focus.
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books?$filter=PublisherId eq null&$expand=Publisher",
            serviceCollection: ConfigureServices);
        var body = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK, because: body);
    }
}
