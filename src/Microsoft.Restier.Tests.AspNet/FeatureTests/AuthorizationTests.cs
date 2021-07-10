using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
#if NETCOREAPP3_1_OR_GREATER
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

#else
using CloudNimble.Breakdance.WebApi;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Restier.Tests.Shared.Common;
#endif
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Restier.Core;
using Microsoft.AspNet.OData.Query;

#if NETCOREAPP3_1_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else
namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{
    [TestClass]
    public class AuthorizationTests : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <LibraryApi>
#endif
    {

#if NETCOREAPP3_1_OR_GREATER

        #region Constructors

        public AuthorizationTests()
        {

            MapRestierAction = (routeBuilder) =>
            {
                routeBuilder.MapApiRoute<LibraryApi>(WebApiConstants.RouteName, WebApiConstants.RoutePrefix);
            };
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public void AuthTestSetup()
        {
            TestHostBuilder.Configure(app => app.UseAuthorization());
            TestSetup();
        }

#endif
        /// <summary>
        /// Tests if the query pipeline is correctly returning 403 StatusCodes when <see cref="IQueryExpressionAuthorizer.Authorize()"/> returns <see cref="false"/>.
        /// </summary>
        [TestMethod]
        public async Task Authorization_FilterReturns403()
        {

#if NETCOREAPP3_1_OR_GREATER
            AddRestierAction = (apiBuilder) =>
            {
                apiBuilder.AddRestierApi<LibraryApi>(restierServices =>
                {
                    restierServices
                        .AddEntityFrameworkServices<LibraryContext>()
                        .AddTestDefaultServices()
                        .AddSingleton<IQueryExpressionAuthorizer, DisallowEverythingAuthorizer>();

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

            AuthTestSetup();
            var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader);
#else
            void di(IServiceCollection services)
            {
                services
                    .AddEntityFrameworkServices<LibraryContext>()
                    .AddTestDefaultServices()
                    .AddSingleton<IQueryExpressionAuthorizer, DisallowEverythingAuthorizer>();
            }
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books", serviceCollection: di);
#endif
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeFalse();

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task UpdateEmployee_ShouldReturn400()
        {
#if !NETCOREAPP3_1_OR_GREATER
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new JsonTimeSpanConverter(),
                    new JsonTimeOfDayConverter()
                },
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatString = "yyyy-MM-ddTHH:mm:ssZ",
            };
            var employeeResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Readers?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
#else
            AddRestierAction = (apiBuilder) =>
            {
                apiBuilder.AddRestierApi<LibraryApi>(restierServices =>
                {
                    restierServices
                        .AddEntityFrameworkServices<LibraryContext>()
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
            AuthTestSetup();
            var employeeResponse = await ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader);
#endif
            var content = await TestContext.LogAndReturnMessageContentAsync(employeeResponse);

            employeeResponse.IsSuccessStatusCode.Should().BeTrue();
#if NETCOREAPP3_1_OR_GREATER
            var (employeeList, ErrorContent) = await employeeResponse.DeserializeResponseAsync<ODataV4List<Employee>>();
#else
            var (employeeList, ErrorContent) = await employeeResponse.DeserializeResponseAsync<ODataV4List<Employee>>(settings);
#endif
            employeeList.Should().NotBeNull();
            employeeList.Items.Should().NotBeNullOrEmpty();
            var employee = employeeList.Items.First();

            employee.Should().NotBeNull();

            employee.FullName += " Can't Update";
            //employee.Universe = null;

            //RWM: APIs are read-only by default.
#if NETCOREAPP3_1_OR_GREATER
            var employeeEditResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Put, resource: $"/Readers({employee.Id})", payload: employee, acceptHeader: WebApiConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
#else
            var employeeEditResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Put, resource: $"/Readers({employee.Id})", payload: employee, acceptHeader: WebApiConstants.DefaultAcceptHeader, jsonSerializerSettings: settings, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
#endif
            var editResponseContent = await TestContext.LogAndReturnMessageContentAsync(employeeEditResponse);

            employeeEditResponse.IsSuccessStatusCode.Should().BeFalse();
            employeeEditResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }


    }

}