using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// Represents a hook point that implements a submit flow.
    /// </summary>
    /// <remarks>
    /// This is a singleton hook point with a default implementation.
    /// </remarks>
    public interface ISubmitHandler
    {
        /// <summary>
        /// Asynchronously executes the submit flow.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a submit result.
        /// </returns>
        Task<SubmitResult> SubmitAsync(
            SubmitContext context,
            CancellationToken cancellationToken);
    }
}
