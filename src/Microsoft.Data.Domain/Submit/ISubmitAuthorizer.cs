using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// Represents a submit authorizer.
    /// </summary>
    /// <remarks>
    /// This is a multi-cast hook point whose instances
    /// are used in the reverse order of registration.
    /// </remarks>
    public interface ISubmitAuthorizer
    {
        /// <summary>
        /// Asynchronously authorizes a submit.
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
        /// <remarks>
        /// This entry point can be used to perform basic authorization logic.
        /// </remarks>
        Task<bool> AuthorizeAsync(
            SubmitContext context,
            CancellationToken cancellationToken);
    }
}
