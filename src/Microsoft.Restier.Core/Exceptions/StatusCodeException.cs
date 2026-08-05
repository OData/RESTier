// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Runtime.Serialization;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Use this exception when you want to return a specific status code
    /// </summary>
    [Serializable]
    public class StatusCodeException : Exception
    {
        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; } = HttpStatusCode.BadRequest;

        /// <summary>
        /// Initializes a new instance of the StatusCodeException class.
        /// </summary>
        public StatusCodeException()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the StatusCodeException class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        public StatusCodeException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the StatusCodeException class.
        /// </summary>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <param name="innerException">Exception that caused this exception to be thrown.</param>
        public StatusCodeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the StatusCodeException class.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="message">Plain text error message for this exception.</param>
        public StatusCodeException(HttpStatusCode statusCode, string message)
            : this(statusCode, message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the StatusCodeException class.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="message">Plain text error message for this exception.</param>
        /// <param name="innerException">Exception that caused this exception to be thrown.</param>
        public StatusCodeException(HttpStatusCode statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializationInfo"></param>
        /// <param name="streamingContext"></param>
        protected StatusCodeException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }
}