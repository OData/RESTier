using System;
using System.Net.Http;
using System.Threading.Tasks;
#if NET5_0_OR_GREATER
    using CloudNimble.Breakdance.AspNetCore;
#else
    using CloudNimble.Breakdance.WebApi;
#endif
using FluentAssertions;
using Microsoft.Restier.Breakdance;
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
    /// A class for testing OData Actions.
    /// </summary>
    [TestClass]
    public class ActionTests : RestierTestBase
    {

        [TestMethod]
        public async Task ActionParameters_MissingParameter()
        {
            /* JHC Note:
             * in Restier.Tests.AspNetCore, this test fails because because the response content is empty
             * 
             * */
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Post, resource: "/CheckoutBook");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeFalse();
            content.Should().Contain("NullReferenceException");
        }

        [TestMethod]
        public async Task ActionParameters_WrongParameterName()
        {
            var bookPayload = new
            {
                john = new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "Constantly Frustrated: the Robert McLaws Story",
                }
            };

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Post, resource: "/CheckoutBook", acceptHeader: WebApiConstants.DefaultAcceptHeader, payload: bookPayload);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeFalse();

            content.Should().Contain("Model state is not valid");
        }

        [TestMethod]
        public async Task ActionParameters_HasParameter()
        {
            var bookPayload = new
            {
                book = new Book
                {
                    Id = Guid.NewGuid(),
                    Title = "Constantly Frustrated: the Robert McLaws Story",
                }
            };

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Post, resource: "/CheckoutBook", acceptHeader: WebApiConstants.DefaultAcceptHeader, payload: bookPayload);
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();

            content.Should().Contain("Robert McLaws");
            content.Should().Contain("| Submitted");
        }

    }

}