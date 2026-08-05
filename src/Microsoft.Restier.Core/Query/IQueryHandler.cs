using Microsoft.Restier.Core.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Defines the contract for a query handler.
    /// </summary>
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
        Task<QueryResult> QueryAsync(QueryContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Ensures that the Element Type exists in the model.
        /// </summary>
        /// <param name="invocationContext">The model context to use.</param>
        /// <param name="namespaceName">The namespace of the element type. Can be null.</param>
        /// <param name="name">The name of the element type.</param>
        /// <returns>The element type.</returns>
        Type EnsureElementType(InvocationContext invocationContext, string namespaceName, string name);
    }
}
