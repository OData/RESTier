// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Restier.Core.Conventions;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Offers a collection of extension methods to <see cref="ApiConfiguration"/>.
    /// </summary>
    public static class ApiConfigurationExtensions
    {
        /// <summary>
        /// Ignores the given property when building the model.
        /// </summary>
        /// <param name="configuration">An API configuration.</param>
        /// <param name="propertyName">The name of the property to be ignored.</param>
        /// <returns>The current API configuration instance.</returns>
        public static ApiConfiguration IgnoreProperty(
            this ApiConfiguration configuration,
            string propertyName)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(propertyName, "propertyName");

            var ignored = configuration.GetApiService<IgnoredPropertyList>();
            if (ignored != null)
            {
                ignored.Add(propertyName);
            }

            return configuration;
        }

        internal static bool IsPropertyIgnored(this ApiConfiguration configuration, string propertyName)
        {
            Ensure.NotNull(configuration, "configuration");

            var ignored = configuration.GetApiService<IgnoredPropertyList>();
            if (ignored != null)
            {
                return ignored.Contains(propertyName);
            }

            return false;
        }
    }
}