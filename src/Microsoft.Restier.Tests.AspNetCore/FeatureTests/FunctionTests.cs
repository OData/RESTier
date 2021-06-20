using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using FluentAssertions;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
{
    [TestClass]
    public class FunctionTests : RestierTestBase
    {

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        [TestMethod]
        public async Task BoundFunctions_Returns200()
        {
            await Task.CompletedTask;

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books/DiscontinueBooks()");
            var content = await response.Content.ReadAsStringAsync();

            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var results = JsonConvert.DeserializeObject<ODataV4List<Book>>(content);
            results.Should().NotBeNull();
            results.Items.Should().NotBeNullOrEmpty();
            results.Items.Count.Should().BeOneOf(4);
            results.Items.All(c => c.Title.EndsWith(" | Intercepted | Discontinued | Intercepted", StringComparison.CurrentCulture)).Should().BeTrue();
        }

    }
}
