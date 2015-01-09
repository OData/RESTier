// Copyright (c) Microsoft Corporation.  All rights reserved.
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
    public class ODataDomainChangeSetRequestItem : ChangeSetRequestItem
    {
        private DomainContext context;

        public ODataDomainChangeSetRequestItem(IEnumerable<HttpRequestMessage> requests, DomainContext context)
            : base(requests)
        {
            Ensure.NotNull(context, "context");

            this.context = context;
        }

        public override async Task<ODataBatchResponseItem> SendRequestAsync(HttpMessageInvoker invoker, CancellationToken cancellationToken)
        {
            Ensure.NotNull(invoker, "invoker");

            ODataDomainChangeSetProperty changeSetProperty = new ODataDomainChangeSetProperty(this);
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
            SubmitResult submitResults = await Domain.SubmitAsync(this.context, changeSet);
        }

        private void SetChangeSetProperty(ODataDomainChangeSetProperty changeSetProperty)
        {
            foreach (HttpRequestMessage request in this.Requests)
            {
                request.Properties.Add("Microsoft.Restier.Submit.ChangeSet", changeSetProperty);
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
    }
}
