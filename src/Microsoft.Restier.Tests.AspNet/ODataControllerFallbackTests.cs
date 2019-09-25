﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNet.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet
{

    [TestClass]
    public class ODataControllerFallbackTests : RestierTestBase
    {

        void addTestServices(IServiceCollection services)
        {
            services.AddChainedService<IModelBuilder>((sp, next) => new StoreModelProducer(FallbackModel.Model))
                .AddChainedService<IModelMapper>((sp, next) => new FallbackModelMapper())
                .AddChainedService<IQueryExpressionSourcer>((sp, next) => new FallbackQueryExpressionSourcer())
                .AddChainedService<IChangeSetInitializer>((sp, next) => new StoreChangeSetInitializer())
                .AddChainedService<ISubmitExecutor>((sp, next) => new DefaultSubmitExecutor());
        }

        [TestMethod]
        public async Task FallbackApi_EntitySet_ShouldFallBack()
        {
            // Should fallback to PeopleController.
            var response = await RestierTestHelpers.ExecuteTestRequest<FallbackApi, DbContext>(HttpMethod.Get, resource: "/People", serviceCollection: addTestServices);
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
            response.IsSuccessStatusCode.Should().BeTrue();
            ((Person[])((ObjectContent)response.Content).Value).Single().Id.Should().Be(999);
        }

        [TestMethod]
        public async Task FallbackApi_NavigationProperty_ShouldFallBack()
        {
            // Should fallback to PeopleController.
            var response = await RestierTestHelpers.ExecuteTestRequest<FallbackApi, DbContext>(HttpMethod.Get, resource: "/People(1)/Orders", serviceCollection: addTestServices);
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
            response.IsSuccessStatusCode.Should().BeTrue();
            ((Order[])((ObjectContent)response.Content).Value).Single().Id.Should().Be(123);
        }

        [TestMethod]
        public async Task FallbackApi_EntitySet_ShouldNotFallBack()
        {
            // Should be routed to RestierController.
            var response = await RestierTestHelpers.ExecuteTestRequest<FallbackApi, DbContext>(HttpMethod.Get, resource: "/Orders", serviceCollection: addTestServices);
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
            response.IsSuccessStatusCode.Should().BeTrue();
            (await response.Content.ReadAsStringAsync()).Should().Contain("\"Id\":234");
        }

        [TestMethod]
        public async Task FallbackApi_Resource_ShouldNotFallBack()
        {
            // Should be routed to RestierController.
            var response = await RestierTestHelpers.ExecuteTestRequest<FallbackApi, DbContext>(HttpMethod.Get, resource: "/PreservedOrders", serviceCollection: addTestServices);
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
            response.IsSuccessStatusCode.Should().BeTrue();
            (await response.Content.ReadAsStringAsync()).Should().Contain("\"Id\":234");
        }

    }

    #region Test Resources

    internal static class FallbackModel
    {
        public static EdmModel Model { get; private set; }

        static FallbackModel()
        {
            var builder = new ODataConventionModelBuilder
            {
                Namespace = "Microsoft.Restier.Tests.AspNet"
            };
            builder.EntitySet<Order>("Orders");
            builder.EntitySet<Person>("People");
            Model = (EdmModel)builder.GetEdmModel();
        }
    }

    internal class FallbackApi : ApiBase
    {

        [Resource]
        public IQueryable<Order> PreservedOrders => this.GetQueryableSource<Order>("Orders").Where(o => o.Id > 123);

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

    internal class Person
    {
        public int Id { get; set; }

        public IEnumerable<Order> Orders { get; set; }
    }

    internal class Order
    {
        public int Id { get; set; }
    }

    internal class FallbackQueryExpressionSourcer : IQueryExpressionSourcer
    {
        public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
        {
            var orders = new[]
            {
                new Order {Id = 234}
            };

            if (!embedded)
            {
                if (context.VisitedNode.ToString().StartsWith("GetQueryableSource(\"Orders\"", StringComparison.CurrentCulture))
                {
                    return Expression.Constant(orders.AsQueryable());
                }
            }

            return context.VisitedNode;
        }
    }

    internal class FallbackModelMapper : IModelMapper
    {
        public bool TryGetRelevantType(ModelContext context, string name, out Type relevantType)
        {
            relevantType = name == "Person" ? typeof(Person) : typeof(Order);

            return true;
        }

        public bool TryGetRelevantType(ModelContext context, string namespaceName, string name, out Type relevantType) => TryGetRelevantType(context, name, out relevantType);
    }

    #endregion


}