// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.WebApi.Filters
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

        // TODO GitHubIssue#40 : Implement Target for ValidationResultDTO
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
