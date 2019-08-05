﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents context under which an request is processed.
    /// The request could be a query, a submit, an operation execution or a model retrieve.
    /// It has subclass for each kinds of request.
    /// </summary>
    /// <remarks>
    /// An invocation context is created each time an request is parsed to a specified request.
    /// </remarks>
    public class InvocationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvocationContext" /> class.
        /// </summary>
        /// <param name="provider">
        /// An API context.
        /// </param>
        public InvocationContext(IServiceProvider provider)
        {
            Ensure.NotNull(provider, nameof(provider));

            ServiceProvider = provider;
        }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> which contains all services of this scope.
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Gets an API service.
        /// </summary>
        /// <typeparam name="T">The API service type.</typeparam>
        /// <returns>The API service instance.</returns>
        public T GetApiService<T>() where T : class
        {
            return ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// Gets an ordered collection of service instances.
        /// </summary>
        /// <typeparam name="T">The API service type.</typeparam>
        /// <returns>The ordered collection of service instances.</returns>
        public IEnumerable<T> GetApiServices<T>() where T : class
        {
            return ServiceProvider.GetServices<T>();
        }
    }
}
