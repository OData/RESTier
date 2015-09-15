// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// Attribute that indicates a method is an OData action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ActionAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the action.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the namespace of the action.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the entity set associated with the action result.
        /// </summary>
        public string EntitySet { get; set; }
    }
}
