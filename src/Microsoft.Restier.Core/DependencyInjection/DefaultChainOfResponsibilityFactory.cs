// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Microsoft.Restier.Core.DependencyInjection
{
    /// <summary>
    /// Default factory for creating a chain of responsibility.
    /// </summary>
    /// <remarks>
    /// This factory relies on an implementation detail of the default
    /// MS Dependency Injection container. It assumes that multiple services
    /// for the same interface are registered in the container, and that
    /// they can be resolved in the order they were registered.
    /// For other DI containers, this may not be the case and a different
    /// implementation might be needed.
    /// </remarks>
    internal class DefaultChainOfResponsibilityFactory<TService> : IChainOfResponsibilityFactory<TService>
        where TService : class, IChainedService<TService>
    {
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Creates a new instance of the <see cref="DefaultChainOfResponsibilityFactory{T}"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider to use.</param>
        public DefaultChainOfResponsibilityFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Creates a chain of responsibility.
        /// </summary>
        /// <returns>The chained service of type <typeparamref name="TService"/></returns>
        public TService Create()
        {
            TService previous = null;
            foreach (TService service in serviceProvider.GetServices<IChainedService<TService>>().Cast<TService>())
            {
                service.Inner = previous;
                previous = service;
            }
            return previous;
        }
    }
}
