namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// Represents a single result when validating an entity, property, etc.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets the identifier for this validation result.
        /// </summary>
        /// <remarks>
        /// Id allows programmatic matching of validation results between tiers.
        /// </remarks>
        public string Id { get; set; }

        /// <summary>
        /// Gets the item to which the validation result applies.
        /// </summary>
        public object Target { get; set; }

        /// <summary>
        /// Gets the name of the property to which the validation result applies.  If null, the validation result applies to the whole Target.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets the severity of this validation result.
        /// </summary>
        public ValidationSeverity Severity { get; set; }

        /// <summary>
        /// Gets the message to be displayed to the end user for this validation result.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Returns the string that represents this validation result.
        /// </summary>
        public override string ToString()
        {
            return this.Message;
        }
    }
}
