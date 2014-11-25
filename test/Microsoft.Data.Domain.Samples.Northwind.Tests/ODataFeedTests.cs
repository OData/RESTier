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
using Microsoft.Data.Domain.Samples.Northwind;
using Microsoft.Data.Domain.Samples.Northwind.Models;
using Microsoft.Data.Domain.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace NorthwindService.Tests
{
    [TestClass]
    public class ODataFeedTests
    {
        [TestMethod]
        public async Task TestGetNorthwindMetadata()
        {
            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/$metadata");
        }

        [TestMethod]
        public async Task TestCustomersEntitySetQuery()
        {
            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/Customers");
        }

        [TestMethod]
        public async Task TestCustomersEntitySetTopSkipQuery()
        {
            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/Customers?$top=5&$skip=1");
        }

        [TestMethod]
        public async Task TestBatch()
        {
            int id = ODataFeedTests.InsertTestProduct();

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

{""@odata.type"":""#Microsoft.Data.Domain.Samples.Northwind.Models.Product"",""CategoryID"":null,""Discontinued"":false,""ProductID"":0,""ProductName"":""Horizon"",""QuantityPerUnit"":""4"",""ReorderLevel"":10,""SupplierID"":null,""UnitPrice"":2.5,""UnitsInStock"":100,""UnitsOnOrder"":0}
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

{""@odata.type"":""#Microsoft.Data.Domain.Samples.Northwind.Models.Product"",""CategoryID"":null,""Discontinued"":true,""ProductID"":0,""ProductName"":""Commons"",""QuantityPerUnit"":""5"",""ReorderLevel"":11,""SupplierID"":null,""UnitPrice"":15.99,""UnitsInStock"":200,""UnitsOnOrder"":10}
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

            Assert.AreEqual(2, insertedProducts.Length);

            Assert.AreEqual("Commons", insertedProducts[0].ProductName);
            Assert.AreEqual(true, insertedProducts[0].Discontinued);
            Assert.AreEqual("5", insertedProducts[0].QuantityPerUnit);
            Assert.AreEqual((short)11, insertedProducts[0].ReorderLevel);
            Assert.AreEqual(15.99m, insertedProducts[0].UnitPrice);
            Assert.AreEqual((short)200, insertedProducts[0].UnitsInStock);
            Assert.AreEqual((short)10, insertedProducts[0].UnitsOnOrder);

            Assert.AreEqual("Horizon", insertedProducts[1].ProductName);
            Assert.AreEqual(false, insertedProducts[1].Discontinued);
            Assert.AreEqual("4", insertedProducts[1].QuantityPerUnit);
            Assert.AreEqual((short)10, insertedProducts[1].ReorderLevel);
            Assert.AreEqual(2.5m, insertedProducts[1].UnitPrice);
            Assert.AreEqual((short)100, insertedProducts[1].UnitsInStock);
            Assert.AreEqual((short)0, insertedProducts[1].UnitsOnOrder);

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

        [TestMethod]
        public async Task TestPutProduct()
        {
            await TestPut(null, HttpStatusCode.NoContent);
        }

        [TestMethod]
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
    ""@odata.type"":""#Microsoft.Data.Domain.Samples.Northwind.Models.Product"",
	""ProductName"":""TestPut"",
	""ReorderLevel"":23,
	""UnitsInStock"":15,
	""UnitsOnOrder"":1,
	""SupplierID"":1
}";

            StringContent putContent = new StringContent(putContentString, UTF8Encoding.Default, "application/json");

            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/Products(" + id + ")", new HttpMethod("PUT"), putContent, expectedStatusCode, headers, NormalizeProductId, baselineFileName);

            NorthwindContext ctx = GetDbContext();
            Product product = ctx.Products.Find(id);

            Assert.AreEqual("TestPut", product.ProductName);
            Assert.AreEqual(false, product.Discontinued);
            Assert.IsNull(product.QuantityPerUnit);
            Assert.AreEqual((short)23, product.ReorderLevel);
            Assert.IsNull(product.UnitPrice);
            Assert.AreEqual((short)15, product.UnitsInStock);
            Assert.AreEqual((short)1, product.UnitsOnOrder);
            Assert.AreEqual(1, product.SupplierID);
            Assert.IsNull(product.CategoryID);

            ctx.Products.Remove(product);
            ctx.SaveChanges();
        }

        [TestMethod]
        public async Task TestPatchProduct()
        {
            await TestPatch(null, HttpStatusCode.NoContent);
        }

        [TestMethod]
        public async Task TestPatchProductReturnContent()
        {
            KeyValuePair<string, string>[] headers = { new KeyValuePair<string, string>("prefer", "return=representation") };
            await TestPatch(headers, HttpStatusCode.OK);
        }

        private static async Task TestPatch(IEnumerable<KeyValuePair<string, string>> headers, HttpStatusCode expectedStatusCode, [CallerMemberName]string baselineFileName = null)
        {
            int id = ODataFeedTests.InsertTestProduct();

            string patchContentString = @"{
    ""@odata.type"":""#Microsoft.Data.Domain.Samples.Northwind.Models.Product"",
	""ProductName"":""Commons""
}";

            StringContent patchContent = new StringContent(patchContentString, UTF8Encoding.Default, "application/json");

            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/Products(" + id + ")", new HttpMethod("PATCH"), patchContent, expectedStatusCode, headers, NormalizeProductId, baselineFileName);

            NorthwindContext ctx = GetDbContext();
            Product product = ctx.Products.Find(id);

            Assert.AreEqual("Commons", product.ProductName);
            Assert.AreEqual(true, product.Discontinued);
            Assert.AreEqual("95", product.QuantityPerUnit);
            Assert.AreEqual((short)68, product.ReorderLevel);
            Assert.AreEqual(5.6m, product.UnitPrice);
            Assert.AreEqual((short)40, product.UnitsInStock);
            Assert.AreEqual((short)13, product.UnitsOnOrder);

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
            Product product = ctx.Products.Create();
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
            Assert.IsNull(deletedProduct);
        }

        [TestMethod]
        public async Task TestPostOrderInvalidShipVia()
        {
            dynamic order = new ExpandoObject();
            ((IDictionary<string, object>)order)["@odata.type"] = "#Microsoft.Data.Domain.Samples.Northwind.Models.Order";
            order.CustomerID = "ALFKI";
            order.Freight = 35.5m;
            order.ShipVia = 15;

            string newOrderContent = JsonConvert.SerializeObject(order);
            StringContent content = new StringContent(newOrderContent, UTF8Encoding.Default, "application/json");

            await ODataFeedTests.TestODataRequest("http://localhost/api/Northwind/Orders", HttpMethod.Post, content, HttpStatusCode.InternalServerError);

            NorthwindContext ctx = GetDbContext();
            Order notInsertedOrder = ctx.Orders.FirstOrDefault(o => o.Freight == 35.5m);
            Assert.IsNull(notInsertedOrder, "The Order should not have been inserted.");
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
