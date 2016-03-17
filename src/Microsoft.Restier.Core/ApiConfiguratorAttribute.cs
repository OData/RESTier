// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Specifies a set of methods that can participate in the
    /// configuration, initialization and disposal of an API.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public abstract class ApiConfiguratorAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiConfiguratorAttribute" /> class.
        /// </summary>
        protected ApiConfiguratorAttribute()
        {
        }

        /// <summary>
        /// Applies configuration from any API configurator attributes
        /// specified on an API type to an API services.
        /// </summary>
        /// <param name="type">
        /// An API type.
        /// </param>
        /// <param name="services">
        /// The API services registration.
        /// </param>
        [CLSCompliant(false)]
        public static void ApplyApiBuilder(
            Type type, IServiceCollection services)
        {
            Ensure.NotNull(type, "type");
            Ensure.NotNull(services, "services");
            if (type.BaseType != null)
            {
                ApiConfiguratorAttribute.ApplyApiBuilder(
                    type.BaseType, services);
            }

            var attributes = type.GetCustomAttributes(
                typeof(ApiConfiguratorAttribute), false);
            foreach (ApiConfiguratorAttribute attribute in attributes)
            {
                attribute.ConfigureApi(services, type);
            }
        }

        /// <summary>
        /// Applies configuration from any API configurator attributes
        /// specified on an API type to an API configuration.
        /// </summary>
        /// <param name="type">
        /// An API type.
        /// </param>
        /// <param name="configuration">
        /// An API configuration.
        /// </param>
        public static void ApplyConfiguration(
            Type type, ApiConfiguration configuration)
        {
            Ensure.NotNull(type, "type");
            Ensure.NotNull(configuration, "configuration");
            if (type.BaseType != null)
            {
                ApiConfiguratorAttribute.ApplyConfiguration(
                    type.BaseType, configuration);
            }

            var attributes = type.GetCustomAttributes(
                typeof(ApiConfiguratorAttribute), false);
            foreach (ApiConfiguratorAttribute attribute in attributes)
            {
                attribute.Configure(configuration, type);
            }
        }

        /// <summary>
        /// Applies initialization routines from any API configurator
        /// attributes specified on an API type to an API context.
        /// </summary>
        /// <param name="type">
        /// An API type.
        /// </param>
        /// <param name="instance">
        /// An API instance, if applicable.
        /// </param>
        /// <param name="context">
        /// An API context.
        /// </param>
        public static void ApplyInitialization(
            Type type, object instance, ApiContext context)
        {
            Ensure.NotNull(type, "type");
            Ensure.NotNull(context, "context");
            if (type.BaseType != null)
            {
                ApiConfiguratorAttribute.ApplyInitialization(
                    type.BaseType, instance, context);
            }

            var attributes = type.GetCustomAttributes(
                typeof(ApiConfiguratorAttribute), false);
            foreach (ApiConfiguratorAttribute attribute in attributes)
            {
                attribute.Initialize(context, type, instance);
            }
        }

        /// <summary>
        /// Applies disposal routines from any API configurator
        /// attributes specified on an API type to an API context.
        /// </summary>
        /// <param name="type">
        /// An API type.
        /// </param>
        /// <param name="instance">
        /// An API instance, if applicable.
        /// </param>
        /// <param name="context">
        /// An API context.
        /// </param>
        public static void ApplyDisposal(
            Type type, object instance, ApiContext context)
        {
            Ensure.NotNull(type, "type");
            Ensure.NotNull(context, "context");
            var attributes = type.GetCustomAttributes(
                typeof(ApiConfiguratorAttribute), false);
            foreach (ApiConfiguratorAttribute attribute in attributes.Reverse())
            {
                attribute.Dispose(context, type, instance);
            }

            if (type.BaseType != null)
            {
                ApiConfiguratorAttribute.ApplyDisposal(
                    type.BaseType, instance, context);
            }
        }

        /// <summary>
        /// Configures an API services.
        /// </summary>
        /// <param name="services">
        /// The API services registration.
        /// </param>
        /// <param name="type">
        /// The API type on which this attribute was placed.
        /// </param>
        [CLSCompliant(false)]
        public virtual void ConfigureApi(
            IServiceCollection services,
            Type type)
        {
        }

        /// <summary>
        /// Configures an API configuration.
        /// </summary>
        /// <param name="configuration">
        /// An API configuration.
        /// </param>
        /// <param name="type">
        /// The API type on which this attribute was placed.
        /// </param>
        public virtual void Configure(
            ApiConfiguration configuration,
            Type type)
        {
        }

        /// <summary>
        /// Initializes an API context.
        /// </summary>
        /// <param name="context">
        /// An API context.
        /// </param>
        /// <param name="type">
        /// The API type on which this attribute was placed.
        /// </param>
        /// <param name="instance">
        /// An API instance, if applicable.
        /// </param>
        public virtual void Initialize(
            ApiContext context,
            Type type,
            object instance)
        {
        }

        /// <summary>
        /// Disposes an API context.
        /// </summary>
        /// <param name="context">
        /// An API context.
        /// </param>
        /// <param name="type">
        /// The API type on which this attribute was placed.
        /// </param>
        /// <param name="instance">
        /// An API instance, if applicable.
        /// </param>
        public virtual void Dispose(
            ApiContext context,
            Type type,
            object instance)
        {
        }
    }
}
