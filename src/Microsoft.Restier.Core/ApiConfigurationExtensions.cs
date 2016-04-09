﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Offers a collection of extension methods to <see cref="ApiConfiguration"/>.
    /// </summary>
    public static class ApiConfigurationExtensions
    {
        private const string IgnoredPropertiesKey = "Microsoft.Restier.Core.IgnoredProperties";

        #region GetApiService<T>

        /// <summary>
        /// Gets a service instance.
        /// </summary>
        /// <param name="configuration">
        /// An API configuration.
        /// </param>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The service instance.</returns>
        public static T GetApiService<T>(this ApiConfiguration configuration) where T : class
        {
            Ensure.NotNull(configuration, "configuration");
            return configuration.ServiceProvider.GetService<T>();
        }

        #endregion

        #region PropertyBag

        /// <summary>
        /// Indicates if this object has a property.
        /// </summary>
        /// <param name="configuration">
        /// An API configuration.
        /// </param>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <returns>
        /// <c>true</c> if this object has the
        /// property; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasProperty(this ApiConfiguration configuration, string name)
        {
            return configuration.GetPropertyBag().HasProperty(name);
        }

        /// <summary>
        /// Gets a property.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the property.
        /// </typeparam>
        /// <param name="configuration">
        /// An API configuration.
        /// </param>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <returns>
        /// The value of the property.
        /// </returns>
        public static T GetProperty<T>(this ApiConfiguration configuration, string name)
        {
            return configuration.GetPropertyBag().GetProperty<T>(name);
        }

        /// <summary>
        /// Gets a property.
        /// </summary>
        /// <param name="configuration">
        /// An API configuration.
        /// </param>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <returns>
        /// The value of the property.
        /// </returns>
        public static object GetProperty(this ApiConfiguration configuration, string name)
        {
            return configuration.GetPropertyBag().GetProperty(name);
        }

        /// <summary>
        /// Sets a property.
        /// </summary>
        /// <param name="configuration">
        /// An API configuration.
        /// </param>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <param name="value">
        /// A value for the property.
        /// </param>
        public static void SetProperty(this ApiConfiguration configuration, string name, object value)
        {
            configuration.GetPropertyBag().SetProperty(name, value);
        }

        /// <summary>
        /// Clears a property.
        /// </summary>
        /// <param name="configuration">
        /// An API configuration.
        /// </param>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        public static void ClearProperty(this ApiConfiguration configuration, string name)
        {
            configuration.GetPropertyBag().ClearProperty(name);
        }

        #endregion

        [CLSCompliant(false)]
        public static ApiContext CreateContextWithin(this ApiConfiguration obj, IServiceScope scope)
        {
            return obj.GetApiService<IApiContextFactory>().CreateWithin(scope);
        }

        /// <summary>
        /// Creates an <see cref="ApiContext"/> configured by current <see cref="ApiConfiguration"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiConfiguration"/>.</param>
        /// <returns>An <see cref="ApiContext"/>.</returns>
        public static ApiContext CreateContext(this ApiConfiguration obj)
        {
            return obj.CreateContextWithin(obj.GetApiService<IServiceScopeFactory>().CreateScope());
        }

        #region IgnoreProperty

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

            configuration.GetIgnoredPropertiesImplementation().Add(propertyName);
            return configuration;
        }

        #endregion

        #region IgnoreProperty Internal

        internal static bool IsPropertyIgnored(this ApiConfiguration configuration, string propertyName)
        {
            Ensure.NotNull(configuration, "configuration");

            return configuration.GetIgnoredPropertiesImplementation().Contains(propertyName);
        }

        #endregion

        #region IgnoreProperty Private

        private static ICollection<string> GetIgnoredPropertiesImplementation(this ApiConfiguration configuration)
        {
            var ignoredProperties = configuration.GetProperty<ICollection<string>>(IgnoredPropertiesKey);
            if (ignoredProperties == null)
            {
                ignoredProperties = new HashSet<string>();
                configuration.SetProperty(IgnoredPropertiesKey, ignoredProperties);
            }

            return ignoredProperties;
        }

        #endregion

        #region PropertyBag Private

        private static PropertyBag GetPropertyBag(this ApiConfiguration configuration)
        {
            Ensure.NotNull(configuration, "configuration");
            return configuration.GetApiService<PropertyBag>();
        }

        #endregion
    }
}