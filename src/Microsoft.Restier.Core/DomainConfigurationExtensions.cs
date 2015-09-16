// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Offers a collection of extension methods to <see cref="DomainConfiguration"/>.
    /// </summary>
    public static class DomainConfigurationExtensions
    {
        private const string IgnoredPropertiesKey = "Microsoft.Restier.Core.IgnoredProperties";

        /// <summary>
        /// Ignores the given property when building the model.
        /// </summary>
        /// <param name="configuration">A domain configuration.</param>
        /// <param name="propertyName">The name of the property to be ignored.</param>
        /// <returns>The current domain configuration instance.</returns>
        public static DomainConfiguration IgnoreProperty(
            this DomainConfiguration configuration,
            string propertyName)
        {
            Ensure.NotNull(configuration, "configuration");
            Ensure.NotNull(propertyName, "propertyName");

            configuration.GetIgnoredPropertiesImplementation().Add(propertyName);
            return configuration;
        }

        internal static bool IsPropertyIgnored(this DomainConfiguration configuration, string propertyName)
        {
            Ensure.NotNull(configuration, "configuration");

            return configuration.GetIgnoredPropertiesImplementation().Contains(propertyName);
        }

        private static ICollection<string> GetIgnoredPropertiesImplementation(this DomainConfiguration configuration)
        {
            var ignoredProperties = configuration.GetProperty<ICollection<string>>(IgnoredPropertiesKey);
            if (ignoredProperties == null)
            {
                ignoredProperties = new HashSet<string>();
                configuration.SetProperty(IgnoredPropertiesKey, ignoredProperties);
            }

            return ignoredProperties;
        }
    }
}