// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.Tracing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents a single result when validating an entity, property, etc.
    /// </summary>
    public class ChangeSetItemValidationResult
    {
        /// <summary>
        /// Gets or sets the identifier for this validation result.
        /// </summary>
        /// <remarks>
        /// Id allows programmatic matching of validation results between tiers.
        /// </remarks>
        [JsonProperty(PropertyName = "validatortype")]
        public string ValidatorType { get; set; }

        /// <summary>
        /// Gets or sets the item to which the validation result applies.
        /// </summary>
        [JsonIgnore]
        public object Target { get; set; }

        /// <summary>
        /// Gets or sets the name of the property to which the validation result applies.
        /// If null, the validation result applies to the whole Target.
        /// </summary>
        [JsonProperty(PropertyName = "propertyname")]
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the severity of this validation result.
        /// </summary>
        [JsonProperty(PropertyName = "severity")]
        [JsonConverter(typeof(StringEnumConverter))]
        public EventLevel Severity { get; set; }

        /// <summary>
        /// Gets or sets the message to be displayed to the end user for this validation result.
        /// </summary>
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        /// <summary>
        /// Returns the string that represents this validation result.
        /// </summary>
        /// <returns>
        /// The string that represents this validation result.
        /// </returns>
        public override string ToString()
        {
            return this.Message;
        }
    }
}
