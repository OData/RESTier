// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents a change set entry validator.
    /// </summary>
    public interface IChangeSetItemValidator
    {
        /// <summary>
        /// Asynchronously validates a change set item.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="item">
        /// The change set item to validate.
        /// </param>
        /// <param name="validationResults">
        /// A set of validation results.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task ValidateChangeSetItemAsync(
            SubmitContext context,
            ChangeSetItem item,
            Collection<ChangeSetItemValidationResult> validationResults,
            CancellationToken cancellationToken);
    }
}
