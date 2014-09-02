using System;

namespace Microsoft.Data.Domain
{
    using System.ComponentModel;
    using Conventions;
    using Submit;

    /// <summary>
    /// Provides a set of static (Shared in Visual Basic)
    /// methods for interacting with objects that implement
    /// <see cref="DomainConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class DomainConfigurationExtensions
    {
        /// <summary>
        /// Enables code-based conventions for a domain.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        /// <param name="targetType">
        /// The type of a class on which code-based conventions are used.
        /// </param>
        /// <remarks>
        /// This method adds hook points to the domain configuration that
        /// inspect a target type for a variety of code-based conventions
        /// such as usage of specific attributes or members that follow
        /// certain naming conventions.
        /// </remarks>
        public static void EnableConventions(
            this DomainConfiguration configuration,
            Type targetType)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(targetType, "targetType");

            ConventionalChangeSetAuthorizer.ApplyTo(configuration, targetType);
            ConventionalChangeSetEntryFilter.ApplyTo(configuration, targetType);
            configuration.AddHookPoint(typeof(IChangeSetEntryValidator),
                ConventionalChangeSetEntryValidator.Instance);
            ConventionalEntitySetProvider.ApplyTo(configuration, targetType);
            ConventionalEntitySetFilter.ApplyTo(configuration, targetType);
        }
    }
}
