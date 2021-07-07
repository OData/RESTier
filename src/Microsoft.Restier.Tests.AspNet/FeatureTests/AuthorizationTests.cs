using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Common;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

#if NET5_0_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else
namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{
    [TestClass]
    public class AuthorizationTests : RestierTestBase
    {

        /// <summary>
        /// Tests if the query pipeline is correctly returning 403 StatusCodes when <see cref="IQueryExpressionAuthorizer.Authorize()"/> returns <see cref="false"/>.
        /// </summary>
        [TestMethod]
        public async Task Authorization_FilterReturns403()
        {
            /* JHC Note:
             * in Restier.Tests.AspNet, this test throws an exception
             * type:    System.NotSupportedException
             * message: Not supported type: Microsoft.Restier.Tests.Shared.Scenarios.Library.Book
             * site:    Microsoft.Restier.AspNet.Model.EdmHelpers.GetPrimitiveTypeKind
             * note:    This is happening because the line below is returning null on the object Book in Shared.Scenarios.Library
             *              var edmType = model.FindDeclaredType(type.FullName);  << this returns null for all EntityTypes in the scenario
             * 
             * in Restier.Tests.AspNetCore, the request returns HttpStatusCode 200 instead of the expected 403, so the test fails
             * 
             * */
            void di(IServiceCollection services)
            {
                services
                    .AddTestDefaultServices()
                    .AddSingleton<IQueryExpressionAuthorizer, DisallowEverythingAuthorizer>();
            }
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books", serviceCollection: di);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeFalse();

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [TestMethod]
        public async Task UpdateEmployee_ShouldReturn400()
        {
            /* JHC Note:
             * in Restier.Tests.AspNetCore, this test fails because the employeeList is empty after deserialization
             * 
             * */
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

            var employeeResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Readers?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader);
            var content = await TestContext.LogAndReturnMessageContentAsync(employeeResponse);

            employeeResponse.IsSuccessStatusCode.Should().BeTrue();
            var (employeeList, ErrorContent) = await employeeResponse.DeserializeResponseAsync<ODataV4List<Employee>>(settings);

            employeeList.Should().NotBeNull();
            employeeList.Items.Should().NotBeNullOrEmpty();
            var employee = employeeList.Items.First();

            employee.Should().NotBeNull();

            employee.FullName += " Can't Update";
            //employee.Universe = null;

            var employeeEditResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Put, resource: $"/Readers({employee.Id})", payload: employee, acceptHeader: WebApiConstants.DefaultAcceptHeader, jsonSerializerSettings: settings);
            var editResponseContent = await TestContext.LogAndReturnMessageContentAsync(employeeEditResponse);

            employeeEditResponse.IsSuccessStatusCode.Should().BeFalse();
            employeeEditResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }


    }

}