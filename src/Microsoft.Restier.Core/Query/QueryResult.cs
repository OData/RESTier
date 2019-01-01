// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents a query result.
    /// </summary>
    public class QueryResult
    {
        private Exception exception;
        private IEdmEntitySet resultsSource;
        private IEnumerable results;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryResult" /> class with an Exception.
        /// </summary>
        /// <param name="exception">
        /// An Exception.
        /// </param>
        public QueryResult(Exception exception)
        {
            Ensure.NotNull(exception, nameof(exception));
            Exception = exception;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryResult" /> class with in-memory results.
        /// </summary>
        /// <param name="results">
        /// In-memory results.
        /// </param>
        public QueryResult(IEnumerable results)
        {
            Ensure.NotNull(results, nameof(results));
            Results = results;
        }

        /// <summary>
        /// Gets or sets an Exception to be returned.
        /// </summary>
        /// <remarks>
        /// Setting this value will override any existing Exception or results.
        /// </remarks>
        public Exception Exception
        {
            get => exception;

            set
            {
                Ensure.NotNull(value, nameof(value));
                exception = value;
                resultsSource = null;
                results = null;
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
            get => resultsSource;

            set
            {
                if (exception != null)
                {
                    throw new InvalidOperationException(Resources.CannotSetResultsSourceIfThereIsAnyError);
                }

                resultsSource = value;
            }
        }

        /// <summary>
        /// Gets or sets the in-memory results.
        /// </summary>
        /// <remarks>
        /// Setting this value will override any existing Exception or results.
        /// </remarks>
        public IEnumerable Results
        {
            get => results;

            set
            {
                Ensure.NotNull(value, nameof(value));
                exception = null;
                resultsSource = null;
                results = value;
            }
        }
    }
}
