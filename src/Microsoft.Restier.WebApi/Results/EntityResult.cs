// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.WebApi.Results
{
    /// <summary>
    /// Represents a single entity instance being returned from an action.
    /// </summary>
    public class EntityResult : EntityQueryResult
    {
        public EntityResult(IQueryable query, IEdmTypeReference edmType, DomainContext context)
            : base(edmType)
        {
            Ensure.NotNull(query, "query");
            Ensure.NotNull(context, "context");

            this.Context = context;

            this.Result = query.SingleOrDefault();
        }

        public object Result { get; private set; }

        public DomainContext Context { get; private set; }
    }
}
