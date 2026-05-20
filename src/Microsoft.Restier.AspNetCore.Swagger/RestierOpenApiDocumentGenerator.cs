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

namespace Microsoft.Restier.AspNetCore.Swagger
{

    /// <summary>
    /// Generates OpenAPI documents from Restier EDM models.
    /// Shared logic used by both the net8.0 middleware and the net9.0+ document transformer.
    /// </summary>
    internal static class RestierOpenApiDocumentGenerator
    {

        public const string DefaultDocumentName = "default";

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

            var settings = new OpenApiConvertSettings { TopExample = validationOptions?.MaxTop ?? 5 };
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
