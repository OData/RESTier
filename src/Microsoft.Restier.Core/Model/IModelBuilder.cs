// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// The hook point for model generation.
    /// </summary>
    public interface IModelBuilder : IHookHandler
    {
        /// <summary>
        /// Asynchronously gets an API model for an API.
        /// </summary>
        /// <param name="context">
        /// The context for processing
        /// </param>
        /// <param name="cancellationToken">
        /// An optional cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is the API model.
        /// </returns>
        Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken);
    }
}
