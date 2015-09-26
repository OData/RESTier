// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.WebApi.Results
{
    /// <summary>
    /// Represents a collection of non-entity values being returned from an action.
    /// </summary>
    internal class ValueCollectionResult : BaseCollectionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCollectionResult" /> class.
        /// </summary>
        /// <param name="query">The query that returns a collection of non-entity values.</param>
        /// <param name="edmType">The EDM type reference of the values.</param>
        /// <param name="context">The context where the action is executed.</param>
        public ValueCollectionResult(IQueryable query, IEdmTypeReference edmType, DomainContext context)
            : base(query, edmType, context)
        {
        }
    }
}
