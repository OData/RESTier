// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.Restier.AspNet;
using Microsoft.Restier.AspNet.Batch;
using Microsoft.Restier.Core;
using ServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace System.Web.Http
{

    /// <summary>
    /// A set of <see cref="HttpConfiguration"/> extension methods to help ensure proper Restier configuration.
    /// </summary>
    public static class HttpConfigurationExtensions
    {

        #region Private Members

        private const string OwinException = "Restier could not use the GlobalConfiguration to register the Batch handler. This is usually because you're running a self-hosted OWIN context.\r\n"
                    + "Please call `config.MapRestier<ApiType>(routeName, routePrefix, true, new HttpServer(config))` instead to correct this.";

        #endregion

        /// <summary>
        /// Instructs WebApi to use one or more Restier APIs in this application, each with their own additional services.
        /// </summary>
        /// <param name="config">The <see cref="HttpConfiguration"/> instance to enhance.</param>
        /// <param name="configureApisAction">An <see cref="Action{RestierApiBuilder}" /> that allows you to add APIs to the <see cref="RestierApiBuilder"/>.</param>
        /// <returns>The <see cref="HttpConfiguration"/> instance to allow for fluent method chaining.</returns>
        /// <example>
        /// <code>
        /// config.UseRestier(builder =>
        ///     builder
        ///         .AddRestierApi<SomeApi>(services =>
        ///             services
        ///                 .AddEF6ProviderServices<SomeDbContext>()
        ///                 .AddChainedService<IModelBuilder, SomeDbContextModelBuilder>()
        ///                 .AddSingleton(new ODataValidationSettings
        ///                 {
        ///                     MaxAnyAllExpressionDepth = 3,
        ///                     MaxExpansionDepth = 3,
        ///                 })
        ///         )
        ///  
        ///         .AddRestierApi<AnotherApi>(services =>
        ///             services
        ///                 .AddEF6ProviderServices<AnotherDbContext>()
        ///                 .AddChainedService<IModelBuilder, AnotherDbContextModelBuilder>()
        ///                 .AddSingleton(new ODataValidationSettings
        ///                 {
        ///                     MaxAnyAllExpressionDepth = 3,
        ///                     MaxExpansionDepth = 3,
        ///                 })
        ///         );
        ///    );
        /// </code>
        /// </example>
        public static HttpConfiguration UseRestier(this HttpConfiguration config, Action<RestierApiBuilder> configureApisAction)
        {
            Ensure.NotNull(config, nameof(config));

            if (config.Properties.ContainsKey("Microsoft.AspNet.OData.ContainerBuilderFactoryKey"))
            {
                throw new InvalidOperationException("You can't call \"UseRestier()\" more than once in an application. Check your code and try again.");
            }

            config.UseCustomContainerBuilder(() =>
            {
                return new RestierContainerBuilder(configureApisAction);
            });

            return config;
        }

        /// <summary>
        /// Instructs WebApi to map one or more of the registered Restier APIs to the specified Routes, each with it's own isolated Dependency Injection container.
        /// </summary>
        /// <param name="config">The <see cref="HttpConfiguration"/> instance to enhance.</param>
        /// <param name="configureRoutesAction">The action for configuring a set of routes.</param>
        /// An <see cref="Action{RestierRouteBuilder}" /> that allows you to add map APIs added through the <see cref="RestierApiBuilder"/> to your desired routes via a <see cref="RestierRouteBuilder"/>.
        /// </param>
        /// <returns>The <see cref="HttpConfiguration"/> instance to allow for fluent method chaining.</returns>
        /// <example>
        /// <code>
        /// config.MapRestier(builder =>
        ///     builder.
        ///         .MapApiRoute<SomeApi>("SomeApiV1", "someapi/")
        ///         .MapApiRoute<AnotherApi>("AnotherApiV1", "anotherapi/")
        /// );
        /// </code>
        /// </example>
        public static HttpConfiguration MapRestier(this HttpConfiguration config, Action<RestierRouteBuilder> configureRoutesAction)
        {
            var httpServer = GlobalConfiguration.DefaultServer;
            if (httpServer == null)
            {
                throw new Exception(OwinException);
            }

            return MapRestier(config, configureRoutesAction, httpServer);
        }

        /// <summary>
        /// Instructs WebApi to map one or more of the registered Restier APIs to the specified Routes, each with it's own isolated Dependency Injection container.
        /// </summary>
        /// <param name="config">The <see cref="HttpConfiguration"/> instance to enhance.</param>
        /// <param name="configureRoutesAction">The action for configuring a set of routes.</param>
        /// <param name="httpServer">The HttpServer instance to create the routes on.</param>
        /// <returns>The <see cref="HttpConfiguration"/> instance to allow for fluent method chaining.</returns>
        /// <example>
        /// <code>
        /// config.MapRestier(builder =>
        ///     builder
        ///         .MapApiRoute<SomeApi>("SomeApiV1", "someapi/")
        ///         .MapApiRoute<AnotherApi>("AnotherApiV1", "anotherapi/")
        /// );
        /// </code>
        /// </example>
        public static HttpConfiguration MapRestier(this HttpConfiguration config, Action<RestierRouteBuilder> configureRoutesAction, HttpServer httpServer)
        {
            Ensure.NotNull(configureRoutesAction, nameof(configureRoutesAction));

            var rrb = new RestierRouteBuilder();
            configureRoutesAction.Invoke(rrb);

            foreach (var route in rrb.Routes)
            {
                ODataBatchHandler batchHandler = null;
                var conventions = CreateRestierRoutingConventions(config, route.Key);

                if (route.Value.AllowBatching)
                {
                    if (httpServer == null)
                    {
                        throw new ArgumentNullException(nameof(httpServer), OwinException);
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