// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.AspNetCore.Model
{

    /// <summary>
    /// An abstract class containing the common information for registering Actions and Functions to an OData schema.
    /// </summary>
    /// <remarks>
    /// This was turned into an Abstract class in favor or more specific functionality. The old design created situations where
    /// you could not achive the behavior you desired, due to unsupported parameter combinations. Please use [BoundOperation] or 
    /// [UnboundOperation] instead.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class OperationAttribute : Attribute
    {

        /// <summary>
        /// Gets or sets the namespace of the operation. 
        /// The default value will be same as the namespace of entity type.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the function is composable.
        /// Defaults to <see langword="false" />.
        /// </summary>
        public bool IsComposable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating what type of Operation is being registered. <see cref="OperationType.Function">Functions</see> respond to HTTP GET requests,
        /// while <see cref="OperationType.Action">Actions</see> respond to HTTP POST requests. Defaults to <see cref="OperationType.Function"/>.
        /// </summary>
        public OperationType OperationType { get; set; } = OperationType.Function;

    }

}