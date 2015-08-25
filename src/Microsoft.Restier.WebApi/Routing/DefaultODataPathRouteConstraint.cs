// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.WebApi.Routing
{
    /// <summary>
    /// The default implementation of <see cref="ODataPathRouteConstraint"/>.
    /// </summary>
    public class DefaultODataPathRouteConstraint : ODataPathRouteConstraint
    {
        private readonly Func<IDomain> domainFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultODataPathRouteConstraint" /> class.
        /// </summary>
        /// <param name="pathHandler">The path handler.</param>
        /// <param name="model">The EDM model.</param>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routingConventions">The routing convention.</param>
        /// <param name="domainFactory">The domain factory.</param>
        public DefaultODataPathRouteConstraint(
            IODataPathHandler pathHandler,
            IEdmModel model,
            string routeName,
            IEnumerable<IODataRoutingConvention> routingConventions,
            Func<IDomain> domainFactory)
            : base(pathHandler, model, routeName, routingConventions)
        {
            this.domainFactory = domainFactory;
        }

        /// <summary>
        /// Determines whether this instance equals a specified route.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="route">The route to compare.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="values">A list of parameter values.</param>
        /// <param name="routeDirection">The route direction.</param>
        /// <returns>
        /// True if this instance equals a specified route; otherwise, false.
        /// </returns>
        public override bool Match(
            HttpRequestMessage request,
            IHttpRoute route,
            string parameterName,
            IDictionary<string, object> values,
            HttpRouteDirection routeDirection)
        {
            request.SetDomainFactory(this.domainFactory);
            return base.Match(request, route, parameterName, values, routeDirection);
        }

        /// <summary>
        /// Selects the controller to handle the request.
        /// </summary>
        /// <param name="path">The OData path of the request.</param>
        /// <param name="request">The incoming request.</param>
        /// <returns>The name of the controller.</returns>
        protected override string SelectControllerName(ODataPath path, HttpRequestMessage request)
        {
            var controllers = request.GetConfiguration().Services.GetHttpControllerSelector().GetControllerMapping();
            foreach (var routingConvention in RoutingConventions)
            {
                var controllerName = routingConvention.SelectController(path, request);
                if (controllerName != null)
                {
                    HttpControllerDescriptor descriptor;
                    if (controllers.TryGetValue(controllerName, out descriptor) && descriptor != null)
                    {
                        return controllerName;
                    }
                }
            }

            return null;
        }
    }
}
