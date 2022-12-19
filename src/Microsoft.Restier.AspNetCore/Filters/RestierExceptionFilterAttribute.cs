// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.OData;
using Microsoft.Restier.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore
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
            HandleCommonException,
        };

        private delegate Task<bool> ExceptionHandlerDelegate(
            ExceptionContext context,
            CancellationToken cancellationToken);

        /// <summary>
        /// The callback to execute when exception occurs.
        /// </summary>
        /// <param name="context">The context where the exception occured.</param>
        /// <returns>The task object that represents the callback execution.</returns>
        public override async Task OnExceptionAsync(ExceptionContext context)
        {
            foreach (var handler in Handlers)
            {
                if (await handler.Invoke(context, context.HttpContext.RequestAborted).ConfigureAwait(false))
                {
                    return;
                }
            }
        }

        private static Task<bool> HandleChangeSetValidationException(
            ExceptionContext context,
            CancellationToken cancellationToken)
        {
            if (context.Exception is ChangeSetValidationException validationException)
            {
                var result = new
                {
                    error = new
                    {
                        code = string.Empty,
                        innererror = new
                        {
                            message = validationException.Message,
                            type = validationException.GetType().FullName,
                        },
                        message = "Validaion failed for one or more objects.",
                        validationentries = validationException.ValidationResults,
                    },
                };

                // TODO: check that content negotiation etc works.
                var objectResult = new UnprocessableEntityObjectResult(result);
                context.Result = objectResult;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static Task<bool> HandleCommonException(ExceptionContext context, CancellationToken cancellationToken)
        {
            var exception = context.Exception.Demystify();
            if (exception is AggregateException)
            {
                // In async call, the exception will be wrapped as AggregateException
                exception = exception.InnerException.Demystify();
            }

            if (exception is null)
            {
                return Task.FromResult(false);
            }

            HttpStatusCode code;
            switch (true)
            {
                case true when exception is StatusCodeException statusCodeException:
                    code = statusCodeException.StatusCode;
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
                case true when exception is TargetInvocationException && exception.InnerException is ArgumentNullException:
                    exception = exception.InnerException;
                    code = HttpStatusCode.BadRequest;
                    break;
                default:
                    code = HttpStatusCode.InternalServerError;
                    break;
            }

            Trace.TraceError($"Exception: {exception.Message} \nStackTrace: {exception.StackTrace}");

            // When exception occured in a ChangeSet request,
            // exception must be handled in OnChangeSetCompleted
            // to avoid deadlock in Github Issue #82.
            var changeSetProperty = context.HttpContext.GetChangeSet();
            if (changeSetProperty is not null)
            {
                changeSetProperty.Exceptions.Add(exception);
                changeSetProperty.OnChangeSetCompleted();
            }

            if (code != HttpStatusCode.Unused)
            {
                var response = EnableQueryAttribute.CreateErrorResponse(exception.Message, exception);

                //RWM: Don't leak stack traces if we're not local.
                if (!context.HttpContext.Request.IsLocal())
                {
                    response.Remove("StackTrace");
                }

                // RWM: BadRequestObjectResult extends ObjectResult with a specified code. So just use ObjectResult every time.
                context.Result = new ObjectResult(response)
                {
                    StatusCode = (int)code,
                };

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
