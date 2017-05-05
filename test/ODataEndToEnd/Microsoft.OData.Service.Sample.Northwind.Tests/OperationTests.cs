using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.OData.Service.Sample.Northwind.Models;
using Microsoft.Restier.Tests;
using Xunit;

namespace Microsoft.OData.Service.Sample.Northwind.Tests
{
    public class OperationTests : TestBase
    {
        [Fact]
        public async Task AsyncFunctionCallWithFullName()
        {
            await AsyncFunctionCall(true, (config, server) => WebApiConfig.RegisterNorthwind(config, server));
        }

        [Fact]
        public async Task AsyncFunctionCallWithUnqualifiedName()
        {
            await AsyncFunctionCall(false, (config, server) =>
            {
                WebApiConfig.RegisterNorthwind2(config, server);
            });
        }

        private async Task AsyncFunctionCall(bool isqualified, Action<HttpConfiguration, HttpServer> registerOData)
        {
            var response = await ODataTestHelpers.GetResponseNoContentValidation(
                isqualified ?
                "http://localhost/api/Northwind/Products/Microsoft.OData.Service.Sample.Northwind.Models.MostExpensiveAsync"
                : "http://localhost/api/Northwind/Products/MostExpensiveAsync",
                HttpMethod.Get,
                null,
                registerOData,
                HttpStatusCode.OK,
                null);
        }

        [Fact]
        public async Task FunctionCallWithFullName()
        {
            await FunctionCall(true, (config, server) => WebApiConfig.RegisterNorthwind(config, server));
        }

        [Fact]
        public async Task FunctionCallWithUnqualifiedName()
        {
            await FunctionCall(false, (config, server) =>
                {
                    WebApiConfig.RegisterNorthwind2(config, server);
                });
        }

        private async Task FunctionCall(bool isqualified, Action<HttpConfiguration, HttpServer> registerOData)
        {
            var response = await ODataTestHelpers.GetResponseNoContentValidation(
                isqualified ?
                "http://localhost/api/Northwind/Products/Microsoft.OData.Service.Sample.Northwind.Models.MostExpensive"
                : "http://localhost/api/Northwind/Products/MostExpensive",
                HttpMethod.Get,
                null,
                registerOData,
                HttpStatusCode.OK,
                null);
        }

        [Fact]
        public async Task AsyncActionCallWithFullName()
        {
            await AsyncActionCall(false, (config, server) => { WebApiConfig.RegisterNorthwind(config, server); });
        }

        [Fact]
        public async Task AsyncActionCallWithUnqualifiedName()
        {
            await AsyncActionCall(true, (config, server) =>
            {
                WebApiConfig.RegisterNorthwind2(config, server);
            });
        }

        private async Task AsyncActionCall(bool isqualified, Action<HttpConfiguration, HttpServer> registerOData)
        {
            NorthwindContext ctx = GetDbContext();
            Product product = ctx.Products.First();

            var productID = product.ProductID;
            var price = product.UnitPrice;

            var response = await ODataTestHelpers.GetResponseNoContentValidation(
                isqualified ?
                string.Format("http://localhost/api/Northwind/Products({0})/IncreasePriceAsync", productID)
                : string.Format("http://localhost/api/Northwind/Products({0})/Microsoft.OData.Service.Sample.Northwind.Models.IncreasePriceAsync", productID),
                HttpMethod.Post,
                new StringContent(@"{""diff"":2}", UTF8Encoding.Default, "application/json"),
                registerOData,
                HttpStatusCode.NoContent,
                null);

            var getResponse = await ODataTestHelpers.GetResponseNoContentValidation(
                string.Format("http://localhost/api/Northwind/Products({0})", productID),
                HttpMethod.Get,
                null,
                (config, server) => { WebApiConfig.RegisterNorthwind(config, server); },
                HttpStatusCode.OK,
                null);
        }

        [Fact]
        public async Task ActionCallWithFullName()
        {
            await ActionCall(false, (config, server) => { WebApiConfig.RegisterNorthwind(config, server); });
        }

        [Fact]
        public async Task ActionCallWithUnqualifiedName()
        {
            await ActionCall(true, (config, server) =>
            {
                WebApiConfig.RegisterNorthwind2(config, server);
            });
        }

        private async Task ActionCall(bool isqualified, Action<HttpConfiguration, HttpServer> registerOData)
        {
            NorthwindContext ctx = GetDbContext();
            Product product = ctx.Products.First();
            
            var productID = product.ProductID;
            var price = product.UnitPrice;

            var response = await ODataTestHelpers.GetResponseNoContentValidation(
                isqualified ?
                string.Format("http://localhost/api/Northwind/Products({0})/IncreasePrice", productID)
                : string.Format("http://localhost/api/Northwind/Products({0})/Microsoft.OData.Service.Sample.Northwind.Models.IncreasePrice", productID),
                HttpMethod.Post,
                new StringContent(@"{""diff"":2}", UTF8Encoding.Default, "application/json"),
                registerOData,
                HttpStatusCode.NoContent,
                null);
            
            var getResponse = await ODataTestHelpers.GetResponseNoContentValidation(
                string.Format("http://localhost/api/Northwind/Products({0})", productID),
                HttpMethod.Get,
                null,
                (config, server) => { WebApiConfig.RegisterNorthwind(config, server); },
                HttpStatusCode.OK,
                null);
        }

        private static NorthwindContext GetDbContext()
        {
            return new NorthwindContext();
        }
    }
}
