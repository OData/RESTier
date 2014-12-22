// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents a hook point that executes a submission.
    /// </summary>
    /// <remarks>
    /// This is a singleton hook point that should be
    /// implemented by an underlying data provider.
    /// </remarks>
    public interface ISubmitExecutor
    {
        /// <summary>
        /// Asynchronously executes a submission and produces a submit result.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a submit result.
        /// </returns>
        Task<SubmitResult> ExecuteSubmitAsync(
            SubmitContext context,
            CancellationToken cancellationToken);
    }
}
