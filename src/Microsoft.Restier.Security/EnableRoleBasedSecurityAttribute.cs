// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Security
{
    /// <summary>
    /// Specifies that principal-supplied role-based
    /// security should be enabled for an API.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EnableRoleBasedSecurityAttribute : ApiConfiguratorAttribute
    {
        /// <summary>
        /// Configures an API configuration.
        /// </summary>
        /// <param name="services">
        /// The API services registration.
        /// </param>
        /// <param name="type">
        /// The API type on which this attribute was placed.
        /// </param>
        [CLSCompliant(false)]
        public override void ConfigureApi(
            IServiceCollection services,
            Type type)
        {
            services.EnableRoleBasedSecurity();
        }
    }
}
