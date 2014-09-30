// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using Microsoft.Data.Domain.Submit;

namespace System.Web.OData.Domain.Filters
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
                    HttpStatusCode.InternalServerError,
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
