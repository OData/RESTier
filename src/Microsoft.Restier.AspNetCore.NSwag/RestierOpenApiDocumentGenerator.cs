// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.OpenApi.OData;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.Core;
using System;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.NSwag
{

    /// <summary>
    /// Generates OpenAPI documents from Restier EDM models. Shared logic used by
    /// <see cref="RestierOpenApiMiddleware"/>.
    /// </summary>
    internal static class RestierOpenApiDocumentGenerator
    {

        /// <summary>
        /// The document name used for Restier routes registered with an empty prefix.
        /// </summary>
        public const string DefaultDocumentName = "default";

        /// <summary>
        /// Generates an <see cref="OpenApiDocument"/> for the specified Restier route.
        /// </summary>
        /// <param name="documentName">The document name. May be a version group name (e.g., "v1") or a route prefix.</param>
        /// <param name="odataOptions">The OData options.</param>
        /// <param name="request">The current HTTP request, or null.</param>
        /// <param name="openApiSettings">Optional settings configurator.</param>
        /// <param name="registry">Optional version registry. If non-null and non-empty, group-name lookup is tried first.</param>
        /// <returns>The generated document, or null if the route was not found.</returns>
        public static OpenApiDocument GenerateDocument(
            string documentName,
            ODataOptions odataOptions,
            HttpRequest request,
            Action<OpenApiConvertSettings> openApiSettings,
            IRestierApiVersionRegistry registry = null)
        {
            var routePrefix = ResolveRoutePrefix(documentName, registry);

            if (!odataOptions.RouteComponents.TryGetValue(routePrefix, out var routeComponent))
            {
                return null;
            }

            var model = routeComponent.EdmModel;
            var routeServices = odataOptions.GetRouteServices(routePrefix);
            var validationOptions = routeServices.GetService<RestierValidationOptions>();
            var topExample = Routing.RestierValidationOptionsResolver.ResolveMaxTop(validationOptions, odataOptions) ?? 5;

            var settings = new OpenApiConvertSettings { TopExample = topExample };
            openApiSettings?.Invoke(settings);

            if (request is not null)
            {
                var pathParts = new[]
                {
                    $"{request.Scheme}:/",
                    request.Host.Value,
                    request.PathBase.HasValue ? request.PathBase.Value.TrimStart('/') : null,
                    routePrefix
                };
                settings.ServiceRoot = new Uri(string.Join("/", pathParts.Where(c => !string.IsNullOrWhiteSpace(c))));
            }

            return model.ConvertToOpenApi(settings);
        }

        /// <summary>
        /// Resolves a route prefix from a document name. When the registry has descriptors,
        /// the registry's group-name lookup wins for matching names; otherwise (or when no
        /// match) the existing rule applies: <c>"default"</c> → empty prefix, anything else →
        /// itself.
        /// </summary>
        private static string ResolveRoutePrefix(string documentName, IRestierApiVersionRegistry registry)
        {
            if (registry is { Descriptors.Count: > 0 })
            {
                var descriptor = registry.FindByGroupName(documentName);
                if (descriptor is not null)
                {
                    return descriptor.RoutePrefix;
                }
            }

            return string.Equals(documentName, DefaultDocumentName, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : documentName;
        }

    }

}
