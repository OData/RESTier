// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents a base class for an API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An API configuration is intended to be long-lived, and can be
    /// statically cached according to an API type specified when the
    /// configuration is created. Additionally, the API model produced
    /// as a result of a particular configuration is cached under the same
    /// API type to avoid re-computing it on each invocation.
    /// </para>
    /// </remarks>
    public abstract class ApiBase : IDisposable
    {
        private static readonly ConcurrentDictionary<Type, ApiConfiguration> Configurations =
            new ConcurrentDictionary<Type, ApiConfiguration>();

        private ApiConfiguration apiConfiguration;
        private ApiContext apiContext;

        /// <summary>
        /// Gets the API context for this API.
        /// </summary>
        public ApiContext Context
        {
            get
            {
                if (this.IsDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                if (this.apiContext == null)
                {
                    this.apiContext = this.CreateApiContext(
                        this.Configuration);
                    var apiScope = this.apiContext.GetApiService<ApiHolder>();
                    if (apiScope != null)
                    {
                        apiScope.Api = this;
                    }
                }

                return this.apiContext;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this API has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets or sets the API configuration for this API.
        /// </summary>
        public ApiConfiguration Configuration
        {
            get
            {
                if (this.apiConfiguration != null)
                {
                    return this.apiConfiguration;
                }

                Configurations.TryGetValue(this.GetType(), out this.apiConfiguration);
                return this.apiConfiguration;
            }

            set
            {
                this.apiConfiguration = value;
                bool isSuccess = Configurations.TryAdd(GetType(), apiConfiguration);
                if (isSuccess)
                {
                    UpdateApiConfiguration(this.apiConfiguration);
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with
        /// freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsDisposed = true;

            if (this.apiContext != null)
            {
                this.apiContext.DisposeScope();
                this.apiContext = null;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Configure services for this API.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> with which to create an <see cref="ApiConfiguration"/>.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        [CLSCompliant(false)]
        public virtual IServiceCollection ConfigureApi(IServiceCollection services)
        {
            Type apiType = this.GetType();

            // Add core and convention's services
            services = services.AddCoreServices(apiType)
                .AddConventionBasedServices(apiType);

            // This is used to add the publisher's services
            ApiConfiguration.GetPublisherServiceCallback(apiType)(services);

            return services;
        }

        /// <summary>
        /// Allow user to update the ApiConfiguration
        /// <see cref="ApiConfiguration"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="ApiConfiguration"/> for the Api instance.</param>
        [CLSCompliant(false)]
        protected virtual void UpdateApiConfiguration(ApiConfiguration configuration)
        {
        }

        /// <summary>
        /// Creates the API context for this API.
        /// Descendants may further configure the built <see cref="ApiContext"/>.
        /// </summary>
        /// <param name="configuration">
        /// The API configuration to use.
        /// </param>
        /// <returns>
        /// The API context for this API.
        /// </returns>
        protected virtual ApiContext CreateApiContext(
            ApiConfiguration configuration)
        {
            return new ApiContext(configuration);
        }

        // Registered as a scoped service so that IApi and ApiContext could be exposed as scoped service.
        // If a descendant class wants to expose these 2 services in another way, it must ensure they could be
        // resolved after CreateApiContext call.
        internal class ApiHolder
        {
            public ApiBase Api { get; set; }
        }
    }
}
