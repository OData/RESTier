// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Batch;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNet.Batch
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
        public override async Task<IList<ODataBatchRequestItem>> ParseBatchRequestsAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Ensure.NotNull(request, nameof(request));

            var requestContainer = request.CreateRequestContainer(ODataRouteName);
            requestContainer.GetRequiredService<ODataMessageReaderSettings>().BaseUri = GetBaseUri(request);

            // TODO: JWS: needs to be a constructor dependency probably, but that's impossible now.
            var api = requestContainer.GetRequiredService<ApiBase>();

#pragma warning disable CA1062 // Validate public arguments
            var reader = await request.Content.GetODataMessageReaderAsync(requestContainer, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1062 // Validate public arguments
            request.RegisterForDispose(reader);

            var requests = new List<ODataBatchRequestItem>();
            var batchReader = await reader.CreateODataBatchReaderAsync().ConfigureAwait(false);
            var batchId = Guid.NewGuid();
            while (await batchReader.ReadAsync().ConfigureAwait(false))
            {
                if (batchReader.State == ODataBatchReaderState.ChangesetStart)
                {
                    var changeSetRequests = await batchReader.ReadChangeSetRequestAsync(batchId, cancellationToken).ConfigureAwait(false);
                    foreach (var changeSetRequest in changeSetRequests)
                    {
                        changeSetRequest.CopyBatchRequestProperties(request);
                        changeSetRequest.DeleteRequestContainer(false);
                    }

                    requests.Add(CreateRestierBatchChangeSetRequestItem(api, changeSetRequests));
                }
                else if (batchReader.State == ODataBatchReaderState.Operation)
                {
                    var operationRequest = await batchReader.ReadOperationRequestAsync(batchId, true, cancellationToken).ConfigureAwait(false);
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
        /// <param name="api">A reference to the Api.</param>
        /// <param name="changeSetRequests">The list of changeset requests.</param>
        /// <returns>The created <see cref="RestierBatchChangeSetRequestItem"/> instance.</returns>
        protected virtual RestierBatchChangeSetRequestItem CreateRestierBatchChangeSetRequestItem(ApiBase api, IList<HttpRequestMessage> changeSetRequests) => 
            new RestierBatchChangeSetRequestItem(api, changeSetRequests);
    }
}
