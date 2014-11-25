// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.OData.Domain;
using System.Web.OData.Domain.Batch;
using Microsoft.Data.Domain.Samples.Northwind.Controllers;

namespace Microsoft.Data.Domain.Samples.Northwind
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

        public static async void RegisterNorthwind(
            HttpConfiguration config, HttpServer server)
        {
            await config.MapODataDomainRoute<NorthwindController>(
                "NorthwindApi", "api/Northwind",
                new ODataDomainBatchHandler(server));
        }
    }
}
