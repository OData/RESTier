﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Threading;
using CloudNimble.EasyAF.Http.OData;

#if NETCOREAPP3_1_OR_GREATER
using CloudNimble.Breakdance.AspNetCore;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else

using CloudNimble.Breakdance.WebApi;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

    [TestClass]
    public class UpdateTests : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <LibraryApi>
#endif
    {

        [TestMethod]
        public async Task UpdateBookWithPublisher_ShouldReturn400()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books?$expand=Publisher&$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, ErrorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeNullOrEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();
            book.Publisher.Should().NotBeNull();

            book.Title += " Test";

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Put, resource: $"/Books({book.Id})", payload: book, acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            bookEditRequest.IsSuccessStatusCode.Should().BeFalse();
            bookEditRequest.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task UpdateBook()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, ErrorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeNullOrEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();
            
            var originalBookTitle = book.Title;
            book.Title += " Test";

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Put, resource: $"/Books({book.Id})", payload: book, acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            bookEditRequest.IsSuccessStatusCode.Should().BeTrue();

            var bookCheckRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/Books({book.Id})", acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            bookCheckRequest.IsSuccessStatusCode.Should().BeTrue();
            var (book2, ErrorContent2) = await bookCheckRequest.DeserializeResponseAsync<Book>();
            book2.Should().NotBeNull();
            book2.Title.Should().Be($"{originalBookTitle} Test");

            await Cleanup(book.Id, originalBookTitle);
        }

        [TestMethod]
        public async Task PatchBook()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, ErrorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeNullOrEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();

            var originalBookTitle = book.Title;

            var payload = new {
                Title = $"{book.Title} | Patch Test"
            };

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(new HttpMethod("PATCH"), resource: $"/Books({book.Id})", payload: payload, acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            bookEditRequest.IsSuccessStatusCode.Should().BeTrue();

            var bookCheckRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/Books({book.Id})", acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            bookCheckRequest.IsSuccessStatusCode.Should().BeTrue();
            var (book2, ErrorContent2) = await bookCheckRequest.DeserializeResponseAsync<Book>();
            book2.Should().NotBeNull();
            book2.Title.Should().Be($"{originalBookTitle} | Patch Test");

            await Cleanup(book.Id, originalBookTitle);
        }

        [TestMethod]
        public async Task UpdatePublisher_ShouldCallInterceptor()
        {
            var publisherRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Publishers('Publisher1')", acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            publisherRequest.IsSuccessStatusCode.Should().BeTrue();
            var (publisher, ErrorContent) = await publisherRequest.DeserializeResponseAsync<Publisher>();

            publisher.Should().NotBeNull();
            publisher.LastUpdated.Should().NotBeCloseTo(DateTimeOffset.Now, new TimeSpan(0, 0, 0, 5));

            publisher.Books = null;
            var publisherEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Put, resource: $"/Publishers('{publisher.Id}')", payload: publisher, acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var result = await TestContext.LogAndReturnMessageContentAsync(publisherEditRequest);
            
            publisherEditRequest.IsSuccessStatusCode.Should().BeTrue();

            var publisherRequest2 = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Publishers('Publisher1')", acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            publisherRequest2.IsSuccessStatusCode.Should().BeTrue();
            var (publisher2, ErrorContent2) = await publisherRequest2.DeserializeResponseAsync<Publisher>();

            publisher2.Should().NotBeNull();
            publisher2.LastUpdated.Should().BeCloseTo(DateTimeOffset.Now, new TimeSpan(0, 0, 0, 5));
        }

        public async Task Cleanup(Guid bookId, string title)
        {
            var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var book = api.DbContext.Books.First(c => c.Id == bookId);
            book.Title = title;
            await api.DbContext.SaveChangesAsync();
        }

    }

}