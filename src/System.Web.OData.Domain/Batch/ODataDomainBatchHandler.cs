// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Batch;
using System.Web.OData.Batch;
using Microsoft.Data.Domain;
using Microsoft.OData.Core;

namespace System.Web.OData.Domain.Batch
{
    public class ODataDomainBatchHandler : DefaultODataBatchHandler
    {
        public ODataDomainBatchHandler(HttpServer httpServer, Func<DomainContext> contextFactory = null)
            : base(httpServer)
        {
            this.ContextFactory = contextFactory;
        }

        public Func<DomainContext> ContextFactory { get; set; }

        public override async Task<IList<ODataBatchRequestItem>> ParseBatchRequestsAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (this.ContextFactory == null)
            {
                throw new InvalidOperationException("ODataDomainBatchHandler was called without a DomainContext Factory.");
            }

            Ensure.NotNull(request, "request");

            ODataMessageReaderSettings oDataReaderSettings = new ODataMessageReaderSettings
            {
                DisableMessageStreamDisposal = true,
                MessageQuotas = MessageQuotas,
                BaseUri = GetBaseUri(request)
            };

            ODataMessageReader reader = await request.Content.GetODataMessageReaderAsync(oDataReaderSettings, cancellationToken);
            request.RegisterForDispose(reader);

            List<ODataBatchRequestItem> requests = new List<ODataBatchRequestItem>();
            ODataBatchReader batchReader = reader.CreateODataBatchReader();
            Guid batchId = Guid.NewGuid();
            while (batchReader.Read())
            {
                if (batchReader.State == ODataBatchReaderState.ChangesetStart)
                {
                    IList<HttpRequestMessage> changeSetRequests = await batchReader.ReadChangeSetRequestAsync(batchId, cancellationToken);
                    foreach (HttpRequestMessage changeSetRequest in changeSetRequests)
                    {
                        changeSetRequest.CopyBatchRequestProperties(request);
                    }
                    requests.Add(this.CreateChangeSetRequestItem(changeSetRequests));
                }
                else if (batchReader.State == ODataBatchReaderState.Operation)
                {
                    HttpRequestMessage operationRequest = await batchReader.ReadOperationRequestAsync(batchId, bufferContentStream: true, cancellationToken: cancellationToken);
                    operationRequest.CopyBatchRequestProperties(request);
                    requests.Add(new OperationRequestItem(operationRequest));
                }
            }

            return requests;
        }

        protected virtual ChangeSetRequestItem CreateChangeSetRequestItem(IList<HttpRequestMessage> changeSetRequests)
        {
            return new ODataDomainChangeSetRequestItem(changeSetRequests, this.ContextFactory());
        }
    }
}
