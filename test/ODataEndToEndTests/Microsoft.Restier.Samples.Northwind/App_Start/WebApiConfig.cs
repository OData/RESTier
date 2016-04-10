// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.Samples.Northwind.Models;
using Microsoft.Restier.WebApi;
using Microsoft.Restier.WebApi.Batch;

namespace Microsoft.Restier.Samples.Northwind
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.EnableUnqualifiedNameCall(true);
            RegisterNorthwind(config, GlobalConfiguration.DefaultServer);

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }

        public static void RegisterNorthwind(
            HttpConfiguration config, HttpServer server)
        {
            config.MapRestierRoute<NorthwindApi>(
                "NorthwindApi", "api/Northwind",
                new RestierBatchHandler(server));

            var configuration = new Lazy<ApiConfiguration>(
                BuildNorthwind2Configuration, LazyThreadSafetyMode.ExecutionAndPublication);
            config.MapRestierRoute<NorthwindApi2>(
                "NorthwindApi2", "api/Northwind2",
                () => configuration.Value.CreateContext(),
                new RestierBatchHandler(server));
        }

        private static ApiConfiguration BuildNorthwind2Configuration()
        {
            return ApiConfiguration.Create(services =>
            {
                var customizer = ApiConfiguration.Customize<NorthwindApi2>();
                services
                    .Apply(customizer.InnerMost)
                    .AddScoped<NorthwindApi2>()
                    .UseDbContext<NorthwindContext>()
                    .ChainPrevious<IModelBuilder, NorthwindModelExtender>()
                    .Apply(customizer.PrivateApi)
                    .UseAttributes<NorthwindApi2>()
                    .UseConventions<NorthwindApi2>()
                    .Apply(customizer.Overrides)
                    .Apply(customizer.OuterMost);
            });
        }

        private class NorthwindModelExtender : IModelBuilder
        {
            public IModelBuilder InnerHandler { get; set; }

            public async Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                var model = await InnerHandler.GetModelAsync(context, cancellationToken);

                // Way 2: enable auto-expand through model annotation.
                var orderType = (EdmEntityType)model.SchemaElements.Single(e => e.Name == "Order");
                var orderDetailsProperty = (EdmNavigationProperty)orderType.DeclaredProperties
                    .Single(prop => prop.Name == "Order_Details");
                model.SetAnnotationValue(orderDetailsProperty,
                    new QueryableRestrictionsAnnotation(new QueryableRestrictions { AutoExpand = true }));

                return model;
            }
        }
    }
}
