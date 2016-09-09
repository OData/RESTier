// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

extern alias Net;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using Microsoft.OData;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Net::System.Net.Http.Formatting;

namespace Microsoft.Restier.Publishers.OData
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
            var useVerboseErros = config.GetUseVerboseErrors();

            foreach (var handler in Handlers)
            {
                var result = await handler.Invoke(actionExecutedContext, useVerboseErros, cancellationToken);

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
            ChangeSetValidationException validationException = context.Exception as ChangeSetValidationException;
            if (validationException != null)
            {
                var exceptionResult = new NegotiatedContentResult<IEnumerable<ValidationResultDto>>(
                    HttpStatusCode.BadRequest,
                    validationException.ValidationResults.Select(r => new ValidationResultDto(r)),
                    context.ActionContext.RequestContext.Configuration.Services.GetContentNegotiator(),
                    context.Request,
                    new MediaTypeFormatterCollection());
                return await exceptionResult.ExecuteAsync(cancellationToken);
            }

            return null;
        }

        private static Task<HttpResponseMessage> HandleCommonException(
            HttpActionExecutedContext context,
            bool useVerboseErros,
            CancellationToken cancellationToken)
        {
            var exception = context.Exception;
            if (exception is AggregateException)
            {
                // In async call, the exception will be wrapped as AggregateException
                exception = exception.InnerException;
            }

            if (exception == null)
            {
                return Task.FromResult<HttpResponseMessage>(null);
            }

            HttpStatusCode code = HttpStatusCode.Unused;
            if (exception is ODataException)
            {
                code = HttpStatusCode.BadRequest;
            }
            else if (exception is SecurityException)
            {
                code = HttpStatusCode.Forbidden;
            }
            else if (exception is ResourceNotFoundException)
            {
                code = HttpStatusCode.NotFound;
            }
            else if (exception is PreconditionFailedException)
            {
                code = HttpStatusCode.PreconditionFailed;
            }
            else if (exception is PreconditionRequiredException)
            {
                code = (HttpStatusCode)428;
            }
            else if (context.Exception is NotImplementedException)
            {
                code = HttpStatusCode.NotImplemented;
            }

            // When exception occured in a ChangeSet request,
            // exception must be handled in OnChangeSetCompleted
            // to avoid deadlock in Github Issue #82.
            var changeSetProperty = context.Request.GetChangeSet();
            if (changeSetProperty != null)
            {
                changeSetProperty.Exceptions.Add(exception);
                changeSetProperty.OnChangeSetCompleted(context.Request);
            }

            if (code != HttpStatusCode.Unused)
            {
                if (useVerboseErros)
                {
                    return Task.FromResult(context.Request.CreateErrorResponse(code, exception));
                }

                return Task.FromResult(context.Request.CreateErrorResponse(code, exception.Message));
            }

            return Task.FromResult<HttpResponseMessage>(null);
        }
    }
}
