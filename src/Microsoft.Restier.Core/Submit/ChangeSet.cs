// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents a change set.
    /// </summary>
    public class ChangeSet
    {
        private List<ChangeSetItem> entries;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSet" /> class.
        /// </summary>
        public ChangeSet()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeSet" /> class.
        /// </summary>
        /// <param name="entries">
        /// A set of change set entries.
        /// </param>
        public ChangeSet(IEnumerable<ChangeSetItem> entries)
        {
            if (entries != null)
            {
                this.entries = new List<ChangeSetItem>(entries);
            }
        }

        /// <summary>
        /// Gets the entries in this change set.
        /// </summary>
        public IList<ChangeSetItem> Entries
        {
            get
            {
                if (this.entries == null)
                {
                    this.entries = new List<ChangeSetItem>();
                }

                return this.entries;
            }
        }
    }
}
