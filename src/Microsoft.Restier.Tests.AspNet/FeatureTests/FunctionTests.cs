using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Restier.Breakdance;
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

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        [Ignore]
        [TestMethod]
        public async Task BoundFunctions_CanHaveFilterPathSegment()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books/$filter(endswith(Title,'The'))/DiscontinueBooks()");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = JsonConvert.DeserializeObject<ODataV4List<Book>>(content);
            results.Should().NotBeNull();
            results.Items.Should().NotBeNullOrEmpty();
            results.Items.Should().HaveCount(2);
            results.Items.All(c => c.Title.EndsWith(" | Discontinued", StringComparison.CurrentCulture)).Should().BeTrue();
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        [TestMethod]
        public async Task BoundFunctions_Returns200()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books/DiscontinueBooks()");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = JsonConvert.DeserializeObject<ODataV4List<Book>>(content);
            results.Should().NotBeNull();
            results.Items.Should().NotBeNullOrEmpty();
            results.Items.Count.Should().BeOneOf(4);
            results.Items.All(c => c.Title.EndsWith(" | Intercepted | Discontinued | Intercepted", StringComparison.CurrentCulture)).Should().BeTrue();
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        [TestMethod]
        public async Task BoundFunctions_WithParameter_Returns200()
        {
            var metadata = RestierTestHelpers.GetApiMetadataAsync<LibraryApi, LibraryContext>();

            var payload = new { bookId = new Guid("2D760F15-974D-4556-8CDF-D610128B537E") };

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Post, resource: "/Publishers('Publisher1')/PublishNewBook()", payload: payload, acceptHeader: WebApiConstants.DefaultAcceptHeader);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = JsonConvert.DeserializeObject<Publisher>(content);
            results.Should().NotBeNull();
            results.Books.All(c => c.Title == "Sea of Rust").Should().BeTrue();
        }

        [TestMethod]
        public async Task BoundFunctions_WithExpand()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Publishers('Publisher1')/PublishedBooks()?$expand=Publisher");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Publisher Way");
        }

        [TestMethod]
        public async Task FunctionWithFilter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/FavoriteBooks()?$filter=contains(Title,'Cat')");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Cat");
            content.Should().NotContain("Mouse");
        }

        [TestMethod]
        public async Task FunctionWithExpand()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/FavoriteBooks()?$expand=Publisher");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Publisher Way");
        }

        [TestMethod]
        public async Task FunctionParameters_BooleanParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/PublishBook(IsActive=true)");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("in the Hat");
        }

        [TestMethod]
        public async Task FunctionParameters_IntParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/PublishBooks(Count=5)");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Comes Back");
        }

        [TestMethod]
        public async Task FunctionParameters_GuidParameter()
        {
            var testGuid = Guid.NewGuid();
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: $"/SubmitTransaction(Id={testGuid})");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain(testGuid.ToString());
            content.Should().Contain("Shrugged");
        }

    }

}