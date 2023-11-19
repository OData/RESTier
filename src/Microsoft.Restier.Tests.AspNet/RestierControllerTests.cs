// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Restier.Breakdance;
#if NET6_0_OR_GREATER
    using CloudNimble.Breakdance.AspNetCore;
#else
    using CloudNimble.Breakdance.WebApi;
#endif
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore
#else
namespace Microsoft.Restier.Tests.AspNet
#endif
{

    [TestClass]
    public class RestierControllerTests : RestierTestBase
#if NET6_0_OR_GREATER
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
        public async Task Post_WithBody_ShouldReturnCreated()
        {
            var payload = new {
                Name = "var1",
                Addr = new Address { Zip = 330 }
            };

            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/Products", payload: payload,
                acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: di);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [TestMethod]
        public async Task Post_WithoutBody_ShouldReturnBadRequest()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/Products", 
                acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: di);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            content.Should().Contain("A POST requires an object to be present in the request body.");
        }

        [TestMethod]
        public async Task FunctionImport_NotInModel_ShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/GetBestProduct2", serviceCollection: di);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task FunctionImport_NotInController_ShouldReturnNotImplemented()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/GetBestProduct", serviceCollection: di);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
        }

        [TestMethod]
        public async Task ActionImport_NotInModel_ShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/RemoveWorstProduct2", serviceCollection: di);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task ActionImport_NotInController_ShouldReturnNotImplemented()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/RemoveWorstProduct", serviceCollection: di);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

#if !NET7_0_OR_GREATER
            response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
#else
            // RWM: ASP.NET Core 7.0 Breaking change: 
            // https://docs.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/7.0/mvc-empty-body-model-binding
            // TODO: RWM or JHC: Fix the RestierController to return the right result on .NET 7.
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            content.Should().Contain("Model state is not valid");
#endif
        }

        [TestMethod]
        public async Task GetActionImport_ShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/RemoveWorstProduct", serviceCollection: di);
            var content = TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task FunctionImport_Post_WithoutBody_ShouldReturnMethodNotAllowed()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/GetBestProduct", serviceCollection: di);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

    }

}