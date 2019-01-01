// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.AspNet.Batch
{
    /// <summary>
    /// Represents an API <see cref="ChangeSet"/> request.
    /// </summary>
    public class RestierBatchChangeSetRequestItem : ChangeSetRequestItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierBatchChangeSetRequestItem" /> class.
        /// </summary>
        /// <param name="requests">The request messages.</param>
        public RestierBatchChangeSetRequestItem(IEnumerable<HttpRequestMessage> requests)
            : base(requests)
        {
        }

        /// <summary>
        /// Asynchronously sends the request.
        /// </summary>
        /// <param name="invoker">The invoker.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that contains the batch response.</returns>
        public override async Task<ODataBatchResponseItem> SendRequestAsync(
            HttpMessageInvoker invoker,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(invoker, nameof(invoker));

            var changeSetProperty = new RestierChangeSetProperty(this)
            {
                ChangeSet = new ChangeSet()
            };
            SetChangeSetProperty(changeSetProperty);

            var contentIdToLocationMapping = new Dictionary<string, string>();
            var responseTasks = new List<Task<Task<HttpResponseMessage>>>();

            foreach (var request in Requests)
            {
                // Since exceptions may occure before the request is sent to RestierController,
                // we must catch the exceptions here and call OnChangeSetCompleted,
                // so as to avoid deadlock mentioned in Github Issue #82.
                var tcs = new TaskCompletionSource<HttpResponseMessage>();
                var task =
                    SendMessageAsync(invoker, request, cancellationToken, contentIdToLocationMapping)
                        .ContinueWith(
                            t =>
                            {
                                if (t.Exception != null)
                                {
                                    var taskEx = (t.Exception.InnerExceptions != null &&
                                                  t.Exception.InnerExceptions.Count == 1)
                                        ? t.Exception.InnerExceptions.First()
                                        : t.Exception;
                                    changeSetProperty.Exceptions.Add(taskEx);
                                    changeSetProperty.OnChangeSetCompleted(request);
                                    tcs.SetException(taskEx);
                                }
                                else
                                {
                                    tcs.SetResult(t.Result);
                                }

                                return tcs.Task;
                            },
                            cancellationToken);

                responseTasks.Add(task);
            }

            // the responseTasks will be complete after:
            // - the ChangeSet is submitted
            // - the responses are created and
            // - the controller actions have returned
            await Task.WhenAll(responseTasks).ConfigureAwait(false);

            var responses = new List<HttpResponseMessage>();
            try
            {
                foreach (var responseTask in responseTasks)
                {
                    var response = responseTask.Result.Result;
                    if (response.IsSuccessStatusCode)
                    {
                        responses.Add(response);
                    }
                    else
                    {
                        DisposeResponses(responses);
                        responses.Clear();
                        responses.Add(response);
                        return new ChangeSetResponseItem(responses);
                    }
                }
            }
            catch
            {
                DisposeResponses(responses);
                throw;
            }

            return new ChangeSetResponseItem(responses);
        }

#pragma warning disable CA1822 // Do not declare static members on generic types
        internal async Task SubmitChangeSet(HttpRequestMessage request, ChangeSet changeSet)
#pragma warning restore CA1822 // Do not declare static members on generic types

        {
            var requestContainer = request.GetRequestContainer();
            using (var api = requestContainer.GetService<ApiBase>())
            {
                var submitResults = await api.SubmitAsync(changeSet).ConfigureAwait(false);
            }
        }

        private static void DisposeResponses(IEnumerable<HttpResponseMessage> responses)
        {
            foreach (var response in responses)
            {
                if (response != null)
                {
                    response.Dispose();
                }
            }
        }

        private void SetChangeSetProperty(RestierChangeSetProperty changeSetProperty)
        {
            foreach (var request in Requests)
            {
                request.SetChangeSet(changeSetProperty);
            }
        }
    }
}
