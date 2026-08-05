// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Swagger;
using Microsoft.Restier.AspNetCore.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Extension methods on <see cref="IApplicationBuilder"/> for Restier Swagger support.
    /// </summary>
    public static class Restier_AspNetCore_Swagger_IApplicationBuilderExtensions
    {

        /// <summary>
        /// Adds middleware to serve OpenAPI documents and the Swagger UI for all registered Restier routes.
        /// Registry descriptors are enumerated first (one endpoint per <c>GroupName</c>), then any remaining
        /// route prefixes not represented in the registry.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add middleware to.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
        public static IApplicationBuilder UseRestierSwaggerUI(this IApplicationBuilder app)
        {
            app.UseMiddleware<RestierOpenApiMiddleware>();

            // Materialization invariant.
            var odataOptions = app.ApplicationServices
                .GetRequiredService<IOptions<ODataOptions>>().Value;
            var registry = app.ApplicationServices.GetService<IRestierApiVersionRegistry>();

            var hasRegistryDescriptors = registry is { Descriptors.Count: > 0 };
            var registryPrefixes = hasRegistryDescriptors
                ? new HashSet<string>(registry.Descriptors.Select(d => d.RoutePrefix), StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            app.UseSwaggerUI(c =>
            {
                if (hasRegistryDescriptors)
                {
                    foreach (var descriptor in registry.Descriptors)
                    {
                        var documentName = descriptor.GroupName;
                        c.SwaggerEndpoint($"swagger/{documentName}/swagger.json", documentName);
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

                    c.SwaggerEndpoint($"swagger/{documentName}/swagger.json", documentName);
                }
            });

            return app;
        }

    }

}
