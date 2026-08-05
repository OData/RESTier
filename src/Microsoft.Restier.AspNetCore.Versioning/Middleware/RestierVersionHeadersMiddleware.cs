// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;

namespace Microsoft.Restier.AspNetCore.Versioning.Middleware
{

    /// <summary>
    /// Emits <c>api-supported-versions</c>, <c>api-deprecated-versions</c>, and <c>Sunset</c>
    /// response headers on requests whose path matches a registered Restier versioned route.
    /// Headers are scoped to the matched descriptor's <see cref="RestierApiVersionDescriptor.BasePrefix"/>
    /// group so unrelated APIs at other base prefixes do not leak versions into each other's headers.
    /// Headers are applied via <see cref="HttpResponse.OnStarting(System.Func{object,Task},object)"/>
    /// so they fire after the inner pipeline, just before the response begins.
    /// </summary>
    internal sealed class RestierVersionHeadersMiddleware
    {

        private readonly RequestDelegate _next;
        private readonly IRestierApiVersionRegistry _registry;
        private readonly IOptions<ODataOptions> _odataOptions;

        public RestierVersionHeadersMiddleware(
            RequestDelegate next,
            IRestierApiVersionRegistry registry,
            IOptions<ODataOptions> odataOptions)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _odataOptions = odataOptions ?? throw new ArgumentNullException(nameof(odataOptions));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Materialization invariant: ensure the registry has been populated.
            _ = _odataOptions.Value;

            var matched = TryMatch(_registry, context.Request.Path);
            if (matched is not null)
            {
                context.Response.OnStarting(static state =>
                {
                    var (response, descriptor, registry) = ((HttpResponse, RestierApiVersionDescriptor, IRestierApiVersionRegistry))state;
                    ApplyHeaders(response, descriptor, registry);
                    return Task.CompletedTask;
                }, (context.Response, matched, _registry));
            }

            await _next(context);
        }

        /// <summary>
        /// Longest-prefix-match against the registry. Uses <see cref="PathString.StartsWithSegments(PathString)"/>
        /// for segment-boundary safety. <see cref="HttpRequest.Path"/> is already
        /// <see cref="HttpRequest.PathBase"/>-relative when middleware see it, so we don't need to
        /// strip <c>PathBase</c> ourselves.
        /// </summary>
        internal static RestierApiVersionDescriptor TryMatch(IRestierApiVersionRegistry registry, PathString path)
        {
            RestierApiVersionDescriptor longest = null;
            foreach (var descriptor in registry.Descriptors)
            {
                var candidate = new PathString("/" + descriptor.RoutePrefix);
                if (path.StartsWithSegments(candidate))
                {
                    if (longest is null || descriptor.RoutePrefix.Length > longest.RoutePrefix.Length)
                    {
                        longest = descriptor;
                    }
                }
            }

            return longest;
        }

        private static void ApplyHeaders(HttpResponse response, RestierApiVersionDescriptor matched, IRestierApiVersionRegistry registry)
        {
            var group = registry.FindByBasePrefix(matched.BasePrefix);

            if (!response.Headers.ContainsKey("api-supported-versions"))
            {
                var supported = string.Join(", ", group.Select(d => d.Version));
                if (supported.Length > 0)
                {
                    response.Headers["api-supported-versions"] = supported;
                }
            }

            if (!response.Headers.ContainsKey("api-deprecated-versions"))
            {
                var deprecated = string.Join(", ", group.Where(d => d.IsDeprecated).Select(d => d.Version));
                if (deprecated.Length > 0)
                {
                    response.Headers["api-deprecated-versions"] = deprecated;
                }
            }

            if (matched.SunsetDate is { } sunset && !response.Headers.ContainsKey("Sunset"))
            {
                response.Headers["Sunset"] = sunset.UtcDateTime.ToString("R", CultureInfo.InvariantCulture);
            }
        }

    }

}
