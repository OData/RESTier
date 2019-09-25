// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Data.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFramework;

namespace Microsoft.Restier.EntityFramework
{

    /// <summary>
    /// Contains extension methods of <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// This method is used to add entity framework providers service into container.
        /// </summary>
        /// <typeparam name="TDbContext">The DbContext type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddEF6ProviderServices<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            services.TryAddScoped<DbContext>(sp =>
            {
                var dbContext = Activator.CreateInstance<TDbContext>();
                dbContext.Configuration.ProxyCreationEnabled = false;
                return dbContext;
            });

            return services
                .AddChainedService<IModelBuilder, EFModelProducer>()
                .AddChainedService<IModelMapper>((sp, next) => new EFModelMapper(typeof(TDbContext)))
                .AddChainedService<IQueryExpressionSourcer, EFQueryExpressionSourcer>()
                .AddChainedService<IQueryExecutor, EFQueryExecutor>()
                .AddChainedService<IQueryExpressionProcessor, EFQueryExpressionProcessor>()
                .AddChainedService<IChangeSetInitializer, EFChangeSetInitializer>()
                .AddChainedService<ISubmitExecutor, EFSubmitExecutor>();
        }

    }

}