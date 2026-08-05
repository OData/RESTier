// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core.DependencyInjection
{
    /// <summary>
    /// Interface implemented by factories that create a chain of responsibility.
    /// </summary>
    /// <typeparam name="TService">The service type to create.</typeparam>
    public interface IChainOfResponsibilityFactory<TService>
        where TService : class, IChainedService<TService>
    {
        /// <summary>
        /// Creates a chain of responsibility.
        /// </summary>
        /// <returns>The chained service of type <typeparamref name="TService"/></returns>
        TService Create();
    }
}