// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;
using System;

namespace Microsoft.Restier.AspNetCore
{

    /// <summary>
    /// 
    /// </summary>
    internal static class ODataEndpointRoutingPattern
    {

        /// <summary>
        /// Wildcard route template for the OData Endpoint route pattern.
        /// </summary>
        public static readonly string ODataEndpointRoutingPath = "ODataEndpointPath_";

        /// <summary>
        /// Wildcard route template for the OData path route variable.
        /// </summary>
        public static readonly string ODataEndpointRoutingTemplate = "{{**" + ODataEndpointRoutingPath + "{0}}}";

        /// <summary>
        /// Create an OData Endpoint route pattern.
        /// The route pattern is in this format: "routePrefix/{*ODataEndpointPath_routeName}"
        /// </summary>
        /// <param name="routeName">The route name. It can not be null and verify upper layer.</param>
        /// <param name="routePrefix">The route prefix. It could be null or empty</param>
        /// <returns>The OData route endpoint pattern.</returns>
        public static string CreateODataEndpointRoutingPattern(string routeName, string routePrefix)
        {
            Ensure.NotNull(routeName, nameof(routeName));

            return string.IsNullOrEmpty(routePrefix) ?
                string.Format(ODataEndpointRoutingTemplate, routeName) :
                routePrefix + "/" + string.Format(ODataEndpointRoutingTemplate, routeName);
        }

        /// <summary>
        /// Get the OData route name and path value.
        /// </summary>
        /// <param name="values">The dictionary contains route value.</param>
        /// <returns>A tuple contains the route name and path value.</returns>
        public static (string, object) GetODataRouteInfo(this RouteValueDictionary values)
        {
            Ensure.NotNull(values, nameof(values));

            string routeName = null;
            object odataPathValue = null;
            foreach (var item in values)
            {
                var keyString = item.Key;

                if (keyString.StartsWith(ODataEndpointRoutingPath))
                {
                    routeName = keyString.Substring(ODataEndpointRoutingPath.Length);
                    odataPathValue = item.Value;
                    break;
                }
            }

            return (routeName, odataPathValue);
        }

    }

}
