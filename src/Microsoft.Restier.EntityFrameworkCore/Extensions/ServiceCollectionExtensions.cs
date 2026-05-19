// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;


namespace Microsoft.Restier.EntityFrameworkCore;



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
    /// <param name="optionsAction">
    /// An optional action to configure the Microsoft.EntityFrameworkCore.DbContextOptions
    /// for the context. This provides an alternative to performing configuration of
    /// the context by overriding the Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)
    /// method in your derived context.
    /// If an action is supplied here, the Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)
    /// method will still be run if it has been overridden on the derived context. Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)
    /// configuration will be applied in addition to configuration performed here.
    /// In order for the options to be passed into your context, you need to expose a
    /// constructor on your context that takes Microsoft.EntityFrameworkCore.DbContextOptions`1
    /// and passes it to the base constructor of Microsoft.EntityFrameworkCore.DbContext.</param>
    /// <returns>Current <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddEFCoreProviderServices<TDbContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> optionsAction = null)
        where TDbContext : DbContext
    {
        Ensure.NotNull(services, nameof(services));

        services.AddDbContext<TDbContext>(optionsAction);

        return AddEFProviderServices<TDbContext>(services);
    }

    /// <summary>
    /// This method is used to add entity framework providers service into container.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="optionsAction">
    /// An optional action to configure the Microsoft.EntityFrameworkCore.DbContextOptions
    /// for the context. This provides an alternative to performing configuration of
    /// the context by overriding the Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)
    /// method in your derived context.
    /// If an action is supplied here, the Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)
    /// method will still be run if it has been overridden on the derived context. Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)
    /// configuration will be applied in addition to configuration performed here.
    /// In order for the options to be passed into your context, you need to expose a
    /// constructor on your context that takes Microsoft.EntityFrameworkCore.DbContextOptions`1
    /// and passes it to the base constructor of Microsoft.EntityFrameworkCore.DbContext.</param>
    /// <returns>Current <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddEFCoreProviderServices<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction = null)
        where TDbContext : DbContext
    {
        Ensure.NotNull(services, nameof(services));

        services.AddDbContext<TDbContext>(optionsAction);

        return AddEFProviderServices<TDbContext>(services);
    }

    /// <summary>
    /// Adds EFCore provider services with custom RESTier EF options.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="dbContextOptionsAction">An optional action to configure the
    /// <see cref="DbContextOptionsBuilder"/>. See the existing
    /// <c>AddEFCoreProviderServices</c> overloads for the contract.</param>
    /// <param name="configureOptions">An action to configure the
    /// <see cref="RestierEFOptions"/> for this API.</param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddEFCoreProviderServices<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> dbContextOptionsAction,
        Action<RestierEFOptions> configureOptions)
        where TDbContext : DbContext
    {
        Ensure.NotNull(services, nameof(services));
        Ensure.NotNull(configureOptions, nameof(configureOptions));

        var options = new RestierEFOptions();
        configureOptions(options);
        services.AddSingleton(options);

        services.AddDbContext<TDbContext>(dbContextOptionsAction);
        return AddEFProviderServices<TDbContext>(services);
    }

    /// <summary>
    /// Adds EFCore provider services with custom RESTier EF options and a service-aware DbContext options action.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="dbContextOptionsAction">An optional action to configure the
    /// <see cref="DbContextOptionsBuilder"/> with access to the
    /// <see cref="IServiceProvider"/>. See the existing
    /// <c>AddEFCoreProviderServices</c> overloads for the contract.</param>
    /// <param name="configureOptions">An action to configure the
    /// <see cref="RestierEFOptions"/> for this API.</param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddEFCoreProviderServices<TDbContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction,
        Action<RestierEFOptions> configureOptions)
        where TDbContext : DbContext
    {
        Ensure.NotNull(services, nameof(services));
        Ensure.NotNull(configureOptions, nameof(configureOptions));

        var options = new RestierEFOptions();
        configureOptions(options);
        services.AddSingleton(options);

        services.AddDbContext<TDbContext>(dbContextOptionsAction);
        return AddEFProviderServices<TDbContext>(services);
    }
}