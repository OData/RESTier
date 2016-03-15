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
using Microsoft.Restier.WebApi.Batch;
using Microsoft.Restier.WebApi.Routing;
using System.Web.Http.Routing;
using System.Net.Http;
using Microsoft.OData.Edm.Library;

namespace Microsoft.Restier.WebApi
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
            Func<IApi> apiFactory,
            RestierBatchHandler batchHandler = null)
            where TApi : ApiBase
        {
            Ensure.NotNull(apiFactory, "apiFactory");

            HttpRouteCollection routes = config.Routes;
            ////routePrefix = HttpConfigurationExtensions.RemoveTrailingSlash(routePrefix);
            if (batchHandler != null && batchHandler.ApiFactory == null)
            {
                batchHandler.ApiFactory = apiFactory;
                batchHandler.ODataRouteName = routeName;
                string routeTemplate = string.IsNullOrEmpty(routePrefix) ?
                    ODataRouteConstants.Batch : (routePrefix + '/' + ODataRouteConstants.Batch);
                routes.MapHttpBatchRoute(routeName + "Batch", routeTemplate, batchHandler);
            }

            var pathHandler = new DefaultODataPathHandler();
            ////defaultODataPathHandler.ResolverSetttings = config.GetResolverSettings();

            var pathConstraint = new RestierRouteConstraint(pathHandler, routeName, apiFactory);
            ODataRoute oDataRoute = new ODataRoute(routePrefix, pathConstraint);
            routes.Add(routeName, oDataRoute);
            return Task.FromResult(oDataRoute);
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
        public static async Task<ODataRoute> MapRestierRoute<TApi>(
            this HttpConfiguration config,
            string routeName,
            string routePrefix,
            RestierBatchHandler batchHandler = null)
            where TApi : ApiBase, new()
        {
            return await MapRestierRoute<TApi>(
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
            this HttpConfiguration config, IEdmModel model, Func<IApi> apiFactory)
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

        private static void EnsurePayloadValueConverter(this IEdmModel model)
        {
            var payloadValueConverter = model.GetPayloadValueConverter();
            if (payloadValueConverter.GetType() == typeof(ODataPayloadValueConverter))
            {
                // User has not specified custom payload value converter
                // so use RESTier's default converter.
                model.SetPayloadValueConverter(RestierPayloadValueConverter.Default);
            }
        }

        private class RestierRouteConstraint : IHttpRouteConstraint
        {
            public ODataPathRouteConstraint Inner { get; private set; }

            public ODataPathRouteConstraint Placeholder { get; private set; }

            public Func<IApi> ApiFactory { get; private set; }

            public IEdmModel FinalModel { get; private set; }

            public RestierRouteConstraint(IODataPathHandler pathHandler, string routeName, Func<IApi> apiFactory)
            {
                var conventions = ODataRoutingConventions.CreateDefault();
                Placeholder = new ODataPathRouteConstraint(pathHandler, new EdmModel(), routeName, conventions);
                ApiFactory = apiFactory;
                Task.Delay(5000).ContinueWith(_ => TryInitModel());
            }

            public bool Match(
                HttpRequestMessage request,
                IHttpRoute route,
                string parameterName,
                IDictionary<string, object> values,
                HttpRouteDirection routeDirection)
            {
                if (Inner != null)
                {
                    return Inner.Match(request, route, parameterName, values, routeDirection);
                }

                if (FinalModel == null)
                {
                    return Placeholder.Match(request, route, parameterName, values, routeDirection);
                }

                var config = request.GetConfiguration();
                var conventions = ODataRoutingConventions.CreateDefault();
                var attributeConvention = new AttributeRoutingConvention(
                    FinalModel,
                    config.Services.GetHttpControllerSelector().GetControllerMapping().Values);
                conventions.Insert(0, attributeConvention);
                conventions.Insert(1, new RestierRoutingConvention(ApiFactory));

                Inner = new ODataPathRouteConstraint(
                    Placeholder.PathHandler, FinalModel, Placeholder.RouteName, conventions);
                Placeholder = null;
                ApiFactory = null;

                return Inner.Match(request, route, parameterName, values, routeDirection);
            }

            private void TryInitModel()
            {
                var api = ApiFactory();
                api.GetModelAsync().ContinueWith(
                    task =>
                    {
                        api.Dispose();
                        if (task.Status == TaskStatus.RanToCompletion)
                        {
                            task.Result.EnsurePayloadValueConverter();
                            FinalModel = task.Result;
                            return;
                        }

                        // Retry get model
                        Task.Delay(120).ContinueWith(
                            _ => TryInitModel(),
                            TaskContinuationOptions.ExecuteSynchronously);
                    },
                    TaskContinuationOptions.ExecuteSynchronously);
            }
        }
    }
}
