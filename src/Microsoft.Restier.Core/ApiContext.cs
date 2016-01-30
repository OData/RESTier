// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Restier.Core.Properties;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents context under which an API operates.
    /// </summary>
    /// <remarks>
    /// An API context is an instantiation of an API configuration. It
    /// maintains a set of properties that can be used to share instance
    /// data between hook points.
    /// </remarks>
    public class ApiContext : PropertyBag
    {
        private IServiceScope contextScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiContext" /> class.
        /// </summary>
        /// <param name="configuration">
        /// An API configuration.
        /// </param>
        public ApiContext(ApiConfiguration configuration)
        {
            Ensure.NotNull(configuration, "configuration");
            if (!configuration.IsCommitted)
            {
                throw new ArgumentException(Resources.ApiConfigurationShouldBeCommitted);
            }

            this.Configuration = configuration;
            this.contextScope = configuration.ServiceProvider.GetRequiredService<IApiScopeFactory>().CreateApiScope();
        }

        /// <summary>
        /// Gets the API configuration.
        /// </summary>
        public ApiConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> which contains all services of this <see cref="ApiContext"/>.
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get { return this.contextScope.ServiceProvider; }
        }

        /// <summary>
        /// Gets a service instance.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The service instance.</returns>
        public T GetApiService<T>() where T : class
        {
            return this.ServiceProvider.GetService<T>();
        }

        internal void DisposeScope()
        {
            this.contextScope.Dispose();
        }
    }
}
