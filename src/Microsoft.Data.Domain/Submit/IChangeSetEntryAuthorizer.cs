using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// Represents a change set entry authorizer.
    /// </summary>
    /// <remarks>
    /// This is a multi-cast hook point whose instances
    /// are used in the reverse order of registration.
    /// </remarks>
    public interface IChangeSetEntryAuthorizer
    {
        /// <summary>
        /// Asynchronously authorizes the ChangeSetEntry.
        /// </summary>
        /// <param name="context">
        /// The submit context.
        /// </param>
        /// <param name="entry">
        /// A change set entry to be authorized.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// </returns>
        Task<bool> AuthorizeAsync(
            SubmitContext context,
            ChangeSetEntry entry,
            CancellationToken cancellationToken);
    }
}
