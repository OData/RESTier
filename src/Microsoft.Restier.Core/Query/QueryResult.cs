// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Properties;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents a query result.
    /// </summary>
    public class QueryResult
    {
        private Exception error;
        private IEdmEntitySet resultsSource;
        private IEnumerable results;
        private long? totalCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryResult" /> class with an error.
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
        /// Initializes a new instance of the <see cref="QueryResult" /> class with in-memory results.
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
                return this.error;
            }

            set
            {
                Ensure.NotNull(value, "value");
                this.error = value;
                this.resultsSource = null;
                this.results = null;
                this.totalCount = null;
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
                return this.resultsSource;
            }

            set
            {
                if (this.error != null)
                {
                    throw new InvalidOperationException(
                        Resources.CannotSetResultsSourceIfThereIsError);
                }

                this.resultsSource = value;
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
                return this.results;
            }

            set
            {
                Ensure.NotNull(value, "value");
                this.error = null;
                this.resultsSource = null;
                this.results = value;
                this.totalCount = null;
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
                return this.totalCount;
            }

            set
            {
                if (this.results == null)
                {
                    throw new InvalidOperationException(
                        Resources.CannotSetTotalCountIfThereIsNoResult);
                }

                Ensure.NotNull(value, "value");
                this.totalCount = value;
            }
        }
    }
}
