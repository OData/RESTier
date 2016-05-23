// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Publishers.OData.Results
{
    /// <summary>
    /// Represents a single entity instance being returned from an action.
    /// </summary>
    internal class EntityResult : BaseSingleResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityResult" /> class.
        /// </summary>
        /// <param name="query">The query that returns an entity.</param>
        /// <param name="edmType">The EDM type reference of the entity.</param>
        /// <param name="context">The context where the action is executed.</param>
        public EntityResult(IQueryable query, IEdmTypeReference edmType, ApiContext context)
            : base(query, edmType, context)
        {
        }
    }
}
