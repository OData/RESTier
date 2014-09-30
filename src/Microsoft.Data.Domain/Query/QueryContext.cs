// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Microsoft.OData.Edm;

namespace Microsoft.Data.Domain.Query
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
