// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents a query request.
    /// </summary>
    public class QueryRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryRequest" /> class with a composed query.
        /// </summary>
        /// <param name="query">
        /// A composed query that was derived from a queryable source.
        /// </param>
        public QueryRequest(IQueryable query)
        {
            Ensure.NotNull(query, "query");
            if (!(query is QueryableSource))
            {
                throw new NotSupportedException(
                    Resources.QueryableSourceCannotBeUsedAsQuery);
            }

            this.Expression = query.Expression;
        }

        /// <summary>
        /// Gets or sets the composed query expression.
        /// </summary>
        public Expression Expression { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the number
        /// of the items should be returned instead of the
        /// items themselves.
        /// </summary>
        public bool ShouldReturnCount { get; set; }
    }
}
