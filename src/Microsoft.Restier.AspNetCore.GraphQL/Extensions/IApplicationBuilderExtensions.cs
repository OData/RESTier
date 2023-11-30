// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Restier.AspNetCore.OData.Middleware;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// 
    /// </summary>
    public static class Restier_GraphQL_IApplicationBuilderExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRestier<TApi>(this IApplicationBuilder app)
        {
            app.UseMiddleware<RestierGraphQLMiddleware<TApi>>();
            return app;
        }

    }

}