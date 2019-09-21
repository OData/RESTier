using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;

namespace Microsoft.Extensions.DependencyInjection
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
            services.AddService<IModelBuilder>((sp, next) => new StoreModelProducer(StoreModel.Model))
                .AddService<IModelMapper>((sp, next) => new StoreModelMapper())
                .AddService<IQueryExpressionSourcer>((sp, next) => new StoreQueryExpressionSourcer())
                .AddService<IChangeSetInitializer>((sp, next) => new StoreChangeSetInitializer())
                .AddService<ISubmitExecutor>((sp, next) => new DefaultSubmitExecutor());
            return services;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTestDefaultServices(this IServiceCollection services)
        {
            services.AddService<IChangeSetInitializer>((sp, next) => new DefaultChangeSetInitializer())
                .AddService<ISubmitExecutor>((sp, next) => new DefaultSubmitExecutor());
            return services;
        }

    }

}