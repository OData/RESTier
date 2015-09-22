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
    /// Represents a single object being returned from an action.
    /// </summary>
    public abstract class SingleQueryResult : ODataQueryResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleQueryResult" /> class.
        /// </summary>
        /// <param name="query">The query that returns an object.</param>
        /// <param name="edmType">The EDM type reference of the object.</param>
        /// <param name="context">The context where the action is executed.</param>
        protected SingleQueryResult(IQueryable query, IEdmTypeReference edmType, DomainContext context)
            : base(edmType)
        {
            Ensure.NotNull(query, "query");
            Ensure.NotNull(context, "context");

            this.Context = context;

            this.Result = query.SingleOrDefault();
        }

        /// <summary>
        /// Gets the result object.
        /// </summary>
        public object Result { get; private set; }

        /// <summary>
        /// Gets the context where the action is executed.
        /// </summary>
        public DomainContext Context { get; private set; }
    }
}
