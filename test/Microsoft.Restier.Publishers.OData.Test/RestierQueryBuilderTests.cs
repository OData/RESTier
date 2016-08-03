// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;

namespace Microsoft.Restier.Publishers.OData.Test
{
    public class RestierQueryBuilderTests
    {
        private HttpClient client;

        public RestierQueryBuilderTests()
        {
            var configuration = new HttpConfiguration();
            configuration.MapRestierRoute<StoreApi>("store", "store").Wait();
            client = new HttpClient(new HttpServer(configuration));
        }

        [Fact]
        public async Task TestInt16AsKey()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/store/Customers(1)");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task TestInt64AsKey()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/store/Stores(1)");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
