// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents an exception that indicates validation errors occurred on entities.
    /// </summary>
    [Serializable]
    public class EdmModelValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EdmModelValidationException"/> class.
        /// </summary>
        public EdmModelValidationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdmModelValidationException"/> class.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        public EdmModelValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdmModelValidationException"/> class.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        /// <param name="innerException">Inner exception.</param>
        public EdmModelValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializationInfo"></param>
        /// <param name="streamingContext"></param>
        protected EdmModelValidationException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }
}