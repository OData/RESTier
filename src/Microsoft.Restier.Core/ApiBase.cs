// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents a base class for an API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An API configuration is intended to be long-lived, and can be statically cached according to an API type specified when the
    /// configuration is created. Additionally, the API model produced as a result of a particular configuration is cached under the same
    /// API type to avoid re-computing it on each invocation.
    /// </para>
    /// </remarks>
    public abstract class ApiBase : IDisposable
    {
        private readonly ISubmitHandler submitHandler;

        /// <summary>
        /// Gets a reference to the Query Handler for this <see cref="ApiBase"/> instance.
        /// </summary>
        internal IQueryHandler QueryHandler { get; }

        /// <summary>
        /// Gets the model.
        /// </summary>
        public IEdmModel Model { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBase" /> class.
        /// </summary>
        /// <param name="model">
        /// The model that is used by this API.
        /// </param>
        /// <param name="queryHandler">
        /// The handler to use for querying.
        /// </param>
        /// <param name="submitHandler">
        /// The handler to use for submitting changes.
        /// </param>
        protected ApiBase(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        {
            Ensure.NotNull(model, nameof(model));
            Ensure.NotNull(queryHandler, nameof(queryHandler));
            Ensure.NotNull(submitHandler, nameof(submitHandler));
            Model = model;
            QueryHandler = queryHandler;
            this.submitHandler = submitHandler;
        }

        /// <summary>
        /// Asynchronously queries for data using an API context.
        /// </summary>
        /// <param name="request">
        /// A query request.
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a query result.
        /// </returns>
        public async Task<QueryResult> QueryAsync(QueryRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            Ensure.NotNull(request, nameof(request));

            if (!(request.Query is QueryableSource))
            {
                throw new NotSupportedException(
                    Resources.QueryableSourceCannotBeUsedAsQuery);
            }

            var queryContext = new QueryContext(this, request);
            queryContext.Model = Model;
            return await QueryHandler.QueryAsync(queryContext, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously submits changes made using an API context.
        /// </summary>
        /// <param name="changeSet">A change set, or <c>null</c> to submit existing pending changes.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation whose result is a submit result.</returns>
        public async Task<SubmitResult> SubmitAsync(ChangeSet changeSet = null, CancellationToken cancellationToken = default)
        {
            var submitContext = new SubmitContext(this, changeSet);
            return await submitHandler.SubmitAsync(submitContext, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}