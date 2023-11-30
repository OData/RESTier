// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Restier.Core;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.OData.Middleware
{

    /// <summary>
    /// 
    /// </summary>
    public class RestierODataMiddleware<TApi>
    {

        #region Private Members

        private readonly RequestDelegate requestDelegate;

        #endregion

        #region Constructor

        /// <summary>
        /// The default constructor for the middleware.
        /// </summary>
        /// <param name="requestDelegate"></param>
        public RestierODataMiddleware(RequestDelegate requestDelegate)
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
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext httpContext, ProcessingPipeline<TApi> pipeline)
        {
            // @robertmclaws: This is a temporary hack until routing is implemented and we can test routes.
            if (!httpContext.Request.Path.Value.StartsWith("/restier"))
            {
                await requestDelegate(httpContext);
                return;
            }

            switch (httpContext.Request.Method)
            {
                case "GET":
                    await pipeline.ProcessQueryAsync(httpContext.ToQueryContext(), httpContext.RequestAborted);
                    break;
                case "POST":
                case "PUT":
                case "PATCH":
                case "DELETE":
                    // @robertmclaws: Leverage OData features to determine if this is a batch, an attached operation, or an entity set request.
                    //pipeline.ProcessOperationAsync(httpContext.ToOperationContext(), httpContext.RequestAborted);
                    //pipeline.ProcessSubmissionAsync(httpContext.ToSubmissionContext(), httpContext.RequestAborted);
                    break;
                default:
                    break;
            }
            // @robertmclaws: Steps:
            //  - Get the URL
            //  - Map to an entity set or function
            //  - Generate the parameters for the pipeline
            //  - Process the pipeline.
            await requestDelegate(httpContext);
        }

        #endregion



    }

}
