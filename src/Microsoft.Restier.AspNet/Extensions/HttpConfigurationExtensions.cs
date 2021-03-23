// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Microsoft.Restier.AspNet;
using Microsoft.Restier.AspNet.Batch;
using Microsoft.Restier.AspNet.Model;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Startup;
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
        /// <param name="configureApis"></param>
        /// <returns></returns>
        public static HttpConfiguration UseRestier(this HttpConfiguration config, Action<RestierApiBuilder> configureApis)
        {
            config.UseCustomContainerBuilder(() =>
            {
                return new RestierContainerBuilder(configureApis);
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
        public static HttpConfiguration MapRestier(this HttpConfiguration config, Action<RestierRouteBuilder> routeBuilder, HttpServer httpServer)
        {
            Ensure.NotNull(routeBuilder, nameof(routeBuilder));

            var rrb = new RestierRouteBuilder();
            routeBuilder.Invoke(rrb);

            foreach (var route in rrb.Routes)
            {
                ODataBatchHandler batchHandler = null;
                var conventions = CreateRestierRoutingConventions(config, route.Key);

                if (route.Value.AllowBatching)
                {
                    if (httpServer == null)
                    {
                        throw new ArgumentNullException(nameof(httpServer), owinException);
                    }

#pragma warning disable IDE0067 // Dispose objects before losing scope
                    batchHandler = new RestierBatchHandler(httpServer)
                    {
                        ODataRouteName = route.Key
                    };
#pragma warning restore IDE0067 // Dispose objects before losing scope
            }

                var odataRoute = config.MapODataServiceRoute(route.Key, route.Value.RoutePrefix, (containerBuilder, routeName) =>
                {
                    var rcb = containerBuilder as RestierContainerBuilder;
                    rcb.routeBuilder = rrb;
                    rcb.RouteName = routeName;

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

        #region OData Dependency Injection Overrides

        /// <summary>
        /// Maps the specified OData route and the OData route attributes.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="configureAction">The inline method used to add Services to the ContainerBuilder based on the current RouteName.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        private static ODataRoute MapODataServiceRoute(this HttpConfiguration configuration, string routeName, string routePrefix, Action<IContainerBuilder, string> configureAction)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (routeName == null)
            {
                throw new ArgumentNullException(nameof(routeName));
            }

            // 1) Build and configure the root container.
            var rootContainer = configuration.CreateODataRootContainer(routeName, configureAction);

            // 2) Resolve the path handler and set URI resolver to it.
            var pathHandler = rootContainer.GetRequiredService<IODataPathHandler>();

            // if settings is not on local, use the global configuration settings.
            if (pathHandler != null && pathHandler.UrlKeyDelimiter == null)
            {
                var urlKeyDelimiter = configuration.GetUrlKeyDelimiter();
                pathHandler.UrlKeyDelimiter = urlKeyDelimiter;
            }

            // 3) Resolve some required services and create the route constraint.
            var routeConstraint = new ODataPathRouteConstraint(routeName);

            // Attribute routing must initialized before configuration.EnsureInitialized is called.
            rootContainer.GetServices<IODataRoutingConvention>();

            // 4) Resolve HTTP handler, create the OData route and register it.
            ODataRoute route;
            var routes = configuration.Routes;
            routePrefix = RemoveTrailingSlash(routePrefix);
            var messageHandler = rootContainer.GetService<HttpMessageHandler>();
            if (messageHandler != null)
            {
                route = new ODataRoute(routePrefix, routeConstraint, null, null, null, messageHandler);
            }
            else
            {
                var batchHandler = rootContainer.GetService<ODataBatchHandler>();
                if (batchHandler != null)
                {
                    batchHandler.ODataRouteName = routeName;
                    var batchTemplate = string.IsNullOrEmpty(routePrefix) ? ODataRouteConstants.Batch : routePrefix + '/' + ODataRouteConstants.Batch;
                    routes.MapHttpBatchRoute(routeName + "Batch", batchTemplate, batchHandler);
                }

                route = new ODataRoute(routePrefix, routeConstraint);
            }

            routes.Add(routeName, route);
            return route;
        }

        private static string RemoveTrailingSlash(string routePrefix)
        {
            if (!string.IsNullOrEmpty(routePrefix))
            {
                var prefixLastIndex = routePrefix.Length - 1;
                if (routePrefix[prefixLastIndex] == '/')
                {
                    // Remove the last trailing slash if it has one.
                    routePrefix = routePrefix.Substring(0, routePrefix.Length - 1);
                }
            }
            return routePrefix;
        }

        internal static ODataUrlKeyDelimiter GetUrlKeyDelimiter(this HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var urlDelimiterConstant = GetODataConstant("UrlKeyDelimiterKey");
            if (configuration.Properties.TryGetValue(urlDelimiterConstant, out var value))
            {
                return value as ODataUrlKeyDelimiter;
            }

            configuration.Properties[urlDelimiterConstant] = null;
            return null;
        }

        /// <summary>
        /// Create the per-route container from the configuration for a given route.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="routeName">The route name.</param>
        /// <param name="configureAction">The configuring action to add the services to the root container.</param>
        /// <returns>The per-route container from the configuration</returns>
        internal static IServiceProvider CreateODataRootContainer(this HttpConfiguration configuration, string routeName, Action<IContainerBuilder, string> configureAction)
        {
            var perRouteContainer = (PerRouteContainer)configuration.GetPerRouteContainer();

            var configureDefaultServicesMethod = typeof(Microsoft.AspNet.OData.Extensions.HttpConfigurationExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(c => c.Name == "ConfigureDefaultServices");

            var internalServicesAction = (Action<IContainerBuilder>)configureDefaultServicesMethod.Invoke(configuration, new object[] { configuration, null });

            return perRouteContainer.CreateODataRouteContainer(routeName, internalServicesAction, configureAction);
        }

        /// <summary>
        /// Get the per-route container from the configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The per-route container from the configuration</returns>
        internal static IPerRouteContainer GetPerRouteContainer(this HttpConfiguration configuration)
        {
            var perRouteContainerKey = GetODataConstant("PerRouteContainerKey");
            var containerBuilderFactoryKey = GetODataConstant("ContainerBuilderFactoryKey");

            return (IPerRouteContainer)configuration.Properties.GetOrAdd(
                perRouteContainerKey,
                key =>
                {
                    IPerRouteContainer perRouteContainer = new PerRouteContainer(configuration);

                    // Attach the build factory if there is one.
                    if (configuration.Properties.TryGetValue(containerBuilderFactoryKey, out var value))
                    {
                        var builderFactory = (Func<IContainerBuilder>)value;
                        perRouteContainer.BuilderFactory = builderFactory;
                    }

                    return perRouteContainer;
                });
        }

        /// <summary>
        /// This method prevents us from having to inline key names that may change. Reflection to the rescue!
        /// </summary>
        /// <param name="constantName"></param>
        /// <returns></returns>
        private static string GetODataConstant(string constantName)
        {
            var extensionsClass = typeof(Microsoft.AspNet.OData.Extensions.HttpConfigurationExtensions);
            var constants = extensionsClass.GetConstants();
            return (string)constants.FirstOrDefault(c => c.Name == constantName).GetRawConstantValue();
        }

        #endregion

        #endregion

    }

}