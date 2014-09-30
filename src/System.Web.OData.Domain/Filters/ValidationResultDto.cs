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

using Microsoft.Data.Domain.Submit;

namespace System.Web.OData.Domain.Filters
{
    /// <summary>
    /// A data transfer object that is used to serialize ValidationResult instances to the client.
    /// </summary>
    public class ValidationResultDto
    {
        private ValidationResult result;

        public ValidationResultDto(ValidationResult result)
        {
            this.result = result;
        }

        public string Id
        {
            get { return this.result.Id; }
        }

        public string Message
        {
            get { return this.result.Message; }
        }

        public string PropertyName
        {
            get { return this.result.PropertyName; }
        }

        // TODO: implement Target.  if this is a $batch request return the ContentId "$0" for the target.
        //public string Target
        //{
        //    get { return this.result.Target.ToString(); }
        //}

        public string Severity
        {
            get { return Enum.GetName(typeof(ValidationSeverity), this.result.Severity); }
        }
    }
}
