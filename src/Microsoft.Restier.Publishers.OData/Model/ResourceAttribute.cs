// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Publishers.OData.Model
{
    /// <summary>
    /// Attribute that indicates a property is an entity set or singleton.
    /// The name will be same as property name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ResourceAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets a value indicating whether it is singleton or entity set.
        /// The default value is false means it is an entity set
        /// </summary>
        public bool IsSingleton { get; set; }
    }
}
