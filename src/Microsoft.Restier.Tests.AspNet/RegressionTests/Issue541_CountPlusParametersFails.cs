using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet.RegressionTests
{

    /// <summary>
    /// Regression tests for https://github.com/OData/RESTier/issues/541.
    /// </summary>
    [TestClass]
    public class Issue541_CountPlusParametersFails : RestierTestBase
    {

        [TestMethod]
        public async Task CountShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi, LibraryContext>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$count=true");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

        [TestMethod]
        public async Task CountPlusTopShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi, LibraryContext>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=5&$count=true");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

        [TestMethod]
        public async Task CountPlusTopPlusFilterShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi, LibraryContext>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=5&$count=true&$filter=FullName eq 'p1'");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":1,");
        }

        [TestMethod]
        public async Task CountPlusTopPlusProjectionShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi, LibraryContext>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$top=5&$count=true&$select=Id,FullName");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

        [TestMethod]
        public async Task CountPlusSelectShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi, LibraryContext>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Readers?$count=true&$select=Id,FullName");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

        [TestMethod]
        public async Task CountPlusExpandShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi, LibraryContext>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Publishers?$top=5&$count=true&$expand=Books");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("\"@odata.count\":2,");
        }

    }

}