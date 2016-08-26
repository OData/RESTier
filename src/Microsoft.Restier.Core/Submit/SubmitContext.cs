// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents context under which a submit flow operates.
    /// </summary>
    public class SubmitContext : InvocationContext
    {
        private ChangeSet changeSet;
        private SubmitResult result;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitContext" /> class.
        /// </summary>
        /// <param name="provider">
        /// The service provider used to get service from container
        /// </param>
        /// <param name="changeSet">
        /// A change set.
        /// </param>
        public SubmitContext(IServiceProvider provider, ChangeSet changeSet)
            : base(provider)
        {
            this.changeSet = changeSet;
        }

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
                return this.changeSet;
            }

            set
            {
                if (this.Result != null)
                {
                    throw new InvalidOperationException(
                        Resources.CannotSetChangeSetIfThereIsResult);
                }

                this.changeSet = value;
            }
        }

        /// <summary>
        /// Gets or sets the submit result.
        /// </summary>
        public SubmitResult Result
        {
            get
            {
                return this.result;
            }

            set
            {
                Ensure.NotNull(value, "value");
                this.result = value;
            }
        }
    }
}
