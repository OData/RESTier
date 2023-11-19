// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.EasyAF.Http.OData;

#if NET6_0_OR_GREATER
using CloudNimble.Breakdance.AspNetCore;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else

using CloudNimble.Breakdance.WebApi;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

    [TestClass]
    public class FunctionTests : RestierTestBase
#if NET6_0_OR_GREATER
        <LibraryApi>
#endif
    {

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        [Ignore("Filter Segments not supported in WebAPI OData")]
        [TestMethod]
        public async Task BoundFunctions_CanHaveFilterPathSegment()
        {
            /* JHC Note:
             * in Restier.Tests.AspNet, this test throws an exception
             * type:    System.NotImplementedException
             * message: The method or operation is not implemented.
             * site:    Microsoft.OData.UriParser.PathSegmentHandler.Handle
             * 
             * */
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books/$filter(endswith(Title,'The'))/DiscontinueBooks()", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            results.Should().NotBeNull();
            results.Response.Should().NotBeNull();
            results.Response.Items.Should().NotBeNullOrEmpty();
            results.Response.Items.Should().HaveCount(2);
            results.Response.Items.All(c => c.Title.EndsWith(" | Discontinued", StringComparison.CurrentCulture)).Should().BeTrue();
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        [TestMethod]
        public async Task BoundFunctions_Returns200()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books/DiscontinueBooks()", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            results.Should().NotBeNull();
            results.Response.Should().NotBeNull();
            results.Response.Items.Should().NotBeNullOrEmpty();
            results.Response.Items.Count.Should().BeGreaterThanOrEqualTo(4);
            results.Response.Items.All(c => c.Title.EndsWith(" | Intercepted | Discontinued | Intercepted", StringComparison.CurrentCulture)).Should().BeTrue();
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        [TestMethod]
        public async Task BoundFunctions_WithParameter_Returns200()
        {
            var metadata = RestierTestHelpers.GetApiMetadataAsync<LibraryApi>(serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());

            var payload = new { bookId = new Guid("2D760F15-974D-4556-8CDF-D610128B537E") };

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Post, resource: "/Publishers('Publisher1')/PublishNewBook()", payload: payload, acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = await response.DeserializeResponseAsync<Publisher>();
            results.Should().NotBeNull();
            results.Response.Should().NotBeNull();
            results.Response.Books.All(c => c.Title == "Sea of Rust").Should().BeTrue();
        }

        [TestMethod]
        public async Task BoundFunctions_WithExpand()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Publishers('Publisher1')/PublishedBooks()?$expand=Publisher", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Publisher Way");
        }

        [TestMethod]
        public async Task FunctionWithFilter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/FavoriteBooks()?$filter=contains(Title,'Cat')", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Cat");
            content.Should().NotContain("Mouse");
        }

        [TestMethod]
        public async Task FunctionWithExpand()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/FavoriteBooks()?$expand=Publisher", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Publisher Way");
        }

        [TestMethod]
        public async Task FunctionParameters_BooleanParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/PublishBook(IsActive=true)", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("in the Hat");
        }

        [TestMethod]
        public async Task FunctionParameters_IntParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/PublishBooks(Count=5)", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Comes Back");
        }

        [TestMethod]
        public async Task FunctionParameters_GuidParameter()
        {
            var testGuid = Guid.NewGuid();
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/SubmitTransaction(Id={testGuid})", serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain(testGuid.ToString());
            content.Should().Contain("Shrugged");
        }

    }

}