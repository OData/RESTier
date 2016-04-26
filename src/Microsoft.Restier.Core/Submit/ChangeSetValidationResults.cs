// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents a collection of ValidationResult instances that identify what is not valid on a validated item.
    /// </summary>
    public class ChangeSetValidationResults : Collection<ChangeSetValidationResult>
    {
        /// <summary>
        /// Gets a value indicating whether there is any result that has Severity equal to "Error"
        /// in the current validation results.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return this.Errors.Any();
            }
        }

        /// <summary>
        /// Gets a collection of ValidationResult instances that have Severity equal to "Error"
        /// in the current validation results.
        /// </summary>
        public IEnumerable<ChangeSetValidationResult> Errors
        {
            get
            {
                return this.Where(result =>
                    result.Severity == ChangeSetValidationSeverity.Error);
            }
        }
    }
}
