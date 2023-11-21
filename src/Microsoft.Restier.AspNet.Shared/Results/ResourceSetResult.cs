// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.OData.Edm;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.AspNetCore
#else
namespace Microsoft.Restier.AspNet
#endif
{
    /// <summary>
    /// Represents a collection of entity instances being returned from an action.
    /// </summary>
    internal class ResourceSetResult : BaseCollectionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceSetResult" /> class.
        /// </summary>
        /// <param name="query">The query that returns a collection of resources.</param>
        /// <param name="edmType">The EDM type reference of the entities or complex.</param>
        public ResourceSetResult(IQueryable query, IEdmTypeReference edmType)
            : base(query, edmType)
        {
        }
    }
}
