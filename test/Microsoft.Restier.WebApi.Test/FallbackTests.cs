// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Builder;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.WebApi.Results;
using Xunit;

namespace Microsoft.Restier.WebApi.Test
{
    public class FallbackTests
    {
        private HttpClient client;

        public FallbackTests()
        {
            var configuration = new HttpConfiguration();
            configuration.MapODataDomainRoute<FallbackDomain>("fallback", "fallback").Wait();
            client = new HttpClient(new HttpServer(configuration));
        }

        [Fact]
        public async Task FallbackEntitySetTest()
        {
            // Should fallback to PeopleController.
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/fallback/People");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(999, ((Person[])((ObjectContent)response.Content).Value).Single().Id);
        }

        [Fact]
        public async Task FallbackNavigationPropertyTest()
        {
            // Should fallback to PeopleController.
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/fallback/People(1)/Orders");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(123, ((Order[])((ObjectContent)response.Content).Value).Single().Id);
        }

        [Fact]
        public async Task NonFallbackTest()
        {
            // Should be routed to ODataDomainController.
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/fallback/Orders");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(234, ((Order)((EntityCollectionResult)((ObjectContent)response.Content).Value).Query.SingleOrDefault()).Id);
        }

        [Fact]
        public async Task FallbackConventionalProviderTest()
        {
            // Should be routed to ODataDomainController.
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/fallback/PreservedOrders");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(234, ((Order)((EntityCollectionResult)((ObjectContent)response.Content).Value).Query.SingleOrDefault()).Id);
        }
    }

    public static class FallbackModel
    {
        public static EdmModel Model { get; private set; }

        static FallbackModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.Namespace = "Microsoft.Restier.WebApi.Test";
            builder.EntitySet<Order>("Orders");
            builder.EntitySet<Person>("People");
            Model = (EdmModel)builder.GetEdmModel();
        }
    }

    internal class FallbackDomain : DomainBase
    {
        protected override DomainConfiguration CreateDomainConfiguration()
        {
            var configuration = base.CreateDomainConfiguration();
            configuration.AddHookHandler<IModelBuilder>(new TestModelProducer(FallbackModel.Model));
            configuration.AddHookHandler<IModelMapper>(new FallbackModelMapper());
            configuration.SetHookPoint(typeof(IQueryExpressionSourcer), new FallbackQueryExpressionSourcer());
            return configuration;
        }

        protected IQueryable<Order> PreservedOrders
        {
            get { return this.Source<Order>("Orders").Where(o => o.Id > 123); }
        }
    }

    public class PeopleController : ODataController
    {
        public IHttpActionResult Get()
        {
            var people = new[]
            {
                new Person {Id = 999}
            };

            return Ok(people);
        }

        public IHttpActionResult GetOrders(int key)
        {
            var orders = new[]
            {
                new Order {Id = 123},
            };

            return Ok(orders);
        }
    }

    class Person
    {
        public int Id { get; set; }

        public IEnumerable<Order> Orders { get; set; }
    }

    class Order
    {
        public int Id { get; set; }
    }

    class FallbackQueryExpressionSourcer : IQueryExpressionSourcer
    {
        public Expression Source(QueryExpressionContext context, bool embedded)
        {
            var orders = new[]
            {
                new Order {Id = 234}
            };

            if (!embedded)
            {
                if (context.VisitedNode.ToString().StartsWith("Source(\"Orders\""))
                {
                    return Expression.Constant(orders.AsQueryable());
                }
            }

            return context.VisitedNode;
        }
    }

    class FallbackModelMapper : IModelMapper
    {
        public bool TryGetRelevantType(DomainContext context, string name, out Type relevantType)
        {
            relevantType = name == "Person" ? typeof(Person) : typeof(Order);

            return true;
        }

        public bool TryGetRelevantType(DomainContext context, string namespaceName, string name, out Type relevantType)
        {
            return TryGetRelevantType(context, name, out relevantType);
        }
    }
}
