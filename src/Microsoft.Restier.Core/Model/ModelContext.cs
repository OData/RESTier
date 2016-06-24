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
        /// <param name="apiContext">
        /// An API context.
        /// </param>
        public ModelContext(ApiContext apiContext)
            : base(apiContext)
        {
        }

        /// <summary>
        /// Gets or sets Entity set and entity type map dictionary, it will be used by publisher for model build.
        /// </summary>
        public IDictionary<string, Type> EntitySetTypeMapDictionary { get; set; }

        /// <summary>
        /// Gets or sets entity type and its key properties map dictionary, and used by publisher for model build.
        /// This is useful when key properties does not have key attribute
        /// or follow Web Api OData key property naming convention.
        /// Otherwise, this collection is not needed.
        /// </summary>
        public IDictionary<Type, ICollection<PropertyInfo>> EntityTypeKeyPropertiesMapDictionary { get; set; }
    }
}
