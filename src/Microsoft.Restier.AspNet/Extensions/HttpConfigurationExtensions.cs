// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Extensions;
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

            config.MapODataServiceRoute(routeName, routePrefix, (builder) =>
            {
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