// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CloudNimble.EasyAF.Http.OData;

#if NET6_0_OR_GREATER
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.AspNet.OData.Query;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.AspNetCore.FallbackTests;

namespace Microsoft.Restier.Tests.AspNetCore

#else
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.AspNet.FallbackTests;

namespace Microsoft.Restier.Tests.AspNet
#endif
{

#if NET6_0_OR_GREATER

    [TestClass]
    [TestCategory("Endpoint Routing")]
    public class ODataControllerFallbackTests_EndpointRouting : ODataControllerFallbackTests
    {
        public ODataControllerFallbackTests_EndpointRouting() : base(true)
        {
        }
    }

    [TestClass]
    [TestCategory("Legacy Routing")]
    public class ODataControllerFallbackTests_LegacyRouting : ODataControllerFallbackTests
    {
        public ODataControllerFallbackTests_LegacyRouting() : base(false)
        {
        }
    }

    [TestClass]
    public abstract class ODataControllerFallbackTests : RestierTestBase<FallbackApi>
    {

        public ODataControllerFallbackTests(bool useEndpointRouting) : base(useEndpointRouting)
        {
            AddRestierAction = (restier) => restier.AddRestierApi<FallbackApi>(restierServices =>
            {
                restierServices
                    .AddSingleton(new ODataValidationSettings
                    {
                        MaxTop = 5,
                        MaxAnyAllExpressionDepth = 3,
                        MaxExpansionDepth = 3,
                    });
                addTestServices(restierServices);
            });
            MapRestierAction = (routeBuilder) =>
            {
                routeBuilder.MapApiRoute<FallbackApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix);
            };
        }

        [TestInitialize]
        public override void TestSetup() => base.TestSetup();

#else

    [TestClass]
    public class ODataControllerFallbackTests : RestierTestBase
    {

#endif

        void addTestServices(IServiceCollection services)
        {
            services
                .AddChainedService<IModelBuilder>((sp, next) => new StoreModelProducer(FallbackModel.Model))
                .AddChainedService<IModelMapper>((sp, next) => new FallbackModelMapper())
                .AddChainedService<IQueryExpressionSourcer>((sp, next) => new FallbackQueryExpressionSourcer())
                .AddChainedService<IChangeSetInitializer>((sp, next) => new StoreChangeSetInitializer())
                .AddChainedService<ISubmitExecutor>((sp, next) => new DefaultSubmitExecutor());
        }

        [TestMethod]
        public async Task FallbackApi_EntitySet_ShouldFallBack()
        {
            // Should fallback to PeopleController.

#if NET6_0_OR_GREATER
            var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/People");
#else
            var response = await RestierTestHelpers.ExecuteTestRequest<FallbackApi>(HttpMethod.Get, resource: "/People", serviceCollection: addTestServices);
#endif            
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
            response.IsSuccessStatusCode.Should().BeTrue();
            var (Response, ErrorContent) = await response.DeserializeResponseAsync<ODataV4List<Person>>();
            var first = Response.Items.FirstOrDefault();
            first.Should().NotBeNull();
            first.Id.Should().Be(999);
        }

        [TestMethod]
        public async Task FallbackApi_NavigationProperty_ShouldFallBack()
        {
            // Should fallback to PeopleController.

#if NET6_0_OR_GREATER
            var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/People(1)/Orders");
#else
            var response = await RestierTestHelpers.ExecuteTestRequest<FallbackApi>(HttpMethod.Get, resource: "/People(1)/Orders", serviceCollection: addTestServices);
#endif 
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
            response.IsSuccessStatusCode.Should().BeTrue();

            var (Response, ErrorContent) = await response.DeserializeResponseAsync<ODataV4List<Order>>();
            var first = Response.Items.FirstOrDefault();
            first.Should().NotBeNull();
            first.Id.Should().Be(123);
        }

        [TestMethod]
        public async Task FallbackApi_EntitySet_ShouldNotFallBack()
        {
            // Should be routed to RestierController.

#if NET6_0_OR_GREATER
            var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Orders");
#else
            var response = await RestierTestHelpers.ExecuteTestRequest<FallbackApi>(HttpMethod.Get, resource: "/Orders", serviceCollection: addTestServices);
#endif 
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
            response.IsSuccessStatusCode.Should().BeTrue();
            (await response.Content.ReadAsStringAsync()).Should().Contain("\"Id\":234");
        }

        [TestMethod]
        public async Task FallbackApi_Resource_ShouldNotFallBack()
        {
            // Should be routed to RestierController.

#if NET6_0_OR_GREATER
            var metadata = await GetApiMetadataAsync();
            var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/PreservedOrders");
#else
            var metadata = await RestierTestHelpers.GetApiMetadataAsync<FallbackApi>(serviceCollection: addTestServices);
            var response = await RestierTestHelpers.ExecuteTestRequest<FallbackApi>(HttpMethod.Get, resource: "/PreservedOrders", serviceCollection: addTestServices);
#endif

            metadata.Should().NotBeNull();
            metadata.Descendants().Where(c => c.Name.LocalName == "EntitySet").Should().HaveCount(3);

            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("\"Id\":234");
        }

    }

}