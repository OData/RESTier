// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Restier.EntityFramework;
using System.Data.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;


namespace Microsoft.Restier.EntityFramework;



/// <summary>
/// Contains extension methods of <see cref="IServiceCollection"/>.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// This method is used to add entity framework providers service into container.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>Current <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddEF6ProviderServices<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        Ensure.NotNull(services, nameof(services));

        services.TryAddScoped(sp =>
        {
            var dbContext = Activator.CreateInstance<TDbContext>();
            dbContext.Configuration.ProxyCreationEnabled = false;
            return dbContext;
        });

        return AddEFProviderServices<TDbContext>(services);
    }

    /// <summary>
    /// This method is used to add entity framework providers service into container with an explicit connection string.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="connectionString">The connection string to use for the DbContext.</param>
    /// <returns>Current <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddEF6ProviderServices<TDbContext>(this IServiceCollection services, string connectionString)
        where TDbContext : DbContext
    {
        Ensure.NotNull(services, nameof(services));
        Ensure.NotNull(connectionString, nameof(connectionString));

        services.TryAddScoped(sp =>
        {
            var dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext), connectionString);
            dbContext.Configuration.ProxyCreationEnabled = false;
            return dbContext;
        });

        return AddEFProviderServices<TDbContext>(services);
    }

    /// <summary>
    /// Adds EF6 provider services with custom RESTier EF options.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configureOptions">An action to configure the
    /// <see cref="RestierEFOptions"/> for this API.</param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddEF6ProviderServices<TDbContext>(
        this IServiceCollection services,
        Action<RestierEFOptions> configureOptions)
        where TDbContext : DbContext
    {
        Ensure.NotNull(services, nameof(services));
        Ensure.NotNull(configureOptions, nameof(configureOptions));

        var options = new RestierEFOptions();
        configureOptions(options);
        services.AddSingleton(options);

        services.TryAddScoped(sp =>
        {
            var dbContext = Activator.CreateInstance<TDbContext>();
            dbContext.Configuration.ProxyCreationEnabled = false;
            return dbContext;
        });

        return AddEFProviderServices<TDbContext>(services);
    }

    /// <summary>
    /// Adds EF6 provider services with an explicit connection string and custom RESTier EF options.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="connectionString">The connection string to use for the DbContext.</param>
    /// <param name="configureOptions">An action to configure the
    /// <see cref="RestierEFOptions"/> for this API.</param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddEF6ProviderServices<TDbContext>(
        this IServiceCollection services,
        string connectionString,
        Action<RestierEFOptions> configureOptions)
        where TDbContext : DbContext
    {
        Ensure.NotNull(services, nameof(services));
        Ensure.NotNull(connectionString, nameof(connectionString));
        Ensure.NotNull(configureOptions, nameof(configureOptions));

        var options = new RestierEFOptions();
        configureOptions(options);
        services.AddSingleton(options);

        services.TryAddScoped(sp =>
        {
            var dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext), connectionString);
            dbContext.Configuration.ProxyCreationEnabled = false;
            return dbContext;
        });

        return AddEFProviderServices<TDbContext>(services);
    }
}