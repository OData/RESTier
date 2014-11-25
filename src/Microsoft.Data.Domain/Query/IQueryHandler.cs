// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Data.Domain.Query
{
    /// <summary>
    /// Represents a hook point that implements a query flow.
    /// </summary>
    /// <remarks>
    /// This is a singleton hook point with a default implementation.
    /// </remarks>
    public interface IQueryHandler
    {
        /// <summary>
        /// Asynchronously executes the query flow.
        /// </summary>
        /// <param name="context">
        /// The query context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a query result.
        /// </returns>
        Task<QueryResult> QueryAsync(
            QueryContext context,
            CancellationToken cancellationToken);
    }
}
