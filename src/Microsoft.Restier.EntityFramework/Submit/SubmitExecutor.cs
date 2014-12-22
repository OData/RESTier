// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Data.Entity;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.EntityFramework.Submit
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
