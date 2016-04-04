// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents context under which an API flow operates.
    /// </summary>
    /// <remarks>
    /// An invocation context is created each time an API is invoked and
    /// is used for a specific API flow.
    /// </remarks>
    public class InvocationContext : PropertyBag
    {
        private readonly IServiceScope scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvocationContext" /> class.
        /// </summary>
        /// <param name="apiContext">
        /// An API context.
        /// </param>
        public InvocationContext(ApiContext apiContext)
        {
            Ensure.NotNull(apiContext, "apiContext");

            this.ApiContext = apiContext;
            this.scope = apiContext.ServiceProvider
                .GetRequiredService<IServiceScopeFactory>().CreateScope();
        }

        /// <summary>
        /// Gets the API context.
        /// </summary>
        public ApiContext ApiContext { get; private set; }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> which contains all services of this <see cref="ApiContext"/>.
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get { return this.scope.ServiceProvider; }
        }

        /// <summary>
        /// Gets a service from the <see cref="ApiContext"/>.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The service instance.</returns>
        public T GetApiContextService<T>() where T : class
        {
            return this.ApiContext.GetApiService<T>();
        }

        /// <summary>
        /// Gets an ordered collection of service instances from the <see cref="ApiContext"/>.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The ordered collection of service instances.</returns>
        public IEnumerable<T> GetApiContextServices<T>() where T : class
        {
            return this.ApiContext.GetApiServices<T>();
        }

        internal void DisposeScope()
        {
            this.scope.Dispose();
        }
    }
}
