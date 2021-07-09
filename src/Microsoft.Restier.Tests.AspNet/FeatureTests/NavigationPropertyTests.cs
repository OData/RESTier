using System.Net.Http;
using System.Threading.Tasks;
#if NET5_0_OR_GREATER
    using CloudNimble.Breakdance.AspNetCore;
#else
    using CloudNimble.Breakdance.WebApi;
#endif
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

#if NET5_0_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else
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
            //var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi, DbContext>(HttpMethod.Get, resource: "/Products", serviceCollection: di);
            //var content = await response.Content.ReadAsStringAsync();
            //TestContext.WriteLine(content);
            //response.IsSuccessStatusCode.Should().BeTrue();
            //var list = JsonConvert.DeserializeObject<ODataV4List<Product>>(content);
            //list.Items.Count.Should().Be(1);

            var response = await RestierTestHelpers.ExecuteTestRequest<StoreApi>(HttpMethod.Get, resource: "/Customers(1)/FavoriteProducts", serviceCollection: di);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            var list = JsonConvert.DeserializeObject<ODataV4List<Product>>(content);
            list.Items.Count.Should().Be(3);
        }


    }
}