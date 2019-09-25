using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core.Submit
{

    /// <summary>
    /// Provides a default implementation of the <see cref="IChangeSetInitializer"/> interface.
    /// </summary>
    public class DefaultChangeSetInitializer : IChangeSetInitializer
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task InitializeAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, nameof(context));
            context.ChangeSet = new ChangeSet();
            return Task.FromResult(0);
        }

    }

}