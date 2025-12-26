// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
#if EFCore
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
#else
using Microsoft.Restier.EntityFramework;
using System.Data.Entity;
#endif
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;


namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Contains extension methods of <see cref="IServiceCollection"/>.
    /// </summary>
    public static class RestierEntityFrameworkServiceCollectionExtensions
    {
#if EFCore
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

            return AddEFProviderServices(services);
        }

        /* JHC: not sure why we had this overload, the simpler builder should work file
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

            return AddEFProviderServices(services);
        }
        */
#else
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

            return AddEFProviderServices(services);
        }
#endif

        /// <summary>
        /// This method is used to add entity framework providers service into container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/>.</returns>
        internal static IServiceCollection AddEFProviderServices(this IServiceCollection services)
        {
            if (services.HasService<DefaultEFProviderServicesDetectionDummy>())
            {
                // Avoid applying multiple times to a same service collection.
                return services;
            }

            services.AddSingleton<DefaultEFProviderServicesDetectionDummy>()
                .AddChainedService<IModelBuilder, EFModelBuilder>()
                .AddChainedService<IModelMapper, EFModelMapper>()
                .AddChainedService<IQueryExpressionSourcer, EFQueryExpressionSourcer>()
                .AddChainedService<IQueryExecutor, EFQueryExecutor>()
                .AddChainedService<IQueryExpressionProcessor, EFQueryExpressionProcessor>()
                .AddChainedService<IChangeSetInitializer, EFChangeSetInitializer>()
                .AddChainedService<ISubmitExecutor, EFSubmitExecutor>();

            return services;
        }
    }
}