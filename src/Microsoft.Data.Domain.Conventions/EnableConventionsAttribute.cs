using System;

namespace Microsoft.Data.Domain
{
    /// <summary>
    /// Specifies that code-based conventions will be enabled for a domain.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    public class EnableConventionsAttribute : DomainParticipantAttribute
    {
        /// <inheritdoc/>
        public override void Configure(
            DomainConfiguration configuration,
            Type type)
        {
            configuration.EnableConventions(type);
        }

        /// <inheritdoc/>
        public override void Initialize(
            DomainContext context,
            Type type, object instance)
        {
            context.SetProperty(type.AssemblyQualifiedName, instance);
        }
    }
}
