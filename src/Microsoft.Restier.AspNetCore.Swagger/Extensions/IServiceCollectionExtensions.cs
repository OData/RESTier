// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OpenApi.OData;
using Microsoft.Restier.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.Swagger;
using System;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// 
    /// </summary>
    public static class Restier_AspNetCore_Swagger_IServiceCollectionExtensions
    {

        /// <summary>
        /// Adds the required services to use Swagger with Restier.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register Swagger services with.</param>
        /// <param name="openApiSettings">An <see cref="Action{OpenApiConvertSettings}"/> that allows you to configure the core Swagger output.</param>
        /// <returns></returns>
        public static IServiceCollection AddRestierSwagger(this IServiceCollection services, Action<OpenApiConvertSettings> openApiSettings = null)
        {
            services.AddScoped<ISwaggerProvider, RestierSwaggerProvider>();
            if (openApiSettings is not null)
            {
                services.AddScoped(sp => openApiSettings);
            }
            return services;
        }

    }

}
