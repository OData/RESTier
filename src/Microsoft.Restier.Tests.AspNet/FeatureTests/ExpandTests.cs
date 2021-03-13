using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Restier.Breakdance;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
{

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    public class ExpandTests : RestierTestBase
    {

        [TestMethod]
        public async Task CountPlusExpandShouldntThrowExceptions()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Publishers?$expand=Books");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();

            content.Should().Contain("A Clockwork Orange");
        }

    }

}