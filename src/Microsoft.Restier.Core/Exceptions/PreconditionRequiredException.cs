// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// This exception is used for 428 Precondition required response.
    /// </summary>
    [Serializable]
    public class PreconditionRequiredException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the PreconditionRequiredException class.
        /// </summary>
        public PreconditionRequiredException()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PreconditionRequiredException class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        public PreconditionRequiredException(string message)
            : this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PreconditionRequiredException class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <param name="innerException">Exception that caused this exception to be thrown.</param>
        public PreconditionRequiredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
