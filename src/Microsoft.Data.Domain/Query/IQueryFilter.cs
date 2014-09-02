using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Data.Domain.Query
{
    /// <summary>
    /// Represents a hook point that filters a query.
    /// </summary>
    /// <remarks>
    /// This is a multi-cast hook point whose request filters are
    /// used in the reverse order of registration and whose result
    /// filters are used in the original order of registration.
    /// </remarks>
    public interface IQueryFilter
    {
        /// <summary>
        /// Asynchronously filters an incoming query request.
        /// </summary>
        /// <param name="context">
        /// The query context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task FilterRequestAsync(
            QueryContext context,
            CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously filters an outgoing query result.
        /// </summary>
        /// <param name="context">
        /// The query context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task FilterResultAsync(
            QueryContext context,
            CancellationToken cancellationToken);
    }
}
