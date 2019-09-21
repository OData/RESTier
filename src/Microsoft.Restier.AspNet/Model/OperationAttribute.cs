// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.AspNet.Model
{

    /// <summary>
    /// Attribute that indicates a method is an OData Operation (Function or Action).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OperationAttribute : Attribute
    {

        /// <summary>
        /// Gets or sets the namespace of the operation.
        /// The default value will be same as the namespace of entity type.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the entity set associated with the operation result.
        /// Only need to be set for unbound operations
        /// when there are multiple entity sets with same entity type as result entity type.
        /// </summary>
        public string EntitySet { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the function is composable.
        /// The default value is false.
        /// </summary>
        public bool IsComposable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the operation is bound or not.
        /// If it is set to true, then no matter what's the first parameter, it will be considered as bound.
        /// If it is set to false, then no matter what's the first parameter, it will be considered as unbound.
        /// The default value is false.
        /// </summary>
        public bool IsBound { get; set; }

        /// <summary>
        /// [DEPRECATED] Gets or sets a value indicating whether the operation has side effects. Please use OperationType instead.
        /// </summary>
        [Obsolete("HasSideEffects is confusing and is being deprecated. Please specify an explicit value for OperationType instead.", false)]
        public bool HasSideEffects
        {
            get => OperationType == OperationType.Action;
            set => OperationType = value ? OperationType.Action : OperationType.Function;
        }

        /// <summary>
        /// Gets or sets a value indicating what type of Operation is being registered. <see cref="OperationType.Function">Functions</see> respond to HTTP GET requests,
        /// while <see cref="OperationType.Action">Actions</see> respond to HTTP POST requests. Defaults to <see cref="OperationType.Function"/>.
        /// </summary>
        public OperationType OperationType { get; set; } = OperationType.Function;

    }

}