// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.OData;
using Microsoft.Restier.AspNetCore.Versioning;
using System;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.NSwag
{

    /// <summary>
    /// Middleware that serves OpenAPI documents generated from Restier EDM models at
    /// <c>/openapi/{documentName}/openapi.json</c>. NSwag UI hosts (configured via
    /// <c>UseRestierReDoc</c> / <c>UseRestierNSwagUI</c>) load these URLs.
    /// </summary>
    internal class RestierOpenApiMiddleware
    {

        private const string PathPrefix = "/openapi/";
        private const string PathSuffix = "/openapi.json";

        private readonly RequestDelegate next;
        private readonly IOptions<ODataOptions> odataOptions;
        private readonly Action<OpenApiConvertSettings> openApiSettings;
        private readonly IServiceProvider rootServices;

        public RestierOpenApiMiddleware(
            RequestDelegate next,
            IOptions<ODataOptions> odataOptions,
            IServiceProvider rootServices,
            Action<OpenApiConvertSettings> openApiSettings = null)
        {
            this.next = next;
            this.odataOptions = odataOptions;
            this.rootServices = rootServices;
            this.openApiSettings = openApiSettings;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            if (path is not null
                && path.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase)
                && path.EndsWith(PathSuffix, StringComparison.OrdinalIgnoreCase))
            {
                if (path.Length <= PathPrefix.Length + PathSuffix.Length)
                {
                    await next(context);
                    return;
                }

                var documentName = path.Substring(PathPrefix.Length, path.Length - PathPrefix.Length - PathSuffix.Length);
                if (!string.IsNullOrEmpty(documentName))
                {
                    // Touching IOptions<ODataOptions>.Value already happens inside GenerateDocument
                    // via the odataOptions.RouteComponents read; for the registry, ensure the
                    // configurator has run by reading .Value first (materialization invariant).
                    var options = odataOptions.Value;
                    var registry = rootServices.GetService<IRestierApiVersionRegistry>();

                    var document = RestierOpenApiDocumentGenerator.GenerateDocument(
                        documentName,
                        options,
                        context.Request,
                        openApiSettings,
                        registry);

                    if (document is not null)
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";
                        var json = await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0);
                        await context.Response.WriteAsync(json);
                        return;
                    }

                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
            }

            await next(context);
        }

    }

}
