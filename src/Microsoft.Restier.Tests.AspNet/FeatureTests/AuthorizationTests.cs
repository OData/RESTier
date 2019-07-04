using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
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
            var response = await RestierTestHelpers.ExecuteTestRequest<UnauthorizedLibraryApi>(HttpMethod.Get, resource: "/Books");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

    }

}