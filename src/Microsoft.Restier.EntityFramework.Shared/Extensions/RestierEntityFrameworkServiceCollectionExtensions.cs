// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
#if EF7
    using Microsoft.Restier.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
#else
using Microsoft.Restier.EntityFramework;
using System.Data.Entity;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core;
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
#if !EF7
        /// <summary>
        /// This method is used to add entity framework providers service into container.
        /// </summary>
        /// <typeparam name="TDbContext">The DbContext type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddEF6ProviderServices<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
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
        public static IServiceCollection AddEFProviderServices(this IServiceCollection services)
        {
            if (services.HasService<DefaultEF6ProviderServicesDetectionDummy>())
            {
                // Avoid applying multiple times to a same service collection.
                return services;
            }

            services.AddSingleton<DefaultEF6ProviderServicesDetectionDummy>()
                .AddChainedService<IModelBuilder, EFModelBuilder>()
                .AddChainedService<IModelMapper, EFModelMapper>()
                .AddChainedService<IQueryExpressionSourcer, EFQueryExpressionSourcer>()
                .AddChainedService<IQueryExecutor, EFQueryExecutor>()
                .AddChainedService<IQueryExpressionProcessor, EFQueryExpressionProcessor>()
                .AddChainedService<IChangeSetInitializer, EFChangeSetInitializer>()
                .AddChainedService<ISubmitExecutor, EFSubmitExecutor>();

            return services;
        }

        #region Private Members

        /// <summary>
        /// Dummy class to detect double registration of Default restier services inside a container.
        /// </summary>
        private sealed class DefaultEF6ProviderServicesDetectionDummy
        {

        }

        #endregion

    }

}