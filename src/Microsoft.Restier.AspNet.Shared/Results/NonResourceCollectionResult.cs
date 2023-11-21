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
    /// Represents a collection of non-entity or complex values being returned from an action.
    /// </summary>
    internal class NonResourceCollectionResult : BaseCollectionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NonResourceCollectionResult" /> class.
        /// </summary>
        /// <param name="query">The query that returns a collection of non-entity or complex values.</param>
        /// <param name="edmType">The EDM type reference of the values.</param>
        public NonResourceCollectionResult(IQueryable query, IEdmTypeReference edmType)
            : base(query, edmType)
        {
        }
    }
}
