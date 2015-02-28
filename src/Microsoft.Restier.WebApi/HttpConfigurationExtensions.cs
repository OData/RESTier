// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.WebApi.Batch;
using Microsoft.Restier.WebApi.Routing;

namespace Microsoft.Restier.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpConfigurationExtensions
    {
        // TODO GitHubIssue#51 : Support model lazy loading
        public static async Task<ODataRoute> MapODataDomainRoute<TController>(
            this HttpConfiguration config, string routeName, string routePrefix,
            ODataDomainBatchHandler batchHandler = null)
            where TController : ODataDomainController, new()
        {
            using (TController controller = new TController())
            {
                var model = await controller.Domain.GetModelAsync();
                var conventions = CreateODataDomainRoutingConventions<TController>(config, model);

                if (batchHandler != null && batchHandler.ContextFactory == null)
                {
                    batchHandler.ContextFactory = () => new TController().Domain.Context;
                }

                var routes = config.Routes;
                routePrefix = RemoveTrailingSlash(routePrefix);

                if (batchHandler != null)
                {
                    batchHandler.ODataRouteName = routeName;
                    var batchTemplate = String.IsNullOrEmpty(routePrefix) ? ODataRouteConstants.Batch
                        : routePrefix + '/' + ODataRouteConstants.Batch;
                    routes.MapHttpBatchRoute(routeName + "Batch", batchTemplate, batchHandler);
                }

                var routeConstraint = new DefaultODataPathRouteConstraint(new DefaultODataPathHandler(), model,
                    routeName, conventions);
                var route = new ODataRoute(routePrefix, routeConstraint);
                routes.Add(routeName, route);
                return route;
            }
        }

        public static IList<IODataRoutingConvention> CreateODataDomainRoutingConventions<TController>(
            this HttpConfiguration config, IEdmModel model)
            where TController : ODataDomainController, new()
        {
            var conventions = ODataRoutingConventions.CreateDefault();
            var index = 0;
            for (; index < conventions.Count; index++)
            {
                var unmapped = conventions[index] as UnmappedRequestRoutingConvention;
                if (unmapped != null)
                {
                    break;
                }
            }

            conventions.Insert(index, new DefaultODataRoutingConvention(typeof(TController).Name));
            conventions.Insert(0, new AttributeRoutingConvention(model, config));
            return conventions;
        }

        private static string RemoveTrailingSlash(string routePrefix)
        {
            if (String.IsNullOrEmpty(routePrefix))
            {
                return routePrefix;
            }

            var prefixLastIndex = routePrefix.Length - 1;
            if (routePrefix[prefixLastIndex] == '/')
            {
                // Remove the last trailing slash if it has one.
                routePrefix = routePrefix.Substring(0, routePrefix.Length - 1);
            }
            return routePrefix;
        }
    }
}
