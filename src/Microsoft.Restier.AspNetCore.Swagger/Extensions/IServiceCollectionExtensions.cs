// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OpenApi.OData;
using System;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Extension methods on <see cref="IServiceCollection"/> for Restier Swagger support.
    /// </summary>
    public static class Restier_AspNetCore_Swagger_IServiceCollectionExtensions
    {

        /// <summary>
        /// Adds the required services to use Swagger with Restier.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register Swagger services with.</param>
        /// <param name="openApiSettings">An <see cref="Action{OpenApiConvertSettings}"/> that allows you to configure the core Swagger output.</param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddRestierSwagger(this IServiceCollection services, Action<OpenApiConvertSettings> openApiSettings = null)
        {
            services.AddHttpContextAccessor();

            if (openApiSettings is not null)
            {
                services.AddSingleton(openApiSettings);
            }

            return services;
        }

    }

}
