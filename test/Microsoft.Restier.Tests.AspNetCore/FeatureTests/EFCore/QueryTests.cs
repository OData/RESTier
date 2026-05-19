// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore;

[Collection("LibraryApiEFCore")]
public class QueryTests : QueryTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();

    [Fact]
    public async Task NullNavigationPropertyOnExistingEntityReturns204()
    {
        // Create an isolated book with no Publisher so concurrent TFM runs can't interfere.
        var bookId = Guid.NewGuid();
        var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(
            serviceCollection: ConfigureServices);
        context.Books.Add(new Book
        {
            Id = bookId,
            Isbn = "9999999999999",
            Title = "Isolated Test Book",
            IsActive = true,
        });
        context.SaveChanges();

        try
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
                HttpMethod.Get,
                resource: $"/Books({bookId})/Publisher",
                serviceCollection: ConfigureServices);
            _ = await TraceListener.LogAndReturnMessageContentAsync(response);

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
        finally
        {
            var book = context.Books.FirstOrDefault(b => b.Id == bookId);
            if (book is not null)
            {
                context.Books.Remove(book);
                context.SaveChanges();
            }
        }
    }

    [Fact]
    public async Task CollectionNavFromMissingParentReturns200ByDefault()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books(00000000-0000-0000-0000-000000000000)/Reviews",
            serviceCollection: ConfigureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CollectionNavFromMissingParentReturns404WhenStrict()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books(00000000-0000-0000-0000-000000000000)/Reviews",
            serviceCollection: ConfigureServices,
            configureOptions: options => options.Conformance.StrictMissingParentForCollections = true);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CollectionNavFromExistingParentReturns200WhenStrict()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Publishers('Publisher1')/Books",
            serviceCollection: ConfigureServices,
            configureOptions: options => options.Conformance.StrictMissingParentForCollections = true);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CollectionNavCountFromMissingParentReturns200ByDefault()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books(00000000-0000-0000-0000-000000000000)/Reviews/$count",
            serviceCollection: ConfigureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CollectionNavCountFromMissingParentReturns404WhenStrict()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/Books(00000000-0000-0000-0000-000000000000)/Reviews/$count",
            serviceCollection: ConfigureServices,
            configureOptions: options => options.Conformance.StrictMissingParentForCollections = true);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
