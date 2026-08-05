// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Restier.Core
{

    /// <summary>
    /// Represents an exception that indicates validation errors occurred on entities.
    /// </summary>
    [Serializable]
    public class ConventionInvocationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EdmModelValidationException"/> class.
        /// </summary>
        public ConventionInvocationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdmModelValidationException"/> class.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        public ConventionInvocationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdmModelValidationException"/> class.
        /// </summary>
        /// <param name="message">Message of the exception.</param>
        /// <param name="innerException">Inner exception.</param>
        public ConventionInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializationInfo"></param>
        /// <param name="streamingContext"></param>
        protected ConventionInvocationException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }
}