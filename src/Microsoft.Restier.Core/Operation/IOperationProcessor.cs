// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Operation
{
    /// <summary>
    /// Represents a operation processor.
    /// </summary>
    public interface IOperationProcessor
    {
        /// <summary>
        /// Asynchronously applies logic before a operation is executed.
        /// </summary>
        /// <param name="context">
        /// The operation context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task OnExecutingOperationAsync(
            OperationContext context,
            CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously applies logic after an operation is executed.
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
        Task OnExecutedOperationAsync(
            OperationContext context,
            CancellationToken cancellationToken);
    }
}
