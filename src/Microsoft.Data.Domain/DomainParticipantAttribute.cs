// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;

namespace Microsoft.Data.Domain
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
        /// Initializes a new domain participant attribute.
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
