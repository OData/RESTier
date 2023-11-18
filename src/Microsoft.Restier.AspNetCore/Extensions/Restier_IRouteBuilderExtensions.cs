// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.Restier.AspNetCore.Batch;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNetCore
{

    /// <summary>
    /// Extension methods for the <see cref="IRouteBuilder"/> interface.
    /// </summary>
    public static class Restier_IRouteBuilderExtensions
    {
        /// <summary>
        /// Instructs WebApi to map one or more of the registered Restier APIs to the specified Routes, each with it's own isolated Dependency Injection container.
        /// </summary>
        /// <param name="routeBuilder">The <see cref="HttpConfiguration"/> instance to enhance.</param>
        /// <param name="configureRoutesAction">The action for configuring a set of routes.</param>
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
        public static IRouteBuilder MapRestier(this IRouteBuilder routeBuilder, Action<RestierRouteBuilder> configureRoutesAction)
        {
            Ensure.NotNull(routeBuilder, nameof(routeBuilder));
            Ensure.NotNull(configureRoutesAction, nameof(configureRoutesAction));

            var perRouteContainer = routeBuilder.ServiceProvider.GetRequiredService<IPerRouteContainer>();
            var apiBuilderAction = routeBuilder.ServiceProvider.GetRequiredService<Action<RestierApiBuilder>>();

            perRouteContainer.BuilderFactory = () => new RestierContainerBuilder(apiBuilderAction);

            var rrb = new RestierRouteBuilder();
            configureRoutesAction.Invoke(rrb);

            foreach (var route in rrb.Routes)
            {
                ODataBatchHandler batchHandler = null;

                if (route.Value.AllowBatching)
                {
                    batchHandler = new RestierBatchHandler()
                    {
                        ODataRouteName = route.Key
                    };
                }

                var odataRoute = routeBuilder.MapODataServiceRoute(route.Key, route.Value.RoutePrefix, (containerBuilder, routeName) =>
                {
                    if (containerBuilder is not RestierContainerBuilder rcb)
                    {
                        throw new Exception($"MapRestier expected a RestierContainerBuilder but got an {containerBuilder.GetType().Name} instead. " +
                            $"This is usually because you did not call services.AddRestier() first. Please see the Restier Northwind Sample application for " +
                            $"more details on how to properly register Restier.");
                    }
                    rcb.routeBuilder = rrb;
                    rcb.RouteName = routeName;

                    containerBuilder.AddService<IEnumerable<IODataRoutingConvention>>(OData.ServiceLifetime.Singleton, sp => routeBuilder.CreateRestierRoutingConventions(route.Key));
                    if (batchHandler is not null)
                    {
                        //RWM: DO NOT simplify this generic signature. It HAS to stay this way, otherwise the code breaks.
                        containerBuilder.AddService<ODataBatchHandler>(OData.ServiceLifetime.Singleton, sp => batchHandler);
                    }
                });
            }

            return routeBuilder;
        }

        /// <summary>
        /// Creates the default routing conventions.
        /// </summary>
        /// <param name="builder">The <see cref="IRouteBuilder"/> instance.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <returns>The routing conventions created.</returns>
        private static IList<IODataRoutingConvention> CreateRestierRoutingConventions(this IRouteBuilder builder, string routeName)
        {
            var conventions = ODataRoutingConventions.CreateDefaultWithAttributeRouting(routeName, builder);
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

        /// <summary>
        /// Maps the specified OData route and the OData route attributes.
        /// </summary>
        /// <param name="builder">The <see cref="IRouteBuilder"/> to add the route to.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="configureAction">The configuring action to add the services to the root container.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        public static ODataRoute MapODataServiceRoute(this IRouteBuilder builder, string routeName,
            string routePrefix, Action<IContainerBuilder, string> configureAction)
        {
            Ensure.NotNullOrWhiteSpace(routeName, nameof(routeName));
            Ensure.NotNull(builder, nameof(builder));

            #region Stuff that's done on configuration.CreateODataRootCountainer

            // Build and configure the root container.
            var perRouteContainer = builder.ServiceProvider.GetRequiredService<IPerRouteContainer>() ?? 
                throw new InvalidOperationException("Could not find the PerRouteContainer.");

            // Create an service provider for this route. Add the default services to the custom configuration actions.
            var configureDefaultServicesMethod = typeof(ODataRouteBuilderExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(c => c.Name == "ConfigureDefaultServices");
            var internalServicesAction = (Action<IContainerBuilder>)configureDefaultServicesMethod.Invoke(builder, [builder, null]);

            var serviceProvider = (perRouteContainer as PerRouteContainer).CreateODataRouteContainer(routeName, internalServicesAction, configureAction);

            #endregion

            // Make sure the MetadataController is registered with the ApplicationPartManager.
            var applicationPartManager = builder.ServiceProvider.GetRequiredService<ApplicationPartManager>();
            applicationPartManager.ApplicationParts.Add(new AssemblyPart(typeof(MetadataController).Assembly));

            // Resolve the path handler and set URI resolver to it.
            var pathHandler = serviceProvider.GetRequiredService<IODataPathHandler>();

            // If settings is not on local, use the global configuration settings.
            var options = builder.ServiceProvider.GetRequiredService<ODataOptions>();
            if (pathHandler is not null && pathHandler.UrlKeyDelimiter is null)
            {
                pathHandler.UrlKeyDelimiter = options.UrlKeyDelimiter;
            }

            // Resolve some required services and create the route constraint.
            var routeConstraint = new ODataPathRouteConstraint(routeName);

            // Get constraint resolver.
            var inlineConstraintResolver = builder.ServiceProvider.GetRequiredService<IInlineConstraintResolver>();
            routePrefix = RemoveTrailingSlash(routePrefix);

            var customRouter = serviceProvider.GetService<IRouter>();
            // Resolve HTTP handler, create the OData route and register it.
            var route = new ODataRoute(
                customRouter ?? builder.DefaultHandler,
                routeName,
                routePrefix,
                routeConstraint,
                inlineConstraintResolver);

            // If a batch handler is present, register the route with the batch path mapper. This will be used
            // by the batching middleware to handle the batch request. Batching still requires the injection
            // of the batching middleware via UseODataBatching().
            var batchHandler = serviceProvider.GetService<ODataBatchHandler>();
            if (batchHandler is not null)
            {
                batchHandler.ODataRoute = route;
                batchHandler.ODataRouteName = routeName;

                var batchPath = string.IsNullOrEmpty(routePrefix)
                    ? '/' + ODataRouteConstants.Batch
                    : '/' + routePrefix + '/' + ODataRouteConstants.Batch;

                var batchMapping = builder.ServiceProvider.GetRequiredService<ODataBatchPathMapping>();
                batchMapping.AddRoute(routeName, batchPath);
            }

            builder.Routes.Add(route);
            return route;
        }

        /// <summary>
        /// Remote the trailing slash from a route prefix string.
        /// </summary>
        /// <param name="routePrefix">The route prefix string.</param>
        /// <returns>The route prefix string without a trailing slash.</returns>
        internal static string RemoveTrailingSlash(string routePrefix)
        {
            if (!string.IsNullOrEmpty(routePrefix))
            {
                var prefixLastIndex = routePrefix.Length - 1;
                if (routePrefix[prefixLastIndex] == '/')
                {
                    // Remove the last trailing slash if it has one.
                    routePrefix = routePrefix[0..^1];
                }
            }

            return routePrefix;
        }

    }

}
