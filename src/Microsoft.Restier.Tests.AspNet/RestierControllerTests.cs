// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Restier.Breakdance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET6_0_OR_GREATER
using CloudNimble.Breakdance.AspNetCore;

namespace Microsoft.Restier.Tests.AspNetCore
#else
using CloudNimble.Breakdance.WebApi;

namespace Microsoft.Restier.Tests.AspNet
#endif
{

#if NET6_0_OR_GREATER

    [TestClass]
    [TestCategory("Endpoint Routing")]
    public class RestierControllerTests_EndpointRouting : RestierControllerTests
    {
        public RestierControllerTests_EndpointRouting() : base(true)
        {
        }
    }

    [TestClass]
    [TestCategory("Legacy Routing")]
    public class RestierControllerTests_LegacyRouting : RestierControllerTests
    {
        public RestierControllerTests_LegacyRouting() : base(false)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public abstract class RestierControllerTests : RestierTestBase<StoreApi>
    {

        public RestierControllerTests(bool useEndpointRouting) : base(useEndpointRouting)
        {
            //AddRestierAction = builder =>
            //{
            //    builder.AddRestierApi<StoreApi>(services => services.AddEntityFrameworkServices<LibraryContext>());
            //};
            //MapRestierAction = routeBuilder =>
            //{
            //    routeBuilder.MapApiRoute<StoreApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix, false);
            //};
        }

        //[TestInitialize]
        //public void ClaimsTestSetup() => TestSetup();

#else

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class RestierControllerTests : RestierTestBase
    {

#endif

        void di(IServiceCollection services)
        {
            services.AddTestStoreApiServices();
        }

        [TestMethod]
        public async Task GetTest()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Products(1)", serviceCollection: di, useEndpointRouting: UseEndpointRouting);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [TestMethod]
        public async Task GetNonExistingEntityTest()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Products(-1)", serviceCollection: di, useEndpointRouting: UseEndpointRouting);
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
                acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: di, useEndpointRouting: UseEndpointRouting);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [TestMethod]
        public async Task Post_WithoutBody_ShouldReturnBadRequest()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/Products", 
                acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: di, useEndpointRouting: UseEndpointRouting);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            content.Should().Contain("A POST requires an object to be present in the request body.");
        }

        [TestMethod]
        public async Task FunctionImport_NotInModel_ShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/GetBestProduct2", serviceCollection: di, useEndpointRouting: UseEndpointRouting);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task FunctionImport_NotInController_ShouldReturnNotImplemented()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/GetBestProduct", serviceCollection: di, useEndpointRouting: UseEndpointRouting);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
        }

        [TestMethod]
        public async Task ActionImport_NotInModel_ShouldReturnNotFound()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/RemoveWorstProduct2", serviceCollection: di, useEndpointRouting: UseEndpointRouting);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task ActionImport_NotInController_ShouldReturnNotImplemented()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/RemoveWorstProduct", serviceCollection: di, useEndpointRouting: UseEndpointRouting);
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
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/RemoveWorstProduct", serviceCollection: di, useEndpointRouting: UseEndpointRouting);
            var content = TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task FunctionImport_Post_WithoutBody_ShouldReturnMethodNotAllowed()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Post, resource: "/GetBestProduct", serviceCollection: di, useEndpointRouting: UseEndpointRouting);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

    }

}