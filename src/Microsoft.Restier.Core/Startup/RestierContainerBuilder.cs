// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using DIServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;
using ODataServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace Microsoft.Restier.Core.Startup
{
    /// <summary>
    /// The default container builder implementation based on the Microsoft dependency injection framework.
    /// </summary>
    public class RestierContainerBuilder : IContainerBuilder
    {

        #region Private Members

        internal readonly Action<RestierApiBuilder> configureApis;

        /// <summary>
        /// The Builder instance used to map 
        /// </summary>
        internal RestierRouteBuilder routeBuilder;

        internal RestierApiBuilder apiBuilder;

        //internal List<Action<IServiceCollection>> preRegistrationActions;

        //internal List<Action<IServiceCollection>> postRegistrationActions;

        #endregion

        #region Properties

        internal string RouteName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal ServiceCollection Services { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierContainerBuilder" /> class.
        /// </summary>
        /// <param name="configureApis">Action to configure the <see cref="ApiBase"/> registrations that are available to the Container.</param>
        /// <remarks>
        /// The API registrations are re-created every time because new Containers are spun up per-route. It make make more sense to create a static 
        /// instance to do this, so the Dictionary is only created once.
        /// </remarks>
        public RestierContainerBuilder(Action<RestierApiBuilder> configureApis = null)
        {
            this.configureApis = configureApis;
            //preRegistrationActions = new();
            //postRegistrationActions = new();
            Services = new ServiceCollection();
            apiBuilder = new();
            routeBuilder = new();
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
        public IContainerBuilder AddService(ODataServiceLifetime lifetime, Type serviceType, Type implementationType)
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
            configureApis?.Invoke(apiBuilder);

            if (!apiBuilder.Apis.Any())
            {
                throw new Exception("Restier was registered without adding any Apis. Please see the documentation for adding an Api to the 'config.UseRestier()' call.");
            }

            if (!routeBuilder.Routes.Any())
            {
                throw new Exception("Restier was registered without mapping any Routes. Please see the documentation for adding a Route to the 'config.MapRestier()' call.");
            }

            var route = routeBuilder.Routes[RouteName];
            var apiServiceActions = apiBuilder.Apis[route.ApiType];

            apiServiceActions.Invoke(Services);


            //RWM: Build temp container, build model, add to container.



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