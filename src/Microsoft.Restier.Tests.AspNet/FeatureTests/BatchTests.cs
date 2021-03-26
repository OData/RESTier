#if !RELEASE

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Restier.Breakdance;
using CloudNimble.Breakdance.WebApi;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
{

    [TestClass]
    public class BatchTests : RestierTestBase
    {

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task BatchTests_AddMultipleEntries()
        {
            var config = await RestierTestHelpers.GetTestableRestierConfiguration<LibraryApi, LibraryContext>().ConfigureAwait(false);
            var httpClient = config.GetTestableHttpClient();
            httpClient.BaseAddress = new Uri($"{WebApiConstants.Localhost}{WebApiConstants.RoutePrefix}");

            var odataSettings = new ODataClientSettings(httpClient, new Uri("", UriKind.Relative))
            {
                OnTrace = (x, y) => TestContext.WriteLine(string.Format(x, y)),
                // RWM: Need a batter way to capture the payload... this event fires before the payload is written to the stream.
                //BeforeRequestAsync = async (x) => {
                //    var ms = new MemoryStream();
                //    if (x.Content != null)
                //    {
                //        await x.Content.CopyToAsync(ms).ConfigureAwait(false);
                //        var streamContent = new StreamContent(ms);
                //        var request = await streamContent.ReadAsStringAsync();
                //        TestContext.WriteLine(request);
                //    }
                //},
                //AfterResponseAsync = async (x) => TestContext.WriteLine(await x.Content.ReadAsStringAsync()),
            };

            var odataBatch = new ODataBatch(odataSettings);
            var odataClient = new ODataClient(odataSettings);

            var publisher = await odataClient.For<Publisher>()
                .Key("Publisher1")
                .FindEntryAsync();

            odataBatch += async c =>
                await c.For<Book>()
                .Set(new { Id = Guid.NewGuid(), Isbn = "1111111111111", Title = "Batch Test #1", Publisher = publisher })
                .InsertEntryAsync();

            odataBatch += async c =>
                await c.For<Book>()
                .Set(new { Id = Guid.NewGuid(), Isbn = "2222222222222", Title = "Batch Test #2", Publisher = publisher })
                .InsertEntryAsync();

            //RWM: This way should also work.
            //var payload = odataBatch.ToString();

            try
            {
                await odataBatch.ExecuteAsync();
            }
            catch (WebRequestException exception)
            {
                TestContext.WriteLine(exception.Response);
                throw;
            }

            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi, LibraryContext>(HttpMethod.Get, resource: "/Books?$expand=Publisher");
            var content = await TestContext.LogAndReturnMessageContentAsync(response);

            response.IsSuccessStatusCode.Should().BeTrue();

            content.Should().Contain("1111111111111");
            content.Should().Contain("2222222222222");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task BatchTests_SelectPlusFunctionResult()
        {
            var config = await RestierTestHelpers.GetTestableRestierConfiguration<LibraryApi, LibraryContext>().ConfigureAwait(false);
            var httpClient = config.GetTestableHttpClient();
            httpClient.BaseAddress = new Uri($"{WebApiConstants.Localhost}{WebApiConstants.RoutePrefix}");

            var odataSettings = new ODataClientSettings(httpClient, new Uri("", UriKind.Relative))
            {
                OnTrace = (x, y) => TestContext.WriteLine(string.Format(x, y)),
                // RWM: Need a batter way to capture the payload... this event fires before the payload is written to the stream.
                //BeforeRequestAsync = async (x) => {
                //    var ms = new MemoryStream();
                //    if (x.Content != null)
                //    {
                //        await x.Content.CopyToAsync(ms).ConfigureAwait(false);
                //        var streamContent = new StreamContent(ms);
                //        var request = await streamContent.ReadAsStringAsync();
                //        TestContext.WriteLine(request);
                //    }
                //},
                //AfterResponseAsync = async (x) => TestContext.WriteLine(await x.Content.ReadAsStringAsync()),
            };

            var odataBatch = new ODataBatch(odataSettings);
            var odataClient = new ODataClient(odataSettings);

            Publisher publisher = null;
            Book book = null;

            odataBatch += async c =>
                publisher = await odataClient
                    .For<Publisher>()
                    .Key("Publisher1")
                    .FindEntryAsync();

            odataBatch += async c =>
            {
                book = await c
                    .Unbound<Book>()
                    .Function("PublishBook")
                    .Set(new { IsActive = true })
                    .ExecuteAsSingleAsync();
            };

            //RWM: This way should also work.
            //var payload = odataBatch.ToString();

            try
            {
                await odataBatch.ExecuteAsync();
            }
            catch (WebRequestException exception)
            {
                TestContext.WriteLine(exception.Response);
                throw;
            }

            publisher.Should().NotBeNull();
            publisher.Addr.Zip.Should().Be("00010");
            book.Should().NotBeNull();
            book.Title.Should().Be("The Cat in the Hat");
        }


    }

}
#endif