// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;
using System;
using System.Collections;

namespace Microsoft.Restier.AspNetCore.Operation
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
        /// The function used to retrieve a parameter's URL value alongside a presence flag.
        /// </param>
        /// <param name="operationName">
        /// The operation name.
        /// </param>
        /// <param name="isFunction">
        /// A flag indicating this is a function call or action call.
        /// </param>
        /// <param name="bindingParameterValue">
        /// A queryable for the binding-parameter value; <see langword="null"/> for function/action imports.
        /// </param>
        public RestierOperationContext(
            ApiBase api,
            Func<string, (bool Present, object Value)> getParameterValueFunc,
            string operationName,
            bool isFunction,
            IEnumerable bindingParameterValue)
            : base(api, getParameterValueFunc, operationName, isFunction, bindingParameterValue)
        {
        }

        /// <summary>
        /// Gets or sets the Request.
        /// </summary>
        public HttpRequest Request { get; set; }
    }
}
