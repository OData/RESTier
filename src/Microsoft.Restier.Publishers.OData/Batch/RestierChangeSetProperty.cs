// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Publishers.OData.Batch
{
    /// <summary>
    /// Represents an API <see cref="ChangeSet"/> property.
    /// TODO need to redesign this class
    /// </summary>
    internal class RestierChangeSetProperty
    {
        private readonly RestierBatchChangeSetRequestItem changeSetRequestItem;
        private readonly TaskCompletionSource<bool> changeSetCompletedTaskSource;
        private int subRequestCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierChangeSetProperty" /> class.
        /// </summary>
        /// <param name="changeSetRequestItem">The changeset request item.</param>
        public RestierChangeSetProperty(RestierBatchChangeSetRequestItem changeSetRequestItem)
        {
            this.changeSetRequestItem = changeSetRequestItem;
            this.changeSetCompletedTaskSource = new TaskCompletionSource<bool>();
            this.subRequestCount = this.changeSetRequestItem.Requests.Count();
        }

        /// <summary>
        /// Gets or sets the changeset.
        /// </summary>
        public ChangeSet ChangeSet { get; set; }

        /// <summary>
        /// The callback to execute when the changeset is completed.
        /// </summary>
        /// <returns>The task object that represents this callback execution.</returns>
        public Task OnChangeSetCompleted()
        {
            if (Interlocked.Decrement(ref this.subRequestCount) == 0)
            {
                this.changeSetRequestItem.SubmitChangeSet(this.ChangeSet)
                    .ContinueWith(t =>
                    {
                        if (t.Exception != null)
                        {
                            var taskEx =
                                (t.Exception.InnerExceptions != null
                                    && t.Exception.InnerExceptions.Count == 1)
                                ?
                                t.Exception.InnerExceptions.First()
                                :
                                t.Exception;
                            this.changeSetCompletedTaskSource.SetException(taskEx);
                        }
                        else
                        {
                            this.changeSetCompletedTaskSource.SetResult(true);
                        }
                    });
            }

            return this.changeSetCompletedTaskSource.Task;
        }
    }
}
