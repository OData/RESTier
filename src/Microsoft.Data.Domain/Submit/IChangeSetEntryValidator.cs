// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// Represents a change set entry validator.
    /// </summary>
    /// <remarks>
    /// This is a multi-cast hook point whose instances
    /// are used in the reverse order of registration.
    /// </remarks>
    public interface IChangeSetEntryValidator
    {
        /// <summary>
        /// Asynchronously validates a change set entry.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="entry">
        /// The change set entry to validate.
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
        Task ValidateEntityAsync(
            SubmitContext context,
            ChangeSetEntry entry,
            ValidationResults validationResults,
            CancellationToken cancellationToken);
    }
}
