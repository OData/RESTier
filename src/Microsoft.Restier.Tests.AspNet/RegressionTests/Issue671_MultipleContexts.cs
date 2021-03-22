using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Restier.Breakdance;
using FluentAssertions;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions.Common;
using Microsoft.Restier.Core.Startup;

namespace Microsoft.Restier.Tests.AspNet.RegressionTests
{

    /// <summary>
    /// Regression tests for https://github.com/OData/RESTier/issues/541.
    /// </summary>
    [TestClass]
    public class Issue671_MultipleContexts : RestierTestBase
    {

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when EntitySet tables are just empty.
        /// </summary>
        [TestMethod]
        public async Task SingleContext_LibraryApiWorks()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/LibraryCards");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when EntitySet tables are just empty.
        /// </summary>
        [TestMethod]
        public async Task SingleContext_MarvelApiWorks()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<MarvelApi, MarvelContext>(HttpMethod.Get, resource: "/Characters");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task MultipleContexts_ShouldQueryFirstContext()
        {
            var config = new HttpConfiguration();

            config.SetDefaultQuerySettings(QueryDefaults);
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.SetTimeZoneInfo(TimeZoneInfo.Utc);

            config.UseRestier((builder) => {
                builder.AddRestierApi<LibraryApi>(services =>
                {
                    services.AddEF6ProviderServices<LibraryContext>();
                });
                builder.AddRestierApi<MarvelApi>(services =>
                {
                    services.AddEF6ProviderServices<MarvelContext>();
                });
            });

            config.MapRestier((builder) =>
            {
                builder.MapApiRoute<LibraryApi>("Library", "Library", false);
                builder.MapApiRoute<MarvelApi>("Marvel", "Marvel", false);
            });

            var client = config.GetTestableHttpClient();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, routePrefix: "Library", resource: "/Books");

            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("\"@odata.count\":3,");
        }

        [TestMethod]
        public async Task MultipleContexts_ShouldQuerySecondContext()
        {
            var config = new HttpConfiguration();

            config.SetDefaultQuerySettings(QueryDefaults);
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.SetTimeZoneInfo(TimeZoneInfo.Utc);

            config.UseRestier((builder) => {
                builder.AddRestierApi<LibraryApi>(services =>
                {
                    services.AddEF6ProviderServices<LibraryContext>();
                });
                builder.AddRestierApi<MarvelApi>(services =>
                {
                    services.AddEF6ProviderServices<MarvelContext>();
                });
            });

            config.MapRestier((builder) =>
            {
                builder.MapApiRoute<LibraryApi>("Library", "Library", false);
                builder.MapApiRoute<MarvelApi>("Marvel", "Marvel", false);
            });

            var client = config.GetTestableHttpClient();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, routePrefix: "Marvel", resource: "/Characters?$count=true");

            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("\"@odata.count\":1,");
        }

        private static readonly DefaultQuerySettings QueryDefaults = new DefaultQuerySettings
        {
            EnableCount = true,
            EnableExpand = true,
            EnableFilter = true,
            EnableOrderBy = true,
            EnableSelect = true,
            MaxTop = 10
        };

    }

}
