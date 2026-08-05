// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.NSwag;
using Microsoft.Restier.AspNetCore.Versioning;
using NSwag.AspNetCore;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Extension methods on <see cref="IApplicationBuilder"/> for Restier NSwag support.
    /// </summary>
    public static class Restier_AspNetCore_NSwag_IApplicationBuilderExtensions
    {

        /// <summary>
        /// Adds middleware that serves OpenAPI 3.0 JSON for every registered Restier route at
        /// <c>/openapi/{documentName}/openapi.json</c>.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add middleware to.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
        public static IApplicationBuilder UseRestierOpenApi(this IApplicationBuilder app)
        {
            app.UseMiddleware<RestierOpenApiMiddleware>();
            return app;
        }

        /// <summary>
        /// Adds NSwag's ReDoc middleware once per Restier route, configured with the matching
        /// <c>/openapi/{name}/openapi.json</c> document URL.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add middleware to.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
        public static IApplicationBuilder UseRestierReDoc(this IApplicationBuilder app)
        {
            // Materialization invariant: read .Value first.
            var odataOptions = app.ApplicationServices
                .GetRequiredService<IOptions<ODataOptions>>().Value;
            var registry = app.ApplicationServices
                .GetService<IRestierApiVersionRegistry>();

            var hasRegistryDescriptors = registry is { Descriptors.Count: > 0 };
            var registryPrefixes = hasRegistryDescriptors
                ? new System.Collections.Generic.HashSet<string>(
                    registry.Descriptors.Select(d => d.RoutePrefix), System.StringComparer.Ordinal)
                : new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);

            if (hasRegistryDescriptors)
            {
                foreach (var descriptor in registry.Descriptors)
                {
                    var documentName = descriptor.GroupName;
                    app.UseReDoc(settings =>
                    {
                        settings.Path = $"/redoc/{documentName}";
                        settings.DocumentPath = $"/openapi/{documentName}/openapi.json";
                    });
                }
            }

            foreach (var prefix in odataOptions.GetRestierRoutePrefixes())
            {
                if (registryPrefixes.Contains(prefix))
                {
                    continue;
                }

                var documentName = string.IsNullOrEmpty(prefix)
                    ? RestierOpenApiDocumentGenerator.DefaultDocumentName
                    : prefix;
                app.UseReDoc(settings =>
                {
                    settings.Path = $"/redoc/{documentName}";
                    settings.DocumentPath = $"/openapi/{documentName}/openapi.json";
                });
            }

            return app;
        }

        /// <summary>
        /// Adds NSwag's Swagger UI 3 host at <c>/swagger</c> with a dropdown listing every Restier route.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add middleware to.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
        public static IApplicationBuilder UseRestierNSwagUI(this IApplicationBuilder app)
        {
            // Materialization invariant: read .Value first.
            var odataOptions = app.ApplicationServices
                .GetRequiredService<IOptions<ODataOptions>>().Value;
            var registry = app.ApplicationServices
                .GetService<IRestierApiVersionRegistry>();
            var nswagDocuments = app.ApplicationServices.GetServices<OpenApiDocumentRegistration>();

            var hasRegistryDescriptors = registry is { Descriptors.Count: > 0 };
            var registryPrefixes = hasRegistryDescriptors
                ? new System.Collections.Generic.HashSet<string>(
                    registry.Descriptors.Select(d => d.RoutePrefix), System.StringComparer.Ordinal)
                : new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);

            app.UseSwaggerUi(settings =>
            {
                settings.Path = "/swagger";

                if (hasRegistryDescriptors)
                {
                    foreach (var descriptor in registry.Descriptors)
                    {
                        var documentName = descriptor.GroupName;
                        settings.SwaggerRoutes.Add(new SwaggerUiRoute(documentName, $"/openapi/{documentName}/openapi.json"));
                    }
                }

                foreach (var prefix in odataOptions.GetRestierRoutePrefixes())
                {
                    if (registryPrefixes.Contains(prefix))
                    {
                        continue;
                    }

                    var documentName = string.IsNullOrEmpty(prefix)
                        ? RestierOpenApiDocumentGenerator.DefaultDocumentName
                        : prefix;
                    settings.SwaggerRoutes.Add(new SwaggerUiRoute(documentName, $"/openapi/{documentName}/openapi.json"));
                }

                foreach (var registration in nswagDocuments)
                {
                    settings.SwaggerRoutes.Add(new SwaggerUiRoute(registration.DocumentName, $"/swagger/{registration.DocumentName}/swagger.json"));
                }
            });
            return app;
        }

    }

}
