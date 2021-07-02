using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Restier.Breakdance;
#if NET5_0_OR_GREATER
    using CloudNimble.Breakdance.AspNetCore;
#else
    using CloudNimble.Breakdance.WebApi;
#endif
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

    [TestClass]
    public class ValidationTests : RestierTestBase
    {

        [TestMethod]
        public async Task Validation_StringLengthExceeded()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader);
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, ErrorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeNullOrEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();

            book.Isbn = "This is a really really long string.";

            var bookEditResponse = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Put, resource: $"/Books({book.Id})", payload: book, acceptHeader: WebApiConstants.DefaultAcceptHeader);
            var content = await TestContext.LogAndReturnMessageContentAsync(bookEditResponse);

            bookEditResponse.IsSuccessStatusCode.Should().BeFalse();
            content.Should().Contain("validationentries");
            content.Should().Contain("MaxLengthAttribute");
        }

    }

}