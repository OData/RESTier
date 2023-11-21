// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if !NET6_0_OR_GREATER
    using System;
    using System.Web.Http;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNet.OData.Query;
#endif
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests
#else
namespace Microsoft.Restier.Tests.AspNet.RegressionTests
#endif
{

    /// <summary>
    /// Regression tests for https://github.com/OData/RESTier/issues/541.
    /// </summary>
    [TestClass]
    public class Issue671_MultipleContexts : RestierTestBase
#if NET6_0_OR_GREATER
        <LibraryApi>
#endif
    {

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when EntitySet tables are just empty.
        /// </summary>
        [TestMethod]
        public async Task SingleContext_LibraryApiWorks()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/LibraryCards", 
                serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>(), useEndpointRouting: UseEndpointRouting);
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
            var response = await RestierTestHelpers.ExecuteTestRequest<MarvelApi>(HttpMethod.Get, resource: "/Characters", 
                serviceCollection: (services) => services.AddEntityFrameworkServices<MarvelContext>(), useEndpointRouting: UseEndpointRouting);
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task MultipleContexts_ShouldQueryFirstContext()
        {
#if !NET6_0_OR_GREATER

            var config = new HttpConfiguration();

            config.SetDefaultQuerySettings(QueryDefaults);
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.SetTimeZoneInfo(TimeZoneInfo.Utc);

            config.UseRestier((builder) => {
                builder.AddRestierApi<LibraryApi>(services =>
                {
                    services.AddEntityFrameworkServices<LibraryContext>();
                });
                builder.AddRestierApi<MarvelApi>(services =>
                {
                    services.AddEntityFrameworkServices<MarvelContext>();
                });
            });

            config.MapRestier((builder) =>
            {
                builder.MapApiRoute<LibraryApi>("Library", "Library", false);
                builder.MapApiRoute<MarvelApi>("Marvel", "Marvel", false);
            });

            var client = config.GetTestableHttpClient();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, routePrefix: "Library", resource: "/Books?$count=true");
#else
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
            var response = await ExecuteTestRequest(HttpMethod.Get, routePrefix: "Library", resource: "/Books?$count=true");
#endif

            var content = await TestContext.LogAndReturnMessageContentAsync(response);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("\"@odata.count\":4,");
        }

        [TestMethod]
        public async Task MultipleContexts_ShouldQuerySecondContext()
        {
#if !NET6_0_OR_GREATER

            var config = new HttpConfiguration();

            config.SetDefaultQuerySettings(QueryDefaults);
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.SetTimeZoneInfo(TimeZoneInfo.Utc);

            config.UseRestier((builder) => {
                builder.AddRestierApi<LibraryApi>(services =>
                {
                    services.AddEntityFrameworkServices<LibraryContext>();
                });
                builder.AddRestierApi<MarvelApi>(services =>
                {
                    services.AddEntityFrameworkServices<MarvelContext>();
                });
            });

            config.MapRestier((builder) =>
            {
                builder.MapApiRoute<LibraryApi>("Library", "Library", false);
                builder.MapApiRoute<MarvelApi>("Marvel", "Marvel", false);
            });

            var client = config.GetTestableHttpClient();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, routePrefix: "Marvel", resource: "/Characters?$count=true");

#else
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

#endif
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("\"@odata.count\":1,");
        }

#if !NET6_0_OR_GREATER
        private static readonly DefaultQuerySettings QueryDefaults = new DefaultQuerySettings
        {
            EnableCount = true,
            EnableExpand = true,
            EnableFilter = true,
            EnableOrderBy = true,
            EnableSelect = true,
            MaxTop = 10
        };
#endif

    }

}
