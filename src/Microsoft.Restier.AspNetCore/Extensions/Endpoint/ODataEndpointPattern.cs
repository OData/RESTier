using Microsoft.AspNetCore.Routing;
using System;

namespace Microsoft.Restier.AspNetCore
{
    internal static class ODataEndpointPattern
    {
        /// <summary>
        /// Wildcard route template for the OData Endpoint route pattern.
        /// </summary>
        public static readonly string ODataEndpointPath = "ODataEndpointPath_";

        /// <summary>
        /// Wildcard route template for the OData path route variable.
        /// </summary>
        public static readonly string ODataEndpointTemplate = "{{**" + ODataEndpointPath + "{0}}}";

        /// <summary>
        /// Create an OData Endpoint route pattern.
        /// The route pattern is in this format: "routePrefix/{*ODataEndpointPath_routeName}"
        /// </summary>
        /// <param name="routeName">The route name. It can not be null and verify upper layer.</param>
        /// <param name="routePrefix">The route prefix. It could be null or empty</param>
        /// <returns>The OData route endpoint pattern.</returns>
        public static string CreateODataEndpointPattern(string routeName, string routePrefix)
        {
            Ensure.NotNull(routeName, nameof(routeName));

            return string.IsNullOrEmpty(routePrefix) ?
                string.Format(ODataEndpointTemplate, routeName) :
                routePrefix + "/" + string.Format(ODataEndpointTemplate, routeName);
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
                string keyString = item.Key;

                if (keyString.StartsWith(ODataEndpointPath))
                {
                    routeName = keyString.Substring(ODataEndpointPath.Length);
                    odataPathValue = item.Value;
                    break;
                }
            }

            return (routeName, odataPathValue);
        }
    }
}
