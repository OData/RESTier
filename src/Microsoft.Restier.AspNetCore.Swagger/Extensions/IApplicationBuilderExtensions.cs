// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// 
    /// </summary>
    public static class Restier_AspNetCore_Swagger_IApplicationBuilderExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="addUI"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRestierSwagger(this IApplicationBuilder app, bool addUI = true)
        {
            app.UseSwagger();

            if (addUI)
            {
                app.UseSwaggerUI(c =>
                {
                    var rrb = app.ApplicationServices.GetRequiredService<RestierRouteBuilder>();
                    foreach (var route in rrb.Routes)
                    { 
                        c.SwaggerEndpoint($"/swagger/{route.Key}/swagger.json", route.Value.RouteName);
                    }
                    //c.DocumentTitle
                });
            }
            return app;
        }

    }

}