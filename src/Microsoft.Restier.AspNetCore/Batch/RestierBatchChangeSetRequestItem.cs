// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.AspNetCore.Batch
{
    /// <summary>
    /// Represents an API <see cref="ChangeSet"/> request.
    /// </summary>
    public class RestierBatchChangeSetRequestItem : ChangeSetRequestItem
    {
        /// <summary>
        /// An Api.
        /// </summary>
        private readonly ApiBase api;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierBatchChangeSetRequestItem" /> class.
        /// </summary>
        /// <param name="api">An Api.</param>
        /// <param name="contexts">The request messages.</param>
        public RestierBatchChangeSetRequestItem(ApiBase api, IEnumerable<HttpContext> contexts)
            : base(contexts)
        {
            Ensure.NotNull(api, nameof(api));
            this.api = api;
        }

        /// <summary>
        /// Asynchronously sends the request.
        /// </summary>
        /// <param name="handler">The handler for processing a message.</param>
        /// <returns>The task object that contains the batch response.</returns>
        public override async Task<ODataBatchResponseItem> SendRequestAsync(RequestDelegate handler)
        {
            Ensure.NotNull(handler, nameof(handler));

            var changeSetProperty = new RestierChangeSetProperty(this)
            {
                ChangeSet = new ChangeSet(),
            };
            SetChangeSetProperty(changeSetProperty);

            var contentIdToLocationMapping = new ConcurrentDictionary<string, string>();
            var responseTasks = new List<Task<Task<HttpContext>>>();

            foreach (var context in Contexts)
            {
                // Since exceptions may occur before the request is sent to RestierController,
                // we must catch the exceptions here and call OnChangeSetCompleted,
                // so as to avoid deadlock mentioned in GitHub Issue #82.
                var tcs = new TaskCompletionSource<HttpContext>();
                var task =
                    ODataBatchRequestItem.SendRequestAsync(handler, context, contentIdToLocationMapping)
                        .ContinueWith(
                            t =>
                            {
                                if (t.Exception is not null)
                                {
                                    var taskEx = (t.Exception.InnerExceptions is not null &&
                                                  t.Exception.InnerExceptions.Count == 1)
                                        ? t.Exception.InnerExceptions.First()
                                        : t.Exception;
                                    changeSetProperty.Exceptions.Add(taskEx);
                                    changeSetProperty.OnChangeSetCompleted();
                                    tcs.SetException(taskEx.Demystify());
                                }
                                else
                                {
                                    tcs.SetResult(context);
                                }

                                return tcs.Task;
                            },
                            context.RequestAborted,
                            TaskContinuationOptions.None,
                            TaskScheduler.Current);

                responseTasks.Add(task);
            }

            // the responseTasks will be complete after:
            // - the ChangeSet is submitted
            // - the responses are created and
            // - the controller actions have returned
            await Task.WhenAll(responseTasks).ConfigureAwait(false);

            var returnContexts = new List<HttpContext>();

            foreach (var responseTask in responseTasks)
            {
                var returnContext = responseTask.Result.Result;
                if (returnContext.Response.IsSuccessStatusCode())
                {
                    returnContexts.Add(returnContext);
                }
                else
                {
                    returnContexts.Clear();
                    returnContexts.Add(returnContext);
                    return new ChangeSetResponseItem(returnContexts);
                }
            }

            return new ChangeSetResponseItem(returnContexts);
        }

        /// <summary>
        /// Asynchronously submits a <see cref="ChangeSet"/>.
        /// </summary>
        /// <param name="changeSet">The change set to submit.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
#pragma warning disable CA1822 // Do not declare static members on generic types
        internal async Task SubmitChangeSet(ChangeSet changeSet)
#pragma warning restore CA1822 // Do not declare static members on generic types
        {
            var submitResults = await api.SubmitAsync(changeSet).ConfigureAwait(false);
        }

        private void SetChangeSetProperty(RestierChangeSetProperty changeSetProperty)
        {
            foreach (var context in Contexts)
            {
                context.SetChangeSet(changeSetProperty);
            }
        }
    }
}
