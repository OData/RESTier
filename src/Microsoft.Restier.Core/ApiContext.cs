// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
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
        private IServiceScope scope;

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

            this.scope = contextScope;
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
                if (this.scope == null)
                {
                    throw new ObjectDisposedException("ApiContext");
                }

                return this.scope.ServiceProvider;
            }
        }

        public void Dispose()
        {
            var scope = this.scope;
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
                this.scope = null;
                scope.Dispose();
            }
        }
    }
}
