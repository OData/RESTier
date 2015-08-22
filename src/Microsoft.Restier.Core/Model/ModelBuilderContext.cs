// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// Represents context under which a model flow operates.
    /// </summary>
    public class ModelBuilderContext : InvocationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBuilderContext" /> class.
        /// </summary>
        /// <param name="domainContext">
        /// A domain context.
        /// </param>
        public ModelBuilderContext(DomainContext domainContext)
            : base(domainContext)
        {
        }

        /// <summary>
        /// Gets or sets the resulting model.
        /// </summary>
        public IEdmModel Model { get; set; }
    }
}
