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
        private IServiceProvider serviceProvider;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> which contains all services of this <see cref="ApiConfiguration"/>.
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get
            {
                return serviceProvider;
            }

            set
            {
                // TODO use set but not in constructor as need to update lots of test cases
                this.serviceProvider = value;
            }
        }

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
                    this.apiContext = serviceProvider.GetService<ApiContext>();
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
                if (this.apiConfiguration == null)
                {
                    this.apiConfiguration = serviceProvider.GetService<ApiConfiguration>();
                }

                return this.apiConfiguration;
            }

            set
            {
                // TODO keep now as lots of test cases need to update
                this.apiConfiguration = value;
                Configurations.TryAdd(GetType(), apiConfiguration);
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
            ApiConfiguration.GetPublisherServiceCallback(apiType)(services);

            return services;
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
                this.apiContext = null;
            }

            GC.SuppressFinalize(this);
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
