using System;

namespace Microsoft.Data.Domain.Security
{
    /// <summary>
    /// Specifies that principal-supplied role-based
    /// security should be enabled for a domain.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    public class EnableRoleBasedSecurityAttribute : DomainParticipantAttribute
    {
        /// <summary>
        /// Configures a domain configuration.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        /// <param name="type">
        /// The domain type on which this attribute was placed.
        /// </param>
        public override void Configure(
            DomainConfiguration configuration,
            Type type)
        {
            configuration.EnableRoleBasedSecurity();
        }
    }
}
