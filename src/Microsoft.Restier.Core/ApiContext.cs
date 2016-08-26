// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents context under which an API operates.
    /// </summary>
    /// <remarks>
    /// An API context is an instantiation of an API configuration.
    /// </remarks>
    public class ApiContext : IDisposable
    {
        private IServiceProvider serviceProvider;
        private ApiConfiguration apiConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiContext" /> class.
        /// </summary>
        /// <param name="provider">
        /// The service provider.
        /// </param>
        public ApiContext(IServiceProvider provider)
        {
            this.serviceProvider = provider;
        }

        /// <summary>
        /// Gets a value indicating whether this API context has been disposed.
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
        /// Gets the <see cref="IServiceProvider"/> which contains all services of this <see cref="ApiContext"/>.
        /// </summary>
        internal IServiceProvider ServiceProvider
        {
            get { return this.serviceProvider; }
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
