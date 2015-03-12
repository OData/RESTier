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
    public class SubmitExecutor : ISubmitExecutor
    {
        private SubmitExecutor()
        {
        }

        private static readonly SubmitExecutor instance = new SubmitExecutor();

        public static SubmitExecutor Instance { get { return instance; } }

        public async Task<SubmitResult> ExecuteSubmitAsync(
            SubmitContext context, CancellationToken cancellationToken)
        {
            DbContext dbContext = context.DomainContext.GetProperty<DbContext>("DbContext");

            await dbContext.SaveChangesAsync(cancellationToken);

            return new SubmitResult(context.ChangeSet);
        }
    }
}
