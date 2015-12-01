// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Properties;
using Microsoft.Restier.Core.Query;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Extensions;

namespace Microsoft.Restier.Core
{
    public delegate T HookHandlerContributor<T>(IServiceProvider sp, Func<T> next) where T : IHookHandler;

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

        [CLSCompliant(false)]
        public ApiConfiguration(IServiceCollection services)
        {
            if (services == null)
            {
                services = new ServiceCollection();
            }

            this.services = services;
            this.AddDefaultHookHandlers();
        }

        public IServiceProvider ServiceProvider
        {
            get { return serviceProvider; }
        }

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
        /// Override this to use your favorite DI container, as long as it has an IServiceProvider wrapper.
        /// </summary>
        /// <returns></returns>
        protected virtual IServiceProvider BuildServiceProvider()
        {
            return Services.BuildServiceProvider();
        }

        /// <summary>
        /// Ensures this API configuration has been committed.
        /// </summary>
        public void EnsureCommitted()
        {
            if (this.IsCommitted) return;

            this.serviceProvider = BuildServiceProvider();
            this.services = null;

            this.IsCommitted = true;
        }

        private class LegacyHookHandler<T> where T : class, IHookHandler
        {
            public LegacyHookHandler(T instance)
            {
                Instance = instance;
            }

            public T Instance { get; private set; }
        }

        private static class HookHandlerType<T> where T : class, IHookHandler
        {
            public readonly static Func<IServiceProvider, T> DefaultFactory = sp =>
            {
                var instances = sp.GetServices<HookHandlerContributor<T>>().Reverse();

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

            public static T BuildLegacyHandlers(IServiceProvider sp)
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
                        if (delegateHandler == null) break;

                        delegateHandler.InnerHandler = current = e.Current.Instance;
                    }

                    return first;
                }
            }
        };

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
                this.services.AddInstance<HookHandlerContributor<T>>((sp, next) =>
                {
                    return cached ?? (cached = HookHandlerType<T>.BuildLegacyHandlers(sp));
                });

                // Hook handlers have singleton lifetime by default, call Make... to change.
                this.services.TryAddSingleton(typeof(T), HookHandlerType<T>.DefaultFactory);
            }

            this.services.AddInstance(new LegacyHookHandler<T>(handler));

            return this;
        }

        /// <summary>
        /// Adds a hook handler contributor, which has a chance to chain previously registered hook handler instances.
        /// </summary>
        /// <typeparam name="T">The hook handler interface.</typeparam>
        /// <param name="contributor">An instance of <see cref="HookHandlerContributor{T}"/>.</param>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public ApiConfiguration AddHookHandler<T>(HookHandlerContributor<T> contributor) where T : class, IHookHandler
        {
            Ensure.NotNull(contributor, nameof(contributor));

            if (this.IsCommitted)
            {
                throw new InvalidOperationException(Resources.ApiConfigurationIsCommitted);
            }

            if (!typeof(T).IsInterface)
            {
                throw new InvalidOperationException(Resources.ShouldBeInterfaceType);
            }

            // Hook handlers have singleton lifetime by default, call Make... to change.
            this.services.TryAddSingleton(typeof(T), HookHandlerType<T>.DefaultFactory);
            this.services.AddInstance(contributor);

            return this;
        }

        /// <summary>
        /// Call this to make singleton lifetime of a hook handler.
        /// </summary>
        /// <typeparam name="T">The hook handler interface.</typeparam>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public ApiConfiguration MakeSingleton<T>() where T : class, IHookHandler
        {
            this.services.AddSingleton<T>(HookHandlerType<T>.DefaultFactory);
            return this;
        }

        /// <summary>
        /// Call this to make scoped lifetime of a hook handler.
        /// </summary>
        /// <typeparam name="T">The hook handler interface.</typeparam>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public ApiConfiguration MakeScoped<T>() where T : class, IHookHandler
        {
            this.services.AddScoped<T>(HookHandlerType<T>.DefaultFactory);
            return this;
        }

        /// <summary>
        /// Call this to make transient lifetime of a hook handler.
        /// </summary>
        /// <typeparam name="T">The hook handler interface.</typeparam>
        /// <returns>Current <see cref="ApiConfiguration"/></returns>
        public ApiConfiguration MakeTransient<T>() where T : class, IHookHandler
        {
            this.services.AddTransient<T>(HookHandlerType<T>.DefaultFactory);
            return this;
        }

        /// <summary>
        /// Gets a hook handler instance.
        /// </summary>
        /// <typeparam name="T">The hook handler interface.</typeparam>
        /// <returns>The hook handler instance.</returns>
        public T GetHookHandler<T>() where T : class, IHookHandler
        {
            return this.serviceProvider.GetService<T>();
        }
        #endregion

        private void AddDefaultHookHandlers()
        {
            this.AddHookHandler<IQueryExecutor>(DefaultQueryExecutor.Instance);
        }
    }
}
