// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.AspNetCore
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Batch;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNet.OData.Query;
    using Microsoft.AspNet.OData.Routing.Conventions;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.OData;
    using Microsoft.Restier.AspNetCore.Batch;
    using Microsoft.Restier.Core;

    /// <summary>
    /// Extension methods for the <see cref="IRouteBuilder"/> interface.
    /// </summary>
    public static class RestierRouteBuilderExtensions
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
            perRouteContainer.BuilderFactory = () => routeBuilder.ServiceProvider.GetRequiredService<IContainerBuilder>();

            var rrb = new RestierRouteBuilder();
            configureRoutesAction.Invoke(rrb);

            foreach (var route in rrb.Routes)
            {
                ODataBatchHandler batchHandler = null;

                if (route.Value.AllowBatching)
                {

#pragma warning disable IDE0067 // Dispose objects before losing scope
                    batchHandler = new RestierBatchHandler()
                    {
                        ODataRouteName = route.Key
                    };
#pragma warning restore IDE0067 // Dispose objects before losing scope
                }

                var odataRoute = routeBuilder.MapODataServiceRoute(route.Key, route.Value.RoutePrefix, (containerBuilder) =>
                {
                    var rcb = containerBuilder as RestierContainerBuilder;
                    rcb.routeBuilder = rrb;
                    rcb.RouteName = route.Key;

                    containerBuilder.AddService<IEnumerable<IODataRoutingConvention>>(OData.ServiceLifetime.Singleton, sp => routeBuilder.CreateRestierRoutingConventions(route.Key));
                    if (batchHandler != null)
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
    }
}
