// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Batch;
using System.Web.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Publishers.OData.Batch
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
            Ensure.NotNull(invoker, "invoker");

            RestierChangeSetProperty changeSetProperty = new RestierChangeSetProperty(this);
            changeSetProperty.ChangeSet = new ChangeSet();
            this.SetChangeSetProperty(changeSetProperty);

            Dictionary<string, string> contentIdToLocationMapping = new Dictionary<string, string>();
            var responseTasks = new List<Task<Task<HttpResponseMessage>>>();

            foreach (HttpRequestMessage request in Requests)
            {
                // Since exceptions may occure before the request is sent to RestierController,
                // we must catch the exceptions here and call OnChangeSetCompleted,
                // so as to avoid deadlock mentioned in Github Issue #82.
                TaskCompletionSource<HttpResponseMessage> tcs = new TaskCompletionSource<HttpResponseMessage>();
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
            await Task.WhenAll(responseTasks);

            List<HttpResponseMessage> responses = new List<HttpResponseMessage>();
            try
            {
                foreach (var responseTask in responseTasks)
                {
                    HttpResponseMessage response = responseTask.Result.Result;
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

        internal async Task SubmitChangeSet(HttpRequestMessage request, ChangeSet changeSet)
        {
            var requestContainer = request.GetRequestContainer();
            using (var api = requestContainer.GetService<ApiBase>())
            {
                SubmitResult submitResults = await api.SubmitAsync(changeSet);
            }
        }

        private static void DisposeResponses(IEnumerable<HttpResponseMessage> responses)
        {
            foreach (HttpResponseMessage response in responses)
            {
                if (response != null)
                {
                    response.Dispose();
                }
            }
        }

        private void SetChangeSetProperty(RestierChangeSetProperty changeSetProperty)
        {
            foreach (HttpRequestMessage request in this.Requests)
            {
                request.SetChangeSet(changeSetProperty);
            }
        }
    }
}
