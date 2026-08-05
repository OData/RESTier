// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Opt-in toggles for stricter OData v4 spec conformance. Defaults preserve
    /// Restier's existing pragmatic behavior.
    /// </summary>
    public class RestierConformanceOptions
    {
        /// <summary>
        /// When <c>true</c>, requests to a collection-valued navigation property
        /// whose parent entity does not exist (e.g. <c>/Books(missing)/Reviews</c>)
        /// return <c>404 Not Found</c> per OData v4 Part 1 §9.1.5 / §11.2.6.
        /// When <c>false</c> (default), an empty collection
        /// (<c>200 OK { "value": [] }</c>) is returned, matching Restier's
        /// historical behavior. Setting this to <c>true</c> incurs one extra
        /// parent-existence query per collection-nav request whose path
        /// includes a key segment.
        /// </summary>
        public bool StrictMissingParentForCollections { get; set; }
    }
}
