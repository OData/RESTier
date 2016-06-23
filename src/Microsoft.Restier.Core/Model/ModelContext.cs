// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        /// Gets or sets Entity set and entity type map collection, it will be used by publisher for model build.
        /// </summary>
        public Collection<KeyValuePair<string, Type>> EntitySetTypeMapCollection { get; set; }
    }
}
