// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Restier.Core;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.OData.Middleware
{

    /// <summary>
    /// 
    /// </summary>
    public class RestierMiddleware
    {

        #region Private Members

        private readonly RequestDelegate requestDelegate;

        #endregion

        #region Constructor

        /// <summary>
        /// The default constructor for the middleware.
        /// </summary>
        /// <param name="requestDelegate"></param>
        public RestierMiddleware(RequestDelegate requestDelegate)
        {
            this.requestDelegate = requestDelegate;
        }

        #endregion

        #region Middleware

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="pipeline"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext httpContext, ProcessingPipeline<string> pipeline, 
            CancellationToken cancellationToken)
        {
            // @robertmclaws: Steps:
            //  - Get the URL
            //  - Map to an entity set or function
            //  - Generate the parameters for the pipeline
            //  - Process the pipeline.
            await pipeline.ProcessQueryAsync(cancellationToken);
            await requestDelegate(httpContext);
        }

        #endregion

    }

}
