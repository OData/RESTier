// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.WebApi.Routing
{
    public class DefaultODataPathRouteConstraint : ODataPathRouteConstraint
    {
        public DefaultODataPathRouteConstraint(IODataPathHandler pathHandler, IEdmModel model, string routeName,
            IEnumerable<IODataRoutingConvention> routingConventions)
            : base(pathHandler, model, routeName, routingConventions)
        {
        }

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
