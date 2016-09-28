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
using System.Web.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;

namespace Microsoft.Restier.Publishers.OData.Batch
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
        public RestierBatchHandler(HttpServer httpServer)
            : base(httpServer)
        {
        }

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
            Ensure.NotNull(request, "request");

            IServiceProvider requestContainer = request.CreateRequestContainer(ODataRouteName);
            requestContainer.GetRequiredService<ODataMessageReaderSettings>().BaseUri = GetBaseUri(request);

            ODataMessageReader reader
                = await request.Content.GetODataMessageReaderAsync(requestContainer, cancellationToken);
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
                        changeSetRequest.DeleteRequestContainer(false);
                    }

                    requests.Add(this.CreateRestierBatchChangeSetRequestItem(changeSetRequests));
                }
                else if (batchReader.State == ODataBatchReaderState.Operation)
                {
                    HttpRequestMessage operationRequest = await batchReader.ReadOperationRequestAsync(
                        batchId, true, cancellationToken);
                    operationRequest.CopyBatchRequestProperties(request);
                    operationRequest.DeleteRequestContainer(false);
                    requests.Add(new OperationRequestItem(operationRequest));
                }
            }

            return requests;
        }

        /// <summary>
        /// Creates the <see cref="RestierBatchChangeSetRequestItem"/> instance.
        /// </summary>
        /// <param name="changeSetRequests">The list of changeset requests.</param>
        /// <returns>The created <see cref="RestierBatchChangeSetRequestItem"/> instance.</returns>
        protected virtual RestierBatchChangeSetRequestItem CreateRestierBatchChangeSetRequestItem(
            IList<HttpRequestMessage> changeSetRequests)
        {
            return new RestierBatchChangeSetRequestItem(changeSetRequests);
        }
    }
}
