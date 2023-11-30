// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// 
    /// </summary>
    public static class Restier_OData_IServiceCollectionExtensions
    {

        ///<summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <remarks>
        /// If Restier is API format-gnostic, then registering *everything* will need to happen in the API format library (here).
        /// </remarks>
        public static IServiceCollection AddRestier<TApi>(this IServiceCollection services) where TApi: class
        {
            services.AddScoped<TApi>();
            services.AddScoped<ProcessingPipeline<TApi>>();
            //services.AddOData(); @robertmclaws: right now you can't add OData services without adding MVC controllers.
            return services;
        }
    }

}
