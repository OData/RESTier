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

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
{

    [TestClass]
    public class UpdateTests : RestierTestBase
    {

        [TestMethod]
        public async Task UpdateBookWithPublisher_ShouldReturn400()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$expand=Publisher&$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader);
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, ErrorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeNullOrEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();
            book.Publisher.Should().NotBeNull();

            book.Title += " Test";

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Put, resource: $"/Books({book.Id})", payload: book, acceptHeader: WebApiConstants.DefaultAcceptHeader);
            bookEditRequest.IsSuccessStatusCode.Should().BeFalse();
            bookEditRequest.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        public async Task UpdateBook()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader);
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, ErrorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeNullOrEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();

            book.Title += " Test";

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Put, resource: $"/Books({book.Id})", payload: book, acceptHeader: WebApiConstants.DefaultAcceptHeader);
            bookEditRequest.IsSuccessStatusCode.Should().BeTrue();
        }

        [TestMethod]
        public async Task PatchBook()
        {
            var bookRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$top=1", acceptHeader: ODataConstants.DefaultAcceptHeader);
            bookRequest.IsSuccessStatusCode.Should().BeTrue();
            var (bookList, ErrorContent) = await bookRequest.DeserializeResponseAsync<ODataV4List<Book>>();

            bookList.Should().NotBeNull();
            bookList.Items.Should().NotBeNullOrEmpty();
            var book = bookList.Items.First();

            book.Should().NotBeNull();

            var payload = new
            {
                Title = book.Title + " | Patch Test"
            };

            var bookEditRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(new HttpMethod("PATCH"), resource: $"/Books({book.Id})", payload: payload, acceptHeader: WebApiConstants.DefaultAcceptHeader);
            bookEditRequest.IsSuccessStatusCode.Should().BeTrue();

            var bookCheckRequest = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: $"/Books({book.Id})", acceptHeader: ODataConstants.DefaultAcceptHeader);
            bookCheckRequest.IsSuccessStatusCode.Should().BeTrue();
            var (book2, ErrorContent2) = await bookCheckRequest.DeserializeResponseAsync<Book>();
            book2.Should().NotBeNull();
            book2.Title.Should().EndWith(" | Patch Test");
        }

    }

}