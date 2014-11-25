// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm.Library;

namespace Microsoft.Data.Domain.Model
{
    /// <summary>
    /// Represents context under which a model flow operates.
    /// </summary>
    public class ModelContext : InvocationContext
    {
        /// <summary>
        /// Initializes a new model context.
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
        public EdmModel Model { get; set; }
    }
}
