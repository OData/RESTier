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
        private static ConcurrentDictionary<Type, Action<IServiceCollection>> publisherServicesCallback =
            new ConcurrentDictionary<Type, Action<IServiceCollection>>();

        private static Action<IServiceCollection> emptyConfig = _ => { };

        private ApiConfiguration apiConfiguration;
        private IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBase" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        /// An <see cref="IServiceProvider"/> containing all services of this <see cref="ApiConfiguration"/>.
        /// </param>
        protected ApiBase(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> which contains all services of this <see cref="ApiConfiguration"/>.
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get
            {
                return serviceProvider;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this API has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets the API configuration for this API.
        /// </summary>
        internal ApiConfiguration Configuration
        {
            get
            {
                if (this.apiConfiguration == null)
                {
                    this.apiConfiguration = serviceProvider.GetService<ApiConfiguration>();
                }

                return this.apiConfiguration;
            }
        }

        /// <summary>
        /// Configure services for this API.
        /// </summary>
        /// <param name="apiType">
        /// The Api type.
        /// </param>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> with which is used to store all services.
        /// </param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        [CLSCompliant(false)]
        public static IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            // Add core and convention's services
            services = services.AddCoreServices(apiType)
                .AddConventionBasedServices(apiType);

            // This is used to add the publisher's services
            GetPublisherServiceCallback(apiType)(services);

            return services;
        }

        /// <summary>
        /// Adds a configuration procedure for apiType.
        /// This is expected to be called by publisher like WebApi to add services.
        /// </summary>
        /// <param name="apiType">
        /// The Api Type.
        /// </param>
        /// <param name="configurationCallback">
        /// An action that will be called during the configuration of apiType.
        /// </param>
        [CLSCompliant(false)]
        public static void AddPublisherServices(Type apiType, Action<IServiceCollection> configurationCallback)
        {
            publisherServicesCallback.AddOrUpdate(
                apiType,
                configurationCallback,
                (type, existing) => existing + configurationCallback);
        }

        /// <summary>
        /// Get publisher registering service callback for specified Api.
        /// </summary>
        /// <param name="apiType">
        /// The Api type of which to get the publisher registering service callback.
        /// </param>
        /// <returns>The service registering callback.</returns>
        [CLSCompliant(false)]
        public static Action<IServiceCollection> GetPublisherServiceCallback(Type apiType)
        {
            Action<IServiceCollection> val;
            if (publisherServicesCallback.TryGetValue(apiType, out val))
            {
                return val;
            }

            return emptyConfig;
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
            GC.SuppressFinalize(this);
        }
    }
}
