// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents an exception that indicates validation errors occurred on entities.
    /// </summary>
    public class ChangeSetValidationException : Exception
    {
        private IEnumerable<ChangeSetItemValidationResult> errorValidationResults;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetValidationException"/> class.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        public ChangeSetValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSetValidationException"/> class.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        /// <param name="innerException">Inner exception.</param>
        public ChangeSetValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Gets or sets the failed validation results.
        /// </summary>
        public IEnumerable<ChangeSetItemValidationResult> ValidationResults
        {
            get
            {
                if (this.errorValidationResults == null)
                {
                    return Enumerable.Empty<ChangeSetItemValidationResult>();
                }
                else
                {
                    return this.errorValidationResults;
                }
            }

            set
            {
                this.errorValidationResults = value;
            }
        }
    }
}
