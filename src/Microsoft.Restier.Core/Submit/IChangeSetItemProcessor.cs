// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents a change set entry filter.
    /// </summary>
    public interface IChangeSetItemProcessor
    {
        /// <summary>
        /// Asynchronously applies logic before a change set item is processed.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="item">
        /// A change set item.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task PreProcessChangeSetItemAsync(
            SubmitContext context,
            ChangeSetItem item,
            CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously applies logic after a change set item is processed.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="item">
        /// A change set item.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task PostProcessChangeSetItemAsync(
            SubmitContext context,
            ChangeSetItem item,
            CancellationToken cancellationToken);
    }
}
