// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Extensions;
using Microsoft.OData.Service.Sample.Northwind.Models;
using Microsoft.Restier.Tests;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.OData.Service.Sample.Northwind.Tests
{
    public class ODataFeedTests : TestBase
    {
        [Fact]
        public async Task TestGetNorthwindMetadata()
        {
            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/$metadata");
        }

        [Fact]
        public async Task TestCustomersEntitySetQuery()
        {
            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/Customers");
        }

        [Fact]
        public async Task TestOrdersEntitySetAutoExpandQuery()
        {
            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/Orders?$top=5");
        }

        [Fact]
        public async Task TestCustomersEntitySetTopSkipQuery()
        {
            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/Customers?$top=5&$skip=1");
        }

        [Fact]
        public async Task TestCustomersEntitySetCountQuery()
        {
            // The count should NOT include the entities that have been filtered out.
            // In this case, only count of French customers should be returned.
            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/Customers/$count");
        }

        [Fact]
        public async Task TestCustomerKeyAsSegment()
        {
            // BLONP is the key for a customer
            var requestUri = "http://localhost/api/Northwind/Customers/BLONP";

            //Enable Key as Segment
            HttpConfiguration httpConfig = new HttpConfiguration();
            httpConfig.SetUrlKeyDelimiter(ODataUrlKeyDelimiter.Slash);

            Action<HttpConfiguration, HttpServer> registerOData = (config, server) => WebApiConfig.RegisterNorthwind(config, server);
            string baselineFileName = "TestCustomerKeyAsSegment";
            using (HttpResponseMessage response = await ODataTestHelpers.GetResponseWithConfig(requestUri, HttpMethod.Get, null, httpConfig, registerOData, HttpStatusCode.OK, baselineFileName,null))
            {
            }
        }

        [Fact]
        public async Task TestBatch()
        {
            int id = ODataFeedTests.InsertTestProduct();

            NorthwindContext ctx2 = GetDbContext();
            Product[] insertedProducts2 = ctx2.Products
                .Where(b => b.ProductName == "Horizon" || b.ProductName == "Commons")
                .OrderBy(b => b.ProductName)
                .ToArray();
            ctx2.Products.RemoveRange(insertedProducts2);
            ctx2.SaveChanges();

            string batchContentString =
@"--batch_35114042-958d-48fd-8189-bd93264b31de
Content-Type: multipart/mixed; boundary=changeset_3ffaecfa-069f-4ad7-bb41-bcc2481ea0dd

--changeset_3ffaecfa-069f-4ad7-bb41-bcc2481ea0dd
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 1

POST http://localhost/api/Northwind/Products HTTP/1.1
OData-Version: 4.0;NetFx
OData-MaxVersion: 4.0;NetFx
Content-Type: application/json;odata.metadata=minimal
Accept: application/json;odata.metadata=minimal
Accept-Charset: UTF-8
User-Agent: Microsoft ADO.NET Data Services

{'@odata.type':'#Microsoft.OData.Service.Sample.Northwind.Models.Product','CategoryID':null,'Discontinued':false,'ProductID':0,'ProductName':'Horizon','QuantityPerUnit':'4','ReorderLevel':10,'SupplierID':null,'UnitPrice':2.5,'UnitsInStock':100,'UnitsOnOrder':0}
--changeset_3ffaecfa-069f-4ad7-bb41-bcc2481ea0dd
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 2

POST http://localhost/api/Northwind/Products HTTP/1.1
OData-Version: 4.0;NetFx
OData-MaxVersion: 4.0;NetFx
Content-Type: application/json;odata.metadata=minimal
Accept: application/json;odata.metadata=minimal
Accept-Charset: UTF-8
User-Agent: Microsoft ADO.NET Data Services

{'@odata.type':'#Microsoft.OData.Service.Sample.Northwind.Models.Product','CategoryID':null,'Discontinued':true,'ProductID':0,'ProductName':'Commons','QuantityPerUnit':'5','ReorderLevel':11,'SupplierID':null,'UnitPrice':15.99,'UnitsInStock':200,'UnitsOnOrder':10}
--changeset_3ffaecfa-069f-4ad7-bb41-bcc2481ea0dd
Content-Type: application/http
Content-Transfer-Encoding: binary
Content-ID: 4

DELETE http://localhost/api/Northwind/Products(" + id.ToString() + @") HTTP/1.1
OData-Version: 4.0;NetFx
OData-MaxVersion: 4.0;NetFx
Accept: application/json;odata.metadata=minimal
Accept-Charset: UTF-8
User-Agent: Microsoft ADO.NET Data Services


--changeset_3ffaecfa-069f-4ad7-bb41-bcc2481ea0dd--
--batch_35114042-958d-48fd-8189-bd93264b31de--
";

            StringContent batchContent = new StringContent(batchContentString, UTF8Encoding.Default);
            batchContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/mixed");
            batchContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", "batch_35114042-958d-48fd-8189-bd93264b31de"));

            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/$batch", HttpMethod.Post, batchContent, HttpStatusCode.OK, null, NormalizeBatch);

            ODataFeedTests.EnsureProductDeleted(id);

            NorthwindContext ctx = GetDbContext();
            Product[] insertedProducts = ctx.Products
                .Where(b => b.ProductName == "Horizon" || b.ProductName == "Commons")
                .OrderBy(b => b.ProductName)
                .ToArray();

            Assert.Equal(2, insertedProducts.Length);

            Assert.Equal("Commons", insertedProducts[0].ProductName);
            Assert.Equal(true, insertedProducts[0].Discontinued);
            Assert.Equal("5", insertedProducts[0].QuantityPerUnit);
            Assert.Equal((short)11, insertedProducts[0].ReorderLevel);
            Assert.Equal(15.99m, insertedProducts[0].UnitPrice);
            Assert.Equal((short)200, insertedProducts[0].UnitsInStock);
            Assert.Equal((short)10, insertedProducts[0].UnitsOnOrder);

            Assert.Equal("Horizon", insertedProducts[1].ProductName);
            Assert.Equal(false, insertedProducts[1].Discontinued);
            Assert.Equal("4", insertedProducts[1].QuantityPerUnit);
            Assert.Equal((short)10, insertedProducts[1].ReorderLevel);
            Assert.Equal(2.5m, insertedProducts[1].UnitPrice);
            Assert.Equal((short)100, insertedProducts[1].UnitsInStock);
            Assert.Equal((short)0, insertedProducts[1].UnitsOnOrder);

            ctx.Products.RemoveRange(insertedProducts);
            ctx.SaveChanges();
        }

        private static string NormalizeBatch(string input)
        {
            input = NormalizeProductUrl(input);
            input = NormalizeProductId(input);
            return NormalizeBatchDelimiters(input);
        }

        private static string NormalizeProductUrl(string input)
        {
            const string regexPattern = @"http://localhost/api/Northwind/Products\([0-9]+\)";
            const string replacementString = @"http://localhost/api/Northwind/Products(XX)";
            return Regex.Replace(input, regexPattern, replacementString);
        }

        private static string NormalizeBatchDelimiters(string input)
        {
            const string regexPattern = @"(?<responseName>changesetresponse|batchresponse)_([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}";
            const string replacementString = "${responseName}_XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX";
            return Regex.Replace(input, regexPattern, replacementString);
        }

        [Fact]
        public async Task TestPutProduct()
        {
            await TestPut(null, HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task TestPutProductReturnContent()
        {
            KeyValuePair<string, string>[] headers = { new KeyValuePair<string, string>("prefer", "return=representation") };
            await TestPut(headers, HttpStatusCode.OK);
        }

        private static async Task TestPut(IEnumerable<KeyValuePair<string, string>> headers, HttpStatusCode expectedStatusCode, [CallerMemberName]string baselineFileName = null)
        {
            int id = ODataFeedTests.InsertTestProduct();

            // NOTE: explicitly leaving CategoryID, Discontinued, QuantityPerUnit and UnitPrice
            // out of the PUT request content to test these values get set to their default value
            string putContentString = @"{
    ""@odata.type"":""#Microsoft.OData.Service.Sample.Northwind.Models.Product"",
    ""ProductName"":""TestPut"",
    ""ReorderLevel"":23,
    ""UnitsInStock"":15,
    ""UnitsOnOrder"":1,
    ""SupplierID"":1
}";

            StringContent putContent = new StringContent(putContentString, UTF8Encoding.Default, "application/json");

            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/Products(" + id + ")", new HttpMethod("PUT"), putContent, expectedStatusCode, headers, NormalizeProductId, baselineFileName);

            NorthwindContext ctx = GetDbContext();
            Product product = ctx.Products.FirstOrDefault(e => e.ProductID == id);

            Assert.Equal("TestPut", product.ProductName);
            Assert.Equal(false, product.Discontinued);
            Assert.Null(product.QuantityPerUnit);
            Assert.Equal((short)23, product.ReorderLevel);
            Assert.Null(product.UnitPrice);
            Assert.Equal((short)15, product.UnitsInStock);
            Assert.Equal((short)1, product.UnitsOnOrder);
            Assert.Equal(1, product.SupplierID);
            Assert.Null(product.CategoryID);

            ctx.Products.Remove(product);
            ctx.SaveChanges();
        }

        [Fact]
        public async Task TestPatchProduct()
        {
            await TestPatch(null, HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task TestPatchProductReturnContent()
        {
            KeyValuePair<string, string>[] headers = { new KeyValuePair<string, string>("prefer", "return=representation") };
            await TestPatch(headers, HttpStatusCode.OK);
        }

        private static async Task TestPatch(IEnumerable<KeyValuePair<string, string>> headers, HttpStatusCode expectedStatusCode, [CallerMemberName]string baselineFileName = null)
        {
            int id = ODataFeedTests.InsertTestProduct();

            string patchContentString = @"{
    ""@odata.type"":""#Microsoft.OData.Service.Sample.Northwind.Models.Product"",
    ""ProductName"":""Commons""
}";

            StringContent patchContent = new StringContent(patchContentString, UTF8Encoding.Default, "application/json");

            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/Products(" + id + ")", new HttpMethod("PATCH"), patchContent, expectedStatusCode, headers, NormalizeProductId, baselineFileName);

            NorthwindContext ctx = GetDbContext();
            Product product = ctx.Products.FirstOrDefault(e => e.ProductID == id);

            Assert.Equal("Commons", product.ProductName);
            Assert.Equal(true, product.Discontinued);
            Assert.Equal("95", product.QuantityPerUnit);
            Assert.Equal((short)68, product.ReorderLevel);
            Assert.Equal(5.6m, product.UnitPrice);
            Assert.Equal((short)40, product.UnitsInStock);
            Assert.Equal((short)13, product.UnitsOnOrder);

            ctx.Products.Remove(product);
            ctx.SaveChanges();
        }

        private static string NormalizeProductId(string input)
        {
            const string regexPattern = @"""ProductID"":[0-9]+,";
            const string replacementString = @"""ProductID"":XX,";
            return Regex.Replace(input, regexPattern, replacementString);
        }

        private static int InsertTestProduct()
        {
            NorthwindContext ctx = GetDbContext();
#if EF7
            Product product = new Product();
#else
            Product product = ctx.Products.Create();
#endif
            product.ProductName = "Deleted";
            product.Discontinued = true;
            product.QuantityPerUnit = "95";
            product.ReorderLevel = 68;
            product.UnitPrice = 5.6m;
            product.UnitsInStock = 40;
            product.UnitsOnOrder = 13;
            ctx.Products.Add(product);
            ctx.SaveChanges();

            return product.ProductID;
        }

        private static void EnsureProductDeleted(int productId)
        {
            NorthwindContext ctx = GetDbContext();
            Product deletedProduct = ctx.Products.SingleOrDefault(p => p.ProductID == productId);
            Assert.Null(deletedProduct);
        }

        [Fact]
        public async Task TestPostOrderInvalidShipVia()
        {
            dynamic order = new ExpandoObject();
            ((IDictionary<string, object>)order)["@odata.type"] = "#Microsoft.OData.Service.Sample.Northwind.Models.Order";
            order.CustomerID = "ALFKI";
            order.Freight = 35.5m;
            order.ShipVia = 15;

            string newOrderContent = JsonConvert.SerializeObject(order);
            StringContent content = new StringContent(newOrderContent, UTF8Encoding.Default, "application/json");

            await ODataFeedTests.TestODataRequest(
                "http://localhost/api/Northwind/Orders",
                HttpMethod.Post,
                content,
                HttpStatusCode.BadRequest);

            NorthwindContext ctx = GetDbContext();
            Order notInsertedOrder = ctx.Orders.FirstOrDefault(o => o.Freight == 35.5m);
            Assert.Null(notInsertedOrder);
        }

        private static Task TestODataRequest(
            string requestUri,
            [CallerMemberName] string baselineFileName = "")
        {
            return TestODataRequest(requestUri, HttpMethod.Get, null, HttpStatusCode.OK, null, null, baselineFileName);
        }

        private static async Task TestODataRequest(
            string requestUri,
            HttpMethod httpMethod,
            HttpContent requestContent,
            HttpStatusCode expectedStatusCode,
            IEnumerable<KeyValuePair<string, string>> headers = null,
            Func<string, string> postProcessContentHandler = null,
            [CallerMemberName] string baselineFileName = "")
        {
            await ODataTestHelpers.TestRequest(
                requestUri,
                httpMethod,
                requestContent,
                expectedStatusCode,
                (config, server) => WebApiConfig.RegisterNorthwind(config, server),
                headers,
                postProcessContentHandler,
                baselineFileName);
        }

        private static NorthwindContext GetDbContext()
        {
            return new NorthwindContext();
        }
    }
}
