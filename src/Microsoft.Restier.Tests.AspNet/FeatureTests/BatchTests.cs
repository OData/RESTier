using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Restier.Breakdance;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Simple.OData.Client;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared;
using System.Threading;

#if NETCOREAPP3_1_OR_GREATER

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests
#else

using CloudNimble.Breakdance.WebApi;
using System.Web.Http;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
#endif

{

    [TestClass]
    public class BatchTests : RestierTestBase
#if NETCOREAPP3_1_OR_GREATER
        <LibraryApi>
#endif

    {

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task BatchTests_AddMultipleEntries()
        {
#if NETCOREAPP3_1_OR_GREATER
            var httpClient = await RestierTestHelpers.GetTestableHttpClient<LibraryApi>(serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
#else
            var config = await RestierTestHelpers.GetTestableRestierConfiguration<LibraryApi>(serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>()).ConfigureAwait(false);
            var httpClient = config.GetTestableHttpClient();
            httpClient.BaseAddress = new Uri($"{WebApiConstants.Localhost}{WebApiConstants.RoutePrefix}");
#endif

            var odataSettings = new ODataClientSettings(httpClient, new Uri("", UriKind.Relative))
            {
                OnTrace = (x, y) => TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture, x, y)),
                // RWM: Need a batter way to capture the payload... this event fires before the payload is written to the stream.
                //BeforeRequestAsync = async (x) => {
                //    var ms = new MemoryStream();
                //    if (x.Content is not null)
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

            Thread.Sleep(5000);
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/Books?$expand=Publisher", serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
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
#if NETCOREAPP3_1_OR_GREATER
            var httpClient = await RestierTestHelpers.GetTestableHttpClient<LibraryApi>(serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
#else
            var config = await RestierTestHelpers.GetTestableRestierConfiguration<LibraryApi>(serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>()).ConfigureAwait(false);
            var httpClient = config.GetTestableHttpClient();
            httpClient.BaseAddress = new Uri($"{WebApiConstants.Localhost}{WebApiConstants.RoutePrefix}");
#endif

            var odataSettings = new ODataClientSettings(httpClient, new Uri("", UriKind.Relative))
            {
                OnTrace = (x, y) => TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture, x, y)),
                // RWM: Need a batter way to capture the payload... this event fires before the payload is written to the stream.
                //BeforeRequestAsync = async (x) => {
                //    var ms = new MemoryStream();
                //    if (x.Content is not null)
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
