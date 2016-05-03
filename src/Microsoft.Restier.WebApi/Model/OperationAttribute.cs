// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.WebApi.Model
{
    /// <summary>
    /// Attribute that indicates a method is an OData Operation (Function or Action).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class OperationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the function.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the namespace of the function.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the entity set associated with the function result.
        /// </summary>
        public string EntitySet { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the function is composable.
        /// </summary>
        public bool IsComposable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the operation has side effects.
        /// If a operation does not have side effect, it means it is a function.
        /// If a operation has side effect, it means it is a action.
        /// </summary>
        public bool HasSideEffects { get; set; } = false;
    }
}
