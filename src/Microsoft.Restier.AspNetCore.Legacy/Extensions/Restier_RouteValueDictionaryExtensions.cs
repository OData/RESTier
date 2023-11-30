// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.AspNetCore;
using System;

namespace Microsoft.AspNetCore.Routing
{

    /// <summary>
    /// 
    /// </summary>
    public static class Restier_RouteValueDictionaryExtensions
    {

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

                if (keyString.StartsWith(Restier_IEndpointRouteBuilderExtensions.ODataEndpointRoutingPath))
                {
                    routeName = keyString[Restier_IEndpointRouteBuilderExtensions.ODataEndpointRoutingPath.Length..];
                    odataPathValue = item.Value;
                    break;
                }
            }

            return (routeName, odataPathValue);
        }

    }

}
