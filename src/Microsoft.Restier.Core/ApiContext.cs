// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
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
    public class ApiContext : PropertyBag, IDisposable
    {
        private IServiceScope contextScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiContext" /> class.
        /// </summary>
        /// <param name="configuration">
        /// An API configuration.
        /// </param>
        public ApiContext(ApiConfiguration configuration)
            : this(configuration.ServiceProvider.GetService<IServiceScopeFactory>().CreateScope())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiContext" /> class.
        /// </summary>
        /// <param name="contextScope">
        /// The <see cref="IServiceScope"/> within which to initialize the <see cref="ApiContext"/>.
        /// </param>
        [CLSCompliant(false)]
        public ApiContext(IServiceScope contextScope)
        {
            Ensure.NotNull(contextScope, "contextScope");

            this.contextScope = contextScope;
            this.Configuration = contextScope.ServiceProvider.GetRequiredService<ApiConfiguration>();
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
            get
            {
                if (this.contextScope == null)
                {
                    throw new ObjectDisposedException("ApiContext");
                }

                return this.contextScope.ServiceProvider;
            }
        }

        public void Dispose()
        {
            var scope = this.contextScope;
            if (scope == null)
            {
                return;
            }

            try
            {
                var configs = scope.ServiceProvider
                    .GetServices<IApiContextConfigurator>().Reverse();
                foreach (var e in configs)
                {
                    e.Cleanup(this);
                }
            }
            finally
            {
                this.contextScope = null;
                scope.Dispose();
            }
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

        /// <summary>
        /// Gets all registered service instances.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The ordered collection of service instances.</returns>
        public IEnumerable<T> GetApiServices<T>() where T : class
        {
            return this.ServiceProvider.GetServices<T>();
        }
    }
}
