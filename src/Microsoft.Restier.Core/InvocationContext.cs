// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents context under which an API flow operates.
    /// </summary>
    /// <remarks>
    /// An invocation context is created each time an API is invoked and
    /// is used for a specific API flow. It maintains a set of properties
    /// that can store data that lives for the lifetime of the flow.
    /// </remarks>
    public class InvocationContext
    {
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
        }

        /// <summary>
        /// Gets the API context.
        /// </summary>
        public ApiContext ApiContext { get; private set; }

        /// <summary>
        /// Gets an API service.
        /// </summary>
        /// <typeparam name="T">The API service type.</typeparam>
        /// <returns>The service instance.</returns>
        /// <remarks>
        /// This method directly returns the API service instance from
        /// the configuration of the inner context.
        /// </remarks>
        public T GetApiService<T>() where T : class
        {
            return this.ApiContext.GetApiService<T>();
        }

        /// <summary>
        /// Gets an ordered collection of service instances.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The ordered collection of service instances.</returns>
        public IEnumerable<T> GetApiServices<T>() where T : class
        {
            return this.ApiContext.GetApiServices<T>();
        }
    }
}
