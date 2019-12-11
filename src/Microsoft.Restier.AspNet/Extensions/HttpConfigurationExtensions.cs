// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Microsoft.Restier.AspNet;
using Microsoft.Restier.AspNet.Batch;
using Microsoft.Restier.Core;
using ServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace System.Web.Http
{

    /// <summary>
    /// Methods that extend <see cref="HttpConfiguration"/> to make registering Restier easier.
    /// </summary>
    public static class HttpConfigurationExtensions
    {

        #region Private Members

        private const string owinException = "Restier could not use the GlobalConfiguration to register the Batch handler. This is usually because you're running a self-hosted OWIN context.\r\n"
                    + "Please call `config.MapRestier<ApiType>(routeName, routePrefix, true, new HttpServer(config))` instead to correct this.";

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TApi"></typeparam>
        /// <param name="config"></param>
        /// <param name="configureAction"></param>
        /// <returns></returns>
        public static HttpConfiguration UseRestier<TApi>(this HttpConfiguration config, Action<IServiceCollection> configureAction) where TApi : ApiBase
        {
            config.UseCustomContainerBuilder(() =>
            {
                var builder = new RestierContainerBuilder(typeof(TApi), (services) =>
                {
                    // remove the default ODataQuerySettings from OData as we will add our own.
                    services.RemoveAll<ODataQuerySettings>();

                    services
                   .AddRestierCoreServices(typeof(TApi))
                   .AddRestierConventionBasedServices(typeof(TApi));

                    configureAction(services);

                    services.AddRestierDefaultServices<TApi>();
                });

                return builder;
            });

            return config;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TApi"></typeparam>
        /// <param name="config"></param>
        /// <param name="routeName"></param>
        /// <param name="routePrefix"></param>
        /// <param name="allowBatching"></param>
        /// <returns></returns>
        public static HttpConfiguration MapRestier<TApi>(this HttpConfiguration config, string routeName, string routePrefix, bool allowBatching = true)
        {
            var httpServer = GlobalConfiguration.DefaultServer;
            if (httpServer == null)
            {
                throw new Exception(owinException);
            }

            return MapRestier<TApi>(config, routeName, routePrefix, allowBatching, httpServer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TApi"></typeparam>
        /// <param name="config"></param>
        /// <param name="routeName"></param>
        /// <param name="routePrefix"></param>
        /// <param name="allowBatching"></param>
        /// <param name="httpServer"></param>
        /// <returns></returns>
        public static HttpConfiguration MapRestier<TApi>(this HttpConfiguration config, string routeName, string routePrefix, bool allowBatching, HttpServer httpServer)
        {
            ODataBatchHandler batchHandler = null;
            var conventions = CreateRestierRoutingConventions(config, routeName);

            if (allowBatching)
            {
                if (httpServer == null)
                {
                    throw new ArgumentNullException(nameof(httpServer), owinException);
                }

#pragma warning disable IDE0067 // Dispose objects before losing scope
                batchHandler = new RestierBatchHandler(httpServer)
                {
                    ODataRouteName = routeName
                };
#pragma warning restore IDE0067 // Dispose objects before losing scope
            }

            config.MapODataServiceRoute(routeName, routePrefix, (builder) =>
            {
                builder.AddService<IEnumerable<IODataRoutingConvention>>(ServiceLifetime.Singleton, sp => conventions);
                if (batchHandler != null)
                {
                    //RWM: DO NOT simplify this generic signature. It HAS to stay this way, otherwise the code breaks.
                    builder.AddService<ODataBatchHandler>(ServiceLifetime.Singleton, sp => batchHandler);
                }
            });

            return config;
        }


        #region Private Methods

        /// <summary>
        /// Creates the default routing conventions.
        /// </summary>
        /// <param name="config">The <see cref="HttpConfiguration"/> instance.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <returns>The routing conventions created.</returns>
        private static IList<IODataRoutingConvention> CreateRestierRoutingConventions(this HttpConfiguration config, string routeName)
        {
            var conventions = ODataRoutingConventions.CreateDefaultWithAttributeRouting(routeName, config);
            var index = 0;
            for (; index < conventions.Count; index++)
            {
                if (conventions[index] is AttributeRoutingConvention)
                {
                    break;
                }
            }

            conventions.Insert(index + 1, new RestierRoutingConvention());
            return conventions;
        }

        #endregion

    }

}