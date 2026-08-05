// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.Middleware
{
    /// <summary>
    /// Fixes the issue outlined in https://github.com/OData/WebApi/issues/2294
    /// </summary>
    /// <remarks>
    /// Solution adapted from https://stackoverflow.com/questions/71338662/ihttpcontextaccessor-httpcontext-is-null-after-execution-falls-out-of-the-useoda
    /// </remarks>
    public class RestierClaimsPrincipalMiddleware
    {
        private readonly RequestDelegate requestDelegate;

        /// <summary>
        /// The default constructor for the middleware.
        /// </summary>
        /// <param name="requestDelegate"></param>
        public RestierClaimsPrincipalMiddleware(RequestDelegate requestDelegate)
        {
            this.requestDelegate = requestDelegate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="contextAccessor">The <see cref="IHttpContextAccessor"/> injected from DI for the current request,</param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext httpContext, IHttpContextAccessor contextAccessor)
        {
            contextAccessor.HttpContext ??= httpContext;
            ClaimsPrincipal.ClaimsPrincipalSelector = () => contextAccessor.HttpContext.User;
            await requestDelegate(httpContext);
        }
    }
}

