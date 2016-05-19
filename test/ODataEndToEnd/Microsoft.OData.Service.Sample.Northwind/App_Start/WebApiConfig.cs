// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Web.Http;
using System.Web.OData.Extensions;
using Microsoft.OData.Service.Sample.Northwind.Models;
using Microsoft.Restier.WebApi.Batch;
using Microsoft.Restier.WebApi.Routing;

namespace Microsoft.OData.Service.Sample.Northwind
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
        }
    }
}
