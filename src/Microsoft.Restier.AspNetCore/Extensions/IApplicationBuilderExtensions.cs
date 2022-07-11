using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// 
    /// </summary>
    public static class IApplicationBuilderExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        [SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "<Pending>")]
        public static IApplicationBuilder UseClaimsPrincipals(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                ClaimsPrincipal.ClaimsPrincipalSelector = () => context.User;
                await next();
            });
            return app;
        }

        /// <summary>
        /// Register the app for Restier OData Batching.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance to enhance.</param>
        /// <returns>The flient <see cref="IApplicationBuilder"/> instance.</returns>
        public static IApplicationBuilder UseRestierBatching(this IApplicationBuilder app)
        {

#if NET6_0_OR_GREATER

            // RWM: The 7.x version of AspNetCore.OData has a sync bug. Silently do the best thing we can do for now.
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.ToString().Contains(ODataRouteConstants.Batch))
                {
                    var syncIoFeature = context.Features.Get<IHttpBodyControlFeature>();
                    if (syncIoFeature != null)
                    {
                        syncIoFeature.AllowSynchronousIO = true;
                    }
                }

                await next();
            });
#endif
            app.UseODataBatching();
            return app;
        }

    }

}