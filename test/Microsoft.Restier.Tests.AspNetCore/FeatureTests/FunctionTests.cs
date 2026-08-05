// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
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

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
{
    public abstract class FunctionTests<TApi, TContext> : RestierTestBase<TApi> where TApi : ApiBase where TContext : class
    {
        protected abstract Action<IServiceCollection> ConfigureServices { get; }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
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
            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Get, resource: "/Books/$filter(endswith(Title,'The'))/DiscontinueBooks()",
                serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            results.Should().NotBeNull();
            results.Response.Should().NotBeNull();
            results.Response.Items.Should().NotBeNullOrEmpty();
            results.Response.Items.Should().HaveCount(2);
            results.Response.Items.All(c => c.Title.EndsWith(" | Intercepted | Discontinued | Intercepted", StringComparison.CurrentCulture)).Should().BeTrue();
        }

        [TestMethod]
        public async Task FilterPathSegment_FiltersCollection()
        {
            // $filter as a path segment without a subsequent bound function
            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Get, resource: "/Books/$filter(endswith(Title,'The'))",
                serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            results.Should().NotBeNull();
            results.Response.Should().NotBeNull();
            results.Response.Items.Should().NotBeNullOrEmpty();
            results.Response.Items.Should().HaveCount(2);
            results.Response.Items.All(c => c.Title.EndsWith("The", StringComparison.Ordinal)).Should().BeTrue();
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        [TestMethod]
        public async Task BoundFunctions_Returns200()
        {
            //var response = await RestierTestHelpers.RouteDebug<LibraryApi>(routePrefix: string.Empty, serviceCollection : ConfigureServices);


            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Get, resource: "/Books/DiscontinueBooks()",
                serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = await response.DeserializeResponseAsync<ODataV4List<Book>>();
            results.Should().NotBeNull();
            results.Response.Should().NotBeNull();
            results.Response.Items.Should().NotBeNullOrEmpty();
            results.Response.Items.Count.Should().BeGreaterThanOrEqualTo(4);
            results.Response.Items.All(c => c.Title.EndsWith(" | Intercepted | Discontinued | Intercepted", StringComparison.CurrentCulture)).Should().BeTrue();
        }

        [TestMethod]
        public async Task BoundFunctions_WithExpand()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Get, resource: "/Publishers('Publisher1')/PublishedBooks()?$expand=Publisher",
                serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Publisher Way");
        }

        [TestMethod]
        public async Task FunctionWithFilter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Get, resource: "/FavoriteBooks()?$filter=contains(Title,'Cat')",
                serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Cat");
            content.Should().NotContain("Mouse");
        }

        [TestMethod]
        public async Task FunctionWithExpand()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Get, resource: "/FavoriteBooks()?$expand=Publisher",
                serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Publisher Way");
        }

        [TestMethod]
        public async Task FunctionParameters_BooleanParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Get, resource: "/PublishBook(IsActive=true)",
                serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("in the Hat");
        }

        [TestMethod]
        public async Task FunctionParameters_IntParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Get, resource: "/PublishBooks(Count=5)",
                serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Comes Back");
        }

        [TestMethod]
        public async Task FunctionParameters_GuidParameter()
        {
            var testGuid = Guid.NewGuid();
            var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(HttpMethod.Get, resource: $"/SubmitTransaction(Id={testGuid})",
                serviceCollection: ConfigureServices);
            var content = await TraceListener.LogAndReturnMessageContentAsync(response);
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain(testGuid.ToString());
            content.Should().Contain("Shrugged");
        }

    }

}
