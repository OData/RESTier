﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using ServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// The default container builder implementation based on the Microsoft dependency injection framework.
    /// </summary>
    public class RestierContainerBuilder : IContainerBuilder
    {
        private readonly IServiceCollection services = new ServiceCollection();
        private Type apiType;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierContainerBuilder" /> class.
        /// </summary>
        /// <param name="apiType">The Api Type</param>
        public RestierContainerBuilder(Type apiType)
        {
            this.apiType = apiType;
        }

        /// <summary>
        /// Adds a service of <paramref name="serviceType"/> with an <paramref name="implementationType"/>.
        /// </summary>
        /// <param name="lifetime">The lifetime of the service to register.</param>
        /// <param name="serviceType">The type of the service to register.</param>
        /// <param name="implementationType">The implementation type of the service.</param>
        /// <returns>The <see cref="IContainerBuilder"/> instance itself.</returns>
        public virtual IContainerBuilder AddService(
            ServiceLifetime lifetime,
            Type serviceType,
            Type implementationType)
        {
            if (serviceType == null)
            {
                throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture, Resources.ArgumentCanNotBeNull, "serviceType"));
            }

            if (implementationType == null)
            {
                throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture, Resources.ArgumentCanNotBeNull, "implementationType"));
            }

            services.Add(new ServiceDescriptor(
                serviceType, implementationType, TranslateServiceLifetime(lifetime)));

            return this;
        }

        /// <summary>
        /// Adds a service of <paramref name="serviceType"/> with an <paramref name="implementationFactory"/>.
        /// </summary>
        /// <param name="lifetime">The lifetime of the service to register.</param>
        /// <param name="serviceType">The type of the service to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>The <see cref="IContainerBuilder"/> instance itself.</returns>
        public IContainerBuilder AddService(
            ServiceLifetime lifetime,
            Type serviceType,
            Func<IServiceProvider, object> implementationFactory)
        {
            if (serviceType == null)
            {
                throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture, Resources.ArgumentCanNotBeNull, "serviceType"));
            }

            if (implementationFactory == null)
            {
                throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture, Resources.ArgumentCanNotBeNull, "implementationFactory"));
            }

            services.Add(new ServiceDescriptor(
                serviceType, implementationFactory, TranslateServiceLifetime(lifetime)));

            return this;
        }

        /// <summary>
        /// Builds a container which implements <see cref="IServiceProvider"/> and contains
        /// all the services registered.
        /// </summary>
        /// <returns>The container built by this builder.</returns>
        public virtual IServiceProvider BuildContainer()
        {
            AddRestierService();
            return services.BuildServiceProvider();
        }

        internal IContainerBuilder AddRestierService()
        {
            Func<IServiceProvider, IEdmModel> modelFactory = sp =>
            {
                var api = sp.GetService<ApiBase>();
                var model = api.GetModelAsync(default(CancellationToken)).Result;
                return model;
            };

            // Configure the API via reflection call
            var methodDeclaredType = apiType;

            MethodInfo method = null;
            while (method == null && methodDeclaredType != null)
            {
                // In case the subclass does not override the method, call super class method
                method = methodDeclaredType.GetMethod("ConfigureApi");
                methodDeclaredType = methodDeclaredType.BaseType;
            }

            var parameters = new object[]
            {
                apiType, services
            };

            method.Invoke(null, parameters);

            services.AddSingleton(modelFactory);
            return this;
        }

        private static Microsoft.Extensions.DependencyInjection.ServiceLifetime TranslateServiceLifetime(
            ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Scoped:
                    return Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped;
                case ServiceLifetime.Singleton:
                    return Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton;
                default:
                    return Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient;
            }
        }
    }
}