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
    public class ApiContext
    {
        private IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiContext" /> class.
        /// </summary>
        /// <param name="configuration">
        /// An API configuration.
        /// </param>
        /// <param name="provider">
        /// The service provider.
        /// </param>
        public ApiContext(IServiceProvider provider, ApiConfiguration configuration)
        {
            Ensure.NotNull(configuration, "configuration");

            this.Configuration = configuration;
            this.serviceProvider = provider;
        }

        /// <summary>
        /// Gets the API configuration.
        /// </summary>
        public ApiConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> which contains all services of this <see cref="ApiContext"/>.
        /// </summary>
        internal IServiceProvider ServiceProvider
        {
            get { return this.serviceProvider; }
        }
    }
}
