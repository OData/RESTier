namespace Microsoft.Data.Domain
{
    /// <summary>
    /// Represents an expandable domain.
    /// </summary>
    /// <remarks>
    /// An expandable domain is a domain that can operate given a domain
    /// configuration that was derived from its own. To achieve this, it
    /// defers creation of its domain context until either the Context
    /// property is accessed, in which case its own configuration is used,
    /// or the Initialize method is called with a derived configuration.
    /// </remarks>
    public interface IExpandableDomain : IDomain
    {
        /// <summary>
        /// Gets the configuration for this expandable domain.
        /// </summary>
        DomainConfiguration Configuration { get; }

        /// <summary>
        /// Gets a value indicating if this
        /// expandable domain has been initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes this expandable domain.
        /// </summary>
        /// <param name="derivedConfiguration">
        /// A derived domain configuration.
        /// </param>
        void Initialize(DomainConfiguration derivedConfiguration);
    }
}
