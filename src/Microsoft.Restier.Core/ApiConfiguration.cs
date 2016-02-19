// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Properties;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents a configuration that defines an API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An API configuration defines the model and behavior of an API
    /// through a set of registered hook points. It also maintains a set of
    /// properties that can be used to share static data between hook points.
    /// </para>
    /// <para>
    /// Hook points may be singletons, meaning there is at most one instance of
    /// the hook point registered, or multi-cast, in which case there can be
    /// zero or more instances of the hook point that are registered. In the
    /// multi-cast case, registration order is maintained, and such hook points
    /// are normally used in the original or reverse order of registration.
    /// </para>
    /// <para>
    /// In order to use an API configuration, it must first be committed.
    /// This fixes the configuration so that its set of hook points are
    /// immutable, ensuring that any active use of the configuration sees a
    /// consistent set of hook points throughout a particular API flow.
    /// </para>
    /// </remarks>
    public class ApiConfiguration : PropertyBag
    {
        private IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiConfiguration" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        /// An <see cref="IServiceProvider"/> containing all services of this <see cref="ApiConfiguration"/>.
        /// </param>
        public ApiConfiguration(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> which contains all services of this <see cref="ApiConfiguration"/>.
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get { return serviceProvider; }
        }

        /// <summary>
        /// Gets a value indicating whether this API configuration has been committed.
        /// </summary>
        public bool IsCommitted
        {
            get { return serviceProvider != null; }
        }

        internal IEdmModel Model { get; set; }

        /// <summary>
        /// Ensures this API configuration has been committed.
        /// </summary>
        public void EnsureCommitted()
        {
            if (!IsCommitted)
            {
                throw new ArgumentException(Resources.ApiConfigurationShouldBeCommitted);
            }
        }

        /// <summary>
        /// Gets a service instance.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The service instance.</returns>
        public T GetHookHandler<T>() where T : class
        {
            return this.serviceProvider.GetService<T>();
        }
    }
}
