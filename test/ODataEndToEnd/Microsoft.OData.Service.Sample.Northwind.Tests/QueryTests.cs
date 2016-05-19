// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.OData.Service.Sample.Northwind.Models;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Xunit;

namespace Microsoft.OData.Service.Sample.Northwind.Tests
{
    public class QueryTests : TestBase
    {
        private NorthwindApi api = new NorthwindApi();

        private IQueryable<Order> OrdersQuery
        {
            get { return this.api.GetQueryableSource<Order>("Orders"); }
        }

        [Fact]
        public async Task TestTakeIncludeTotalCount()
        {
            using (HttpConfiguration config = new HttpConfiguration())
            {
                using (HttpServer server = new HttpServer(config))
                {
                    WebApiConfig.RegisterNorthwind(config, server);
                    QueryResult result = await this.api.QueryAsync(
                        new QueryRequest(this.OrdersQuery.OrderBy(o => o.OrderDate).Take(10)));

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
                    QueryResult result = await this.api.QueryAsync(
                        new QueryRequest(this.OrdersQuery.OrderBy(o => o.OrderDate).Skip(10)));

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
                    QueryResult result = await this.api.QueryAsync(
                        new QueryRequest(this.OrdersQuery.OrderBy(o => o.OrderDate).Skip(10).Take(25)));

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
                    QueryResult result = await this.api.QueryAsync(
                        new QueryRequest(this.OrdersQuery.Take(10).OrderBy(o => o.OrderDate)));

                    var orderResults = result.Results.OfType<Order>();
                    Assert.Equal(10, orderResults.Count());
                }
            }
        }
    }
}
