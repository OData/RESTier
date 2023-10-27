// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NETCOREAPP3_1_OR_GREATER
using CloudNimble.Breakdance.AspNetCore;
using Microsoft.AspNet.OData.Query;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared.Common;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else

using CloudNimble.Breakdance.WebApi;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Restier.Tests.Shared.Common;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

    [TestClass]
    public class AuthorizationTests : RestierTestBase
#if NET6_0_OR_GREATER
        <LibraryApi>
#endif
    {

#if NET6_0_OR_GREATER

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
            TestSetup();
        }

#endif
        /// <summary>
        /// Tests if the query pipeline is correctly returning 403 StatusCodes when <see cref="IQueryExpressionAuthorizer.Authorize"/> returns <see langword="false"/>.
        /// </summary>
        [TestMethod]
        public async Task Authorization_FilterReturns403()
        {

#if NET6_0_OR_GREATER
            AddRestierAction = (apiBuilder) =>
            {
                apiBuilder.AddRestierApi<LibraryApi>(restierServices =>
                {
                    restierServices
                        .AddEntityFrameworkServices<LibraryContext>()
                        .AddTestDefaultServices()
                        .AddSingleton<IQueryExpressionAuthorizer, DisallowEverythingAuthorizer>();
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
        public async Task Authorization_UpdateEmployee_ShouldReturn400()
        {
#if NET6_0_OR_GREATER
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
                });

            };

            AuthTestSetup();
            var settings = new JsonSerializerOptions
            {
#if NET6_0_OR_GREATER
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
#endif
            };
            settings.Converters.Add(new SystemTextJsonTimeSpanConverter());
            settings.Converters.Add(new SystemTextJsonTimeOfDayConverter());

            var employeeResponse = await ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader, jsonSerializerOptions: settings);

#else
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter>
                {
                    new NewtonsoftTimeSpanConverter(),
                    new NewtonsoftTimeOfDayConverter()
                },
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatString = "yyyy-MM-ddTHH:mm:ssZ",
            };
            var employeeResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Readers?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
#endif

            var content = await TestContext.LogAndReturnMessageContentAsync(employeeResponse);

            employeeResponse.IsSuccessStatusCode.Should().BeTrue();
            var (employeeList, ErrorContent) = await employeeResponse.DeserializeResponseAsync<ODataV4List<Employee>>(settings);

            employeeList.Should().NotBeNull();
            employeeList.Items.Should().NotBeNullOrEmpty();
            var employee = employeeList.Items.First();

            employee.Should().NotBeNull();

            employee.FullName += " Can't Update";
            //employee.Universe = null;

            //RWM: APIs are read-only by default.
            var employeeEditResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Put, resource: $"/Readers({employee.Id})", payload: employee, acceptHeader: WebApiConstants.DefaultAcceptHeader, jsonSerializerSettings: settings, serviceCollection: (services) => services.AddEntityFrameworkServices<LibraryContext>());
            var editResponseContent = await TestContext.LogAndReturnMessageContentAsync(employeeEditResponse);

            employeeEditResponse.IsSuccessStatusCode.Should().BeFalse();
            employeeEditResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }


    }

}