// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Provides a set of static (Shared in Visual Basic)
    /// methods for interacting with objects that implement
    /// <see cref="ApiConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ApiConfigurationExtensions
    {
        /// <summary>
        /// Enables principal-supplied role-based security for an API.
        /// </summary>
        /// <param name="services">
        /// The API services registration.
        /// </param>
        /// <remarks>
        /// This method adds hook points to the API configuration that
        /// authorize according to roles assigned to the current principal
        /// along with any that have been asserted during an API flow.
        /// </remarks>
        [CLSCompliant(false)]
        public static void EnableRoleBasedSecurity(
            this IServiceCollection services)
        {
            Ensure.NotNull(services, "services");
            if (!services.HasService<RoleBasedAuthorizer>())
            {
                services.CutoffPrevious<IQueryExpressionInspector, RoleBasedAuthorizer>();
            }
        }

        [CLSCompliant(false)]
        public static void EnableApiPolicy(
            this IServiceCollection services, Type apiType)
        {
            Ensure.NotNull(services, "services");
            services.ChainPrevious<IQueryExpressionExpander>(next => new ApiPolicyActivator(apiType)
            {
                InnerHandler = next,
            });
        }

        [CLSCompliant(false)]
        public static void EnableApiPolicy<T>(this IServiceCollection services)
            where T : class
        {
            services.EnableApiPolicy(typeof(T));
        }
    }
}
