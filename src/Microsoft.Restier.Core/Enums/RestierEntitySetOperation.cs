namespace Microsoft.Restier.Core
{

    /// <summary>
    /// Represents the Restier operations available to an EntitySet.
    /// </summary>
    public enum RestierEntitySetOperation
    {

        /// <summary>
        /// Represents a Filter operation.
        /// </summary>
        Filter = 1,

        /// <summary>
        /// Represents an Insert operation.
        /// </summary>
        Insert = 2,

        /// <summary>
        /// Represents an Update operation.
        /// </summary>
        Update = 3,

        /// <summary>
        /// Represents a Delete operation.
        /// </summary>
        Delete = 4

    }

}