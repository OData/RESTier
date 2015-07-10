// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents a submit result.
    /// </summary>
    public class SubmitResult
    {
        private Exception _error;
        private ChangeSet _completedChangeSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitResult" /> class with an error.
        /// </summary>
        /// <param name="error">
        /// An error.
        /// </param>
        public SubmitResult(Exception error)
        {
            Ensure.NotNull(error, "error");
            this.Error = error;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitResult" /> class
        /// </summary>
        /// <param name="completedChangeSet">
        /// A completed change set.
        /// </param>
        public SubmitResult(ChangeSet completedChangeSet)
        {
            Ensure.NotNull(completedChangeSet, "completedChangeSet");
            this._completedChangeSet = completedChangeSet;
        }

        /// <summary>
        /// Gets or sets an error to be returned.
        /// </summary>
        /// <remarks>
        /// Setting this value will override any
        /// existing error or completed change set.
        /// </remarks>
        public Exception Error
        {
            get
            {
                return this._error;
            }
            set
            {
                Ensure.NotNull(value, "value");
                this._error = value;
                this._completedChangeSet = null;
            }
        }

        /// <summary>
        /// Gets or sets the completed change set.
        /// </summary>
        /// <remarks>
        /// Setting this value will override any
        /// existing error or completed change set.
        /// </remarks>
        public ChangeSet CompletedChangeSet
        {
            get
            {
                return this._completedChangeSet;
            }
            set
            {
                Ensure.NotNull(value, "value");
                this._completedChangeSet = value;
            }
        }
    }
}
