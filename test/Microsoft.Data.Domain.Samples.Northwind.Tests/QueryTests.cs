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
