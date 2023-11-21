// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.Restier.AspNetCore.Batch;
using Microsoft.Restier.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Restier.AspNetCore
{
    /// <summary>
    /// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add Restier routes.
    /// </summary>
    public static class Restier_IEndpointRouteBuilderExtensions
    {

        #region Internal Constants

        /// <summary>
        /// Wildcard route template for the OData Endpoint route pattern.
        /// </summary>
        internal const string ODataEndpointRoutingPath = "ODataEndpointPath_";

        /// <summary>
        /// Wildcard route template for the OData path route variable.
        /// </summary>
        /// <remarks>
        /// The route pattern needs to be in double-brackets, so to use interpolation you need to double each individual bracket that needs to end up in the string.
        /// </remarks>
        internal static readonly string ODataEndpointRoutingTemplate = $@"{{{{**{ODataEndpointRoutingPath}{{0}}}}}}";

        #endregion

        /// <summary>
        /// Instructs WebApi to map one or more of the registered Restier APIs to the specified Routes, each with it's own isolated Dependency Injection container.
        /// </summary>
        /// <param name="routeBuilder">The <see cref="IEndpointRouteBuilder"/> instance to enhance.</param>
        /// <param name="configureRoutesAction">The action for configuring a set of routes.</param>
        /// <returns>The <see cref="HttpConfiguration"/> instance to allow for fluent method chaining.</returns>
        /// <example>
        /// <code>
        /// endpoints.MapRestier(builder =>
        ///     builder
        ///         .MapApiRoute<SomeApi>("SomeApiV1", "someapi/")
        ///         .MapApiRoute<AnotherApi>("AnotherApiV1", "anotherapi/")
        /// );
        /// </code>
        /// </example>
        public static IEndpointRouteBuilder MapRestier(this IEndpointRouteBuilder routeBuilder, Action<RestierRouteBuilder> configureRoutesAction)
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

                // @robertmclaws: Endpoint Routing cannot have certain characters in the name. Fix it for them so the runtime just works.
                var newRouteKey = GetCleanRouteName(route.Key);

                if (route.Value.AllowBatching)
                {
                    batchHandler = new RestierBatchHandler()
                    {
                        ODataRouteName = newRouteKey
                    };
                }

                var odataRoute = routeBuilder.MapODataServiceRoute(newRouteKey, route.Value.RoutePrefix, (containerBuilder, routeName) =>
                {
                    if (containerBuilder is not RestierContainerBuilder rcb)
                    {
                        throw new Exception($"MapRestier expected a RestierContainerBuilder but got an {containerBuilder.GetType().Name} instead. " +
                            $"This is usually because you did not call services.AddRestier() first. Please see the Restier Northwind Sample application for " +
                            $"more details on how to properly register Restier.");
                    }
                    rcb.routeBuilder = rrb;
                    rcb.RouteName = routeName;

                    containerBuilder.AddService<IEnumerable<IODataRoutingConvention>>(OData.ServiceLifetime.Singleton, sp => routeBuilder.CreateRestierRoutingConventions(newRouteKey));
                    if (batchHandler is not null)
                    {
#pragma warning disable IDE0001 // @robertmclaws: DO NOT simplify this generic signature, or the code breaks.                      
                        containerBuilder.AddService<ODataBatchHandler>(OData.ServiceLifetime.Singleton, sp => batchHandler);
#pragma warning restore IDE0001
                    }
                });
            }

            return routeBuilder;
        }

        /// <summary>
        /// Maps the specified OData route and the OData route attributes.
        /// </summary>
        /// <param name="builder">The <see cref="IRouteBuilder"/> to add the route to.</param>
        /// <param name="routeName">The name of the route to map.</param>
        /// <param name="routePrefix">The prefix to add to the OData route's path template.</param>
        /// <param name="configureAction">The configuring action to add the services to the root container.</param>
        /// <returns>The added <see cref="ODataRoute"/>.</returns>
        internal static IEndpointRouteBuilder MapODataServiceRoute(this IEndpointRouteBuilder builder,
            string routeName,
            string routePrefix,
            Action<IContainerBuilder, string> configureAction)
        {
            Ensure.NotNull(builder, nameof(builder));
            Ensure.NotNull(routeName, nameof(routeName));

            #region Stuff that's done on configuration.CreateODataRootCountainer

            // Build and configure the root container.
            var perRouteContainer = builder.ServiceProvider.GetRequiredService<IPerRouteContainer>() ??
                throw new InvalidOperationException("Could not find the PerRouteContainer.");

            // Create an service provider for this route. Add the default services to the custom configuration actions.
            var configureDefaultServicesMethod = typeof(ODataEndpointRouteBuilderExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(c => c.Name == "ConfigureDefaultServices");
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

            // Resolve HTTP handler, create the OData route and register it.
            routePrefix = Restier_IRouteBuilderExtensions.RemoveTrailingSlash(routePrefix);

            // If a batch handler is present, register the route with the batch path mapper. This will be used
            // by the batching middleware to handle the batch request. Batching still requires the injection
            // of the batching middleware via UseODataBatching().
            var batchHandler = serviceProvider.GetService<ODataBatchHandler>();

            if (batchHandler != null)
            {
                // TODO: for the $batch, need refactor/test it for more.
                batchHandler.ODataRouteName = routeName;

                var batchPath = string.IsNullOrEmpty(routePrefix)
                    ? '/' + ODataRouteConstants.Batch
                    : '/' + routePrefix + '/' + ODataRouteConstants.Batch;

                var batchMapping = builder.ServiceProvider.GetRequiredService<ODataBatchPathMapping>();

                // we need reflection to set this internal property.
                var property = batchMapping.GetType().GetProperty("IsEndpointRouting", BindingFlags.Instance | BindingFlags.NonPublic);
                property.SetValue(batchMapping, true);
                batchMapping.AddRoute(routeName, batchPath);
            }

            builder.MapDynamicControllerRoute<ODataEndpointRouteValueTransformer>(FormatRoutingPattern(routeName, routePrefix));

            perRouteContainer.AddRoute(routeName, routePrefix);

            return builder;
        }

        #region Private Methods

        /// <summary>
        /// Creates the default routing conventions.
        /// </summary>
        /// <param name="builder">The <see cref="IRouteBuilder"/> instance.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <returns>The routing conventions created.</returns>
        internal static IList<IODataRoutingConvention> CreateRestierRoutingConventions(this IEndpointRouteBuilder builder, string routeName)
        {
            var conventions = ODataRoutingConventions.CreateDefaultWithAttributeRouting(routeName, builder.ServiceProvider);
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
        /// Properly formats the DynamicControllerRoute pattern.
        /// </summary>
        /// <param name="routeName">The name of this route.</param>
        /// <param name="routePrefix">
        /// The portion of URL between the host base and where you want to start accepting requests for this route.
        /// </param>
        /// <returns>The route formatted in the way Dynamic Endpoint Routing expects.</returns>
        /// <remarks>
        /// The route pattern requires the following format: "routePrefix/{*ODataEndpointPath_routeName}"
        /// </remarks>
        internal static string FormatRoutingPattern(string routeName, string routePrefix)
        {
            Ensure.NotNull(routeName, nameof(routeName));

            return string.IsNullOrEmpty(routePrefix) ?
                string.Format(ODataEndpointRoutingTemplate, routeName) :
                routePrefix + "/" + string.Format(ODataEndpointRoutingTemplate, routeName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="routeName"></param>
        /// <returns></returns>
        internal static string GetCleanRouteName(string routeName)
        {
            return routeName.Replace("/", "_").Replace("{", "_").Replace("}", "_");
        }

        #endregion

    }

}
