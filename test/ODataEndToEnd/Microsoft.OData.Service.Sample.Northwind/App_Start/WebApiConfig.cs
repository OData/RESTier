// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Web.Http;
using System.Web.OData.Extensions;
using Microsoft.OData.Service.Sample.Northwind.Models;
using Microsoft.Restier.Publishers.OData;
using Microsoft.Restier.Publishers.OData.Batch;

namespace Microsoft.OData.Service.Sample.Northwind
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();
            
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
            config.Filter().Expand().Select().OrderBy().MaxTop(null).Count();
            config.SetUseVerboseErrors(true);
            config.MapRestierRoute<NorthwindApi>(
                "NorthwindApi", "api/Northwind",
                new RestierBatchHandler(server));
        }

        public static void RegisterNorthwind2(
            HttpConfiguration config, HttpServer server)
        {
            config.SetUseVerboseErrors(true);
            config.MapRestierRoute<NorthwindApi2>(
                "NorthwindApi", "api/Northwind",
                new RestierBatchHandler(server));
        }
    }
}
