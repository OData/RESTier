﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core.Properties;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Contains extension methods of <see cref="ApiBuilder"/>.
    /// </summary>
    public static class ApiBuilderExtensions
    {
        /// <summary>
        /// Make the built <see cref="ApiConfiguration"/> to create <see cref="ApiContext"/> with its own instance
        /// of <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder UseSharedApiScope(this ApiBuilder obj)
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.AddSingleton<IApiScopeFactory>(SharedApiScopeFactory.Creator);
            return obj;
        }

        /// <summary>
        /// Make the built <see cref="ApiConfiguration"/> to create <see cref="ApiContext"/> with a scoped
        /// <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder UseContextApiScope(this ApiBuilder obj)
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.AddSingleton<IApiScopeFactory>(ContextApiScopeFactory.Creator);
            return obj;
        }

        /// <summary>
        /// If service scope is not yet configured, make the built <see cref="ApiConfiguration"/> to create
        /// <see cref="ApiContext"/> with its own instance of <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder TryUseSharedApiScope(this ApiBuilder obj)
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.TryAddSingleton(typeof(IApiScopeFactory), SharedApiScopeFactory.Creator);
            return obj;
        }

        /// <summary>
        /// If service scope is not yet configured, make the built <see cref="ApiConfiguration"/> to create
        /// <see cref="ApiContext"/> with a scoped <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder TryUseContextApiScope(this ApiBuilder obj)
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.TryAddSingleton(typeof(IApiScopeFactory), ContextApiScopeFactory.Creator);
            return obj;
        }

        /// <summary>
        /// Return true if the <see cref="ApiBuilder"/> has any <typeparamref name="T"/> service registered.
        /// </summary>
        /// <typeparam name="T">The hook handler interface.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <returns>
        /// True if the hook handler is registered.
        /// </returns>
        public static bool HasHookHandler<T>(this ApiBuilder obj) where T : class, IHookHandler
        {
            Ensure.NotNull(obj, "obj");

            return obj.Services.Any(sd => sd.ServiceType == typeof(LegacyHookHandler<T>));
        }

        /// <summary>
        /// Adds a hook handler instance.
        /// </summary>
        /// <typeparam name="T">The hook handler interface.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="handler">An instance of hook handler for <typeparamref name="T"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddHookHandler<T>(this ApiBuilder obj, T handler) where T : class, IHookHandler
        {
            Ensure.NotNull(obj, "obj");
            Ensure.NotNull(handler, "handler");

            if (!typeof(T).IsInterface)
            {
                throw new InvalidOperationException(Resources.ShouldBeInterfaceType);
            }

            // Since legacy hook handlers are registered with instance, they must have singleton lifetime.
            // And so a singleton HookHandlerContributor is registered for each hook handler type, and it
            // will cache the legacy handler chain once built.
            if (!obj.HasHookHandler<T>())
            {
                T cached = null;
                obj.Services.AddInstance<ApiServiceContributor<T>>((sp, next) =>
                {
                    return cached ?? (cached = HookHandlerType<T>.BuildLegacyHandlers(sp, next));
                });

                // Hook handlers have singleton lifetime by default, call Make... to change.
                obj.Services.TryAddSingleton(typeof(T), ChainedService<T>.DefaultFactory);
            }

            obj.Services.AddInstance(new LegacyHookHandler<T>(handler));
            return obj;
        }

        /// <summary>
        /// Adds a service contributor, which has a chance to chain previously registered service instances.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="instance">An instance of <typeparamref name="T"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddInstance<T>(this ApiBuilder obj, T instance) where T : class
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.AddInstance<T>(instance);
            return obj;
        }

        /// <summary>
        /// Adds a service contributor, which has a chance to chain previously registered service instances.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="contributor">An instance of <see cref="ApiServiceContributor{T}"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddContributor<T>(this ApiBuilder obj, ApiServiceContributor<T> contributor)
            where T : class
        {
            Ensure.NotNull(obj, "obj");
            Ensure.NotNull(contributor, "contributor");

            return obj.AddContributorNoCheck<T>(contributor);
        }

        /// <summary>
        /// Adds a service contributor, which has a chance to chain previously registered service instances.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="factory">
        /// A factory method to create a new instance of service T, wrapping previous instance."/>.
        /// </param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder ChainPrevious<T>(this ApiBuilder obj, Func<IServiceProvider, T, T> factory)
            where T : class
        {
            Ensure.NotNull(obj, "obj");
            Ensure.NotNull(factory, "factory");
            return obj.AddContributorNoCheck<T>((sp, next) => factory(sp, next()));
        }

        /// <summary>
        /// Adds a service contributor, which has a chance to chain previously registered service instances.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="factory">
        /// A factory method to create a new instance of service T, wrapping previous instance."/>.
        /// </param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder ChainPrevious<T>(this ApiBuilder obj, Func<T, T> factory) where T : class
        {
            Ensure.NotNull(obj, "obj");
            Ensure.NotNull(factory, "factory");
            return obj.AddContributorNoCheck<T>((sp, next) => factory(next()));
        }

        /// <summary>
        /// Adds a service contributor, which has a chance to chain previously registered service instances.
        /// The contributor added will get an instance of <typeparamref name="TImplement"/> from the container, i.e.
        /// <see cref="IServiceProvider"/>, every time it's get called.
        /// This method will try to register <typeparamref name="TImplement"/> as a service with
        /// <see cref="ServiceLifetime.Transient"/> life time, if it's not yet registered. To override, you can
        /// register <typeparamref name="TImplement"/> before or after calling this method.
        /// </summary>
        /// <remarks>
        /// Note: When registering <typeparamref name="TImplement"/>, you must NOT give it a
        /// <see cref="ServiceLifetime"/> that makes it outlives <typeparamref name="TService"/>, that could possibly
        /// make an instance of <typeparamref name="TImplement"/> be used in multiple instantiations of
        /// <typeparamref name="TService"/>, which leads to unpredictable behaviors.
        /// </remarks>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TImplement">The implementation type.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder ChainPrevious<TService, TImplement>(this ApiBuilder obj)
            where TService : class
            where TImplement : class, TService
        {
            Ensure.NotNull(obj, "obj");

            Func<IServiceProvider, Func<TService>, TService> factory = null;

            obj.Services.TryAddTransient<TImplement>();
            return obj.AddContributorNoCheck<TService>((sp, next) =>
            {
                if (factory != null)
                {
                    return factory(sp, next);
                }

                var instance = sp.GetService<TImplement>();
                if (instance == null)
                {
                    return instance;
                }

                var nextProperty = typeof(TImplement).GetTypeInfo().GetProperties()
                    .FirstOrDefault(e => e.SetMethod.IsPublic && e.PropertyType == typeof(TService));
                if (nextProperty == null)
                {
                    factory = (serviceProvider, _) => serviceProvider.GetRequiredService<TImplement>();
                    return instance;
                }

                nextProperty.SetValue(instance, next());
                factory = (serviceProvider, getNext) =>
                {
                    var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider));
                    var nextParam = Expression.Parameter(typeof(Func<TService>));

                    var value = Expression.Variable(typeof(TImplement));
                    var getService = Expression.Call(
                        typeof(ServiceProviderExtensions),
                        "GetRequiredService",
                        new[] { typeof(TImplement) },
                        serviceProviderParam);
                    var inject = Expression.Assign(
                        Expression.MakeMemberAccess(value, nextProperty),
                        Expression.Invoke(nextParam));

                    var block = Expression.Block(
                        typeof(TService),
                        new[] { value },
                        Expression.Assign(value, getService),
                        inject,
                        value);

                    factory = LambdaExpression.Lambda<Func<IServiceProvider, Func<TService>, TService>>(
                        block,
                        serviceProviderParam,
                        nextParam).Compile();
                    nextProperty = null;
                    return factory(serviceProvider, getNext);
                };

                return instance;
            });
        }

        /// <summary>
        /// Call this to make singleton lifetime of a service.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder MakeSingleton<T>(this ApiBuilder obj) where T : class
        {
            Ensure.NotNull(obj, "obj");
            obj.Services.AddSingleton<T>(ChainedService<T>.DefaultFactory);
            return obj;
        }

        /// <summary>
        /// Call this to make scoped lifetime of a service.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder MakeScoped<T>(this ApiBuilder obj) where T : class
        {
            Ensure.NotNull(obj, "obj");
            obj.Services.AddScoped<T>(ChainedService<T>.DefaultFactory);
            return obj;
        }

        /// <summary>
        /// Call this to make transient lifetime of a service.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder MakeTransient<T>(this ApiBuilder obj) where T : class
        {
            Ensure.NotNull(obj, "obj");
            obj.Services.AddTransient<T>(ChainedService<T>.DefaultFactory);
            return obj;
        }

        /// <summary>
        /// Build the <see cref="ApiConfiguration"/>
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <returns>The built <see cref="ApiConfiguration"/></returns>
        public static ApiConfiguration Build(this ApiBuilder obj)
        {
            return obj.Build(null);
        }

        /// <summary>
        /// Build the <see cref="ApiConfiguration"/>
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="serviceProviderFactory">
        /// An optional factory to create an <see cref="IServiceProvider"/>.
        /// Use this to inject your favorite DI container.
        /// </param>
        /// <returns>The built <see cref="ApiConfiguration"/></returns>
        public static ApiConfiguration Build(
            this ApiBuilder obj,
            Func<ApiBuilder, IServiceProvider> serviceProviderFactory)
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.TryAddSingleton<ApiConfiguration>();
            obj.TryUseContextApiScope();

            var serviceProvider = serviceProviderFactory != null ?
                serviceProviderFactory(obj) : obj.Services.BuildServiceProvider();
            return serviceProvider.GetService<ApiConfiguration>();
        }

        /// <summary>
        /// Call this to build a service chain explicitly.
        /// Typically you just resolve the service with <see cref="IServiceProvider.GetService(Type)"/>, but
        /// in case you register your own factory of <typeparamref name="T"/>, you may need this to get the
        /// service chain registered with <see cref="ApiBuilder"/>.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <returns>The service instance built.</returns>
        /// <example>
        /// <code>
        /// services.AddScoped &lt; ISomeService &gt;(sp =>
        ///     new SomeService(sp.BuildApiServiceChain &lt; ISomeService &gt;()));
        /// </code>
        /// </example>
        public static T BuildApiServiceChain<T>(this IServiceProvider obj) where T : class
        {
            Ensure.NotNull(obj, "obj");
            return ChainedService<T>.DefaultFactory(obj);
        }

        private static ApiBuilder AddContributorNoCheck<T>(this ApiBuilder obj, ApiServiceContributor<T> contributor)
            where T : class
        {
            // Services have singleton lifetime by default, call Make... to change.
            obj.Services.TryAddSingleton(typeof(T), ChainedService<T>.DefaultFactory);
            obj.Services.AddInstance(contributor);

            return obj;
        }

        private class SharedApiScopeFactory : IApiScopeFactory
        {
            public static readonly Func<IServiceProvider, IApiScopeFactory> Creator =
                serviceProvider => new SharedApiScopeFactory(serviceProvider);

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
            public static readonly Func<IServiceProvider, IApiScopeFactory> Creator =
                serviceProvider => new ContextApiScopeFactory(
                    serviceProvider.GetRequiredService<IServiceScopeFactory>());

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

    internal static class ChainedService<T> where T : class
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

    internal static class HookHandlerType<T> where T : class, IHookHandler
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

    internal class LegacyHookHandler<T> where T : class, IHookHandler
    {
        public LegacyHookHandler(T instance)
        {
            Instance = instance;
        }

        public T Instance { get; private set; }
    }
}
