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
        private List<ChangeSetEntry> entries;

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
        public ChangeSet(IEnumerable<ChangeSetEntry> entries)
        {
            if (entries != null)
            {
                this.entries = new List<ChangeSetEntry>(entries);
            }
        }

        /// <summary>
        /// Gets the entries in this change set.
        /// </summary>
        public IList<ChangeSetEntry> Entries
        {
            get
            {
                if (this.entries == null)
                {
                    this.entries = new List<ChangeSetEntry>();
                }
                return this.entries;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an Entity has been added, modified, or deleted.
        /// </summary>
        /// TODO GitHubIssue#37 : make the ChangeSet 'dynamic' so it gets added to as things change during the flow
        public bool AnEntityHasChanged { get; set; }
    }
}
