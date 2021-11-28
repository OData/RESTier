// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Restier.Breakdance;
#if NETCOREAPP3_1_OR_GREATER
    using CloudNimble.Breakdance.AspNetCore;
#else
    using CloudNimble.Breakdance.WebApi;
#endif
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NETCOREAPP3_1_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore
#else
namespace Microsoft.Restier.Tests.AspNet
#endif
{

    [TestClass]
    public class RestierControllerTests : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <StoreApi>
#endif
    {

        void di(IServiceCollection services)
        {
            services.AddTestStoreApiServices();
        }

        [TestMethod]
        public async Task GetTest()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Products(1)", serviceCollection: di);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [TestMethod]
        public async Task GetNonExistingEntityTest()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Products(-1)", serviceCollection: di);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task PostTest()
        {
            var payload = new {
                Name = "var1",
                Addr = new Address { Zip = 330 }
            };

            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/Products", payload: payload,
                acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: di);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [TestMethod]
        public async Task FunctionImport_NotInModel_ShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/GetBestProduct2", serviceCollection: di);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task FunctionImport_NotInController_ShouldReturnNotImplemented()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/GetBestProduct", serviceCollection: di);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
        }

        [TestMethod]
        public async Task ActionImport_NotInModel_ShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/RemoveWorstProduct2", serviceCollection: di);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task ActionImport_NotInController_ShouldReturnNotImplemented()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/RemoveWorstProduct", serviceCollection: di);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            // TODO: standalone testing shows 501, but here is 500, will figure out detail reason
            response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);

        }

        [TestMethod]
        public async Task GetActionImport_ShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/RemoveWorstProduct", serviceCollection: di);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task FunctionImport_Post_ShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/GetBestProduct", serviceCollection: di);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            // TODO: standalone testing shows 501, but here is 500, will figure out detail reason
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

    }

}