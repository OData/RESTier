using System.Net;
using System.Net.Http;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Restier.Breakdance;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET5_0_OR_GREATER
namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else
namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif
{

    /// <summary>
    /// Restier tests that cover the general queryablility of the service.
    /// </summary>
    [TestClass]
    public class QueryTests : RestierTestBase
    {

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when EntitySet tables are just empty.
        /// </summary>
        [TestMethod]
        public async Task EmptyEntitySetQueryReturns200Not404()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/LibraryCards", routeName: "ApiTests");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 200 StatusCodes when legitimate queries to a resource simply return no results.
        /// </summary>
        [TestMethod]
        public async Task EmptyFilterQueryReturns200Not404()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$filter=Title eq 'Sesame Street'");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        /// <summary>
        /// Tests if the query pipeline is correctly returning 404 StatusCodes when a resource does not exist.
        /// </summary>
        [TestMethod]
        public async Task NonExistentEntitySetReturns404()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Subscribers");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeFalse();
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Tests if requests to collection navigation properties build as <see cref="ObservableCollection{T}"/> work.
        /// </summary>
        [TestMethod]
        public async Task ObservableCollectionsAsCollectionNavigationProperties()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Publishers('Publisher2')/Books");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

    }

}