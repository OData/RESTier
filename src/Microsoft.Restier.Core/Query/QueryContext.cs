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
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryContext" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service provider to get services.
        /// </param>
        /// <param name="request">
        /// A query request.
        /// </param>
        public QueryContext(IServiceProvider serviceProvider, QueryRequest request)
            : base(serviceProvider)
        {
            Ensure.NotNull(request, "request");
            this.Request = request;
        }

        /// <summary>
        /// Gets the model that informs this query context.
        /// </summary>
        public IEdmModel Model { get; internal set; }

        /// <summary>
        /// Gets the query request.
        /// </summary>
        /// <remarks>
        /// The query request cannot be set if there is already a result.
        /// </remarks>
        public QueryRequest Request { get; private set; }
    }
}
