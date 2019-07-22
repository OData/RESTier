// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if !EF7
using System.Data.Entity;
#endif
using System.Threading;
using System.Threading.Tasks;
#if EF7
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.EntityFramework
{
    /// <summary>
    /// To execute submission of changes to database.
    /// </summary>
    internal class SubmitExecutor : ISubmitExecutor
    {
        /// <summary>
        /// Asynchronously executes the submission.
        /// </summary>
        /// <param name="context">The submit context class used for preparation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that represents this asynchronous operation.</returns>
        public async Task<SubmitResult> ExecuteSubmitAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            var dbContext = (context.Api as IDbContextProvider).DbContext;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new SubmitResult(context.ChangeSet);
        }
    }
}
