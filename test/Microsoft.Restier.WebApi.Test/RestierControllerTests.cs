﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Xunit;

namespace Microsoft.Restier.WebApi.Test
{
    public class RestierControllerTests
    {
        private HttpClient client;

        public RestierControllerTests()
        {
            var configuration = new HttpConfiguration();
            configuration.MapRestierRoute<StoreApi>("store", "store").Wait();
            client = new HttpClient(new HttpServer(configuration));
        }

        [Fact]
        public async Task MetadataTest()
        {
            const string expected = @"<?xml version=""1.0"" encoding=""utf-8""?>
<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Microsoft.Restier.WebApi.Test"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityType Name=""Product"">
        <Key>
          <PropertyRef Name=""Id"" />
        </Key>
        <Property Name=""Id"" Type=""Edm.Int32"" Nullable=""false"" />
        <Property Name=""Name"" Type=""Edm.String"" />
        <Property Name=""Addr"" Type=""Microsoft.Restier.WebApi.Test.Address"" Nullable=""false"" />
        <Property Name=""Addr2"" Type=""Microsoft.Restier.WebApi.Test.Address"" />
        <Property Name=""Addr3"" Type=""Microsoft.Restier.WebApi.Test.Address"" />
      </EntityType>
      <ComplexType Name=""Address"">
        <Property Name=""Zip"" Type=""Edm.Int32"" Nullable=""false"" />
      </ComplexType>
      <Function Name=""GetBestProduct"">
        <ReturnType Type=""Microsoft.Restier.WebApi.Test.Product"" />
      </Function>
      <Action Name=""RemoveWorstProduct"">
        <ReturnType Type=""Microsoft.Restier.WebApi.Test.Product"" />
      </Action>
      <EntityContainer Name=""Container"">
        <EntitySet Name=""Products"" EntityType=""Microsoft.Restier.WebApi.Test.Product"" />
        <FunctionImport Name=""GetBestProduct"" Function=""Microsoft.Restier.WebApi.Test.GetBestProduct"" EntitySet=""Products"" IncludeInServiceDocument=""true"" />
        <ActionImport Name=""RemoveWorstProduct"" Action=""Microsoft.Restier.WebApi.Test.RemoveWorstProduct"" EntitySet=""Products"" />
      </EntityContainer>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";

            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/store/$metadata");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml"));
            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetTest()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/store/Products(1)");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetNonExistingEntityTest()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/store/Products(-1)");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostTest()
        {
            const string payload = "{'Name': 'var1', 'Addr':{'Zip':330}}";
            var request = new HttpRequestMessage(HttpMethod.Post, "http://host/store/Products")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task FunctionImportNotInModelShouldReturnNotFound()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/store/GetBestProduct2");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task FunctionImportNotInControllerShouldReturnNotImplemented()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/store/GetBestProduct");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            HttpResponseMessage response = await client.SendAsync(request);
            // TODO: Should throw 501 instead of 500.
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task ActionImportNotInModelShouldReturnNotFound()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "http://host/store/RemoveWorstProduct2");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ActionImportNotInControllerShouldReturnNotImplemented()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "http://host/store/RemoveWorstProduct");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        }

        [Fact]
        public async Task GetActionImportShouldReturnNotFound()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/store/RemoveWorstProduct");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PostFunctionImportShouldReturnNotFound()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "http://host/store/GetBestProduct");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
