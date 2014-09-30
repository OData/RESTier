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
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// ValidationResults is a collection of ValidationResult instances that identify what is not valid on a validated item.
    /// </summary>
    public class ValidationResults : Collection<ValidationResult>
    {
        /// <summary>
        /// Gets a value indicating whether there are any results that have Severity equal to “Error” in the current validation results.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return this.Errors.Any();
            }
        }

        /// <summary>
        /// Gets a collection of ValidationResult instances that have Severity equal to “Error” in the current validation results.
        /// </summary>
        public IEnumerable<ValidationResult> Errors
        {
            get
            {
                return this.Where(result =>
                    result.Severity == ValidationSeverity.Error);
            }
        }
    }
}
