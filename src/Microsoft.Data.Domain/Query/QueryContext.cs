// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents context under which a query flow operates.
    /// </summary>
    public class QueryContext : InvocationContext
    {
        private QueryRequest _request;
        private QueryResult _result;

        /// <summary>
        /// Initializes a new query context.
        /// </summary>
        /// <param name="domainContext">
        /// A domain context.
        /// </param>
        /// <param name="request">
        /// A query request.
        /// </param>
        public QueryContext(DomainContext domainContext, QueryRequest request)
            : base(domainContext)
        {
            Ensure.NotNull(request, "request");
            this.Request = request;
        }

        /// <summary>
        /// Gets the model that informs this query context.
        /// </summary>
        public IEdmModel Model { get; internal set; }

        /// <summary>
        /// Gets or sets the query request.
        /// </summary>
        /// <remarks>
        /// The query request cannot be set if there is already a result.
        /// </remarks>
        public QueryRequest Request
        {
            get
            {
                return this._request;
            }
            set
            {
                if (this.Result != null)
                {
                    throw new InvalidOperationException();
                }
                Ensure.NotNull(value, "value");
                this._request = value;
            }
        }

        /// <summary>
        /// Gets or sets the query result.
        /// </summary>
        public QueryResult Result
        {
            get
            {
                return this._result;
            }
            set
            {
                Ensure.NotNull(value, "value");
                this._result = value;
            }
        }
    }
}
