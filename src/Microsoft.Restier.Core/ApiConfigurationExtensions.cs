// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Extensions;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Offers a collection of extension methods to <see cref="ApiConfiguration"/>.
    /// </summary>
    public static class ApiConfigurationExtensions
    {
        private const string IgnoredPropertiesKey = "Microsoft.Restier.Core.IgnoredProperties";

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

        /// <summary>
        /// Make the built <see cref="ApiConfiguration"/> to create <see cref="ApiContext"/> with its own instance
        /// of <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiConfiguration"/>.</param>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public static ApiConfiguration UseSharedApiScope(this ApiConfiguration obj)
        {
            obj.Services.AddSingleton<IApiScopeFactory, SharedApiScopeFactory>();
            return obj;
        }

        /// <summary>
        /// Make the built <see cref="ApiConfiguration"/> to create <see cref="ApiContext"/> with a scoped
        /// <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiConfiguration"/>.</param>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public static ApiConfiguration UseContextApiScope(this ApiConfiguration obj)
        {
            obj.Services.AddSingleton<IApiScopeFactory, ContextApiScopeFactory>();
            return obj;
        }

        /// <summary>
        /// If service scope is not yet configured, make the built <see cref="ApiConfiguration"/> to create
        /// <see cref="ApiContext"/> with its own instance of <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiConfiguration"/>.</param>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public static ApiConfiguration TryUseSharedApiScope(this ApiConfiguration obj)
        {
            obj.Services.TryAddSingleton<IApiScopeFactory, SharedApiScopeFactory>();
            return obj;
        }

        /// <summary>
        /// If service scope is not yet configured, make the built <see cref="ApiConfiguration"/> to create
        /// <see cref="ApiContext"/> with a scoped <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiConfiguration"/>.</param>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public static ApiConfiguration TryUseContextApiScope(this ApiConfiguration obj)
        {
            obj.Services.TryAddSingleton<IApiScopeFactory, ContextApiScopeFactory>();
            return obj;
        }

        internal static bool IsPropertyIgnored(this ApiConfiguration configuration, string propertyName)
        {
            Ensure.NotNull(configuration, "configuration");

            return configuration.GetIgnoredPropertiesImplementation().Contains(propertyName);
        }

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

        private class SharedApiScopeFactory : IApiScopeFactory
        {
            public SharedApiScopeFactory(IServiceProvider serviceProvider)
            {
                this.ServiceProvider = serviceProvider;
            }

            public IServiceProvider ServiceProvider
            {
                get; private set;
            }

            public IServiceScope CreateApiScope()
            {
                return new ServiceScope()
                {
                    ServiceProvider = this.ServiceProvider,
                };
            }

            private class ServiceScope : IServiceScope
            {
                public IServiceProvider ServiceProvider
                {
                    get; set;
                }

                public void Dispose()
                {
                    this.ServiceProvider = null;
                }
            }
        }

        private class ContextApiScopeFactory : IApiScopeFactory
        {
            public ContextApiScopeFactory(IServiceScopeFactory factory)
            {
                this.Factory = factory;
            }

            public IServiceScopeFactory Factory
            {
                get; private set;
            }

            public IServiceScope CreateApiScope()
            {
                return this.Factory.CreateScope();
            }
        }
    }
}