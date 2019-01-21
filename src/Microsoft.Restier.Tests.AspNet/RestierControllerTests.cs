// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using CloudNimble.Breakdance.WebApi;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet
{

    [TestClass]
    public class RestierControllerTests : RestierTestBase
    {

        [TestMethod]
        public async Task GetTest()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Products(1)");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [TestMethod]
        public async Task GetNonExistingEntityTest()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Products(-1)");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task PostTest()
        {
            var payload = new
            {
                Name = "var1",
                Addr = new Address { Zip = 330 }
            };

            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/Products", payload: payload, acceptHeader: WebApiConstants.DefaultAcceptHeader);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [TestMethod]
        public async Task FunctionImportNotInModelShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/GetBestProduct2");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task FunctionImportNotInControllerShouldReturnNotImplemented()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/GetBestProduct");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
        }

        [TestMethod]
        public async Task ActionImportNotInModelShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/RemoveWorstProduct2");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task ActionImportNotInControllerShouldReturnNotImplemented()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/RemoveWorstProduct");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            // TODO: standalone testing shows 501, but here is 500, will figure out detail reason
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        [TestMethod]
        public async Task GetActionImportShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/RemoveWorstProduct");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task PostFunctionImportShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/GetBestProduct");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            // TODO: standalone testing shows 501, but here is 500, will figure out detail reason
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

    }

}