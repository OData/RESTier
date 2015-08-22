// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Base class for all hook handlers
    /// </summary>
    /// <typeparam name="TContext">The context class to handle.</typeparam>
    public abstract class HookHandler<TContext> where TContext : InvocationContext
    {
        internal HookHandler<TContext> InnerHandler { get; set; }

        /// <summary>
        /// Handle certain context, the default implementation would invoke the next handler on the handler chain.
        /// </summary>
        /// <param name="context">The context to be proceed.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual async Task HandleAsync(TContext context, CancellationToken cancellationToken)
        {
            if (this.InnerHandler != null)
            {
                await this.InnerHandler.HandleAsync(context, cancellationToken);
            }
        }
    }
}
