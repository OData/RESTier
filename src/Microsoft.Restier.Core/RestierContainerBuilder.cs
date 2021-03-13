// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Routing;
using DIServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;
using ODataServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// The default container builder implementation based on the Microsoft dependency injection framework.
    /// </summary>
    public class RestierContainerBuilder : IContainerBuilder
    {

        #region Private Members

        private readonly Action<IServiceCollection> configureAction;

        internal RestierRouteBuilder RouteBuilder;

        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        internal ServiceCollection Services { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierContainerBuilder" /> class.
        /// </summary>
        /// <param name="configureAction">Action to register services post OData service registration.</param>
        public RestierContainerBuilder(Action<IServiceCollection> configureAction = null)
        {
            this.configureAction = configureAction;
            Services = new ServiceCollection();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a service of <paramref name="serviceType"/> with an <paramref name="implementationType"/>.
        /// </summary>
        /// <param name="lifetime">The lifetime of the service to register.</param>
        /// <param name="serviceType">The type of the service to register.</param>
        /// <param name="implementationType">The implementation type of the service.</param>
        /// <returns>The <see cref="IContainerBuilder"/> instance itself.</returns>
        public virtual IContainerBuilder AddService(ODataServiceLifetime lifetime, Type serviceType, Type implementationType)
        {
            Ensure.NotNull(serviceType, nameof(serviceType));
            Ensure.NotNull(implementationType, nameof(implementationType));

            Services.Add(new ServiceDescriptor(serviceType, implementationType, TranslateServiceLifetime(lifetime)));
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
            Ensure.NotNull(serviceType, nameof(serviceType));
            Ensure.NotNull(implementationFactory, nameof(implementationFactory));

            Services.Add(new ServiceDescriptor(serviceType, implementationFactory, TranslateServiceLifetime(lifetime)));
            return this;
        }

        /// <summary>
        /// Builds a container which implements <see cref="IServiceProvider"/> and contains
        /// all the services registered.
        /// </summary>
        /// <returns>The container built by this builder.</returns>
        public virtual IServiceProvider BuildContainer()
        {
            configureAction?.Invoke(Services);
            if (RouteBuilder != null)
            {
                var routes = RouteBuilder.ToRestierApiRouteDictionary(Services.BuildServiceProvider());
                foreach (var route in routes)
                {
                    AddService(ODataServiceLifetime.Scoped, typeof(IEdmModel), sp => route.Value.Model);
                }
                AddService(ODataServiceLifetime.Singleton, typeof(RestierApiRouteDictionary), sp => routes);
            }

            return Services.BuildServiceProvider();
        }

        #endregion

        #region Private Methods

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

        #endregion
    }
}