// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Restier.AspNetCore.Middleware;

namespace Microsoft.Restier.AspNetCore
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/> to add Restier middleware.
    /// </summary>
    public static class RestierApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds middleware that sets <see cref="System.Security.Claims.ClaimsPrincipal.Current"/> from the current
        /// <see cref="Microsoft.AspNetCore.Http.HttpContext.User"/> so it is available in async contexts.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
        public static IApplicationBuilder UseClaimsPrincipals(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RestierClaimsPrincipalMiddleware>();
        }
    }
}
