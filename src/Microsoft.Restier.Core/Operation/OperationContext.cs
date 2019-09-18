// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Restier.Core.Operation
{
    /// <summary>
    /// Represents context under which a operation is executed.
    /// One instance created for one execution of one operation.
    /// </summary>
    public class OperationContext : InvocationContext
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationContext" /> class.
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
        public OperationContext(
            ApiBase api,
            Func<string, object> getParameterValueFunc,
            string operationName,
            bool isFunction,
            IEnumerable bindingParameterValue)
            : base(api)
        {
            GetParameterValueFunc = getParameterValueFunc;
            OperationName = operationName;
            IsFunction = isFunction;
            BindingParameterValue = bindingParameterValue;
        }

        /// <summary>
        /// Gets the operation name.
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        /// Gets the function that used to retrieve the parameter value name.
        /// </summary>
        public Func<string, object> GetParameterValueFunc { get; }

        /// <summary>
        /// Gets a value indicating whether it is a function call or action call.
        /// </summary>
        public bool IsFunction { get; }

        /// <summary>
        /// Gets the queryable for binding parameter value,
        /// and if it is function/action import, the value will be null.
        /// </summary>
        public IEnumerable BindingParameterValue { get; }


        /// <summary>
        /// Gets or sets the parameters value array used by method,
        /// It is only set after parameters are prepared.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public ICollection<object> ParameterValues { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the http request for this operation call
        /// </summary>
        public HttpRequestMessage Request { get; set; } // TODO: RWM: Move to ApiBase.
    }
}
