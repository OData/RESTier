// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

#if NETCOREAPP3_1_OR_GREATER
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.Breakdance.AspNetCore.OData;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else

using CloudNimble.Breakdance.WebApi;
using CloudNimble.Breakdance.WebApi.OData;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

    [TestClass]
    public class NavigationPropertyTests : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <StoreApi>
#endif
    {
        void di(IServiceCollection services)
        {
            services.AddTestStoreApiServices();
        }

        [TestMethod]
        public async Task NavigationProperties_ChildrenShouldFilter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Customers(1)/FavoriteProducts", serviceCollection: di);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            var (Response, ErrorContent) = await response.DeserializeResponseAsync<ODataV4List<Product>>();
            Response.Items.Should().HaveCount(3);
        }

    }

}