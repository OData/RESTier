// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.AspNetCore.Versioning.Middleware;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Extension methods on <see cref="IApplicationBuilder"/> for the Restier API-versioning package.
    /// </summary>
    public static class Restier_AspNetCore_Versioning_IApplicationBuilderExtensions
    {

        /// <summary>
        /// Adds middleware that emits <c>api-supported-versions</c>, <c>api-deprecated-versions</c>,
        /// and <c>Sunset</c> response headers on requests targeting registered versioned Restier routes.
        /// </summary>
        public static IApplicationBuilder UseRestierVersionHeaders(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RestierVersionHeadersMiddleware>();
        }

    }

}
