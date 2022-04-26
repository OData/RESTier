// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Tests.Shared
{
    public class StoreChangeSetInitializer : DefaultChangeSetInitializer
    {
        public override Task InitializeAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            var changeSetEntry = context.ChangeSet.Entries.Single();

            if (changeSetEntry is DataModificationItem dataModificationEntry)
            {
                dataModificationEntry.Resource = new Product()
                {
                    Name = "var1",
                    Addr = new Address()
                    {
                        Zip = 330
                    }
                };
            }

            return Task.FromResult<object>(null);
        }
    }
}
