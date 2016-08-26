// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core.Query;
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
    internal delegate T ApiServiceContributor<T>(IServiceProvider serviceProvider, Func<T> next) where T : class;

    /// <summary>
    /// Contains extension methods of <see cref="IServiceCollection"/>.
    /// </summary>
    [CLSCompliant(false)]
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Return true if the <see cref="IServiceCollection"/> has any <typeparamref name="TService"/> service
        /// registered.
        /// </summary>
        /// <typeparam name="TService">The API service type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>
        /// True if the service is registered.
        /// </returns>
        public static bool HasService<TService>(this IServiceCollection services) where TService : class
        {
            Ensure.NotNull(services, "services");

            return services.Any(sd => sd.ServiceType == typeof(TService));
        }

        /// <summary>
        /// Adds a service contributor, which has a chance to chain previously registered service instances.
        /// If want to cutoff previous registration, not define a property with type of TService or do not use it.
        /// The first TService in function is the service of inner, and the second TService is the service returned.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="factory">
        /// A factory method to create a new instance of service TService, wrapping previous instance."/>.
        /// </param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddService<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, TService, TService> factory)
            where TService : class
        {
            Ensure.NotNull(services, "services");
            Ensure.NotNull(factory, "factory");
            return services.AddContributorNoCheck<TService>((sp, next) => factory(sp, next()));
        }

        /// <summary>
        /// Adds a service contributor, which has a chance to chain previously registered service instances.
        /// If want to cutoff previous registration, not define a property with type of TService or do not use it.
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
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddService<TService, TImplement>(this IServiceCollection services)
            where TService : class
            where TImplement : class, TService
        {
            Ensure.NotNull(services, "services");

            Func<IServiceProvider, Func<TService>, TService> factory = null;

            services.TryAddTransient<TImplement>();
            return services.AddContributorNoCheck<TService>((sp, next) =>
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
                    //     var service = sp.GetRequiredService<TImplement>();
                    //     service.next = next();
                    //     return service;
                    // }
                    var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider));
                    var nextParam = Expression.Parameter(typeof(Func<TService>));

                    var value = Expression.Variable(typeof(TImplement));
                    var getService = Expression.Call(
                        typeof(ServiceProviderServiceExtensions),
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
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection MakeSingleton<TService>(this IServiceCollection services)
            where TService : class
        {
            Ensure.NotNull(services, "services");
            services.AddSingleton<TService>(ChainedService<TService>.DefaultFactory);
            return services;
        }

        /// <summary>
        /// Call this to make scoped lifetime of a service.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection MakeScoped<TService>(this IServiceCollection services) where TService : class
        {
            Ensure.NotNull(services, "services");
            services.AddScoped<TService>(ChainedService<TService>.DefaultFactory);
            return services;
        }

        /// <summary>
        /// Call this to make transient lifetime of a service.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection MakeTransient<TService>(this IServiceCollection services)
            where TService : class
        {
            Ensure.NotNull(services, "services");
            services.AddTransient<TService>(ChainedService<TService>.DefaultFactory);
            return services;
        }

        /// <summary>
        /// Add core services.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> containing API service registrations.
        /// </param>
        /// <param name="apiType">
        /// The type of a class on which code-based conventions are used.
        /// </param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddCoreServices(this IServiceCollection services, Type apiType)
        {
            Ensure.NotNull(apiType, "apiType");

            services.AddScoped(apiType, apiType)
                .AddScoped(typeof(ApiBase), apiType);

            services.TryAddSingleton<ApiConfiguration>();

            return services.AddService<IQueryExecutor, DefaultQueryExecutor>()
                            .AddScoped<PropertyBag>();
        }

        /// <summary>
        /// Enables code-based conventions for an API.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/> containing API service registrations.
        /// </param>
        /// <param name="apiType">
        /// The type of a class on which code-based conventions are used.
        /// </param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddConventionBasedServices(this IServiceCollection services, Type apiType)
        {
            Ensure.NotNull(apiType, "apiType");

            ConventionBasedChangeSetItemAuthorizer.ApplyTo(services, apiType);
            ConventionBasedChangeSetItemFilter.ApplyTo(services, apiType);
            services.AddService<IChangeSetItemValidator, ConventionBasedChangeSetItemValidator>();
            ConventionBasedQueryExpressionProcessor.ApplyTo(services, apiType);
            ConventionBasedOperationAuthorizer.ApplyTo(services, apiType);
            ConventionBasedOperationFilter.ApplyTo(services, apiType);
            return services;
        }

        private static IServiceCollection AddContributorNoCheck<TService>(
            this IServiceCollection services,
            ApiServiceContributor<TService> contributor)
            where TService : class
        {
            // Services have singleton lifetime by default, call Make... to change.
            services.TryAddSingleton(typeof(TService), ChainedService<TService>.DefaultFactory);
            services.AddSingleton(contributor);

            return services;
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
    }

    internal static class ChainedService<TService> where TService : class
    {
        public static readonly Func<IServiceProvider, TService> DefaultFactory = sp =>
        {
            var instances = sp.GetServices<ApiServiceContributor<TService>>().Reverse();

            using (var e = instances.GetEnumerator())
            {
                Func<TService> next = null;
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
