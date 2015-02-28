// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.WebApi.Filters
{
    /// <summary>
    /// An ExceptionFilter that is capable of serializing well-known exceptions to the client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class ODataDomainExceptionFilterAttribute : ExceptionFilterAttribute
    {
        public override async Task OnExceptionAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            IHttpActionResult exceptionResult = null;

            ValidationException validationException = actionExecutedContext.Exception as ValidationException;
            if (validationException != null)
            {
                exceptionResult = new NegotiatedContentResult<IEnumerable<ValidationResultDto>>(
                    HttpStatusCode.BadRequest,
                    validationException.ValidationResults.Select(r => new ValidationResultDto(r)),
                    actionExecutedContext.ActionContext.RequestContext.Configuration.Services.GetContentNegotiator(),
                    actionExecutedContext.Request,
                    new MediaTypeFormatterCollection());
            }

            if (exceptionResult != null)
            {
                actionExecutedContext.Response = await exceptionResult.ExecuteAsync(cancellationToken);
            }
        }
    }
}
