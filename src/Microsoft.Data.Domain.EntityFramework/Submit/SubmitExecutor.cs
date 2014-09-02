using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Domain.Submit;

namespace Microsoft.Data.Domain.EntityFramework.Submit
{
    public class SubmitExecutor : ISubmitExecutor
    {
        private SubmitExecutor()
        {
        }

        public static readonly SubmitExecutor Instance = new SubmitExecutor();

        public async Task<SubmitResult> ExecuteSubmitAsync(
            SubmitContext context, CancellationToken cancellationToken)
        {
            DbContext dbContext = context.DomainContext.GetProperty<DbContext>("DbContext");

            await dbContext.SaveChangesAsync(cancellationToken);

            return new SubmitResult(context.ChangeSet);
        }
    }
}
