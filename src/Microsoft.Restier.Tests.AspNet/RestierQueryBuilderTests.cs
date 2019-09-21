// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Data.Entity;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet
{

    [TestClass]
    public class RestierQueryBuilderTests : RestierTestBase
    {

        void di(IServiceCollection services)
        {
            services.AddTestStoreApiServices();
        }

        [TestMethod]
        public async Task TestInt16AsKey()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi, DbContext>(HttpMethod.Get, resource: "/Customers(1)", serviceCollection: di);
            response.IsSuccessStatusCode.Should().BeTrue();
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task TestInt64AsKey()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi, DbContext>(HttpMethod.Get, resource: "/Stores(1)", serviceCollection: di);
            response.IsSuccessStatusCode.Should().BeTrue();
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
        }
    }
}
