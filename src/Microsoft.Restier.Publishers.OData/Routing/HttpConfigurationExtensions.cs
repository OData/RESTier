// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Batch;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Publishers.OData.Batch;
using Microsoft.Restier.Publishers.OData.Properties;
using ServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace Microsoft.Restier.Publishers.OData
{
    /// <summary>
    /// Offers a collection of extension methods to <see cref="HttpConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpConfigurationExtensions
    {
        private const string UseVerboseErrorsFlagKey = "Microsoft.Restier.UseVerboseErrorsFlag";
        private const string RootContainerKey = "System.Web.OData.RootContainerMappingsKey";

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
            ApiBase.AddPublisherServices(
                typeof(TApi),
                services =>
                {
                    services.AddODataServices<TApi>();
                });

            Func<IContainerBuilder> func = () => new RestierContainerBuilder(typeof(TApi));
            config.UseCustomContainerBuilder(func);

            var conventions = CreateRestierRoutingConventions(config, routeName);
            if (batchHandler != null)
            {
                batchHandler.ODataRouteName = routeName;
            }

            Action<IContainerBuilder> configureAction = builder => builder
            .AddService<IEnumerable<IODataRoutingConvention>>(ServiceLifetime.Singleton, sp => conventions)
            .AddService<ODataBatchHandler>(ServiceLifetime.Singleton, sp => batchHandler);

            var route = config.MapODataServiceRoute(routeName, routePrefix, configureAction);

            return Task.FromResult(route);
        }

        /// <summary>
        /// Gets the UseVerboseErrors flag from the configuration.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <returns>The flag of UseVerboseErrors for the configuration.</returns>
        public static bool GetUseVerboseErrors(this HttpConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture, Resources.ArguementsCannotbeNull, "configuration"));
            }

            object value;
            bool useVerboseErrorsFlag = false;
            if (configuration.Properties.TryGetValue(UseVerboseErrorsFlagKey, out value))
            {
                useVerboseErrorsFlag = value is bool ? (bool)value : false;
            }

            return useVerboseErrorsFlag;
        }

        /// <summary>
        /// Sets the UseVerboseErrors flag on the configuration.
        /// If this is set to true (suggest for debug model only),
        /// then the whole exception stack will be returned in case there is some error.
        /// </summary>
        /// <param name="configuration">The server configuration.</param>
        /// <param name="useVerboseErrors">The UseVerboseErrors flag for the configuration.</param>
        public static void SetUseVerboseErrors(this HttpConfiguration configuration, bool useVerboseErrors)
        {
            if (configuration == null)
            {
                throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture, Resources.ArguementsCannotbeNull, "configuration"));
            }

            configuration.Properties[UseVerboseErrorsFlagKey] = useVerboseErrors;
        }

        /// <summary>
        /// Creates the default routing conventions.
        /// </summary>
        /// <param name="config">The <see cref="HttpConfiguration"/> instance.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <returns>The routing conventions created.</returns>
        private static IList<IODataRoutingConvention> CreateRestierRoutingConventions(
            this HttpConfiguration config, string routeName)
        {
            var conventions = ODataRoutingConventions.CreateDefaultWithAttributeRouting(routeName, config);
            var index = 0;
            for (; index < conventions.Count; index++)
            {
                var attributeRouting = conventions[index] as AttributeRoutingConvention;
                if (attributeRouting != null)
                {
                    break;
                }
            }

            conventions.Insert(index + 1, new RestierRoutingConvention());
            return conventions;
        }
    }
}
