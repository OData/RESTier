// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.OData;
using Swashbuckle.AspNetCore.Swagger;
using System;

namespace Microsoft.Restier.AspNetCore.Swagger
{

    /// <summary>
    /// 
    /// </summary>
    public class RestierSwaggerProvider : ISwaggerProvider
    {

        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IPerRouteContainer perRouteContainer;
        private readonly Action<OpenApiConvertSettings> openApiSettings;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <param name="perRouteContainer"></param>
        /// <param name="openApiSettings"></param>
        public RestierSwaggerProvider(IHttpContextAccessor httpContextAccessor, IPerRouteContainer perRouteContainer, Action<OpenApiConvertSettings> openApiSettings = null)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.perRouteContainer = perRouteContainer;
            this.openApiSettings = openApiSettings;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="documentName"></param>
        /// <param name="host"></param>
        /// <param name="basePath"></param>
        /// <returns></returns>
        public OpenApiDocument GetSwagger(string documentName, string host = null, string basePath = null)
        {
            var services = perRouteContainer.GetODataRootContainer(documentName);
            var model = services.GetRequiredService<IEdmModel>();
            var odataValidationSettings = services.GetRequiredService<ODataValidationSettings>();
            var defaultQuerySettings = services.GetRequiredService<DefaultQuerySettings>();

            // @robertmclaws: Start off by setting defaults, but allow the user to override it.
            var settings = new OpenApiConvertSettings { TopExample = odataValidationSettings?.MaxTop ?? defaultQuerySettings?.MaxTop ?? 5 };
            openApiSettings?.Invoke(settings);

            // @robertmclaws: The host defaults internally to localhost; isn't set automatically.
            var requestHost = httpContextAccessor.HttpContext.Request.Host.Value;
            if (!settings.ServiceRoot.ToString().StartsWith(requestHost))
            {
                settings.ServiceRoot = new Uri($"{httpContextAccessor.HttpContext.Request.Scheme}://{requestHost}");
            }

            return model.ConvertToOpenApi(settings);
        }

    }

}
