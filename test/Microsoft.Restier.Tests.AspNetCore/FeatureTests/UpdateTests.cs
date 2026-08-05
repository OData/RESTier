// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class UpdateTests<TApi, TContext> : RestierTestBase<TApi> where TApi : ApiBase where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    protected abstract Task Cleanup(Guid bookId, string title);

    [TestMethod]
    public async Task UpdateBookWithPublisher_IgnoresNavigationProperty()
    {
        // Filter to a book with a publisher so the result is deterministic regardless of residual
        // state from sibling tests in the shared Library DB (books inserted with null FKs may sort
        // ahead of the seeded books).
        var bookRequest = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$expand=Publisher&$filter=PublisherId ne null&$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        bookRequest.IsSuccessStatusCode.Should().BeTrue();

        var (bookList, _) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();
        bookList.Should().NotBeNull();
        bookList.Items.Should().NotBeNullOrEmpty();

        var book = bookList.Items.First();
        book.Should().NotBeNull();
        book.Publisher.Should().NotBeNull();
        var originalTitle = book.Title;
        book.Title += " Test";

        // Navigation properties in the payload are silently ignored (not rejected).
        // This enables @odata.bind links to work and prevents embedded entities from causing errors.
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Put,
            resource: $"/Books({book.Id})",
            payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        response.IsSuccessStatusCode.Should().BeTrue();

        await Cleanup(book.Id, originalTitle);
    }

    [TestMethod]
    public async Task UpdateBook()
    {
        var bookRequest = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        bookRequest.IsSuccessStatusCode.Should().BeTrue();

        var (bookList, _) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();
        var book = bookList.Items.First();
        var originalTitle = book.Title;
        book.Title += " Test";

        var updateResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Put,
            resource: $"/Books({book.Id})",
            payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        updateResponse.IsSuccessStatusCode.Should().BeTrue();

        var checkResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({book.Id})",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        checkResponse.IsSuccessStatusCode.Should().BeTrue();

        var (updatedBook, _) = await checkResponse.DeserializeResponseAsync<Book>();
        updatedBook.Should().NotBeNull();
        updatedBook.Title.Should().Be($"{originalTitle} Test");

        await Cleanup(book.Id, originalTitle);
    }

    [TestMethod]
    public async Task PatchBook()
    {
        var bookRequest = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        bookRequest.IsSuccessStatusCode.Should().BeTrue();

        var (bookList, _) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();
        var book = bookList.Items.First();
        var originalTitle = book.Title;

        var payload = new
        {
            Title = $"{book.Title} | Patch Test",
        };

        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"),
            resource: $"/Books({book.Id})",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        patchResponse.IsSuccessStatusCode.Should().BeTrue();

        var checkResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({book.Id})",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        checkResponse.IsSuccessStatusCode.Should().BeTrue();

        var (updatedBook, _) = await checkResponse.DeserializeResponseAsync<Book>();
        updatedBook.Should().NotBeNull();
        updatedBook.Title.Should().Be($"{originalTitle} | Patch Test");

        await Cleanup(book.Id, originalTitle);
    }

    [TestMethod]
    public async Task UpdatePublisher_ShouldCallInterceptor()
    {
        var publisherRequest = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Publishers('Publisher1')",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        publisherRequest.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await publisherRequest.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();

        publisher.Books = null;
        publisher.LastUpdated = DateTimeOffset.MinValue;

        var updateResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Put,
            resource: $"/Publishers('{publisher.Id}')",
            payload: publisher,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(updateResponse);

        updateResponse.IsSuccessStatusCode.Should().BeTrue();

        var checkResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Publishers('Publisher1')",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);
        checkResponse.IsSuccessStatusCode.Should().BeTrue();

        var (updatedPublisher, _) = await checkResponse.DeserializeResponseAsync<Publisher>();
        updatedPublisher.Should().NotBeNull();
        updatedPublisher.LastUpdated.Should().BeCloseTo(DateTimeOffset.Now, new TimeSpan(0, 0, 0, 6));
    }
}
