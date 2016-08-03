// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// This exception is used for 412 Precondition Failed response.
    /// </summary>
    [Serializable]
    public class PreconditionFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the PreconditionFailedException class.
        /// </summary>
        public PreconditionFailedException()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PreconditionFailedException class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        public PreconditionFailedException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PreconditionFailedException class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <param name="innerException">Exception that caused this exception to be thrown.</param>
        public PreconditionFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
