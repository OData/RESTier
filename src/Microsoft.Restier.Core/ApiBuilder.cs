// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A delegate which participate in service creation.
    /// All registered contributors form a chain, and the last registered will be called first.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="serviceProvider">
    /// The <see cref="IServiceProvider"/> to which this contributor call is registered.
    /// </param>
    /// <param name="next">
    /// Return the result of the previous contributor on the chain.
    /// </param>
    /// <returns>A service instance of <typeparamref name="T"/>.</returns>
    public delegate T ApiServiceContributor<T>(IServiceProvider serviceProvider, Func<T> next) where T : class;

    /// <summary>
    /// Builder object to create an <see cref="ApiConfiguration"/>
    /// </summary>
    public sealed class ApiBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBuilder"/> class.
        /// </summary>
        public ApiBuilder()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBuilder"/> class.
        /// </summary>
        /// <param name="services">A service collection.</param>
        [CLSCompliant(false)]
        public ApiBuilder(IServiceCollection services)
        {
            Services = services ?? new ServiceCollection();
            if (!this.HasHookHandler<IQueryExecutor>())
            {
                this.AddHookHandler<IQueryExecutor>(DefaultQueryExecutor.Instance);
            }
        }

        /// <summary>
        /// Gets the service collection containing service registration.
        /// </summary>
        [CLSCompliant(false)]
        public IServiceCollection Services
        {
            get; private set;
        }
    }
}
