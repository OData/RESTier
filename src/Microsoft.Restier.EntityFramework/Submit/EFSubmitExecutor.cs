// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.EntityFramework
{

    /// <summary>
    /// On Submit, commits the changes inside the registered <see cref="DbContext"/> for the current request to the database.
    /// </summary>
    internal class EFSubmitExecutor : DefaultSubmitExecutor
    {
        /// <summary>
        /// Asynchronously executes the submission.
        /// </summary>
        /// <param name="context">The submit context class used for preparation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task object that represents this asynchronous operation.</returns>
        public async override Task<SubmitResult> ExecuteSubmitAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            var dbContext = context.GetApiService<DbContext>();
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return await base.ExecuteSubmitAsync(context, cancellationToken).ConfigureAwait(false);
        }

    }

}