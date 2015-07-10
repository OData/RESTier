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
    public class ValidationException : Exception
    {
        private IEnumerable<ValidationResult> validationResults;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        public ValidationException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        public ValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        /// <param name="innerException">Inner exception.</param>
        public ValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Gets or sets the validation results.
        /// </summary>
        public IEnumerable<ValidationResult> ValidationResults
        {
            get
            {
                if (this.validationResults == null)
                {
                    return Enumerable.Empty<ValidationResult>();
                }
                else
                {
                    return this.validationResults;
                }
            }
            set
            {
                this.validationResults = value;
            }
        }
    }
}
