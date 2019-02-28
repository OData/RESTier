// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData;
using Microsoft.OData.Edm;
using DIServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;
using ODataServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// The default container builder implementation based on the Microsoft dependency injection framework.
    /// </summary>
    public class RestierContainerBuilder : IContainerBuilder
    {
        private readonly IServiceCollection services = new ServiceCollection();
        private readonly Type apiType;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierContainerBuilder" /> class.
        /// </summary>
        /// <param name="apiType">The Api Type</param>
        public RestierContainerBuilder(Type apiType) => this.apiType = apiType;

        /// <summary>
        /// Adds a service of <paramref name="serviceType"/> with an <paramref name="implementationType"/>.
        /// </summary>
        /// <param name="lifetime">The lifetime of the service to register.</param>
        /// <param name="serviceType">The type of the service to register.</param>
        /// <param name="implementationType">The implementation type of the service.</param>
        /// <returns>The <see cref="IContainerBuilder"/> instance itself.</returns>
        public virtual IContainerBuilder AddService(ODataServiceLifetime lifetime, Type serviceType, Type implementationType)
        {
            if (serviceType == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.ArgumentCanNotBeNull, "serviceType"));
            }

            if (implementationType == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.ArgumentCanNotBeNull, "implementationType"));
            }

            services.Add(new ServiceDescriptor(serviceType, implementationType, TranslateServiceLifetime(lifetime)));

            return this;
        }

        /// <summary>
        /// Adds a service of <paramref name="serviceType"/> with an <paramref name="implementationFactory"/>.
        /// </summary>
        /// <param name="lifetime">The lifetime of the service to register.</param>
        /// <param name="serviceType">The type of the service to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>The <see cref="IContainerBuilder"/> instance itself.</returns>
        public IContainerBuilder AddService(ODataServiceLifetime lifetime, Type serviceType, Func<IServiceProvider, object> implementationFactory)
        {
            if (serviceType == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.ArgumentCanNotBeNull, "serviceType"));
            }

            if (implementationFactory == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.ArgumentCanNotBeNull, "implementationFactory"));
            }

            services.Add(new ServiceDescriptor(serviceType, implementationFactory, TranslateServiceLifetime(lifetime)));

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
            IEdmModel modelFactory(IServiceProvider sp)
            {
                var api = sp.GetService<ApiBase>();
                var model = api.GetModelAsync(default).GetAwaiter().GetResult();
                return model;
            }

            // Configure the API via reflection call
            var methodDeclaredType = apiType;

            MethodInfo method = null;
            while (method == null && methodDeclaredType != null)
            {
                // In case the subclass does not override the method, call super class method
                method = methodDeclaredType.GetMethod("ConfigureApi");
                methodDeclaredType = methodDeclaredType.BaseType;
            }

            method.Invoke(null, new object[]
            {
                apiType, services
            });

            services.AddSingleton(modelFactory);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        private static DIServiceLifetime TranslateServiceLifetime(ODataServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ODataServiceLifetime.Scoped:
                    return DIServiceLifetime.Scoped;
                case ODataServiceLifetime.Singleton:
                    return DIServiceLifetime.Singleton;
                default:
                    return DIServiceLifetime.Transient;
            }
        }
    }
}