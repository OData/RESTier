// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore
{

    [TestClass]
    [TestCategory("Endpoint Routing")]
    public class RestierQueryBuilderTests_EndpointRouting : RestierQueryBuilderTests
    {
        public RestierQueryBuilderTests_EndpointRouting() : base(true)
        {
        }
    }

    [TestClass]
    [TestCategory("Legacy Routing")]
    public class RestierQueryBuilderTests_LegacyRouting : RestierQueryBuilderTests
    {
        public RestierQueryBuilderTests_LegacyRouting() : base(false)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public abstract class RestierQueryBuilderTests : RestierTestBase<StoreApi>
    {

        public RestierQueryBuilderTests(bool useEndpointRouting) : base(useEndpointRouting)
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

        void di(IServiceCollection services)
        {
            services.AddTestStoreApiServices();
        }

        [TestMethod]
        public async Task TestInt16AsKey()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Customers(1)", serviceCollection: di, useEndpointRouting: UseEndpointRouting);
            response.IsSuccessStatusCode.Should().BeTrue();
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task TestInt64AsKey()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Stores(1)", serviceCollection: di, useEndpointRouting: UseEndpointRouting);
            response.IsSuccessStatusCode.Should().BeTrue();
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
        }

    }

}
