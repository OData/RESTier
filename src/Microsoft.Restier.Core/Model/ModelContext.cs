// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// Represents context under which a model is requested.
    /// </summary>
    public class ModelContext : InvocationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelContext" /> class.
        /// </summary>
        /// <param name="provider">
        /// The service provider to get services.
        /// </param>
        public ModelContext(IServiceProvider provider) : base(provider)
        {
        }

        /// <summary>
        /// Gets or sets resource set and resource type map dictionary, it will be used by publisher for model build.
        /// </summary>
        public IDictionary<string, Type> ResourceSetTypeMap { get; set; }

        /// <summary>
        /// Gets or sets resource type and its key properties map dictionary, and used by publisher for model build.
        /// This is useful when key properties does not have key attribute
        /// or follow Web Api OData key property naming convention.
        /// Otherwise, this collection is not needed.
        /// </summary>
        public IDictionary<Type, ICollection<PropertyInfo>> ResourceTypeKeyPropertiesMap { get; set; }
    }
}
