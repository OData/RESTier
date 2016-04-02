// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core.Properties;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Conventions;
using Microsoft.Restier.Core.Submit;

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
    /// Contains extension methods of <see cref="IServiceCollection"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Return true if the <see cref="IServiceCollection"/> has any <typeparamref name="T"/> service registered.
        /// </summary>
        /// <typeparam name="T">The API service type.</typeparam>
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
        /// <returns>
        /// True if the service is registered.
        /// </returns>
        public static bool HasService<T>(this IServiceCollection obj) where T : class
        {
            Ensure.NotNull(obj, "obj");

            return obj.Any(sd => sd.ServiceType == typeof(T));
        }

        /// <summary>
        /// Adds an API service instance, ignore all previously registered service instances of type
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The API service type.</typeparam>
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
        /// <param name="handler">An instance of type <typeparamref name="T"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection CutoffPrevious<T>(this IServiceCollection obj, T handler) where T : class
        {
            Ensure.NotNull(obj, "obj");
            Ensure.NotNull(handler, "handler");

            return obj.AddContributorNoCheck<T>((sp, next) => handler);
        }

        /// <summary>
        /// Adds an API service instance, ignore all previously registered service instances of type
        /// <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The API service type.</typeparam>
        /// <typeparam name="TImplement">The API service implementation type.</typeparam>
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection CutoffPrevious<TService, TImplement>(this IServiceCollection obj)
            where TService : class
            where TImplement : class, TService
        {
            Ensure.NotNull(obj, "obj");

            obj.TryAddTransient<TImplement>();
            return obj.AddContributorNoCheck<TService>((sp, next) => sp.GetRequiredService<TImplement>());
        }

        /// <summary>
        /// Adds a service contributor, which has a chance to chain previously registered service instances.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
        /// <param name="contributor">An instance of <see cref="ApiServiceContributor{T}"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddContributor<T>(
            this IServiceCollection obj,
            ApiServiceContributor<T> contributor)
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
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
        /// <param name="factory">
        /// A factory method to create a new instance of service T, wrapping previous instance."/>.
        /// </param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection ChainPrevious<T>(
            this IServiceCollection obj,
            Func<IServiceProvider, T, T> factory)
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
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
        /// <param name="factory">
        /// A factory method to create a new instance of service T, wrapping previous instance."/>.
        /// </param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection ChainPrevious<T>(
            this IServiceCollection obj,
            Func<T, T> factory) where T : class
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
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection ChainPrevious<TService, TImplement>(this IServiceCollection obj)
            where TService : class
            where TImplement : class, TService
        {
            Ensure.NotNull(obj, "obj");

            Func<IServiceProvider, Func<TService>, TService> factory = null;

            obj.TryAddTransient<TImplement>();
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

                var innerMember = FindInnerMemberAndInject(instance, next);
                if (innerMember == null)
                {
                    factory = (serviceProvider, _) => serviceProvider.GetRequiredService<TImplement>();
                    return instance;
                }

                factory = (serviceProvider, getNext) =>
                {
                    // To build a lambda expression like:
                    // (sp, next) =>
                    // {
                    //     var hook = sp.GetRequiredService<TImplement>();
                    //     hook.next = next();
                    //     return hook;
                    // }
                    var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider));
                    var nextParam = Expression.Parameter(typeof(Func<TService>));

                    var value = Expression.Variable(typeof(TImplement));
                    var getService = Expression.Call(
                        typeof(ServiceProviderExtensions),
                        "GetRequiredService",
                        new[] { typeof(TImplement) },
                        serviceProviderParam);
                    var inject = Expression.Assign(
                        Expression.MakeMemberAccess(value, innerMember),
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
                    innerMember = null;
                    return factory(serviceProvider, getNext);
                };

                return instance;
            });
        }

        /// <summary>
        /// Call this to make singleton lifetime of a service.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection MakeSingleton<T>(this IServiceCollection obj) where T : class
        {
            Ensure.NotNull(obj, "obj");
            obj.AddSingleton<T>(ChainedService<T>.DefaultFactory);
            return obj;
        }

        /// <summary>
        /// Call this to make scoped lifetime of a service.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection MakeScoped<T>(this IServiceCollection obj) where T : class
        {
            Ensure.NotNull(obj, "obj");
            obj.AddScoped<T>(ChainedService<T>.DefaultFactory);
            return obj;
        }

        /// <summary>
        /// Call this to make transient lifetime of a service.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection MakeTransient<T>(this IServiceCollection obj) where T : class
        {
            Ensure.NotNull(obj, "obj");
            obj.AddTransient<T>(ChainedService<T>.DefaultFactory);
            return obj;
        }

        /// <summary>
        /// Build the <see cref="ApiConfiguration"/>
        /// </summary>
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The built <see cref="ApiConfiguration"/></returns>
        public static ApiConfiguration BuildApiConfiguration(this IServiceCollection obj)
        {
            return obj.BuildApiConfiguration(null);
        }

        /// <summary>
        /// Build the <see cref="ApiConfiguration"/>
        /// </summary>
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceProviderFactory">
        /// An optional factory to create an <see cref="IServiceProvider"/>.
        /// Use this to inject your favorite DI container.
        /// </param>
        /// <returns>The built <see cref="ApiConfiguration"/></returns>
        public static ApiConfiguration BuildApiConfiguration(
            this IServiceCollection obj,
            Func<IServiceCollection, IServiceProvider> serviceProviderFactory)
        {
            Ensure.NotNull(obj, "obj");

            obj.TryAddSingleton<ApiConfiguration>();

            var serviceProvider = serviceProviderFactory != null ?
                serviceProviderFactory(obj) : obj.BuildServiceProvider();
            var configuration = serviceProvider.GetService<ApiConfiguration>();

            foreach (var e in serviceProvider.GetServices<IApiInitializer>())
            {
                e.Initialize(configuration);
            }

            return configuration;
        }

        public static IServiceCollection DefaultInnerMost(this IServiceCollection obj)
        {
            return obj.CutoffPrevious<IApiContextFactory, ApiContextFactory>()
                .CutoffPrevious<IQueryExecutor>(DefaultQueryExecutor.Instance);
        }

        public static IServiceCollection DefaultOuterMost(this IServiceCollection obj)
        {
            obj.ChainPrevious<IApiContextFactory, ApiContextInitializer>();
            if (!obj.HasService<ApiContext>())
            {
                obj.AddScoped<ContextHolder>()
                    .AddScoped(sp => sp.GetService<ContextHolder>().Context);
            }

            return obj;
        }

        public static IServiceCollection UseAttributes(this IServiceCollection obj, Type apiType)
        {
            Ensure.NotNull(apiType, "type");

            if (apiType.BaseType != null)
            {
                obj = obj.UseAttributes(apiType.BaseType);
            }

            var attributes = apiType.GetCustomAttributes(
                typeof(IApiConfigurator), false);
            if (attributes.Length == 0)
            {
                return obj;
            }

            foreach (IApiConfigurator e in attributes)
            {
                e.Configure(obj, apiType);
            }

            return obj;
        }

        public static IServiceCollection UseAttributes<T>(this IServiceCollection obj)
            where T : class
        {
            return obj.UseAttributes(typeof(T));
        }

        public static IServiceCollection UseConventions(this IServiceCollection obj, Type apiType)
        {
            Ensure.NotNull(apiType, "apiType");

            ConventionBasedChangeSetAuthorizer.ApplyTo(obj, apiType);
            ConventionBasedChangeSetEntryFilter.ApplyTo(obj, apiType);
            obj.CutoffPrevious<IChangeSetEntryValidator, ConventionBasedChangeSetEntryValidator>();
            ConventionBasedApiModelBuilder.ApplyTo(obj, apiType);
            ConventionBasedOperationProvider.ApplyTo(obj, apiType);
            ConventionBasedEntitySetFilter.ApplyTo(obj, apiType);
            return obj;
        }

        public static IServiceCollection UseConventions<T>(this IServiceCollection obj)
            where T : class
        {
            return obj.UseConventions(typeof(T));
        }

        public static IServiceCollection Apply(
            this IServiceCollection obj,
            Action<IServiceCollection> configurationCall)
        {
            configurationCall(obj);
            return obj;
        }

        public static IServiceCollection Apply(
            this IServiceCollection obj,
            ApiConfiguration.Anchor anchor)
        {
            return obj.Apply(anchor.Configuration);
        }

        /// <summary>
        /// Call this to build a service chain explicitly.
        /// Typically you just resolve the service with <see cref="IServiceProvider.GetService(Type)"/>, but
        /// in case you register your own factory of <typeparamref name="T"/>, you may need this to get the
        /// service chain registered with <see cref="IServiceCollection"/>.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <param name="obj">The <see cref="IServiceCollection"/>.</param>
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

        private static IServiceCollection AddContributorNoCheck<T>(
            this IServiceCollection obj,
            ApiServiceContributor<T> contributor)
            where T : class
        {
            // Services have singleton lifetime by default, call Make... to change.
            obj.TryAddSingleton(typeof(T), ChainedService<T>.DefaultFactory);
            obj.AddInstance(contributor);

            return obj;
        }

        private static MemberInfo FindInnerMemberAndInject<TService, TImplement>(
            TImplement instance,
            Func<TService> next)
        {
            var typeInfo = typeof(TImplement).GetTypeInfo();
            var nextProperty = typeInfo
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(e => e.SetMethod != null && e.PropertyType == typeof(TService));
            if (nextProperty != null)
            {
                nextProperty.SetValue(instance, next());
                return nextProperty;
            }

            var nextField = typeInfo
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(e => e.FieldType == typeof(TService));
            if (nextField != null)
            {
                nextField.SetValue(instance, next());
                return nextField;
            }

            return null;
        }

        private class ApiContextFactory : IApiContextFactory
        {
            public ApiContext CreateWithin(IServiceScope scope)
            {
                var context = new ApiContext(scope);
                var holder = context.GetApiService<ContextHolder>();
                if (holder != null)
                {
                    holder.Context = context;
                }

                return context;
            }
        }

        private class ApiContextInitializer : IApiContextFactory
        {
            public IApiContextFactory Inner { get; set; }

            public ApiContext CreateWithin(IServiceScope scope)
            {
                var context = Inner.CreateWithin(scope);
                foreach (var e in context.GetApiServices<IApiContextConfigurator>())
                {
                    e.Initialize(context);
                }

                return context;
            }
        }

        private class ContextHolder
        {
            public ApiContext Context { get; set; }
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
}
