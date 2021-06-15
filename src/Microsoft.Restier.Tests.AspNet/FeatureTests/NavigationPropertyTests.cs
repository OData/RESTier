using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Breakdance.WebApi;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
{
    [TestClass]
    public class NavigationPropertyTests : RestierTestBase
    {
        void di(IServiceCollection services)
        {
            services.AddTestStoreApiServices();
        }

        [TestMethod]
        public async Task NavigationProperties_ChildrenShouldFilter()
        {
            //var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi, DbContext>(HttpMethod.Get, resource: "/Products", serviceCollection: di);
            //var content = await response.Content.ReadAsStringAsync();
            //TestContext.WriteLine(content);
            //response.IsSuccessStatusCode.Should().BeTrue();
            //var list = JsonConvert.DeserializeObject<ODataV4List<Product>>(content);
            //list.Items.Count.Should().Be(1);

            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi, DbContext>(HttpMethod.Get, resource: "/Customers(1)/FavoriteProducts", serviceCollection: di);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            var list = JsonConvert.DeserializeObject<ODataV4List<Product>>(content);
            list.Items.Count.Should().Be(3);
        }


    }
}
