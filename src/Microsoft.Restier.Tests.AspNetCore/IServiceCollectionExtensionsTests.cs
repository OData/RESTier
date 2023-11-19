using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore
{
    /// <summary>
    /// Tests for endpoint routing.
    /// </summary>
    [TestClass]
    public class IServiceCollectionExtensionsTests : RestierTestBase<LibraryApi>
    {

        //[TestMethod]
        //public async Task MultipleContexts_EndpointRouting_ShouldQueryFirstContext()
        //{
        //    var collection = new ServiceCollection();
        //    //collection.AddRestier()
        //}

        [TestMethod]
        public async Task MultipleContexts_EndpointRouting_ShouldQuerySecondContext()
        {
            AddRestierAction = builder =>
            {
                builder.AddRestierApi<LibraryApi>(services => services.AddEntityFrameworkServices<LibraryContext>());
                builder.AddRestierApi<MarvelApi>(services => services.AddEntityFrameworkServices<MarvelContext>());
            };
            MapRestierAction = routeBuilder =>
            {
                routeBuilder.MapApiRoute<LibraryApi>("Library", "Library", false);
                routeBuilder.MapApiRoute<MarvelApi>("Marvel", "Marvel", false);
            };
            TestSetup();
            var response = await ExecuteTestRequest(HttpMethod.Get, routePrefix: "Marvel", resource: "/Characters?$count=true");

            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("\"@odata.count\":1,");
        }
    }
}
