namespace Microsoft.Data.Domain
{
    /// <summary>
    /// Represents a domain.
    /// </summary>
    /// <remarks>
    /// A domain composes a domain configuration with semantics
    /// around the creation and disposal of a domain context.
    /// </remarks>
    public interface IDomain
    {
        /// <summary>
        /// Gets the context for this domain.
        /// </summary>
        DomainContext Context { get; }
    }
}
