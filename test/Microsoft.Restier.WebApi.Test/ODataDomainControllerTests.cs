// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Xunit;

namespace Microsoft.Restier.WebApi.Test
{
    public class ODataDomainControllerTests
    {
        private HttpClient client;

        public ODataDomainControllerTests()
        {
            var configuration = new HttpConfiguration();
            configuration.MapODataDomainRoute<StoreDomainController>("store", "store").Wait();
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
    </Schema>
    <Schema Namespace=""Default"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityContainer Name=""Container"">
        <EntitySet Name=""Products"" EntityType=""Microsoft.Restier.WebApi.Test.Product"" />
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
    }

    public static class StoreModel
    {
        public static EdmModel Model { get; private set; }

        public static IEdmEntityType Product { get; private set; }

        static StoreModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Product>("Products");
            Model = (EdmModel)builder.GetEdmModel();
            Product = (IEdmEntityType)Model.FindType("Microsoft.Restier.WebApi.Test.Product");
        }
    }

    public class StoreDomainController : ODataDomainController<StoreDomain>
    {
    }

    public class StoreDomain : DomainBase
    {
        protected override DomainConfiguration CreateDomainConfiguration()
        {
            var configuration = base.CreateDomainConfiguration();
            configuration.SetHookPoint(typeof(IModelProducer), new TestModelProducer(StoreModel.Model));
            configuration.SetHookPoint(typeof(IModelMapper), new TestModelMapper());
            configuration.SetHookPoint(typeof(IQueryExecutor), new TestQueryExecutor());
            configuration.SetHookPoint(typeof(IQueryExpressionSourcer), new TestQueryExpressionSourcer());
            configuration.SetHookPoint(typeof(ISubmitHandler), new TestSubmitHandler());
            return configuration;
        }
    }

    class Product
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [Required]
        public Address Addr { get; set; }

        public Address Addr2 { get; set; }

        public Address Addr3 { get; set; }
    }

    class Address
    {
        public int Zip { get; set; }
    }

    class TestModelMapper : IModelMapper
    {
        public bool TryGetRelevantType(DomainContext context, string name, out Type relevantType)
        {
            relevantType = typeof(Product);
            return true;
        }

        public bool TryGetRelevantType(DomainContext context, string namespaceName, string name, out Type relevantType)
        {
            relevantType = typeof(Product);
            return true;
        }
    }

    class TestModelProducer : IModelProducer
    {
        private EdmModel model;

        public TestModelProducer(EdmModel model)
        {
            this.model = model;
        }

        public Task<EdmModel> ProduceModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(model);
        }
    }

    class TestQueryExecutor : IQueryExecutor
    {
        public Task<QueryResult> ExecuteQueryAsync<TElement>(
            QueryContext context,
            IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new QueryResult(query.ToList()));
        }

        public Task<QueryResult> ExecuteSingleAsync<TResult>(QueryContext context,
            IQueryable query,
            Expression expression,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new QueryResult(new[] { query.Provider.Execute(expression) }));
        }
    }

    class TestQueryExpressionSourcer : IQueryExpressionSourcer
    {
        public Expression Source(QueryExpressionContext context, bool embedded)
        {
            var a = new[] { new Product
            {
                Id = 1,
                Addr = new Address { Zip = 0001 },
                Addr2= new Address { Zip = 0002 }
            } };

            if (!embedded)
            {
                return Expression.Constant(a.AsQueryable());
            }
            return context.VisitedNode;
        }
    }

    class TestSubmitHandler : ISubmitHandler
    {
        public Task<SubmitResult> SubmitAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            foreach (var entry in context.ChangeSet.Entries.OfType<DataModificationEntry>())
            {
                if (entry.LocalValues.All(l => l.Key != "Addr"))
                {
                    throw new Exception("Addr is required.");
                }

                entry.Entity = new Product
                {
                    Id = 1,
                    Addr = new Address { Zip = 0001 },
                    Addr2 = new Address { Zip = 0002 }
                };
            }

            return Task.FromResult(new SubmitResult(new ChangeSet()));
        }
    }
}
