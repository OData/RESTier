// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Restier.Core.Properties;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Contains extension methods of <see cref="ApiBuilder"/>.
    /// </summary>
    [CLSCompliant(false)]
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
            Ensure.NotNull(obj, nameof(obj));

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
            Ensure.NotNull(obj, nameof(obj));

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
            Ensure.NotNull(obj, nameof(obj));

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
            Ensure.NotNull(obj, nameof(obj));

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
            Ensure.NotNull(obj, nameof(obj));

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
            Ensure.NotNull(obj, nameof(obj));
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
            Ensure.NotNull(obj, nameof(obj));

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
            Ensure.NotNull(obj, nameof(obj));
            Ensure.NotNull(contributor, nameof(contributor));

            // Services have singleton lifetime by default, call Make... to change.
            obj.Services.TryAddSingleton(typeof(T), ChainedService<T>.DefaultFactory);
            obj.Services.AddInstance(contributor);

            return obj;
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
            Ensure.NotNull(factory, nameof(factory));
            return obj.AddContributor<T>((sp, next) => factory(sp, next()));
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
            Ensure.NotNull(factory, nameof(factory));
            return obj.AddContributor<T>((sp, next) => factory(next()));
        }

        /// <summary>
        /// Adds a service contributor, which has a chance to chain previously registered service instances.
        /// Constructor parameters other than the next service instance would be fulfilled from service provider.
        /// And this method is capable of choosing a constructor base on available services.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TImplement">The implementation type.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder ChainPrevious<TService, TImplement>(this ApiBuilder obj)
            where TService : class
            where TImplement : TService
        {
            Func<IServiceProvider, TService, TService> factory = null;

            return obj.AddContributor<TService>((sp, next) =>
            {
                if (factory != null)
                {
                    return factory(sp, next());
                }

                // TODO: See if in the future ActivatorUtilities could replace these implementation. For now its
                // methods can't select constructor appropriately, base on avaialble services.
                var ret = ChainedService.CreateViaReflection(typeof(TService), typeof(TImplement), sp, next);
                var value = ret.Item1;
                var creator = ret.Item2;

                // Create a factory proxy for the first call, to save the expression compilation cost to the 2nd
                // call, in case the service is a singleton or for any reason only resolved once.
                factory = (serviceProvider, nextService) =>
                {
                    factory = ChainedService<TService>.CreateFactory(creator);
                    creator = null;
                    return factory(serviceProvider, nextService);
                };

                return (TService)value;
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
            Ensure.NotNull(obj, nameof(obj));
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
            Ensure.NotNull(obj, nameof(obj));
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
            Ensure.NotNull(obj, nameof(obj));
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
            Ensure.NotNull(obj, nameof(obj));

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
            Ensure.NotNull(obj, nameof(obj));
            return ChainedService<T>.DefaultFactory(obj);
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

    internal static class ChainedService
    {
        public static Tuple<object, ConstructorInfo> CreateViaReflection(
            Type serviceType,
            Type implementType,
            IServiceProvider serviceProvider,
            Func<object> next)
        {
            var typeInfo = implementType.GetTypeInfo();
            var creators = typeInfo.DeclaredConstructors
                .Where(e => e.IsPublic)
                .OrderByDescending(e => e.GetParameters().Length);
            foreach (var creator in creators)
            {
                var paramInfoList = creator.GetParameters();
                var paramList = new object[paramInfoList.Length];
                for (int i = 0; i < paramInfoList.Length; i++)
                {
                    var paramInfo = paramInfoList[i];
                    if (paramInfo.ParameterType == serviceType)
                    {
                        paramList[i] = next();
                        continue;
                    }

                    paramList[i] = serviceProvider.GetService(paramInfo.ParameterType);
                    if (paramList[i] != null)
                    {
                        continue;
                    }

                    if (paramInfo.HasDefaultValue)
                    {
                        paramList[i] = paramInfo.DefaultValue;
                        continue;
                    }

                    paramList = null;
                    break;
                }

                if (paramList != null)
                {
                    return Tuple.Create(creator.Invoke(paramList), creator);
                }
            }

            throw new NotSupportedException("No appropriate constructor found.");
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

        public static Func<IServiceProvider, T, T> CreateFactory(ConstructorInfo creator)
        {
            var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider));
            var nextParam = Expression.Parameter(typeof(T));

            var paramInfoList = creator.GetParameters();
            var locals = new List<ParameterExpression>(paramInfoList.Length);
            var paramList = paramInfoList.Select(e =>
            {
                if (e.ParameterType == typeof(T))
                {
                    return (Expression)nextParam;
                }

                if (e.HasDefaultValue)
                {
                    var value = Expression.Variable(typeof(object));
                    locals.Add(value);
                    var call = Expression.Call(
                        serviceProviderParam,
                        "GetService",
                        null,
                        Expression.Constant(e.ParameterType));

                    return Expression.Condition(
                        Expression.ReferenceNotEqual(
                            Expression.Assign(value, call),
                            Expression.Constant(null)),
                        Expression.Convert(value, e.ParameterType),
                        Expression.Constant(e.DefaultValue, e.ParameterType),
                        e.ParameterType);
                }
                else
                {
                    return Expression.Call(
                        typeof(ServiceProviderExtensions),
                        "GetRequiredService",
                        new[] { e.ParameterType },
                        serviceProviderParam);
                }
            });

            Expression body = Expression.New(creator, paramList);
            if (locals.Count > 0)
            {
                body = Expression.Block(typeof(T), locals, body);
            }

            var ret = Expression.Lambda<Func<IServiceProvider, T, T>>(
                body,
                serviceProviderParam,
                nextParam);
            return ret.Compile();
        }
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
