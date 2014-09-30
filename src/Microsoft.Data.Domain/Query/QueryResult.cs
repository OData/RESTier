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
using System.Collections;
using Microsoft.OData.Edm;

namespace Microsoft.Data.Domain.Query
{
    /// <summary>
    /// Represents a query result.
    /// </summary>
    public class QueryResult
    {
        private Exception _error;
        private IEdmEntitySet _resultsSource;
        private IEnumerable _results;
        private long? _totalCount;

        /// <summary>
        /// Initializes a new query result with an error.
        /// </summary>
        /// <param name="error">
        /// An error.
        /// </param>
        public QueryResult(Exception error)
        {
            Ensure.NotNull(error, "error");
            this.Error = error;
        }

        /// <summary>
        /// Initializes a new query result with in-memory results.
        /// </summary>
        /// <param name="results">
        /// In-memory results.
        /// </param>
        /// <param name="totalCount">
        /// The total number of items represented by the
        /// results had paging operators not been applied.
        /// </param>
        public QueryResult(IEnumerable results, long? totalCount = null)
        {
            Ensure.NotNull(results, "results");
            this.Results = results;
            if (totalCount != null)
            {
                this.TotalCount = totalCount;
            }
        }

        /// <summary>
        /// Gets or sets an error to be returned.
        /// </summary>
        /// <remarks>
        /// Setting this value will override any existing error or results.
        /// </remarks>
        public Exception Error
        {
            get
            {
                return this._error;
            }
            set
            {
                Ensure.NotNull(value, "value");
                this._error = value;
                this._resultsSource = null;
                this._results = null;
                this._totalCount = null;
            }
        }

        /// <summary>
        /// Gets or sets the entity set from which the results were sourced.
        /// </summary>
        /// <remarks>
        /// This property will be <c>null</c> if the results are not instances
        /// of a particular entity type that has an associated entity set.
        /// </remarks>
        public IEdmEntitySet ResultsSource
        {
            get
            {
                return this._resultsSource;
            }
            set
            {
                if (this._error != null)
                {
                    throw new InvalidOperationException();
                }
                this._resultsSource = value;
            }
        }

        /// <summary>
        /// Gets or sets the in-memory results.
        /// </summary>
        /// <remarks>
        /// Setting this value will override any existing error or results.
        /// </remarks>
        public IEnumerable Results
        {
            get
            {
                return this._results;
            }
            set
            {
                Ensure.NotNull(value, "value");
                this._error = null;
                this._resultsSource = null;
                this._results = value;
                this._totalCount = null;
            }
        }

        /// <summary>
        /// Gets or sets the total number of items available but not
        /// returned by a query whose items were filtered for paging.
        /// </summary>
        /// <remarks>
        /// This should be <c>null</c> if total count
        /// is not supported or was not requested.
        /// </remarks>
        public long? TotalCount
        {
            get
            {
                return this._totalCount;
            }
            set
            {
                if (this._results == null)
                {
                    throw new InvalidOperationException();
                }
                Ensure.NotNull(value, "value");
                this._totalCount = value;
            }
        }
    }
}
