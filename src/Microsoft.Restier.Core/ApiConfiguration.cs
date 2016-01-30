// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Properties;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// A delegate which participate in service creation.
    /// All registered contributors form a chain, and the last registered will be called first.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="serviceProvider">
    /// The <see cref="IServiceProvider"/> to which this contributor call is registered.
    /// </param>
    /// <param name="next">
    /// Return the result of the previous contributor on the chain.
    /// </param>
    /// <returns>A service instance of <typeparamref name="T"/>.</returns>
    public delegate T ApiServiceContributor<T>(IServiceProvider serviceProvider, Func<T> next) where T : class;

    /// <summary>
    /// Represents a configuration that defines an API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An API configuration defines the model and behavior of an API
    /// through a set of registered hook points. It also maintains a set of
    /// properties that can be used to share static data between hook points.
    /// </para>
    /// <para>
    /// Hook points may be singletons, meaning there is at most one instance of
    /// the hook point registered, or multi-cast, in which case there can be
    /// zero or more instances of the hook point that are registered. In the
    /// multi-cast case, registration order is maintained, and such hook points
    /// are normally used in the original or reverse order of registration.
    /// </para>
    /// <para>
    /// In order to use an API configuration, it must first be committed.
    /// This fixes the configuration so that its set of hook points are
    /// immutable, ensuring that any active use of the configuration sees a
    /// consistent set of hook points throughout a particular API flow.
    /// </para>
    /// </remarks>
    public class ApiConfiguration : PropertyBag
    {
        private IServiceCollection services;

        private IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiConfiguration" /> class.
        /// </summary>
        public ApiConfiguration() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiConfiguration" /> class.
        /// </summary>
        /// <param name="services">A service collection containing service registrations.</param>
        [CLSCompliant(false)]
        public ApiConfiguration(IServiceCollection services)
        {
            if (services == null)
            {
                services = new ServiceCollection();
            }

            services.AddInstance<ApiConfiguration>(this);
            this.services = services;
            this.TryUseSharedApiScope();

            this.AddDefaultHookHandlers();
        }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> which contains all services of this
        /// <see cref="ApiConfiguration"/>.
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get { return serviceProvider; }
        }

        /// <summary>
        /// Gets the service collection containing service registrations with which to build
        /// <see cref="ServiceProvider"/>.
        /// </summary>
        [CLSCompliant(false)]
        public IServiceCollection Services
        {
            get { return services; }
        }

        /// <summary>
        /// Gets a value indicating whether this API configuration has been committed.
        /// </summary>
        public bool IsCommitted { get; private set; }

        internal IEdmModel Model { get; set; }

        /// <summary>
        /// Ensures this API configuration has been committed.
        /// </summary>
        public void EnsureCommitted()
        {
            if (this.IsCommitted)
            {
                return;
            }

            this.serviceProvider = BuildServiceProvider();
            this.services = null;

            this.IsCommitted = true;
        }

        #region HookHandler
        /// <summary>
        /// Adds a hook handler instance.
        /// </summary>
        /// <typeparam name="T">The hook handler interface.</typeparam>
        /// <param name="handler">An instance of hook handler for TContext.</param>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public ApiConfiguration AddHookHandler<T>(T handler) where T : class, IHookHandler
        {
            Ensure.NotNull(handler, "handler");

            if (this.IsCommitted)
            {
                throw new InvalidOperationException(Resources.ApiConfigurationIsCommitted);
            }

            if (!typeof(T).IsInterface)
            {
                throw new InvalidOperationException(Resources.ShouldBeInterfaceType);
            }

            // Since legacy hook handlers are registered with instance, they must have singleton lifetime.
            // And so a singleton HookHandlerContributor is registered for each hook handler type, and it
            // will cache the legacy handler chain once built.
            if (!this.services.Any(sd => sd.ServiceType == typeof(LegacyHookHandler<T>)))
            {
                T cached = null;
                this.services.AddInstance<ApiServiceContributor<T>>((sp, next) =>
                {
                    return cached ?? (cached = HookHandlerType<T>.BuildLegacyHandlers(sp, next));
                });

                // Hook handlers have singleton lifetime by default, call Make... to change.
                this.services.TryAddSingleton(typeof(T), ChainedService<T>.DefaultFactory);
            }

            this.services.AddInstance(new LegacyHookHandler<T>(handler));

            return this;
        }

        /// <summary>
        /// Adds a service contributor, which has a chance to chain previously registered service instances.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="contributor">An instance of <see cref="ApiServiceContributor{T}"/>.</param>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public ApiConfiguration AddContributor<T>(ApiServiceContributor<T> contributor) where T : class
        {
            Ensure.NotNull(contributor, nameof(contributor));

            if (this.IsCommitted)
            {
                throw new InvalidOperationException(Resources.ApiConfigurationIsCommitted);
            }

            // Services have singleton lifetime by default, call Make... to change.
            this.services.TryAddSingleton(typeof(T), ChainedService<T>.DefaultFactory);
            this.services.AddInstance(contributor);

            return this;
        }

        /// <summary>
        /// Adds a service contributor, which has a chance to chain previously registered service instances.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="factory">
        /// A factory method to create a new instance of service T, wrapping previous instance."/>.
        /// </param>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public ApiConfiguration ChainPrevious<T>(Func<IServiceProvider, T, T> factory)
            where T : class
        {
            Ensure.NotNull(factory, nameof(factory));
            return AddContributor<T>((sp, next) => factory(sp, next()));
        }

        /// <summary>
        /// Adds a service contributor, which has a chance to chain previously registered service instances.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="factory">
        /// A factory method to create a new instance of service T, wrapping previous instance."/>.
        /// </param>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public ApiConfiguration ChainPrevious<T>(Func<T, T> factory) where T : class
        {
            Ensure.NotNull(factory, nameof(factory));
            return AddContributor<T>((sp, next) => factory(next()));
        }

        /// <summary>
        /// Call this to make singleton lifetime of a service.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public ApiConfiguration MakeSingleton<T>() where T : class
        {
            this.services.AddSingleton<T>(ChainedService<T>.DefaultFactory);
            return this;
        }

        /// <summary>
        /// Call this to make scoped lifetime of a service.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public ApiConfiguration MakeScoped<T>() where T : class
        {
            this.services.AddScoped<T>(ChainedService<T>.DefaultFactory);
            return this;
        }

        /// <summary>
        /// Call this to make transient lifetime of a service.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public ApiConfiguration MakeTransient<T>() where T : class
        {
            this.services.AddTransient<T>(ChainedService<T>.DefaultFactory);
            return this;
        }

        /// <summary>
        /// Gets a service instance.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The service instance.</returns>
        public T GetHookHandler<T>() where T : class
        {
            return this.serviceProvider.GetService<T>();
        }
        #endregion

        /// <summary>
        /// Override this to use your favorite DI container, as long as it has an IServiceProvider wrapper.
        /// </summary>
        /// <returns>The built <see cref="IServiceProvider"/>.</returns>
        protected virtual IServiceProvider BuildServiceProvider()
        {
            return Services.BuildServiceProvider();
        }

        private void AddDefaultHookHandlers()
        {
            this.AddHookHandler<IQueryExecutor>(DefaultQueryExecutor.Instance);
        }

        private static class ChainedService<T> where T : class
        {
            public static readonly Func<IServiceProvider, T> DefaultFactory = sp =>
            {
                var instances = sp.GetServices<ApiServiceContributor<T>>().Reverse();

                using (var e = instances.GetEnumerator())
                {
                    Func<T> next = null;
                    next = () =>
                    {
                        if (e.MoveNext())
                        {
                            return e.Current(sp, next);
                        }

                        return null;
                    };

                    return next();
                }
            };
        }

        private static class HookHandlerType<T> where T : class, IHookHandler
        {
            public static T BuildLegacyHandlers(IServiceProvider sp, Func<T> next)
            {
                var instances = sp.GetServices<LegacyHookHandler<T>>().Reverse();

                using (var e = instances.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        return null;
                    }

                    T first = e.Current.Instance;
                    T current = first;
                    while (e.MoveNext())
                    {
                        var delegateHandler = current as IDelegateHookHandler<T>;
                        if (delegateHandler == null)
                        {
                            return first;
                        }

                        delegateHandler.InnerHandler = current = e.Current.Instance;
                    }

                    var finalDelegate = current as IDelegateHookHandler<T>;
                    if (finalDelegate != null)
                    {
                        finalDelegate.InnerHandler = next();
                    }

                    return first;
                }
            }
        }

        private class LegacyHookHandler<T> where T : class, IHookHandler
        {
            public LegacyHookHandler(T instance)
            {
                Instance = instance;
            }

            public T Instance { get; private set; }
        }
    }
}
