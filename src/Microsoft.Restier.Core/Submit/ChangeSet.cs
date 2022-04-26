// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents a change set.
    /// </summary>
    public class ChangeSet
    {
        private ConcurrentQueue<ChangeSetItem> entries;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSet" /> class.
        /// </summary>
        public ChangeSet()
        {
            this.entries = new ConcurrentQueue<ChangeSetItem>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSet" /> class.
        /// </summary>
        /// <param name="entries">
        /// A set of change set entries.
        /// </param>
        public ChangeSet(IEnumerable<ChangeSetItem> entries)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            this.entries = new ConcurrentQueue<ChangeSetItem>(entries);
        }

        /// <summary>
        /// Gets the entries in this change set.
        /// </summary>
        public ConcurrentQueue<ChangeSetItem> Entries
        {
            get
            {
                return entries;
            }
        }
    }
}
