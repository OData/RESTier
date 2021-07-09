// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Restier.Breakdance;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET5_0_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore
#else
namespace Microsoft.Restier.Tests.AspNet
#endif
{

    [TestClass]
    public class RestierQueryBuilderTests : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <StoreApi>
#endif
    {

        void di(IServiceCollection services)
        {
            services.AddTestStoreApiServices();
        }

        [TestMethod]
        public async Task TestInt16AsKey()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Customers(1)", serviceCollection: di);
            response.IsSuccessStatusCode.Should().BeTrue();
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task TestInt64AsKey()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Stores(1)", serviceCollection: di);
            response.IsSuccessStatusCode.Should().BeTrue();
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
        }
    }
}
