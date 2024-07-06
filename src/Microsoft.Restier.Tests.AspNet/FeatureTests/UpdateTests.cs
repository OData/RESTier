// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.EasyAF.Http.OData;
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

#if NET6_0_OR_GREATER
using CloudNimble.Breakdance.AspNetCore;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else

using CloudNimble.Breakdance.WebApi;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

#if NET6_0_OR_GREATER

    [TestClass]
    [TestCategory("Endpoint Routing")]
    public class UpdateTests_EndpointRouting : UpdateTests
    {
        public UpdateTests_EndpointRouting() : base(true)
        {
        }
    }

    [TestClass]
    [TestCategory("Legacy Routing")]
    public class UpdateTests_LegacyRouting : UpdateTests
    {
        public UpdateTests_LegacyRouting() : base(false)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public abstract class UpdateTests : RestierTestBase<LibraryApi>
    {

        public UpdateTests(bool useEndpointRouting) : base(useEndpointRouting)
        {
            //AddRestierAction = builder =>
            //{
            //    builder.AddRestierApi<LibraryApi>(services => services.AddEntityFrameworkServices<LibraryContext>());
            //};
            //MapRestierAction = routeBuilder =>
            //{
            //    routeBuilder.MapApiRoute<LibraryApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix, false);
            //};
        }

        //[TestInitialize]
        //public void ClaimsTestSetup() => TestSetup();

#else

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class UpdateTests : RestierTestBase
    {

#endif

        [TestMethod]
        public async Task UpdateBookWithPublisher_ShouldReturn400()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books?$expand=Publisher&$top=1",
                acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, ErrorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeNullOrEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();
            book.Publisher.Should().NotBeNull();

            book.Title += " Test";

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Put, resource: $"/Books({book.Id})", payload: book,
                acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            bookEditRequest.IsSuccessStatusCode.Should().BeFalse();
            bookEditRequest.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task UpdateBook()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books?$top=1",
                acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, ErrorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeNullOrEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();

            var originalBookTitle = book.Title;
            book.Title += " Test";

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Put, resource: $"/Books({book.Id})", payload: book,
                acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            bookEditRequest.IsSuccessStatusCode.Should().BeTrue();

            var bookCheckRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/Books({book.Id})",
                acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            bookCheckRequest.IsSuccessStatusCode.Should().BeTrue();
            var (book2, ErrorContent2) = await bookCheckRequest.DeserializeResponseAsync<Book>();
            book2.Should().NotBeNull();
            book2.Title.Should().Be($"{originalBookTitle} Test");

            await Cleanup(book.Id, originalBookTitle);
        }

        [TestMethod]
        public async Task PatchBook()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books?$top=1",
                acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
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

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(new HttpMethod("PATCH"), resource: $"/Books({book.Id})", payload: payload,
                acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            bookEditRequest.IsSuccessStatusCode.Should().BeTrue();

            var bookCheckRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/Books({book.Id})",
                acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            bookCheckRequest.IsSuccessStatusCode.Should().BeTrue();
            var (book2, ErrorContent2) = await bookCheckRequest.DeserializeResponseAsync<Book>();
            book2.Should().NotBeNull();
            book2.Title.Should().Be($"{originalBookTitle} | Patch Test");

            await Cleanup(book.Id, originalBookTitle);
        }

        /// <summary>
        /// TODO: @robertmclaws: This test needs to be able to run in parallel between the Legacy and Endpoint Routing tests.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task UpdatePublisher_ShouldCallInterceptor()
        {
            var publisherRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Publishers('Publisher1')",
                acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            publisherRequest.IsSuccessStatusCode.Should().BeTrue();
            var (publisher, ErrorContent) = await publisherRequest.DeserializeResponseAsync<Publisher>();

            publisher.Should().NotBeNull();
            publisher.LastUpdated.Should().NotBeCloseTo(DateTimeOffset.Now, new TimeSpan(0, 0, 0, 5));

            publisher.Books = null;
            var publisherEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Put, resource: $"/Publishers('{publisher.Id}')", payload: publisher,
                acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            var result = await TestContext.LogAndReturnMessageContentAsync(publisherEditRequest);

            publisherEditRequest.IsSuccessStatusCode.Should().BeTrue();

            var publisherRequest2 = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Publishers('Publisher1')",
                acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(),
                useEndpointRouting: UseEndpointRouting);
            publisherRequest2.IsSuccessStatusCode.Should().BeTrue();
            var (publisher2, ErrorContent2) = await publisherRequest2.DeserializeResponseAsync<Publisher>();

            publisher2.Should().NotBeNull();
            publisher2.LastUpdated.Should().BeCloseTo(DateTimeOffset.Now, new TimeSpan(0, 0, 0, 6));
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