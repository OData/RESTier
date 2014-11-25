// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Data.Domain;
using Microsoft.OData.Edm;

namespace System.Web.OData.Domain.Results
{
    /// <summary>
    /// Represents a collection of entity instances being returned from an action.
    /// </summary>
    public class EntityCollectionResult : EntityQueryResult
    {
        public EntityCollectionResult(IQueryable query, IEdmTypeReference edmType, DomainContext context)
            : base(edmType)
        {
            Ensure.NotNull(query, "query");
            Ensure.NotNull(context, "context");

            this.Query = query;
            this.Context = context;
        }

        public IQueryable Query { get; private set; }

        public DomainContext Context { get; private set; }
    }
}
