// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Extensions;
using Microsoft.Restier.AspNetCore.Middleware;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// 
    /// </summary>
    public static class Restier_IApplicationBuilderExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseClaimsPrincipals(this IApplicationBuilder app)
        {
            app.UseMiddleware<RestierClaimsPrincipalMiddleware>();
            return app;
        }

        /// <summary>
        /// Register the app for Restier OData Batching.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance to enhance.</param>
        /// <returns>The fluent <see cref="IApplicationBuilder"/> instance.</returns>
        public static IApplicationBuilder UseRestierBatching(this IApplicationBuilder app)
        {

//#if NET6_0_OR_GREATER

//            // RWM: The 7.x version of AspNetCore.OData has a sync bug. Silently do the best thing we can do for now.
//            app.Use(async (context, next) =>
//            {
//                if (context.Request.Path.ToString().Contains(ODataRouteConstants.Batch))
//                {
//                    var syncIoFeature = context.Features.Get<IHttpBodyControlFeature>();
//                    if (syncIoFeature != null)
//                    {
//                        syncIoFeature.AllowSynchronousIO = true;
//                    }
//                }

//                await next();
//            });
//#endif
            app.UseODataBatching();
            // RWM: This call fixes issues where the batch processor irresponsibly disposes of the HttpContext before it should.
            app.UseMiddleware<ODataBatchHttpContextFixerMiddleware>();
            return app;
        }

    }

}