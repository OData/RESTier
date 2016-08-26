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
        private readonly string operationName;
        private readonly object implementInstance;
        private readonly Func<string, object> getParameterValueFunc;
        private readonly bool isFunction;
        private readonly IEnumerable bindingParameterValue;
        private ICollection<object> parameterValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationContext" /> class.
        /// </summary>
        /// <param name="getParameterValueFunc">
        /// The function that used to retrieve the parameter value name.
        /// </param>
        /// <param name="operationName">
        /// The operation name.
        /// </param>
        /// <param name="implementInstance">
        /// The instance which has the implementation of the operation and used for reflection call
        /// </param>
        /// <param name="isFunction">
        /// A flag indicates this is a function call or action call.
        /// </param>
        /// <param name="bindingParameterValue">
        /// A queryable for binding parameter value and if it is function/action import, the value will be null.
        /// </param>
        /// <param name="provider">
        /// The service provider used to get service from container.
        /// </param>
        public OperationContext(
            Func<string, object> getParameterValueFunc,
            string operationName,
            object implementInstance,
            bool isFunction,
            IEnumerable bindingParameterValue,
            IServiceProvider provider)
            : base(provider)
        {
            this.getParameterValueFunc = getParameterValueFunc;
            this.operationName = operationName;
            this.implementInstance = implementInstance;
            this.isFunction = isFunction;
            this.bindingParameterValue = bindingParameterValue;
        }

        /// <summary>
        /// Gets the operation name.
        /// </summary>
        public string OperationName
        {
            get
            {
                return this.operationName;
            }
        }

        /// <summary>
        /// Gets the instance have implemented the operation and used for reflection call.
        /// </summary>
        public object ImplementInstance
        {
            get
            {
                return this.implementInstance;
            }
        }

        /// <summary>
        /// Gets the function that used to retrieve the parameter value name.
        /// </summary>
        public Func<string, object> GetParameterValueFunc
        {
            get
            {
                return this.getParameterValueFunc;
            }
        }

        /// <summary>
        /// Gets a value indicating whether it is a function call or action call.
        /// </summary>
        public bool IsFunction
        {
            get
            {
                return this.isFunction;
            }
        }

        /// <summary>
        /// Gets the queryable for binding parameter value,
        /// and if it is function/action import, the value will be null.
        /// </summary>
        public IEnumerable BindingParameterValue
        {
            get
            {
                return this.bindingParameterValue;
            }
        }

        /// <summary>
        /// Gets or sets the parameters value array used by method,
        /// It is only set after parameters are prepared.
        /// </summary>
        public ICollection<object> ParameterValues
        {
            get
            {
                return this.parameterValues;
            }

            set
            {
                this.parameterValues = value;
            }
        }

        /// <summary>
        /// Gets or sets the http request for this operation call
        /// TODO consider moving to base class after more investigation
        /// </summary>
        public HttpRequestMessage Request { get; set; }
    }
}
