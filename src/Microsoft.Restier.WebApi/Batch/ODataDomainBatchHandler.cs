// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Batch;
using System.Web.OData.Batch;
using Microsoft.OData.Core;
using Microsoft.Restier.Core;
using Microsoft.Restier.WebApi.Properties;

namespace Microsoft.Restier.WebApi.Batch
{
    /// <summary>
    /// Default implementation of <see cref="ODataBatchHandler"/> in RESTier.
    /// </summary>
    public class RestierBatchHandler : DefaultODataBatchHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierBatchHandler" /> class.
        /// </summary>
        /// <param name="httpServer">The HTTP server instance.</param>
        /// <param name="apiFactory">Gets or sets the callback to create API.</param>
        public RestierBatchHandler(HttpServer httpServer, Func<IApi> apiFactory = null)
            : base(httpServer)
        {
            this.ApiFactory = apiFactory;
        }

        /// <summary>
        /// Gets or sets the callback to create API.
        /// </summary>
        public Func<IApi> ApiFactory { get; set; }

        /// <summary>
        /// Asynchronously parses the batch requests.
        /// </summary>
        /// <param name="request">The HTTP request that contains the batch requests.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that represents this asynchronous operation.</returns>
        public override async Task<IList<ODataBatchRequestItem>> ParseBatchRequestsAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (this.ApiFactory == null)
            {
                throw new InvalidOperationException(Resources.BatchHandlerRequiresApiContextFactory);
            }

            Ensure.NotNull(request, "request");

            ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings
            {
                DisableMessageStreamDisposal = true,
                MessageQuotas = MessageQuotas,
                BaseUri = GetBaseUri(request)
            };

            ODataMessageReader reader =
                await request.Content.GetODataMessageReaderAsync(readerSettings, cancellationToken);
            request.RegisterForDispose(reader);

            List<ODataBatchRequestItem> requests = new List<ODataBatchRequestItem>();
            ODataBatchReader batchReader = reader.CreateODataBatchReader();
            Guid batchId = Guid.NewGuid();
            while (batchReader.Read())
            {
                if (batchReader.State == ODataBatchReaderState.ChangesetStart)
                {
                    IList<HttpRequestMessage> changeSetRequests =
                        await batchReader.ReadChangeSetRequestAsync(batchId, cancellationToken);
                    foreach (HttpRequestMessage changeSetRequest in changeSetRequests)
                    {
                        changeSetRequest.CopyBatchRequestProperties(request);
                    }

                    requests.Add(this.CreateChangeSetRequestItem(changeSetRequests));
                }
                else if (batchReader.State == ODataBatchReaderState.Operation)
                {
                    HttpRequestMessage operationRequest = await batchReader.ReadOperationRequestAsync(
                        batchId,
                        bufferContentStream: true,
                        cancellationToken: cancellationToken);
                    operationRequest.CopyBatchRequestProperties(request);
                    requests.Add(new OperationRequestItem(operationRequest));
                }
            }

            return requests;
        }

        /// <summary>
        /// Creates the <see cref="RestierChangeSetRequestItem"/> instance.
        /// </summary>
        /// <param name="changeSetRequests">The list of changeset requests.</param>
        /// <returns>The created <see cref="RestierChangeSetRequestItem"/> instance.</returns>
        protected virtual ChangeSetRequestItem CreateChangeSetRequestItem(IList<HttpRequestMessage> changeSetRequests)
        {
            return new RestierChangeSetRequestItem(changeSetRequests, this.ApiFactory);
        }
    }
}
