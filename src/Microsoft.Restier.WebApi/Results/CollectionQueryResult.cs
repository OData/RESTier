// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.WebApi.Results
{
    /// <summary>
    /// Represents a collection of objects being returned from an action.
    /// </summary>
    public abstract class CollectionQueryResult : ODataQueryResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionQueryResult" /> class.
        /// </summary>
        /// <param name="query">The query that returns a collection of objects.</param>
        /// <param name="edmType">The EDM type reference of the objects.</param>
        /// <param name="context">The context where the action is executed.</param>
        protected CollectionQueryResult(IQueryable query, IEdmTypeReference edmType, DomainContext context)
            : base(edmType, context)
        {
            Ensure.NotNull(query, "query");

            this.Query = query;
        }

        /// <summary>
        /// Gets the query that returns a collection of objects.
        /// </summary>
        public IQueryable Query { get; private set; }
    }
}
