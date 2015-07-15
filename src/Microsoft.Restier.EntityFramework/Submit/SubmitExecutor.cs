// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EF7
using Microsoft.Data.Entity;
#else
using System.Data.Entity;
#endif
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.EntityFramework.Submit
{
    /// <summary>
    /// To execute submission of changes to database.
    /// </summary>
    public class SubmitExecutor : ISubmitExecutor
    {
        static SubmitExecutor()
        {
            Instance = new SubmitExecutor();
        }

        private SubmitExecutor()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the <see cref="SubmitExecutor"/> class.
        /// </summary>
        public static SubmitExecutor Instance { get; private set; }

        /// <summary>
        /// Asynchronously executes the submission.
        /// </summary>
        /// <param name="context">The submit context class used for preparation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that represents this asynchronous operation.</returns>
        public async Task<SubmitResult> ExecuteSubmitAsync(
            SubmitContext context, CancellationToken cancellationToken)
        {
            DbContext dbContext = context.DomainContext.GetProperty<DbContext>("DbContext");

            await dbContext.SaveChangesAsync(cancellationToken);

            return new SubmitResult(context.ChangeSet);
        }
    }
}
