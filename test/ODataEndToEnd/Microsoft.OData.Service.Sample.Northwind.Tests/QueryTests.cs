// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Service.Sample.Northwind.Models;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Xunit;

namespace Microsoft.OData.Service.Sample.Northwind.Tests
{
    public class QueryTests : TestBase
    {
        [Fact]
        public async Task TestTakeIncludeTotalCount()
        {
            using (HttpConfiguration config = new HttpConfiguration())
            {
                using (HttpServer server = new HttpServer(config))
                {
                    WebApiConfig.RegisterNorthwind(config, server); var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
                    request.SetConfiguration(config);
                    var api = request.CreateRequestContainer("NorthwindApi").GetService<ApiBase>();
                    QueryResult result = await api.QueryAsync(
                        new QueryRequest(api.GetQueryableSource<Order>("Orders").OrderBy(o => o.OrderDate).Take(10)));

                    var orderResults = result.Results.OfType<Order>();
                    Assert.Equal(10, orderResults.Count());
                }
            }
        }

        [Fact]
        public async Task TestSkipIncludeTotalCount()
        {
            using (HttpConfiguration config = new HttpConfiguration())
            {
                using (HttpServer server = new HttpServer(config))
                {
                    WebApiConfig.RegisterNorthwind(config, server);
                    var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
                    request.SetConfiguration(config);
                    var api = request.CreateRequestContainer("NorthwindApi").GetService<ApiBase>();
                    QueryResult result = await api.QueryAsync(
                        new QueryRequest(api.GetQueryableSource<Order>("Orders").OrderBy(o => o.OrderDate).Skip(10)));

                    var orderResults = result.Results.OfType<Order>();
                    Assert.Equal(820, orderResults.Count());
                }
            }
        }

        [Fact]
        public async Task TestSkipTakeIncludeTotalCount()
        {
            using (HttpConfiguration config = new HttpConfiguration())
            {
                using (HttpServer server = new HttpServer(config))
                {
                    WebApiConfig.RegisterNorthwind(config, server);
                    var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
                    request.SetConfiguration(config);
                    var api = request.CreateRequestContainer("NorthwindApi").GetService<ApiBase>();
                    QueryResult result = await api.QueryAsync(
                        new QueryRequest(api.GetQueryableSource<Order>("Orders").OrderBy(o => o.OrderDate).Skip(10).Take(25)));

                    var orderResults = result.Results.OfType<Order>();
                    Assert.Equal(25, orderResults.Count());
                }
            }
        }

        /// <summary>
        /// Tests executing a query that has a Take method before other operators.
        /// This ensures Take methods are not stripped on the TotalCount query if they don't appear at the end.
        /// </summary>
        [Fact]
        public async Task TestTakeNotStrippedIncludeTotalCount()
        {
            using (HttpConfiguration config = new HttpConfiguration())
            {
                using (HttpServer server = new HttpServer(config))
                {
                    WebApiConfig.RegisterNorthwind(config, server);
                    var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");
                    request.SetConfiguration(config);
                    var api = request.CreateRequestContainer("NorthwindApi").GetService<ApiBase>();
                    QueryResult result = await api.QueryAsync(
                        new QueryRequest(api.GetQueryableSource<Order>("Orders").Take(10).OrderBy(o => o.OrderDate)));

                    var orderResults = result.Results.OfType<Order>();
                    Assert.Equal(10, orderResults.Count());
                }
            }
        }
    }
}
