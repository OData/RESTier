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
using Microsoft.Restier.AspNet.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Routing;
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
        /// <param name="config"></param>
        /// <param name="configureAction"></param>
        /// <returns></returns>
        public static HttpConfiguration UseRestier(this HttpConfiguration config, Action<IServiceCollection> configureAction)
        {
            Ensure.NotNull(config, nameof(config));

            if (config.Properties.ContainsKey("Microsoft.AspNet.OData.ContainerBuilderFactoryKey"))
            {
                throw new InvalidOperationException("You can't call \"UseRestier()\" more than once in an application. Check your code and try again.");
            }

            config.UseCustomContainerBuilder(() =>
            {
                var builder = new RestierContainerBuilder((services) =>
                {
                    // RWM: Remove the default ODataQuerySettings from OData as we will add our own.
                    //      Has to be here because ODataQuerySettings is in the ASP.NET library.
                    services.RemoveAll<ODataQuerySettings>()
                        .AddRestierCoreServices();

                    // RWM: Same problem here, can't move this call lower down the stack.
                    services.AddChainedService<IModelBuilder, RestierWebApiModelBuilder>();

                    //RWM: This is where people will register their own APIs and EF Contexts.
                    configureAction(services);

                    //RWM: Register anything else that hasn't been registered already.
                    services.AddRestierDefaultServices();
                });

                return builder;
            });

            return config;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="routeBuilder"></param>
        /// <returns></returns>
        public static HttpConfiguration MapRestier(this HttpConfiguration config, Action<RestierRouteBuilder> routeBuilder)
        {
            var httpServer = GlobalConfiguration.DefaultServer;
            if (httpServer == null)
            {
                throw new Exception(owinException);
            }

            return MapRestier(config, routeBuilder, httpServer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="routeBuilder"></param>
        /// <param name="httpServer"></param>
        /// <returns></returns>
        public static HttpConfiguration MapRestier(this HttpConfiguration config, Action<RestierRouteBuilder> routeBuilder, HttpServer httpServer)
        {
            Ensure.NotNull(routeBuilder, nameof(routeBuilder));

            var rrb = new RestierRouteBuilder();
            routeBuilder.Invoke(rrb);

            foreach (var route in rrb.Routes)
            {
                ODataBatchHandler batchHandler = null;
                var conventions = CreateRestierRoutingConventions(config, route.RouteName);

                if (route.AllowBatching)
                {
                    if (httpServer == null)
                    {
                        throw new ArgumentNullException(nameof(httpServer), owinException);
                    }

#pragma warning disable IDE0067 // Dispose objects before losing scope
                    batchHandler = new RestierBatchHandler(httpServer)
                    {
                        ODataRouteName = route.RouteName
                    };
#pragma warning restore IDE0067 // Dispose objects before losing scope
                }

                config.MapODataServiceRoute(route.RouteName, route.RoutePrefix, (containerBuilder) =>
                {
                    (containerBuilder as RestierContainerBuilder).RouteBuilder = rrb;
                    containerBuilder.AddService<IEnumerable<IODataRoutingConvention>>(ServiceLifetime.Singleton, sp => conventions);
                    if (batchHandler != null)
                    {
                        //RWM: DO NOT simplify this generic signature. It HAS to stay this way, otherwise the code breaks.
                        containerBuilder.AddService<ODataBatchHandler>(ServiceLifetime.Singleton, sp => batchHandler);
                    }
                });
            }

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