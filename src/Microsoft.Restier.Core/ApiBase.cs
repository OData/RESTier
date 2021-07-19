// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using System;
using System.Security.Claims;
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

        #region Private Members

        private readonly DefaultSubmitHandler submitHandler;

        private readonly DefaultQueryHandler queryHandler;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> which contains all services.
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Gets a reference to the Query Handler for this <see cref="ApiBase"/> instance.
        /// </summary>
        internal DefaultQueryHandler QueryHandler => queryHandler;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBase" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        /// An <see cref="IServiceProvider"/> containing all services.
        /// </param>
        protected ApiBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;

            //RWM: This stuff SHOULD be getting passed into a constructor. But the DI implementation is less than awesome.
            //     So we'll work around it for now and still save some allocations.
            //     There are certain unit te
            var queryExpressionSourcer = serviceProvider.GetService<IQueryExpressionSourcer>();
            var queryExpressionAuthorizer = serviceProvider.GetService<IQueryExpressionAuthorizer>();
            var queryExpressionExpander = serviceProvider.GetService<IQueryExpressionExpander>();
            var queryExpressionProcessor = serviceProvider.GetService<IQueryExpressionProcessor>();
            var changeSetInitializer = serviceProvider.GetService<IChangeSetInitializer>();
            var changeSetItemAuthorizer = serviceProvider.GetService<IChangeSetItemAuthorizer>();
            var changeSetItemValidator = serviceProvider.GetService<IChangeSetItemValidator>();
            var changeSetItemFilter = serviceProvider.GetService<IChangeSetItemFilter>();
            var submitExecutor = serviceProvider.GetService<ISubmitExecutor>();

            if (queryExpressionSourcer == null)
            {
                // Missing sourcer
                throw new NotSupportedException(Resources.MissingQueryExpressionSourcer);
            }

            if (changeSetInitializer == null)
            {
                throw new NotSupportedException(Resources.MissingChangeSetInitializer);
            }

            if (submitExecutor == null)
            {
                throw new NotSupportedException(Resources.MissingSubmitExecutor);
            }

            queryHandler = new DefaultQueryHandler(queryExpressionSourcer, queryExpressionAuthorizer, queryExpressionExpander, queryExpressionProcessor);
            submitHandler = new DefaultSubmitHandler(changeSetInitializer, submitExecutor, changeSetItemAuthorizer, changeSetItemValidator, changeSetItemFilter);
        }

        #endregion

        #region Public Methods

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

        #endregion

        #region IDisposable Pattern

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
        /// <remarks>RWM: See https://docs.microsoft.com/en-us/visualstudio/code-quality/ca1063-implement-idisposable-correctly?view=vs-2017 for more information.</remarks>
        protected virtual void Dispose(bool disposing)
        {
            // RWM: This Dispose method isn't implemented properly, and may actually be doing more harm than good.
            //      I'm leaving it for now so we can open an issue and ask the question if this class needs to do more on Dispose,
            //      But I have a feeling we need to kill this with fire.   
            if (disposing)
            {
                // free managed resources
            }
        }

        #endregion

    }

}