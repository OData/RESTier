using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
{

    [TestClass]
    public class InTests : RestierTestBase
    {

        //[Ignore]
        [TestMethod]
        public async Task InQueries_IdInList()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$filter=Id in ['c2081e58-21a5-4a15-b0bd-fff03ebadd30','0697576b-d616-4057-9d28-ed359775129e']");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Jungle Book, The");
            content.Should().Contain("Color Purple, The");
            content.Should().NotContain("A Clockwork Orange");
        }

    }

}