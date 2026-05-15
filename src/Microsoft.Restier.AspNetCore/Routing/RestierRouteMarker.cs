// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Marker registered in per-route DI services AND attached as endpoint metadata so RESTier-specific
/// matcher policies and middleware can identify Restier routes and look up the user's API type
/// without re-scanning <see cref="Microsoft.AspNetCore.OData.ODataOptions"/>.
/// </summary>
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
