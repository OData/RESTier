// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using Microsoft.OData;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.AspNet
{
    /// <summary>
    /// An ExceptionFilter that is capable of serializing well-known exceptions to the client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    internal sealed class RestierExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private static readonly List<ExceptionHandlerDelegate> Handlers = new List<ExceptionHandlerDelegate>
        {
            HandleChangeSetValidationException,
            HandleCommonException
        };

        private delegate Task<HttpResponseMessage> ExceptionHandlerDelegate(
            HttpActionExecutedContext context,
            bool useVerboseErros,
            CancellationToken cancellationToken);

        /// <summary>
        /// The callback to execute when exception occurs.
        /// </summary>
        /// <param name="actionExecutedContext">The context where the action is executed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that represents the callback execution.</returns>
        public override async Task OnExceptionAsync(
            HttpActionExecutedContext actionExecutedContext,
            CancellationToken cancellationToken)
        {
            var config = actionExecutedContext.Request.GetConfiguration();
            var useVerboseErrors = config.IncludeErrorDetailPolicy == IncludeErrorDetailPolicy.Always ||
                (actionExecutedContext.Request.RequestUri.Host.ToUpperInvariant().Contains("LOCALHOST") && config.IncludeErrorDetailPolicy == IncludeErrorDetailPolicy.LocalOnly);

            foreach (var handler in Handlers)
            {
                var result = await handler.Invoke(actionExecutedContext, useVerboseErrors, cancellationToken).ConfigureAwait(false);

                if (result != null)
                {
                    actionExecutedContext.Response = result;
                    return;
                }
            }
        }

        private static async Task<HttpResponseMessage> HandleChangeSetValidationException(
           HttpActionExecutedContext context,
           bool useVerboseErros,
           CancellationToken cancellationToken)
        {
            if (context.Exception is ChangeSetValidationException validationException)
            {
                var result = new
                {
                    error = new
                    {
                        code = "",
                        innererror = new
                        {
                            message = validationException.Message,
                            type = validationException.GetType().FullName
                        },
                        message = "Validaion failed for one or more objects.",
                        validationentries = validationException.ValidationResults
                    },
                };

                var exceptionResult = new NegotiatedContentResult<object>(
                    (HttpStatusCode)422,
                    result,
                    context.ActionContext.RequestContext.Configuration.Services.GetContentNegotiator(),
                    context.Request,
                    new MediaTypeFormatterCollection());

                return await exceptionResult.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        private static Task<HttpResponseMessage> HandleCommonException(
            HttpActionExecutedContext context,
            bool useVerboseErrors,
            CancellationToken cancellationToken)
        {
            var exception = context.Exception.Demystify();
            if (exception is AggregateException)
            {
                // In async call, the exception will be wrapped as AggregateException
                exception = exception.InnerException.Demystify();
            }

            if (exception == null)
            {
                return Task.FromResult<HttpResponseMessage>(null);
            }

            HttpStatusCode code;
            switch (true)
            {
                case true when exception is StatusCodeException:
                    code = (exception as StatusCodeException).StatusCode;
                    break;
                case true when exception is ODataException:
                    code = HttpStatusCode.BadRequest;
                    break;
                case true when exception is SecurityException:
                    code = HttpStatusCode.Forbidden;
                    break;
                case true when exception is NotImplementedException:
                    code = HttpStatusCode.NotImplemented;
                    break;
                default:
                    code = HttpStatusCode.InternalServerError;
                    break;
            }

            // When exception occured in a ChangeSet request,
            // exception must be handled in OnChangeSetCompleted
            // to avoid deadlock in Github Issue #82.
            var changeSetProperty = context.Request.GetChangeSet();
            if (changeSetProperty != null)
            {
                changeSetProperty.Exceptions.Add(exception);
                changeSetProperty.OnChangeSetCompleted();
            }

            if (code != HttpStatusCode.Unused)
            {
                if (useVerboseErrors)
                {
                    return Task.FromResult(context.Request.CreateErrorResponse(code, exception));
                }

                return Task.FromResult(context.Request.CreateErrorResponse(code, exception.Message));
            }

            return Task.FromResult<HttpResponseMessage>(null);
        }
    }
}
