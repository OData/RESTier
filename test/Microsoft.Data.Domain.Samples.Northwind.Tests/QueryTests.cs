// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Domain.Query;
using Microsoft.Data.Domain.Samples.Northwind.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Domain.Samples.Northwind.Tests
{
    [TestClass]
    public class QueryTests
    {
        private NorthwindDomain domain = new NorthwindDomain();

        private IQueryable<Order> OrdersQuery
        {
            get { return this.domain.Source<Order>("Orders"); }
        }

        [TestMethod]
        public async Task TestTakeIncludeTotalCount()
        {
            QueryResult result = await this.domain.QueryAsync(
                new QueryRequest(this.OrdersQuery.OrderBy(o => o.OrderDate).Take(10), true));

            Assert.AreEqual(830, result.TotalCount);
            var orderResults = result.Results.OfType<Order>();
            Assert.AreEqual(10, orderResults.Count());
        }

        [TestMethod]
        public async Task TestSkipIncludeTotalCount()
        {
            QueryResult result = await this.domain.QueryAsync(
                new QueryRequest(this.OrdersQuery.OrderBy(o => o.OrderDate).Skip(10), true));

            Assert.AreEqual(830, result.TotalCount);
            var orderResults = result.Results.OfType<Order>();
            Assert.AreEqual(820, orderResults.Count());
        }

        [TestMethod]
        public async Task TestSkipTakeIncludeTotalCount()
        {
            QueryResult result = await this.domain.QueryAsync(
                new QueryRequest(this.OrdersQuery.OrderBy(o => o.OrderDate).Skip(10).Take(25), true));

            Assert.AreEqual(830, result.TotalCount);
            var orderResults = result.Results.OfType<Order>();
            Assert.AreEqual(25, orderResults.Count());
        }

        /// <summary>
        /// Tests executing a query that has a Take method before other operators.
        /// This ensures Take methods are not stripped on the TotalCount query if they don't appear at the end.
        /// </summary>
        [TestMethod]
        public async Task TestTakeNotStrippedIncludeTotalCount()
        {
            QueryResult result = await this.domain.QueryAsync(
                new QueryRequest(this.OrdersQuery.Take(10).OrderBy(o => o.OrderDate), true));

            Assert.AreEqual(10, result.TotalCount);
            var orderResults = result.Results.OfType<Order>();
            Assert.AreEqual(10, orderResults.Count());
        }
    }
}
