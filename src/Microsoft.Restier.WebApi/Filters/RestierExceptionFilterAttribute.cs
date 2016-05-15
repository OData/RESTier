﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
using Microsoft.OData.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.WebApi.Query;

namespace Microsoft.Restier.WebApi.Filters
{
    /// <summary>
    /// An ExceptionFilter that is capable of serializing well-known exceptions to the client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    internal sealed class RestierExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private static readonly List<ExceptionHandlerDelegate> Handlers = new List<ExceptionHandlerDelegate>
            {
                Handler400,
                Handler403,
                Handler404
            };

        private delegate Task<HttpResponseMessage> ExceptionHandlerDelegate(
            HttpActionExecutedContext context,
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
            foreach (var handler in Handlers)
            {
                var result = await handler.Invoke(actionExecutedContext, cancellationToken);

                if (result != null)
                {
                    actionExecutedContext.Response = result;
                    return;
                }
            }
        }

        private static async Task<HttpResponseMessage> Handler400(
           HttpActionExecutedContext context,
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

            var odataException = context.Exception as ODataException;
            if (odataException != null)
            {
                return context.Request.CreateErrorResponse(HttpStatusCode.BadRequest, context.Exception);
            }

            return null;
        }

        private static Task<HttpResponseMessage> Handler403(
            HttpActionExecutedContext context,
            CancellationToken cancellationToken)
        {
            if (context.Exception is SecurityException)
            {
                return Task.FromResult(
                    context.Request.CreateErrorResponse(HttpStatusCode.Forbidden, context.Exception));
            }

            return Task.FromResult<HttpResponseMessage>(null);
        }

        private static Task<HttpResponseMessage> Handler404(
            HttpActionExecutedContext context,
            CancellationToken cancellationToken)
        {
            var notSupportedException = context.Exception as NotSupportedException;
            if (notSupportedException != null)
            {
                if (notSupportedException.TargetSite.DeclaringType == typeof(RestierQueryBuilder))
                {
                    throw new HttpResponseException(context.Request.CreateErrorResponse(
                        HttpStatusCode.NotFound,
                        notSupportedException.Message));
                }
            }

            return Task.FromResult<HttpResponseMessage>(null);
        }
    }
}
