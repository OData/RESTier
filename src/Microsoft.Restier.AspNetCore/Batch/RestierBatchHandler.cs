// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

            HttpRequest request = context.Request;
            IServiceProvider requestContainer = request.CreateRouteServices(PrefixName);
            requestContainer.GetRequiredService<ODataMessageReaderSettings>().BaseUri = GetBaseUri(request);

            // TODO: JWS: needs to be a constructor dependency probably, but that's impossible now.
            var api = requestContainer.GetRequiredService<ApiBase>();

            CancellationToken cancellationToken = context.RequestAborted;

            // Pre-resolve $ContentId references in the buffered batch body before the framework's
            // batch reader runs. Microsoft.AspNetCore.OData 9.5.0 added a parse-time guard that
            // rejects any sub-request URI outside the service root (e.g. POST http://host/$1/Books)
            // during ReadChangeSetRequestAsync — which executes before RESTier's execution-time
            // ChangeSetDependencyResolver. Resolving the references here keeps that guard intact
            // (resolved URLs are under the service root) while restoring $ContentId support.
            await PreResolveContentIdReferencesInBodyAsync(
                request, requestContainer, api.Model, GetBaseUri(request), cancellationToken).ConfigureAwait(false);

            using var reader = request.GetODataMessageReader(requestContainer);

            var requests = new List<ODataBatchRequestItem>();
            var batchReader = await reader.CreateODataBatchReaderAsync().ConfigureAwait(false);
            var batchId = Guid.NewGuid();
            IDictionary<string, string> contentToLocationMapping = new ConcurrentDictionary<string, string>();

            while (await batchReader.ReadAsync().ConfigureAwait(false))
            {
                if (batchReader.State == ODataBatchReaderState.ChangesetStart)
                {
                    IList<HttpContext> changeSetContexts = await batchReader.ReadChangeSetRequestAsync(context, batchId, cancellationToken).ConfigureAwait(false);
                    foreach (HttpContext changeSetContext in changeSetContexts)
                    {
                        // changeSetContext.Request.CopyBatchRequestProperties(context.Request);
                        changeSetContext.Request.ClearRouteServices();
                    }

                    ChangeSetRequestItem requestItem = CreateRestierBatchChangeSetRequestItem(api, changeSetContexts);
                    requestItem.ContentIdToLocationMapping = contentToLocationMapping;
                    requests.Add(requestItem);
                }
                else if (batchReader.State == ODataBatchReaderState.Operation)
                {
                    // JWS: TODO: Is this correct? Shouldn't we use the api to send the operation requests to?
                    HttpContext operationContext = await batchReader.ReadOperationRequestAsync(context, batchId, cancellationToken).ConfigureAwait(false);
                    // operationContext.Request.CopyBatchRequestProperties(context.Request);
                    operationContext.Request.ClearRouteServices();
                    OperationRequestItem requestItem = new OperationRequestItem(operationContext);
                    requestItem.ContentIdToLocationMapping = contentToLocationMapping;
                    requests.Add(requestItem);
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

        /// <summary>
        /// Matches the request line of a batch sub-request (e.g. <c>POST http://host/$1/Books HTTP/1.1</c>).
        /// </summary>
        private static readonly Regex RequestLineRegex = new Regex(
            @"^(?<method>GET|POST|PUT|PATCH|DELETE|MERGE)[ \t]+(?<url>\S+)[ \t]+HTTP/\d(?:\.\d)?[ \t]*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Rewrites $ContentId references in the batch body's sub-request URLs so they resolve to
        /// concrete entity URLs before the framework's batch reader validates them against the
        /// OData service root.
        /// </summary>
        /// <param name="request">The outer batch request whose body is buffered and rewritten in place.</param>
        /// <param name="requestContainer">The per-route request container used to read the batch.</param>
        /// <param name="model">The EDM model, used to derive entity keys for POST references.</param>
        /// <param name="baseUri">The service base URI, used to absolutize relative sub-request URLs.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        private static async Task PreResolveContentIdReferencesInBodyAsync(
            HttpRequest request,
            IServiceProvider requestContainer,
            IEdmModel model,
            Uri baseUri,
            CancellationToken cancellationToken)
        {
            // Buffer the batch body so it can be read once to build the ContentId map and, if
            // needed, rewritten before the framework re-reads it.
            byte[] originalBytes;
            using (var buffer = new MemoryStream())
            {
                await request.Body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                originalBytes = buffer.ToArray();
            }

            var bodyText = Encoding.UTF8.GetString(originalBytes);

            // Fast path: no '$' means there cannot be a ContentId reference to resolve.
            if (bodyText.IndexOf('$') < 0)
            {
                request.Body = new MemoryStream(originalBytes);
                return;
            }

            // First pass: read the operations with the core OData batch reader (which does not
            // apply the AspNetCore service-root validation) to map each Content-ID to the entity
            // URL its request will create or target.
            var contentIdToEntityUrl = new Dictionary<string, string>(StringComparer.Ordinal);
            request.Body = new MemoryStream(originalBytes);
            using (var reader = request.GetODataMessageReader(requestContainer))
            {
                var batchReader = await reader.CreateODataBatchReaderAsync().ConfigureAwait(false);
                while (await batchReader.ReadAsync().ConfigureAwait(false))
                {
                    if (batchReader.State != ODataBatchReaderState.Operation)
                    {
                        continue;
                    }

                    var operationMessage = await batchReader.CreateOperationRequestMessageAsync().ConfigureAwait(false);
                    var contentId = operationMessage.ContentId;
                    if (string.IsNullOrEmpty(contentId))
                    {
                        continue;
                    }

                    var operationUri = operationMessage.Url;
                    if (operationUri is not null && !operationUri.IsAbsoluteUri && baseUri is not null)
                    {
                        operationUri = new Uri(baseUri, operationUri);
                    }

                    if (operationUri is null)
                    {
                        continue;
                    }

                    using var operationStream = await operationMessage.GetStreamAsync().ConfigureAwait(false);
                    var entityUrl = ChangeSetDependencyResolver.ComputeEntityUrl(
                        operationMessage.Method, operationUri, operationStream, model);

                    if (!string.IsNullOrEmpty(entityUrl))
                    {
                        contentIdToEntityUrl[contentId] = entityUrl;
                    }
                }
            }

            // Rewrite each sub-request line's URL, substituting any resolvable $ContentId reference.
            var rewritten = RewriteRequestLineUrls(bodyText, contentIdToEntityUrl);
            var finalBytes = ReferenceEquals(rewritten, bodyText)
                ? originalBytes
                : Encoding.UTF8.GetBytes(rewritten);

            request.Body = new MemoryStream(finalBytes);
            request.ContentLength = finalBytes.Length;
        }

        /// <summary>
        /// Replaces $ContentId references in every sub-request line's URL using the supplied map.
        /// Returns the original string reference when nothing changed.
        /// </summary>
        private static string RewriteRequestLineUrls(string bodyText, IDictionary<string, string> contentIdToEntityUrl)
        {
            if (contentIdToEntityUrl.Count == 0)
            {
                return bodyText;
            }

            return RequestLineRegex.Replace(bodyText, match =>
            {
                var url = match.Groups["url"].Value;
                var resolved = ChangeSetDependencyResolver.ResolveContentIdReferenceUrl(url, contentIdToEntityUrl);

                return string.Equals(resolved, url, StringComparison.Ordinal)
                    ? match.Value
                    : match.Value.Replace(url, resolved);
            });
        }
    }
}
