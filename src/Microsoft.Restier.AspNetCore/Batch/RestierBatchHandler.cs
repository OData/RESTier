// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNetCore.Batch
{
    /// <summary>
    /// Default implementation of <see cref="ODataBatchHandler"/> in RESTier.
    /// </summary>
    public class RestierBatchHandler : DefaultODataBatchHandler
    {
        /// <summary>
        /// Asynchronously parses the batch requests.
        /// </summary>
        /// <param name="context">The HTTP context that contains the batch requests.</param>
        /// <returns>The task object that represents this asynchronous operation.</returns>
        public override async Task<IList<ODataBatchRequestItem>> ParseBatchRequestsAsync(HttpContext context)
        {
            Ensure.NotNull(context, nameof(context));

            var requestContainer = context.Request.CreateRequestContainer(this.ODataRouteName);
            requestContainer.GetRequiredService<ODataMessageReaderSettings>().BaseUri = this.GetBaseUri(context.Request);

            // TODO: JWS: needs to be a constructor dependency probably, but that's impossible now.
            var api = requestContainer.GetRequiredService<ApiBase>();

#pragma warning disable CA1062 // Validate public arguments
            using var reader = context.Request.GetODataMessageReader(requestContainer);
#pragma warning restore CA1062 // Validate public arguments

            var requests = new List<ODataBatchRequestItem>();
            var batchReader = await reader.CreateODataBatchReaderAsync().ConfigureAwait(false);
            var batchId = Guid.NewGuid();
            while (await batchReader.ReadAsync().ConfigureAwait(false))
            {
                if (batchReader.State == ODataBatchReaderState.ChangesetStart)
                {
                    var changeSetContexts = await batchReader.ReadChangeSetRequestAsync(context, batchId, context.RequestAborted).ConfigureAwait(false);
                    foreach (var changeSetContext in changeSetContexts)
                    {
                        changeSetContext.Request.CopyBatchRequestProperties(context.Request);
                        changeSetContext.Request.DeleteRequestContainer(false);
                    }

                    requests.Add(this.CreateRestierBatchChangeSetRequestItem(api, changeSetContexts));
                }
                else if (batchReader.State == ODataBatchReaderState.Operation)
                {
                    var operationContext = await batchReader.ReadOperationRequestAsync(context, batchId, true, context.RequestAborted).ConfigureAwait(false);
                    operationContext.Request.CopyBatchRequestProperties(context.Request);
                    operationContext.Request.DeleteRequestContainer(false);
                    requests.Add(new OperationRequestItem(operationContext));
                }
            }

            return requests;
        }

        /// <summary>
        /// Creates the <see cref="RestierBatchChangeSetRequestItem"/> instance.
        /// </summary>
        /// <param name="api">A reference to the Api.</param>
        /// <param name="changeSetContexts">The list of changeset contexts.</param>
        /// <returns>The created <see cref="RestierBatchChangeSetRequestItem"/> instance.</returns>
        protected virtual RestierBatchChangeSetRequestItem CreateRestierBatchChangeSetRequestItem(ApiBase api, IList<HttpContext> changeSetContexts)
            => new RestierBatchChangeSetRequestItem(api, changeSetContexts);
    }
}
