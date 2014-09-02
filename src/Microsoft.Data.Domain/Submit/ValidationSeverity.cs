namespace Microsoft.Data.Domain.Submit
{
    /// <summary>
    /// Specifies the severity of a validation result.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Specifies a validation error.
        /// </summary>
        Error,

        /// <summary>
        /// Specifies a validation warning.
        /// </summary>
        Warning,

        /// <summary>
        /// Specifies validation information.
        /// </summary>
        Informational
    }
}
