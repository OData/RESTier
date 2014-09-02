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
