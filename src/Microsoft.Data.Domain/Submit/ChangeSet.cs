// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;

namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// Represents a change set.
    /// </summary>
    public class ChangeSet
    {
        private List<ChangeSetEntry> entries;

        /// <summary>
        /// Initializes a new change set.
        /// </summary>
        public ChangeSet()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new change set.
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
        // TODO: make the ChangeSet 'dynamic' so it gets added to as things change during the flow
        public bool AnEntityHasChanged { get; set; }
    }
}
