// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Publishers.OData.Model
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
        /// Gets or sets a value indicating whether the operation has side effects.
        /// If an operation does not have side effect, it means it is a function,
        /// and need to use HTTP Get to call function.
        /// If an operation has side effect, it means it is an action, and need to use HTTP post to call action.
        /// The default value is false.
        /// </summary>
        public bool HasSideEffects { get; set; }
    }
}
