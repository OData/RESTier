// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Core
{
    /// <content>
    /// Methods in this file are mainly wrapper methods to call IServiceCollection.
    /// </content>
    public static partial class ApiBuilderExtensions
    {
        /// <summary>
        /// Adds a service instance of the specified service type.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="service">The service type.</param>
        /// <param name="instance">The service instance.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddInstance(this ApiBuilder obj, Type service, object instance)
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.AddInstance(service, instance);
            return obj;
        }

        /// <summary>
        /// Adds a service instance of service type <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="instance">An instance of <typeparamref name="TService"/>.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddInstance<TService>(this ApiBuilder obj, TService instance) where TService : class
        {
            return obj.AddInstance(typeof(TService), instance);
        }

        /// <summary>
        /// Adds a singleton service of the specified service type and implementation type.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="service">The service type.</param>
        /// <param name="implementationType">The implementation instance.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddSingleton(this ApiBuilder obj, Type service, Type implementationType)
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.AddSingleton(service, implementationType);
            return obj;
        }

        /// <summary>
        /// Adds a singleton service of the specified service type and implementation type.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="service">The service type.</param>
        /// <param name="factory">The factory method to create the service instance.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddSingleton(
            this ApiBuilder obj,
            Type service,
            Func<IServiceProvider, object> factory)
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.AddSingleton(service, factory);
            return obj;
        }

        /// <summary>
        /// Adds a singleton service of the specified service type.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="serviceType">The service type.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddSingleton(this ApiBuilder obj, Type serviceType)
        {
            return obj.AddSingleton(serviceType, serviceType);
        }

        /// <summary>
        /// Adds a singleton service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddSingleton<TService>(this ApiBuilder obj)
            where TService : class
        {
            return obj.AddSingleton(typeof(TService));
        }

        /// <summary>
        /// Adds a singleton service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="factory">The factory method to create the service instance.</param>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddSingleton<TService>(
            this ApiBuilder obj,
            Func<IServiceProvider, TService> factory)
            where TService : class
        {
            return obj.AddSingleton(typeof(TService), factory);
        }

        /// <summary>
        /// Adds a singleton service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddSingleton<TService, TImplementation>(this ApiBuilder obj)
            where TService : class
            where TImplementation : class, TService
        {
            return obj.AddSingleton(typeof(TService), typeof(TImplementation));
        }

        /// <summary>
        /// Adds a singleton service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="factory">
        /// The factory method to create an instance of <typeparamref name="TImplementation"/>.
        /// </param>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddSingleton<TService, TImplementation>(
            this ApiBuilder obj,
            Func<IServiceProvider, TImplementation> factory)
            where TService : class
            where TImplementation : class, TService
        {
            return obj.AddSingleton(typeof(TService), factory);
        }

        /// <summary>
        /// Adds a scoped service of the specified service type and implementation type.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="service">The service type.</param>
        /// <param name="implementationType">The implementation instance.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddScoped(this ApiBuilder obj, Type service, Type implementationType)
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.AddScoped(service, implementationType);
            return obj;
        }

        /// <summary>
        /// Adds a scoped service of the specified service type and implementation type.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="service">The service type.</param>
        /// <param name="factory">The factory method to create the service instance.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddScoped(
            this ApiBuilder obj,
            Type service,
            Func<IServiceProvider, object> factory)
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.AddScoped(service, factory);
            return obj;
        }

        /// <summary>
        /// Adds a scoped service of the specified service type.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="serviceType">The service type.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddScoped(this ApiBuilder obj, Type serviceType)
        {
            return obj.AddScoped(serviceType, serviceType);
        }

        /// <summary>
        /// Adds a scoped service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddScoped<TService>(this ApiBuilder obj)
            where TService : class
        {
            return obj.AddScoped(typeof(TService));
        }

        /// <summary>
        /// Adds a scoped service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="factory">The factory method to create the service instance.</param>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddScoped<TService>(
            this ApiBuilder obj,
            Func<IServiceProvider, TService> factory)
            where TService : class
        {
            return obj.AddScoped(typeof(TService), factory);
        }

        /// <summary>
        /// Adds a scoped service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddScoped<TService, TImplementation>(this ApiBuilder obj)
            where TService : class
            where TImplementation : class, TService
        {
            return obj.AddScoped(typeof(TService), typeof(TImplementation));
        }

        /// <summary>
        /// Adds a scoped service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="factory">
        /// The factory method to create an instance of <typeparamref name="TImplementation"/>.
        /// </param>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddScoped<TService, TImplementation>(
            this ApiBuilder obj,
            Func<IServiceProvider, TImplementation> factory)
            where TService : class
            where TImplementation : class, TService
        {
            return obj.AddScoped(typeof(TService), factory);
        }

        /// <summary>
        /// Adds a transient service of the specified service type and implementation type.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="service">The service type.</param>
        /// <param name="implementationType">The implementation instance.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddTransient(this ApiBuilder obj, Type service, Type implementationType)
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.AddTransient(service, implementationType);
            return obj;
        }

        /// <summary>
        /// Adds a transient service of the specified service type and implementation type.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="service">The service type.</param>
        /// <param name="factory">The factory method to create the service instance.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddTransient(
            this ApiBuilder obj,
            Type service,
            Func<IServiceProvider, object> factory)
        {
            Ensure.NotNull(obj, "obj");

            obj.Services.AddTransient(service, factory);
            return obj;
        }

        /// <summary>
        /// Adds a transient service of the specified service type.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="serviceType">The service type.</param>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddTransient(this ApiBuilder obj, Type serviceType)
        {
            return obj.AddTransient(serviceType, serviceType);
        }

        /// <summary>
        /// Adds a transient service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddTransient<TService>(this ApiBuilder obj)
            where TService : class
        {
            return obj.AddTransient(typeof(TService));
        }

        /// <summary>
        /// Adds a transient service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="factory">The factory method to create the service instance.</param>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddTransient<TService>(
            this ApiBuilder obj,
            Func<IServiceProvider, TService> factory)
            where TService : class
        {
            return obj.AddTransient(typeof(TService), factory);
        }

        /// <summary>
        /// Adds a singleton service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddTransient<TService, TImplementation>(this ApiBuilder obj)
            where TService : class
            where TImplementation : class, TService
        {
            return obj.AddTransient(typeof(TService), typeof(TImplementation));
        }

        /// <summary>
        /// Adds a transient service of type <typeparamref name="TService"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ApiBuilder"/>.</param>
        /// <param name="factory">
        /// The factory method to create an instance of <typeparamref name="TImplementation"/>.
        /// </param>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>Current <see cref="ApiBuilder"/></returns>
        public static ApiBuilder AddTransient<TService, TImplementation>(
            this ApiBuilder obj,
            Func<IServiceProvider, TImplementation> factory)
            where TService : class
            where TImplementation : class, TService
        {
            return obj.AddTransient(typeof(TService), factory);
        }
    }
}
