// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Domain.Submit;

namespace System.Web.OData.Domain.Batch
{
    public class ODataDomainChangeSetProperty
    {
        private readonly ODataDomainChangeSetRequestItem changeSetRequestItem;
        private readonly TaskCompletionSource<bool> changeSetCompletedTaskSource;
        private int subRequestCount;

        public ODataDomainChangeSetProperty(ODataDomainChangeSetRequestItem changeSetRequestItem)
        {
            this.changeSetRequestItem = changeSetRequestItem;
            this.changeSetCompletedTaskSource = new TaskCompletionSource<bool>();
            this.subRequestCount = this.changeSetRequestItem.Requests.Count();
        }

        public ChangeSet ChangeSet { get; set; }

        public Task OnChangeSetCompleted()
        {
            if (Interlocked.Decrement(ref this.subRequestCount) == 0)
            {
                this.changeSetRequestItem.SubmitChangeSet(this.ChangeSet, () => this.changeSetCompletedTaskSource.SetResult(true));
            }

            return this.changeSetCompletedTaskSource.Task;
        }
    }
}
