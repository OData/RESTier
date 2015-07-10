// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Specifies a set of methods that can participate in the
    /// configuration, initialization and disposal of a domain.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public abstract class DomainParticipantAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainParticipantAttribute" /> class.
        /// </summary>
        protected DomainParticipantAttribute()
        {
        }

        /// <summary>
        /// Applies configuration from any domain participant attributes
        /// specified on a domain type to a domain configuration.
        /// </summary>
        /// <param name="type">
        /// A domain type.
        /// </param>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        public static void ApplyConfiguration(
            Type type, DomainConfiguration configuration)
        {
            Ensure.NotNull(type, "type");
            Ensure.NotNull(configuration, "configuration");
            if (type.BaseType != null)
            {
                DomainParticipantAttribute.ApplyConfiguration(
                    type.BaseType, configuration);
            }
            var attributes = type.GetCustomAttributes(
                typeof(DomainParticipantAttribute), false);
            foreach (DomainParticipantAttribute attribute in attributes)
            {
                attribute.Configure(configuration, type);
            }
        }

        /// <summary>
        /// Applies initialization routines from any domain participant
        /// attributes specified on a domain type to a domain context.
        /// </summary>
        /// <param name="type">
        /// A domain type.
        /// </param>
        /// <param name="instance">
        /// A domain instance, if applicable.
        /// </param>
        /// <param name="context">
        /// A domain context.
        /// </param>
        public static void ApplyInitialization(
            Type type, object instance, DomainContext context)
        {
            Ensure.NotNull(type, "type");
            Ensure.NotNull(context, "context");
            if (type.BaseType != null)
            {
                DomainParticipantAttribute.ApplyInitialization(
                    type.BaseType, instance, context);
            }
            var attributes = type.GetCustomAttributes(
                typeof(DomainParticipantAttribute), false);
            foreach (DomainParticipantAttribute attribute in attributes)
            {
                attribute.Initialize(context, type, instance);
            }
        }

        /// <summary>
        /// Applies disposal routines from any domain participant
        /// attributes specified on a domain type to a domain context.
        /// </summary>
        /// <param name="type">
        /// A domain type.
        /// </param>
        /// <param name="instance">
        /// A domain instance, if applicable.
        /// </param>
        /// <param name="context">
        /// A domain context.
        /// </param>
        public static void ApplyDisposal(
            Type type, object instance, DomainContext context)
        {
            Ensure.NotNull(type, "type");
            Ensure.NotNull(context, "context");
            var attributes = type.GetCustomAttributes(
                typeof(DomainParticipantAttribute), false);
            foreach (DomainParticipantAttribute attribute in attributes.Reverse())
            {
                attribute.Dispose(context, type, instance);
            }
            if (type.BaseType != null)
            {
                DomainParticipantAttribute.ApplyDisposal(
                    type.BaseType, instance, context);
            }
        }

        /// <summary>
        /// Configures a domain configuration.
        /// </summary>
        /// <param name="configuration">
        /// A domain configuration.
        /// </param>
        /// <param name="type">
        /// The domain type on which this attribute was placed.
        /// </param>
        public virtual void Configure(
            DomainConfiguration configuration,
            Type type)
        {
        }

        /// <summary>
        /// Initializes a domain context.
        /// </summary>
        /// <param name="context">
        /// A domain context.
        /// </param>
        /// <param name="type">
        /// The domain type on which this attribute was placed.
        /// </param>
        /// <param name="instance">
        /// A domain instance, if applicable.
        /// </param>
        public virtual void Initialize(
            DomainContext context,
            Type type, object instance)
        {
        }

        /// <summary>
        /// Disposes a domain context.
        /// </summary>
        /// <param name="context">
        /// A domain context.
        /// </param>
        /// <param name="type">
        /// The domain type on which this attribute was placed.
        /// </param>
        /// <param name="instance">
        /// A domain instance, if applicable.
        /// </param>
        public virtual void Dispose(
            DomainContext context,
            Type type, object instance)
        {
        }
    }
}
