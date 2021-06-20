// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Tests.Shared
{

    /// <summary>
    /// Extensions to <see cref="IServiceCollection"/> that make registering services for Restier Tests easier.
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        /// <summary>
        /// Adds the required <see cref="StoreApi"/> services to an <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTestStoreApiServices(this IServiceCollection services)
        {
            services
                .AddChainedService<IModelBuilder>((sp, next) => new StoreModelProducer(StoreModel.Model))
                .AddChainedService<IModelMapper>((sp, next) => new StoreModelMapper())
                .AddChainedService<IQueryExpressionSourcer>((sp, next) => new StoreQueryExpressionSourcer())
                .AddChainedService<IChangeSetInitializer>((sp, next) => new StoreChangeSetInitializer())
                .AddChainedService<ISubmitExecutor>((sp, next) => new DefaultSubmitExecutor());
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTestDefaultServices(this IServiceCollection services)
        {
            services
                .AddChainedService<IChangeSetInitializer>((sp, next) => new DefaultChangeSetInitializer())
                .AddChainedService<ISubmitExecutor>((sp, next) => new DefaultSubmitExecutor());
            return services;
        }

    }

}