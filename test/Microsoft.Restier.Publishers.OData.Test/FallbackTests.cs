// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Publishers.OData.Model;
using Xunit;

namespace Microsoft.Restier.Publishers.OData.Test
{
    public class FallbackTests
    {
        private HttpClient client;

        public FallbackTests()
        {
            var configuration = new HttpConfiguration();
            configuration.MapRestierRoute<FallbackApi>("fallback", "fallback").Wait();
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
            // Should be routed to RestierController.
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/fallback/Orders");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payload = await ((ObjectContent)response.Content).ReadAsStringAsync();
            Assert.Contains("\"Id\":234", payload);
        }

        [Fact]
        public async Task FallbackConventionBasedProviderTest()
        {
            // Should be routed to RestierController.
            var request = new HttpRequestMessage(HttpMethod.Get, "http://host/fallback/PreservedOrders");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            HttpResponseMessage response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payload = await ((ObjectContent)response.Content).ReadAsStringAsync();
            Assert.Contains("\"Id\":234", payload);
        }
    }

    public static class FallbackModel
    {
        public static EdmModel Model { get; private set; }

        static FallbackModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.Namespace = "Microsoft.Restier.Publishers.OData.Test";
            builder.EntitySet<Order>("Orders");
            builder.EntitySet<Person>("People");
            Model = (EdmModel)builder.GetEdmModel();
        }
    }

    internal class FallbackApi : ApiBase
    {
        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            services.AddService<IModelBuilder>((sp, next) => new TestModelProducer(FallbackModel.Model));
            services.AddService<IModelMapper>((sp, next) => new FallbackModelMapper());
            services.AddService<IQueryExpressionSourcer>((sp, next) => new FallbackQueryExpressionSourcer());
            services = ApiBase.ConfigureApi(apiType, services);
            return services;
        }

        [Resource]
        public IQueryable<Order> PreservedOrders
        {
            get { return this.GetQueryableSource<Order>("Orders").Where(o => o.Id > 123); }
        }

        public FallbackApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
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
        public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
        {
            var orders = new[]
            {
                new Order {Id = 234}
            };

            if (!embedded)
            {
                if (context.VisitedNode.ToString().StartsWith("GetQueryableSource(\"Orders\""))
                {
                    return Expression.Constant(orders.AsQueryable());
                }
            }

            return context.VisitedNode;
        }
    }

    class FallbackModelMapper : IModelMapper
    {
        public bool TryGetRelevantType(ModelContext context, string name, out Type relevantType)
        {
            relevantType = name == "Person" ? typeof(Person) : typeof(Order);

            return true;
        }

        public bool TryGetRelevantType(ModelContext context, string namespaceName, string name, out Type relevantType)
        {
            return TryGetRelevantType(context, name, out relevantType);
        }
    }
}
