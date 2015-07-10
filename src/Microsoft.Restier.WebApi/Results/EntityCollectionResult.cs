// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.WebApi.Results
{
    /// <summary>
    /// Represents a collection of entity instances being returned from an action.
    /// </summary>
    public class EntityCollectionResult : EntityQueryResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionResult" /> class.
        /// </summary>
        /// <param name="query">The query that returns a collection of entities.</param>
        /// <param name="edmType">The EDM type reference of the entities.</param>
        /// <param name="context">The context where the action is executed.</param>
        public EntityCollectionResult(IQueryable query, IEdmTypeReference edmType, DomainContext context)
            : base(edmType)
        {
            Ensure.NotNull(query, "query");
            Ensure.NotNull(context, "context");

            this.Query = query;
            this.Context = context;
        }

        /// <summary>
        /// Gets the query that returns a collection of entities.
        /// </summary>
        public IQueryable Query { get; private set; }

        /// <summary>
        /// Gets the context where the action is executed.
        /// </summary>
        public DomainContext Context { get; private set; }
    }
}
