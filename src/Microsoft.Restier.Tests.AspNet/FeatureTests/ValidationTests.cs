using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using CloudNimble.Breakdance.WebApi;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
{

    [TestClass]
    public class ValidationTests : RestierTestBase
    {

        //[Ignore]
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