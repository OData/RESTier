// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents a reference to an existing entity for @odata.bind (4.0) or entity-reference (4.01) linking.
    /// This is a relationship-only operation — the referenced entity is not created or modified.
    /// </summary>
    public class BindReference
    {
        /// <summary>
        /// Gets or sets the target entity set name.
        /// </summary>
        public string ResourceSetName { get; set; }

        /// <summary>
        /// Gets or sets the key of the referenced entity.
        /// </summary>
        public IReadOnlyDictionary<string, object> ResourceKey { get; set; }

        /// <summary>
        /// Gets or sets the resolved entity instance (populated during initialization Phase 1).
        /// </summary>
        public object ResolvedEntity { get; set; }
    }
}
