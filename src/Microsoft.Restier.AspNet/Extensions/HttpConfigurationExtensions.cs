// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
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
    /// Methods that extend <see cref="HttpConfiguration"/> to make registering Restier easier.
    /// </summary>
    public static class HttpConfigurationExtensions
    {

        /// TODO GitHubIssue#51 : Support model lazy loading
        /// <summary>
        /// Maps the API routes to the RestierController.
        /// </summary>
        /// <typeparam name="TApi">The user API.</typeparam>
        /// <param name="config">The <see cref="HttpConfiguration"/> instance.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routePrefix">The prefix of the route.</param>
        /// <param name="batchHandler">The handler for batch requests.</param>
        /// <returns>The task object containing the resulted <see cref="ODataRoute"/> instance.</returns>
        public static Task<ODataRoute> MapRestierRoute<TApi>(
            this HttpConfiguration config,
            string routeName,
            string routePrefix,
            RestierBatchHandler batchHandler = null)
            where TApi : ApiBase
        {
            // This will be added a service to callback stored in ApiConfiguration
            // Callback is called by ApiBase.AddApiServices method to add real services.
            ApiBase.AddPublisherServices(typeof(TApi), services =>
                {
                    services.AddRestierServices<TApi>();
                });

            IContainerBuilder func() => new RestierContainerBuilder(typeof(TApi));
            config.UseCustomContainerBuilder(func);

            var conventions = CreateRestierRoutingConventions(config, routeName);
            if (batchHandler != null)
            {
                batchHandler.ODataRouteName = routeName;
            }

            void configureAction(IContainerBuilder builder) => builder
                .AddService<IEnumerable<IODataRoutingConvention>>(ServiceLifetime.Singleton, sp => conventions)
                .AddService<ODataBatchHandler>(ServiceLifetime.Singleton, sp => batchHandler);

            var route = config.MapODataServiceRoute(routeName, routePrefix, configureAction);

            return Task.FromResult(route);
        }

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
                var builder = new RestierContainerBuilder(typeof(TApi));
                builder.Services
                    .AddCoreServices(typeof(TApi))
                    .AddConventionBasedServices(typeof(TApi));

                configureAction(builder.Services);

                builder.Services.AddRestierServices<TApi>();
                return builder;
            });

            //config.EnableDependencyInjection();
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
            ODataBatchHandler batchHandler = null;
            var conventions = CreateRestierRoutingConventions(config, routeName);

            if (allowBatching)
            {
#pragma warning disable IDE0067 // Dispose objects before losing scope
                batchHandler = new RestierBatchHandler(GlobalConfiguration.DefaultServer)
                {
                    ODataRouteName = routeName
                };
#pragma warning restore IDE0067 // Dispose objects before losing scope
            }

            config.MapODataServiceRoute(routeName, routePrefix, (builder) => {
                builder.AddService<IEnumerable<IODataRoutingConvention>>(ServiceLifetime.Singleton, sp => conventions);
                if (batchHandler != null)
                {
                    builder.AddService(ServiceLifetime.Singleton, sp => batchHandler);
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
                if (conventions[index] is AttributeRoutingConvention attributeRouting)
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