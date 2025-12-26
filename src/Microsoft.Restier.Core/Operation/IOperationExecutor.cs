// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Operation
{
    /// <summary>
    /// Represents a service that executes an operation.
    /// </summary>
    public interface IOperationExecutor
    {
        /// <summary>
        /// Asynchronously executes an operation.
        /// </summary>
        /// <param name="context">
        /// The operation context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a operation result.
        /// </returns>
        Task<IQueryable> ExecuteOperationAsync(OperationContext context, CancellationToken cancellationToken);
    }
}
