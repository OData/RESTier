// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Marker registered in per-route DI services so RESTier-specific matcher policies and middleware
/// can identify Restier routes and look up the user's API type. Resolve it via
/// <c>odataOptions.GetRouteServices(routePrefix).GetService&lt;RestierRouteMarker&gt;()</c>
/// (or, for code that already holds an <see cref="Microsoft.AspNetCore.Http.HttpRequest"/>,
/// <c>request.GetRouteServices().GetService&lt;RestierRouteMarker&gt;()</c>).
/// </summary>
/// <remarks>
/// Attaching the marker to the dynamic-route endpoint's static metadata would let the matcher
/// policy filter cheaply at node-builder time, but the
/// <c>MapDynamicControllerRoute&lt;TTransformer&gt;(string, object)</c> overload returns
/// <see langword="void"/> — no <c>IEndpointConventionBuilder</c> is exposed for that registration —
/// so endpoint-metadata attachment is not currently possible without reflecting on internal
/// ASP.NET Core types. Per-request DI lookup is the chosen alternative.
/// </remarks>
internal sealed class RestierRouteMarker
{
    public RestierRouteMarker(Type apiType)
    {
        ApiType = apiType ?? throw new ArgumentNullException(nameof(apiType));
    }

    /// <summary>
    /// The concrete <see cref="Core.ApiBase"/> subclass registered for this route.
    /// </summary>
    public Type ApiType { get; }
}
