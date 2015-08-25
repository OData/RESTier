// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.WebApi.Batch;
using Microsoft.Restier.WebApi.Routing;
using WebApiODataEx = System.Web.OData.Extensions;

namespace Microsoft.Restier.WebApi
{
    /// <summary>
    /// Offers a collection of extension methods to <see cref="HttpConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpConfigurationExtensions
    {
        /// TODO GitHubIssue#51 : Support model lazy loading
        /// <summary>
        /// Maps the domain routes to the ODataDomainController.
        /// </summary>
        /// <typeparam name="TDomain">The user domain.</typeparam>
        /// <param name="config">The <see cref="HttpConfiguration"/> instance.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routePrefix">The prefix of the route.</param>
        /// <param name="domainFactory">The callback to create domain instances.</param>
        /// <param name="batchHandler">The handler for batch requests.</param>
        /// <returns>The task object containing the resulted <see cref="ODataRoute"/> instance.</returns>
        public static async Task<ODataRoute> MapODataDomainRoute<TDomain>(
            this HttpConfiguration config,
            string routeName,
            string routePrefix,
            Func<IDomain> domainFactory,
            ODataDomainBatchHandler batchHandler = null)
            where TDomain : DomainBase
        {
            Ensure.NotNull(domainFactory, "domainFactory");

            using (var domain = domainFactory())
            {
                var model = await domain.GetModelAsync();
                var conventions = CreateODataDomainRoutingConventions<TDomain>(config, model);

                if (batchHandler != null && batchHandler.DomainFactory == null)
                {
                    batchHandler.DomainFactory = domainFactory;
                }

                var routes = config.Routes;
                routePrefix = RemoveTrailingSlash(routePrefix);

                if (batchHandler != null)
                {
                    batchHandler.ODataRouteName = routeName;
                    var batchTemplate = string.IsNullOrEmpty(routePrefix) ? ODataRouteConstants.Batch
                        : routePrefix + '/' + ODataRouteConstants.Batch;
                    routes.MapHttpBatchRoute(routeName + "Batch", batchTemplate, batchHandler);
                }

                DefaultODataPathHandler odataPathHandler = new DefaultODataPathHandler();

                var getResolverSettings = typeof(WebApiODataEx.HttpConfigurationExtensions)
                    .GetMethod("GetResolverSettings", BindingFlags.NonPublic | BindingFlags.Static);

                if (getResolverSettings != null)
                {
                    var resolveSettings = getResolverSettings.Invoke(null, new object[] { config });
                    PropertyInfo prop = odataPathHandler
                        .GetType().GetProperty("ResolverSetttings", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (null != prop && prop.CanWrite)
                    {
                        prop.SetValue(odataPathHandler, resolveSettings, null);
                    }

                    // In case WebAPI OData fix "ResolverSetttings" to "ResolverSettings".
                    // So we set both "ResolverSetttings" and "ResolverSettings".
                    prop = odataPathHandler
                        .GetType().GetProperty("ResolverSettings", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (null != prop && prop.CanWrite)
                    {
                        prop.SetValue(odataPathHandler, resolveSettings, null);
                    }
                }

                var routeConstraint =
                    new ODataDomainPathRouteConstraint(odataPathHandler, model, routeName, conventions, domainFactory);
                var route = new ODataRoute(routePrefix, routeConstraint);
                routes.Add(routeName, route);
                return route;
            }
        }

        /// <summary>
        /// Maps the domain routes to the ODataDomainController.
        /// </summary>
        /// <typeparam name="TDomain">The user domain.</typeparam>
        /// <param name="config">The <see cref="HttpConfiguration"/> instance.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routePrefix">The prefix of the route.</param>
        /// <param name="batchHandler">The handler for batch requests.</param>
        /// <returns>The task object containing the resulted <see cref="ODataRoute"/> instance.</returns>
        public static async Task<ODataRoute> MapODataDomainRoute<TDomain>(
            this HttpConfiguration config,
            string routeName,
            string routePrefix,
            ODataDomainBatchHandler batchHandler = null)
            where TDomain : DomainBase, new()
        {
            return await MapODataDomainRoute<TDomain>(
                config, routeName, routePrefix, () => new TDomain(), batchHandler);
        }

        /// <summary>
        /// Creates the default routing conventions.
        /// </summary>
        /// <typeparam name="TDomain">The user domain.</typeparam>
        /// <param name="config">The <see cref="HttpConfiguration"/> instance.</param>
        /// <param name="model">The EDM model.</param>
        /// <returns>The routing conventions created.</returns>
        internal static IList<IODataRoutingConvention> CreateODataDomainRoutingConventions<TDomain>(
            this HttpConfiguration config, IEdmModel model)
            where TDomain : DomainBase
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

            conventions.Insert(index, new ODataDomainRoutingConvention(typeof(ODataDomainController).Name));
            conventions.Insert(index, new ODataDomainRoutingConvention(typeof(TDomain).Name));
            conventions.Insert(0, new AttributeRoutingConvention(model, config));
            return conventions;
        }

        private static string RemoveTrailingSlash(string routePrefix)
        {
            if (string.IsNullOrEmpty(routePrefix))
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
