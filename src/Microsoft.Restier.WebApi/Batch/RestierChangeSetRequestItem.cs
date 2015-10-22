﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Batch;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.WebApi.Batch
{
    /// <summary>
    /// Represents an API <see cref="ChangeSet"/> request.
    /// </summary>
    public class RestierChangeSetRequestItem : ChangeSetRequestItem
    {
        private Func<IApi> apiFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierChangeSetRequestItem" /> class.
        /// </summary>
        /// <param name="requests">The request messages.</param>
        /// <param name="apiFactory">Gets or sets the callback to create API.</param>
        public RestierChangeSetRequestItem(IEnumerable<HttpRequestMessage> requests, Func<IApi> apiFactory)
            : base(requests)
        {
            Ensure.NotNull(apiFactory, "apiFactory");

            this.apiFactory = apiFactory;
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
            List<Task<HttpResponseMessage>> responseTasks = new List<Task<HttpResponseMessage>>();
            foreach (HttpRequestMessage request in Requests)
            {
                responseTasks.Add(SendMessageAsync(invoker, request, cancellationToken, contentIdToLocationMapping));
            }

            // the responseTasks will be complete after:
            // - the ChangeSet is submitted
            // - the responses are created and
            // - the controller actions have returned
            await Task.WhenAll(responseTasks);

            List<HttpResponseMessage> responses = new List<HttpResponseMessage>();
            try
            {
                foreach (Task<HttpResponseMessage> responseTask in responseTasks)
                {
                    HttpResponseMessage response = responseTask.Result;
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

        internal async Task SubmitChangeSet(ChangeSet changeSet)
        {
            using (var api = this.apiFactory())
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
                request.Properties.Add("Microsoft.Restier.Submit.ChangeSet", changeSetProperty);
            }
        }
    }
}
