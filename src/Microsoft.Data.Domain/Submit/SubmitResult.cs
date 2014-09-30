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
using System.Collections.Generic;

namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// Represents a submit result.
    /// </summary>
    public class SubmitResult
    {
        private Exception _error;
        private ChangeSet _completedChangeSet;

        /// <summary>
        /// Initializes a new submit result with an error.
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
        /// Initializes a new submit result.
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
