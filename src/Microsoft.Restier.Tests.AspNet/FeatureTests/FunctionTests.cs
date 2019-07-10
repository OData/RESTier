using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using CloudNimble.Breakdance.WebApi;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
{

    [TestClass]
    public class FunctionTests : RestierTestBase
    {

        //[Ignore]
        [TestMethod]
        public async Task FunctionParameters_BooleanParameter ()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/PublishBook(IsActive=true)");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("in the Hat");
        }

        //[Ignore]
        [TestMethod]
        public async Task FunctionParameters_IntParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/PublishBooks(Count=5)");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Comes Back");
        }

        //[Ignore]
        [TestMethod]
        public async Task FunctionParameters_GuidParameter()
        {
            var testGuid = Guid.NewGuid();
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/SubmitTransaction(Id={testGuid})");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain(testGuid.ToString());
            content.Should().Contain("Shrugged");
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        [Ignore]
        [TestMethod]
        public async Task BoundFunctions_CanHaveFilterPathSegment()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books/$filter(endswith(Title,'The'))/DiscontinueBooks()");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = JsonConvert.DeserializeObject<ODataV4List<Book>>(content);
            results.Should().NotBeNull();
            results.Items.Should().NotBeNullOrEmpty();
            results.Items.Should().HaveCount(2);
            results.Items.All(c => c.Title.EndsWith(" | Discontinued")).Should().BeTrue();
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        [TestMethod]
        public async Task BoundFunctions_Returns200()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books/DiscontinueBooks()");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = JsonConvert.DeserializeObject<ODataV4List<Book>>(content);
            results.Should().NotBeNull();
            results.Items.Should().NotBeNullOrEmpty();
            results.Items.Should().HaveCount(3);
            results.Items.All(c => c.Title.EndsWith(" | Discontinued")).Should().BeTrue();
        }

    }

}