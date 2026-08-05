// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore.Middleware;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Auto-wires OData batching middleware whenever any Restier route has
/// <c>RestierRouteOptions.UseRestierBatching = true</c> (the default). Inserts
/// <c>UseODataBatching()</c> and the <see cref="ODataBatchHttpContextFixerMiddleware"/>
/// at the start of the pipeline so consumers don't have to remember to register them
/// by hand.
/// </summary>
internal sealed class RestierBatchingStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            if (AnyRestierRouteHasBatching(app.ApplicationServices))
            {
                app.UseODataBatching();
                app.UseMiddleware<ODataBatchHttpContextFixerMiddleware>();
            }

            next(app);
        };
    }

    private static bool AnyRestierRouteHasBatching(IServiceProvider rootServices)
    {
        var odataOptions = rootServices.GetService<IOptions<ODataOptions>>()?.Value;
        if (odataOptions is null)
        {
            return false;
        }

        foreach (var (prefix, _) in odataOptions.RouteComponents)
        {
            var routeServices = odataOptions.GetRouteServices(prefix);
            if (routeServices is null)
            {
                continue;
            }

            if (routeServices.GetService(typeof(RestierRouteMarker)) is null)
            {
                continue;
            }

            if (routeServices.GetService(typeof(ODataBatchHandler)) is not null)
            {
                return true;
            }
        }

        return false;
    }
}
