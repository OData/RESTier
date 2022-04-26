// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.AspNetCore.Http;
#else
using System.Net.Http;
#endif
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;

#if NETCOREAPP3_1_OR_GREATER
namespace Microsoft.Restier.AspNetCore.Operation
#else
namespace Microsoft.Restier.AspNet.Operation
#endif
{
    /// <summary>
    /// Represents context under which a operation is executed within ASP.NET (Core).
    /// One instance created for one execution of one operation.
    /// </summary>
    public class RestierOperationContext : OperationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierOperationContext" /> class.
        /// </summary>
        /// <param name="api">
        /// An Api.
        /// </param>
        /// <param name="getParameterValueFunc">
        /// The function that used to retrieve the parameter value name.
        /// </param>
        /// <param name="operationName">
        /// The operation name.
        /// </param>
        /// <param name="isFunction">
        /// A flag indicates this is a function call or action call.
        /// </param>
        /// <param name="bindingParameterValue">
        /// A queryable for binding parameter value and if it is function/action import, the value will be null.
        /// </param>
        public RestierOperationContext(
            ApiBase api,
            Func<string, object> getParameterValueFunc,
            string operationName,
            bool isFunction,
            IEnumerable bindingParameterValue)
            : base(api, getParameterValueFunc, operationName, isFunction, bindingParameterValue)
        {
        }

        /// <summary>
        /// Gets or sets the Request.
        /// </summary>
#if NETCOREAPP3_1_OR_GREATER
        public HttpRequest Request { get; set; }
#else
        public HttpRequestMessage Request { get; set; }
#endif
    }
}
