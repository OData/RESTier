// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents a service that can initialize a change set.
    /// </summary>
    public interface IChangeSetInitializer
    {
        /// <summary>
        /// Asynchronously initialize a change set for submission.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        /// <remarks>
        /// Preparing a change set involves creating new entity objects for
        /// new data, loading entities that are pending update or delete from
        /// to get current server values, and using a data provider mechanism
        /// to locally apply the supplied changes to the loaded entities.
        /// </remarks>
        Task InitializeAsync(SubmitContext context, CancellationToken cancellationToken);
    }
}
