// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

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
