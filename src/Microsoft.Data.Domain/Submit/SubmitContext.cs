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

using System;
using Microsoft.OData.Edm;

namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// Represents context under which a submit flow operates.
    /// </summary>
    public class SubmitContext : InvocationContext
    {
        private ChangeSet _changeSet;
        private SubmitResult _result;

        /// <summary>
        /// Initializes a new submit context.
        /// </summary>
        /// <param name="domainContext">
        /// A domain context.
        /// </param>
        /// <param name="changeSet">
        /// A change set.
        /// </param>
        public SubmitContext(DomainContext domainContext, ChangeSet changeSet)
            : base(domainContext)
        {
            this.ChangeSet = changeSet;
        }

        /// <summary>
        /// Gets the model that informs this submit context.
        /// </summary>
        public IEdmModel Model { get; internal set; }

        /// <summary>
        /// Gets or sets the change set.
        /// </summary>
        /// <remarks>
        /// The change set cannot be set if there is already a result.
        /// </remarks>
        public ChangeSet ChangeSet
        {
            get
            {
                return this._changeSet;
            }
            set
            {
                if (this.Result != null)
                {
                    throw new InvalidOperationException();
                }
                this._changeSet = value;
            }
        }

        /// <summary>
        /// Gets or sets the submit result.
        /// </summary>
        public SubmitResult Result
        {
            get
            {
                return this._result;
            }
            set
            {
                Ensure.NotNull(value, "value");
                this._result = value;
            }
        }
    }
}
