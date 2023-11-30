﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// 
    /// </summary>
    public static class Restier_GraphQL_IServiceCollectionExtensions
    {

        ///<summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <remarks>
        /// If Restier is API format-gnostic, then registering *everything* will need to happen in the API format library (here).
        /// </remarks>
        public static IServiceCollection AddRestier(this IServiceCollection services)
        {
            return services;
        }

    }

}
