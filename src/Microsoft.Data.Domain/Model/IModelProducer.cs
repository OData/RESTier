// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm.Library;

namespace Microsoft.Data.Domain.Model
{
    /// <summary>
    /// Represents a hook point that produces a base model.
    /// </summary>
    /// <remarks>
    /// This is a singleton hook point that should be
    /// implemented by an underlying data provider.
    /// </remarks>
    public interface IModelProducer
    {
        /// <summary>
        /// Asynchronously produces a base model.
        /// </summary>
        /// <param name="context">
        /// The model context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is the base model.
        /// </returns>
        Task<EdmModel> ProduceModelAsync(
            ModelContext context,
            CancellationToken cancellationToken);
    }
}
