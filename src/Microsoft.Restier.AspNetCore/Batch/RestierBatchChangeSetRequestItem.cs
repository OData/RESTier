// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Microsoft.Restier.AspNetCore.Batch
{
    /// <summary>
    /// Represents an API <see cref="ChangeSet"/> request.
    /// </summary>
    public class RestierBatchChangeSetRequestItem : ChangeSetRequestItem
    {
        /// <summary>
        /// An Api.
        /// </summary>
        private readonly ApiBase api;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierBatchChangeSetRequestItem" /> class.
        /// </summary>
        /// <param name="api">An Api.</param>
        /// <param name="contexts">The request messages.</param>
        public RestierBatchChangeSetRequestItem(ApiBase api, IEnumerable<HttpContext> contexts)
            : base(contexts)
        {
            Ensure.NotNull(api, nameof(api));
            this.api = api;
        }

        /// <summary>
        /// Asynchronously sends the request.
        /// </summary>
        /// <param name="handler">The handler for processing a message.</param>
        /// <returns>The task object that contains the batch response.</returns>
        public async override Task<ODataBatchResponseItem> SendRequestAsync(RequestDelegate handler)
        {
            Ensure.NotNull(handler, nameof(handler));

            IDictionary<string, string> contentIdToLocationMapping = this.ContentIdToLocationMapping ?? new ConcurrentDictionary<string, string>();

            // Detect $ContentId dependencies across changeset requests.
            var dependencies = DetectDependencies();

            if (dependencies is not null)
            {
                // Dependencies found — attempt pre-resolution using the EDM model.
                // Pre-resolution uses its own private mapping to rewrite dependent request URLs
                // in-place. The framework's contentIdToLocationMapping is NOT pre-populated with
                // entries for ContentIds whose requests are still pending, because the framework
                // expects to populate those itself from each inner response's Location header.
                // Pre-populating leaks state the framework treats as a conflict, and the parent
                // request's 201 response gets silently rewritten to 500.
                if (!TryPreResolve(dependencies))
                {
                    // Pre-resolution failed — fall back to sequential execution.
                    return await SendRequestsSequentiallyAsync(handler, contentIdToLocationMapping)
                        .ConfigureAwait(false);
                }
            }

            // No dependencies, or pre-resolution succeeded — execute concurrently.
            return await SendRequestsConcurrentlyAsync(handler, contentIdToLocationMapping)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Sends all changeset requests concurrently with a shared <see cref="ChangeSet"/>.
        /// </summary>
        private async Task<ODataBatchResponseItem> SendRequestsConcurrentlyAsync(
            RequestDelegate handler,
            IDictionary<string, string> contentIdToLocationMapping)
        {
            var changeSetProperty = new RestierChangeSetProperty(this)
            {
                ChangeSet = new ChangeSet(),
            };
            SetChangeSetProperty(changeSetProperty);

            var responseTasks = new List<Task<Task<HttpContext>>>();

            foreach (var context in Contexts)
            {
                // Since exceptions may occur before the request is sent to RestierController,
                // we must catch the exceptions here and call OnChangeSetCompleted,
                // so as to avoid deadlock mentioned in GitHub Issue #82.
                var tcs = new TaskCompletionSource<HttpContext>();
                var task =
                    ODataBatchRequestItem.SendRequestAsync(handler, context, contentIdToLocationMapping)
                        .ContinueWith(
                            t =>
                            {
                                if (t.Exception is not null)
                                {
                                    var taskEx = (t.Exception.InnerExceptions is not null &&
                                                  t.Exception.InnerExceptions.Count == 1)
                                        ? t.Exception.InnerExceptions.First()
                                        : t.Exception;
                                    changeSetProperty.Exceptions.Add(taskEx);
                                    changeSetProperty.OnChangeSetCompleted();
                                    tcs.SetException(taskEx);
                                }
                                else
                                {
                                    tcs.SetResult(context);
                                }

                                return tcs.Task;
                            },
                            context.RequestAborted,
                            TaskContinuationOptions.None,
                            TaskScheduler.Current);

                responseTasks.Add(task);
            }

            // the responseTasks will be complete after:
            // - the ChangeSet is submitted
            // - the responses are created and
            // - the controller actions have returned

            await Task.WhenAll(responseTasks).ConfigureAwait(false);

            var returnContexts = new List<HttpContext>();

            foreach (var responseTask in responseTasks)
            {
                var returnContext = await (await responseTask.ConfigureAwait(false)).ConfigureAwait(false);
                if (returnContext.Response.IsSuccessStatusCode())
                {
                    returnContexts.Add(returnContext);
                }
                else
                {
                    returnContexts.Clear();
                    returnContexts.Add(returnContext);
                    return new ChangeSetResponseItem(returnContexts);
                }
            }

            return new ChangeSetResponseItem(returnContexts);
        }

        /// <summary>
        /// Sends all changeset requests sequentially within a <see cref="TransactionScope"/>.
        /// Used as a fallback when $ContentId pre-resolution fails (server-generated keys).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Each request is submitted independently (no shared <see cref="RestierChangeSetProperty"/>),
        /// so convention-based interceptors (e.g., <c>OnInsertingEntity</c>) see individual changesets
        /// rather than the combined changeset. The <see cref="TransactionScope"/> provides atomicity
        /// at the database level — if any request fails, all preceding writes are rolled back.
        /// </para>
        /// <para>
        /// EF Core enlists in ambient transactions by default (since EF Core 5.0). However, distributed
        /// transactions (MSDTC) are not available on Linux/macOS. This works correctly as long as all
        /// requests use the same database connection, which is the typical RESTier scenario.
        /// </para>
        /// </remarks>
        private async Task<ODataBatchResponseItem> SendRequestsSequentiallyAsync(
            RequestDelegate handler,
            IDictionary<string, string> contentIdToLocationMapping)
        {
            var returnContexts = new List<HttpContext>();

            using var scope = new TransactionScope(
                TransactionScopeOption.Required,
                TransactionScopeAsyncFlowOption.Enabled);

            foreach (var context in Contexts)
            {
                // No changeset property set — controller submits individually.
                await ODataBatchRequestItem.SendRequestAsync(handler, context, contentIdToLocationMapping)
                    .ConfigureAwait(false);

                if (context.Response.IsSuccessStatusCode())
                {
                    returnContexts.Add(context);
                }
                else
                {
                    returnContexts.Clear();
                    returnContexts.Add(context);
                    return new ChangeSetResponseItem(returnContexts);
                }
            }

            scope.Complete();
            return new ChangeSetResponseItem(returnContexts);
        }

        /// <summary>
        /// Builds a ContentId-to-URL map from the changeset contexts and detects dependencies.
        /// </summary>
        /// <returns>
        /// A dependency map if any request references another via $ContentId; otherwise null.
        /// </returns>
        private Dictionary<string, List<string>> DetectDependencies()
        {
            var contentIdToUrl = new Dictionary<string, string>();

            foreach (var context in Contexts)
            {
                var contentId = context.Request.GetODataContentId();
                if (!string.IsNullOrEmpty(contentId))
                {
                    contentIdToUrl[contentId] = context.Request.GetEncodedUrl();
                }
            }

            if (contentIdToUrl.Count == 0)
            {
                return null;
            }

            var dependencies = ChangeSetDependencyResolver.DetectDependencies(contentIdToUrl);

            return dependencies.Count > 0 ? dependencies : null;
        }

        /// <summary>
        /// Attempts to pre-resolve $ContentId references in dependent request URLs by rewriting
        /// them in-place. Uses a private mapping so the framework's contentIdToLocationMapping
        /// is not pre-populated with entries for ContentIds whose requests are still pending —
        /// the framework expects to fill those entries itself from each inner response's
        /// Location header, and treats pre-existing entries as a conflict that silently
        /// rewrites the parent request's success response to 500.
        /// </summary>
        /// <param name="dependencies">The dependency map from <see cref="DetectDependencies"/>.</param>
        /// <returns>True if all references were resolved; false otherwise.</returns>
        private bool TryPreResolve(Dictionary<string, List<string>> dependencies)
        {
            var resolverMapping = new ConcurrentDictionary<string, string>();
            return ChangeSetDependencyResolver.PreResolveContentIdReferences(
                Contexts,
                dependencies,
                api.Model,
                resolverMapping);
        }

        /// <summary>
        /// Asynchronously submits a <see cref="ChangeSet"/>.
        /// </summary>
        /// <param name="changeSet">The change set to submit.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
#pragma warning disable CA1822 // Do not declare static members on generic types
        internal async Task SubmitChangeSet(ChangeSet changeSet)
#pragma warning restore CA1822 // Do not declare static members on generic types
        {
            var submitResults = await api.SubmitAsync(changeSet).ConfigureAwait(false);
        }

        private void SetChangeSetProperty(RestierChangeSetProperty changeSetProperty)
        {
            foreach (var context in Contexts)
            {
                context.SetChangeSet(changeSetProperty);
            }
        }
    }
}
