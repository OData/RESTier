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
        private SubmitExecutor()
        {
        }

        private static readonly SubmitExecutor instance = new SubmitExecutor();

        /// <summary>
        /// Gets the singleton instance of the <see cref="SubmitExecutor"/> class.
        /// </summary>
        public static SubmitExecutor Instance { get { return instance; } }

        /// <summary>
        /// Asynchronously executes the submission.
        /// </summary>
        /// <param name="context">The context that contains the <see cref="DbContext"/> and the <see cref="ChangeSet"/>.</param>
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
