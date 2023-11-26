// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Microsoft.Restier.Core.Model;
using DIServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;
using ODataServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// The default Dependency Injection container builder for Restier.
    /// </summary>
    public class RestierContainerBuilder : IContainerBuilder
    {

        #region Private Members

        /// <summary>
        /// The <see cref="RestierApiBuilder"/> instance to use for this Container.
        /// </summary>
        internal RestierApiBuilder apiBuilder;

        /// <summary>
        /// 
        /// </summary>
        internal readonly Action<RestierApiBuilder> configureApis;

        /// <summary>
        /// The <see cref="RestierRouteBuilder"/> instance to use for this Container.
        /// </summary>
        internal RestierRouteBuilder routeBuilder;

        #endregion

        #region Properties

        internal string RouteName { get; set; } = "RestierDefault";

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
            Services = new();
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
        /// Builds a container which implements <see cref="IServiceProvider"/> and contains all the services registered for a specific route.
        /// </summary>
        /// <returns>The <see cref="IServiceProvider">dependency injection container</see> for the registered services.</returns>
        /// <remarks>
        /// RWM: For unit test scenarios, this container may be built without any APIs opr Routes. If you are experiencing unexpected behavior, 
        /// turn on Tracing so you can see the warning messages Restier might be generating.
        /// </remarks>
        public virtual IServiceProvider BuildContainer()
        {
            configureApis?.Invoke(apiBuilder);

            Type apiType = null;

            if (routeBuilder.Routes.Any())
            {
                if (routeBuilder.Routes.ContainsKey(RouteName))
                {
                    var route = routeBuilder.Routes[RouteName];
                    var apiServiceActions = apiBuilder.Apis[route.ApiType];
                    apiType = route.ApiType;
                    apiServiceActions.Invoke(Services);
                }
                else
                {
                    Trace.TraceWarning($"Restier: The requested Route {RouteName}, which is not registered. Please check your configuration and try again.");
                }
            }
            else
            {
                Trace.TraceWarning("Restier was registered without mapping any Routes. Please see the documentation for adding a Route to the 'config.MapRestier()' call.");
            }

            //RWM: We might not have had any Routes registered, so if there are any APIs, then grab the first one and run it.
            if (apiBuilder.Apis.Any())
            {
                //RWM: If we already have an API type, then skip this.
                if (apiType is null)
                {
                    var apiRecord = apiBuilder.Apis.FirstOrDefault();
                    apiType = apiRecord.Key;
                    apiRecord.Value.Invoke(Services);
                }
            }
            else
            {
                Trace.TraceWarning("Restier was registered without adding any Apis. Please see the documentation for adding an Api to the 'config.UseRestier()' call.");
            }

            //RWM: Warn the user they need to specify Routes if they registered more than one API.
            if (apiBuilder.Apis.Count != routeBuilder.Routes.Count)
            {
                Trace.TraceWarning($"Restier detected at API mismatch. There are {routeBuilder.Routes.Count} routes registered but {apiBuilder.Apis.Count} Apis registered. Please double-check your configuration.");
            }

            //RWM: It's entirely possible that this container was used some other way. 
            if (apiType is not null)
            {
                Services.AddSingleton(sp =>
                {
                    var api = sp.GetService<ApiBase>();
                    if (api is null)
                    {
                        throw new Exception($"Could not find the API. Please make sure you registered the API using the new 'UseRestier(services => services.AddRestierApi<{apiType.Name}>());' syntax.");
                    }

                    if (sp.GetService(typeof(IModelBuilder)) is not IModelBuilder modelBuilder)
                    {
                        throw new InvalidOperationException(Resources.ModelBuilderNotRegistered);
                    }

                    var buildContext = new ModelContext(api);
                    return modelBuilder.GetModel(buildContext);
                });

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