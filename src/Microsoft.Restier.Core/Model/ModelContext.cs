// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// Represents context under which a model flow operates.
    /// </summary>
    public class ModelContext : InvocationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelContext" /> class.
        /// </summary>
        /// <param name="domainContext">
        /// A domain context.
        /// </param>
        public ModelContext(DomainContext domainContext)
            : base(domainContext)
        {
        }

        /// <summary>
        /// Gets or sets the resulting model.
        /// </summary>
        public IEdmModel Model { get; set; }
    }
}
