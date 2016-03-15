// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Samples.Northwind.Models;
using Xunit;

namespace Microsoft.Restier.Samples.Northwind.Tests
{
    public class QueryTests : TestBase
    {
        private NorthwindApi api = new NorthwindApi();

        private IQueryable<Order> OrdersQuery
        {
            get { return this.api.Source<Order>("Orders"); }
        }

        [Fact]
        public async Task TestTakeIncludeTotalCount()
        {
            long totalCount = await this.api.QueryCountAsync(
                this.OrdersQuery.OrderBy(o => o.OrderDate));
            QueryResult result = await this.api.QueryAsync(
                new QueryRequest(this.OrdersQuery.OrderBy(o => o.OrderDate).Take(10)));

            Assert.Equal(830, totalCount);
            var orderResults = result.Results.OfType<Order>();
            Assert.Equal(10, orderResults.Count());
        }

        [Fact]
        public async Task TestSkipIncludeTotalCount()
        {
            long totalCount = await this.api.QueryCountAsync(
                this.OrdersQuery.OrderBy(o => o.OrderDate));
            QueryResult result = await this.api.QueryAsync(
                new QueryRequest(this.OrdersQuery.OrderBy(o => o.OrderDate).Skip(10)));

            Assert.Equal(830, totalCount);
            var orderResults = result.Results.OfType<Order>();
            Assert.Equal(820, orderResults.Count());
        }

        [Fact]
        public async Task TestSkipTakeIncludeTotalCount()
        {
            long totalCount = await this.api.QueryCountAsync(
                this.OrdersQuery.OrderBy(o => o.OrderDate));
            QueryResult result = await this.api.QueryAsync(
                new QueryRequest(this.OrdersQuery.OrderBy(o => o.OrderDate).Skip(10).Take(25)));

            Assert.Equal(830, totalCount);
            var orderResults = result.Results.OfType<Order>();
            Assert.Equal(25, orderResults.Count());
        }

        /// <summary>
        /// Tests executing a query that has a Take method before other operators.
        /// This ensures Take methods are not stripped on the TotalCount query if they don't appear at the end.
        /// </summary>
        [Fact]
        public async Task TestTakeNotStrippedIncludeTotalCount()
        {
            long totalCount = await this.api.QueryCountAsync(
                this.OrdersQuery.OrderBy(o => o.OrderDate).Take(10));
            QueryResult result = await this.api.QueryAsync(
                new QueryRequest(this.OrdersQuery.Take(10).OrderBy(o => o.OrderDate)));

            Assert.Equal(10, totalCount);
            var orderResults = result.Results.OfType<Order>();
            Assert.Equal(10, orderResults.Count());
        }
    }
}
