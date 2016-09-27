// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Publishers.OData.Batch;

namespace Microsoft.Restier.Publishers.OData.Routing
{
    /// <summary>
    /// Offers a collection of extension methods to <see cref="HttpConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
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
        /// <param name="apiFactory">The callback to create API instances.</param>
        /// <param name="batchHandler">The handler for batch requests.</param>
        /// <returns>The task object containing the resulted <see cref="ODataRoute"/> instance.</returns>
        public static Task<ODataRoute> MapRestierRoute<TApi>(
            this HttpConfiguration config,
            string routeName,
            string routePrefix,
            Func<ApiBase> apiFactory,
            RestierBatchHandler batchHandler = null)
            where TApi : ApiBase
        {
            Ensure.NotNull(apiFactory, "apiFactory");

            // This will be added a service to callback stored in ApiConfiguration
            // Callback is called by ApiBase.AddApiServices method to add real services.
            ApiConfiguration.AddPublisherServices<TApi>(services =>
            {
                services.AddODataServices<TApi>();
            });
            using (var api = apiFactory())
            {
                var model = GetModel(api);

                var conventions = CreateRestierRoutingConventions(config, model, apiFactory);

                if (batchHandler != null && batchHandler.ApiFactory == null)
                {
                    batchHandler.ApiFactory = apiFactory;
                }

                // Customized path handler should be added in ConfigureApi as service
                // Allow to handle URL encoded slash (%2F), and backslash(%5C) with customized handler
                var handler = api.Context.GetApiService<IODataPathHandler>();
                if (handler == null)
                {
                    handler = new DefaultODataPathHandler();
                }

                var route = config.MapODataServiceRoute(
                    routeName, routePrefix, model, handler, conventions, batchHandler);

                // Customized converter should be added in ConfigureApi as service
                var converter = api.Context.GetApiService<ODataPayloadValueConverter>();
                if (converter == null)
                {
                    converter = new RestierPayloadValueConverter();
                }

                model.SetPayloadValueConverter(converter);

                return Task.FromResult(route);
            }
        }

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
            where TApi : ApiBase, new()
        {
            return MapRestierRoute<TApi>(
                config, routeName, routePrefix, () => new TApi(), batchHandler);
        }

        /// <summary>
        /// Creates the default routing conventions.
        /// </summary>
        /// <param name="config">The <see cref="HttpConfiguration"/> instance.</param>
        /// <param name="model">The EDM model.</param>
        /// <param name="apiFactory">The API factory.</param>
        /// <returns>The routing conventions created.</returns>
        private static IList<IODataRoutingConvention> CreateRestierRoutingConventions(
            this HttpConfiguration config, IEdmModel model, Func<ApiBase> apiFactory)
        {
            var conventions = ODataRoutingConventions.CreateDefaultWithAttributeRouting(config, model);
            var index = 0;
            for (; index < conventions.Count; index++)
            {
                var attributeRouting = conventions[index] as AttributeRoutingConvention;
                if (attributeRouting != null)
                {
                    break;
                }
            }

            conventions.Insert(index + 1, new RestierRoutingConvention(apiFactory));
            return conventions;
        }

        private static IEdmModel GetModel(ApiBase api)
        {
            // Here await is not used because if method MapRestierRoute is mapped async,
            // Then during application starts, the http service initialization may complete first
            // before this method call is complete.
            // Then all request will fail, and this happen for some test cases before when get model takes long time.
            IEdmModel model;
            try
            {
                model = api.GetModelAsync().Result;
            }
            catch (AggregateException e)
            {
                // Without await, the exception is wrapped and inner exception has more meaningful message.
                throw e.InnerException;
            }

            return model;
        }
    }
}
