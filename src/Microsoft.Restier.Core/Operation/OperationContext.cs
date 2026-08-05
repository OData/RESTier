// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

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
        /// <param name="api">An Api.</param>
        /// <param name="getParameterValueFunc">
        /// The function used to retrieve a parameter's URL value alongside a presence flag.
        /// The flag is <see langword="true"/> when the parameter name appears in the request
        /// URL or body, regardless of whether the value is <see langword="null"/>.
        /// </param>
        /// <param name="operationName">The operation name.</param>
        /// <param name="isFunction">A flag indicating this is a function call or action call.</param>
        /// <param name="bindingParameterValue">
        /// A queryable for the binding-parameter value; <see langword="null"/> for function/action imports.
        /// </param>
        public OperationContext(
            ApiBase api,
            Func<string, (bool Present, object Value)> getParameterValueFunc,
            string operationName,
            bool isFunction,
            IEnumerable bindingParameterValue)
            : base(api)
        {
            Ensure.NotNull(getParameterValueFunc, nameof(getParameterValueFunc));
            Ensure.NotNullOrWhiteSpace(operationName, nameof(operationName));

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
        /// Gets the function used to retrieve a parameter's URL value along with a
        /// presence flag distinguishing an omitted parameter (Present = false) from
        /// an explicit null value (Present = true, Value = null).
        /// </summary>
        public Func<string, (bool Present, object Value)> GetParameterValueFunc { get; }

        /// <summary>
        /// Gets a value indicating whether it is a function call or action call.
        /// </summary>
        public bool IsFunction { get; }

        /// <summary>
        /// Gets the queryable for the binding-parameter value;
        /// <see langword="null"/> for function/action imports.
        /// </summary>
        public IEnumerable BindingParameterValue { get; }


        /// <summary>
        /// Gets or sets the parameters value array used by method,
        /// It is only set after parameters are prepared.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public ICollection<object> ParameterValues { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
