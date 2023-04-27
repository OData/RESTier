﻿using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.Middleware
{

    /// <summary>
    /// Fixes the issue outlined in https://github.com/OData/WebApi/issues/2294
    /// </summary>
    /// <remarks>
    /// Solution adapted from https://stackoverflow.com/questions/71338662/ihttpcontextaccessor-httpcontext-is-null-after-execution-falls-out-of-the-useoda
    /// </remarks>
    public class ODataBatchHttpContextFixerMiddleware
    {

        #region Private Members

        private readonly RequestDelegate requestDelegate;

        #endregion

        #region Constructor

        /// <summary>
        /// The default constructor for the middleware.
        /// </summary>
        /// <param name="requestDelegate"></param>
        public ODataBatchHttpContextFixerMiddleware(RequestDelegate requestDelegate)
        {
            this.requestDelegate = requestDelegate;
        }

        #endregion

        #region Middleware

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="contextAccessor">The <see cref="IHttpContextAccessor"/> injected from DI for the current request,</param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext httpContext, IHttpContextAccessor contextAccessor)
        {
            contextAccessor.HttpContext ??= httpContext;
            await requestDelegate(httpContext);
        }

        #endregion

    }

}

