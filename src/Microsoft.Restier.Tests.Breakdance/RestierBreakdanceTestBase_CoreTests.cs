#if NETCOREAPP3_1_OR_GREATER

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Restier.Core;
using System;
using Microsoft.AspNet.OData.Query;
using CloudNimble.Breakdance.AspNetCore;

namespace Microsoft.Restier.Tests.Breakdance
{

    [TestClass]
    public class RestierBreakdanceTestBase_CoreTests
    {

        #region Private Members

        private Action<RestierApiBuilder> addRestierAction = (apiBuilder) =>
        {
            apiBuilder.AddRestierApi<LibraryApi>(restierServices =>
            {
                restierServices
                    .AddEFCoreProviderServices<LibraryContext>()
                    .AddSingleton(new ODataValidationSettings
                    {
                        MaxTop = 5,
                        MaxAnyAllExpressionDepth = 3,
                        MaxExpansionDepth = 3,
                    });

#if EFCore
                using var tempServices = restierServices.BuildServiceProvider();

                var scopeFactory = tempServices.GetService<IServiceScopeFactory>();
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetService<LibraryContext>();

                // EnsureCreated() returns false if the database already exists
                if (dbContext.Database.EnsureCreated())
                {
                    var initializer = new LibraryTestInitializer();
                    initializer.Seed(dbContext);
                }
#endif

            });
        };

        private Action<RestierRouteBuilder> mapRestierAction = (routeBuilder) =>
        {
            routeBuilder.MapApiRoute<LibraryApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix);
        };

        #endregion

        [TestMethod]
        public void TestSetup_ServerAndServicesAreAvailable()
        {
            var testBase = GetTestBaseInstance();
            testBase.TestServer.Should().NotBeNull();
            testBase.TestServer.Services.Should().NotBeNull();
        }

        [TestMethod]
        public void TestSetup_ScopeFactoryIsPresent()
        {
            var testBase = GetTestBaseInstance();

            var factory = testBase.TestServer.Services.GetRequiredService<IServiceScopeFactory>();
            factory.Should().NotBeNull();
        }

        [TestMethod]
        public async Task HttpClient_ShouldReturnRootContent()
        {
            var testBase = GetTestBaseInstance();

            var client = testBase.GetHttpClient();
            var result = await client.GetAsync("");
            var resultContent = await result.Content.ReadAsStringAsync();

            resultContent.Should().ContainAll("$metadata", "Books", "LibraryCards", "Publishers", "Readers");
        }

        [TestMethod]
        public async Task GetApiMetadataAsync_ReturnsXDocument()
        {
            var testBase = GetTestBaseInstance();

            var metadata = await testBase.GetApiMetadataAsync();
            metadata.Should().NotBeNull();
        }

        [TestMethod]
        public void GetScopedRequestContainer_ReturnsInstance()
        {
            var testBase = GetTestBaseInstance();

            var container = testBase.GetScopedRequestContainer();
            container.Should().NotBeNull();
        }

        [TestMethod]
        public void GetApiInstance_ReturnsInstanceFromRequestScope()
        {
            var testBase = GetTestBaseInstance();

            var api = testBase.GetApiInstance();
            api.Should().NotBeNull();
        }

        private RestierBreakdanceTestBase<LibraryApi> GetTestBaseInstance()
        {
            var testBase = new RestierBreakdanceTestBase<LibraryApi>
            {
                AddRestierAction = addRestierAction,
                MapRestierAction = mapRestierAction
            };
            testBase.TestSetup();
            return testBase;
        }

    }

}

#endif