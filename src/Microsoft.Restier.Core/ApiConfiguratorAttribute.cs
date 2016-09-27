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
        /// Add Api services into the DI container.
        /// </summary>
        /// <param name="services">
        /// The Api services registration.
        /// </param>
        /// <param name="type">
        /// The Api type on which this attribute was placed.
        /// </param>
        [CLSCompliant(false)]
        public virtual void AddApiServices(
            IServiceCollection services,
            Type type)
        {
        }

        /// <summary>
        /// Update an Api configuration after ApiConfiguration is created.
        /// </summary>
        /// <param name="configuration">
        /// An Api configuration.
        /// </param>
        /// <param name="type">
        /// The Api type on which this attribute was placed.
        /// </param>
        public virtual void UpdateApiConfiguration(
            ApiConfiguration configuration,
            Type type)
        {
        }

        /// <summary>
        /// Update an Api context after ApiContext is created.
        /// </summary>
        /// <param name="context">
        /// An Api context.
        /// </param>
        /// <param name="type">
        /// The Api type on which this attribute was placed.
        /// </param>
        /// <param name="instance">
        /// An Api instance, if applicable.
        /// </param>
        public virtual void UpdateApiContext(
            ApiContext context,
            Type type,
            object instance)
        {
        }

        /// <summary>
        /// Disposes an Api context.
        /// </summary>
        /// <param name="context">
        /// An Api context.
        /// </param>
        /// <param name="type">
        /// The Api type on which this attribute was placed.
        /// </param>
        /// <param name="instance">
        /// An Api instance, if applicable.
        /// </param>
        public virtual void Dispose(
            ApiContext context,
            Type type,
            object instance)
        {
        }
    }
}
