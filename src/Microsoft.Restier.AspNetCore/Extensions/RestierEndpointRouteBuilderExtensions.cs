// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to map Restier dynamic routes.
/// </summary>
public static class RestierEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps dynamic catch-all routes for all registered Restier APIs.
    /// Call this after MapControllers().
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add routes to.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapRestier(this IEndpointRouteBuilder endpoints)
    {
        var odataOptions = endpoints.ServiceProvider
            .GetRequiredService<IOptions<ODataOptions>>().Value;

        foreach (var (prefix, _) in odataOptions.RouteComponents)
        {
            // Only map routes for Restier APIs (identified by the RestierRouteMarker sentinel).
            var routeServices = odataOptions.GetRouteServices(prefix);
            if (routeServices.GetService(typeof(RestierRouteMarker)) is null)
            {
                continue;
            }

            var pattern = string.IsNullOrEmpty(prefix)
                ? "{**odataPath}"
                : prefix + "/{**odataPath}";

            endpoints.MapDynamicControllerRoute<RestierRouteValueTransformer>(pattern, state: prefix);
        }

        return endpoints;
    }
}
