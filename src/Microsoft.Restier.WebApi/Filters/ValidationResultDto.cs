﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResultDto" /> class.
        /// </summary>
        /// <param name="result">The validation result.</param>
        public ValidationResultDto(ValidationResult result)
        {
            this.result = result;
        }

        /// <summary>
        /// Gets the id of the <see cref="ValidationResult"/> instance.
        /// </summary>
        public string Id
        {
            get { return this.result.Id; }
        }

        /// <summary>
        /// Gets the message of the <see cref="ValidationResult"/> instance.
        /// </summary>
        public string Message
        {
            get { return this.result.Message; }
        }

        /// <summary>
        /// Gets the property name of the <see cref="ValidationResult"/> instance.
        /// </summary>
        public string PropertyName
        {
            get { return this.result.PropertyName; }
        }

        // TODO GitHubIssue#40 : Implement Target for ValidationResultDTO
        //public string Target
        //{
        //    get { return this.result.Target.ToString(); }
        //}

        /// <summary>
        /// Gets the string that represents the severity of the <see cref="ValidationResult"/> instance.
        /// </summary>
        public string Severity
        {
            get { return Enum.GetName(typeof(ValidationSeverity), this.result.Severity); }
        }
    }
}
