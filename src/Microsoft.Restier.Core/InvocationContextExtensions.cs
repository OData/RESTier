// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents the API engine and provides a set of static
    /// (Shared in Visual Basic) methods for interacting with objects
    /// that implement <see cref="InvocationContext"/>.
    /// </summary>
    public static class InvocationContextExtensions
    {
        #region GetApiService<T>

        /// <summary>
        /// Gets an API service.
        /// </summary>
        /// <param name="context">
        /// An invocation context.
        /// </param>
        /// <typeparam name="T">The API service type.</typeparam>
        /// <returns>The API service instance.</returns>
        public static T GetApiService<T>(this InvocationContext context) where T : class
        {
            Ensure.NotNull(context, "context");
            if (context.ServiceProvider != null)
            {
                return context.ServiceProvider.GetService<T>();
            }

            return null;
        }

        /// <summary>
        /// Gets an ordered collection of service instances.
        /// </summary>
        /// <param name="context">
        /// An invocation context.
        /// </param>
        /// <typeparam name="T">The API service type.</typeparam>
        /// <returns>The ordered collection of service instances.</returns>
        public static IEnumerable<T> GetApiServices<T>(this InvocationContext context) where T : class
        {
            Ensure.NotNull(context, "context");
            if (context.ServiceProvider != null)
            {
                return context.ServiceProvider.GetServices<T>();
            }

            return null;
        }

        #endregion
    }
}
