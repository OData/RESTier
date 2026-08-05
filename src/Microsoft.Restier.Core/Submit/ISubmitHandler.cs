// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Defines the contract for a submit handler.
    /// </summary>
    public interface ISubmitHandler
    {
        /// <summary>
        /// Asynchronously executes the submit flow.
        /// </summary>
        /// <param name="context">The submit context.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// A task that represents the asynchronous operation whose result is a submit result.
        /// </returns>
        Task<SubmitResult> SubmitAsync(SubmitContext context, CancellationToken cancellationToken);
    }
}
