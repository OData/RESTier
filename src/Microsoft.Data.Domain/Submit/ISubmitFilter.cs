using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// Represents a hook point that filters a submission.
    /// </summary>
    /// <remarks>
    /// This is a multi-cast hook point whose request filters are
    /// used in the reverse order of registration and whose result
    /// filters are used in the original order of registration.
    /// </remarks>
    public interface ISubmitFilter
    {
        /// <summary>
        /// Asynchronously filters an incoming submit request.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task FilterRequestAsync(
            SubmitContext context,
            CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously filters an outgoing submit result.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task FilterResultAsync(
            SubmitContext context,
            CancellationToken cancellationToken);
    }
}
