using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Default implementation of <see cref="ISubmitExecutor"/>.
    /// </summary>
    public class DefaultSubmitExecutor : ISubmitExecutor
    {
        /// <inheritdoc />
        public virtual Task<SubmitResult> ExecuteSubmitAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            return Task.FromResult(new SubmitResult(context.ChangeSet));
        }

    }

}