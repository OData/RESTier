using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Submit
{

    /// <summary>
    /// 
    /// </summary>
    public class DefaultSubmitExecutor : ISubmitExecutor
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<SubmitResult> ExecuteSubmitAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            return Task.FromResult(new SubmitResult(context.ChangeSet));
        }

    }

}