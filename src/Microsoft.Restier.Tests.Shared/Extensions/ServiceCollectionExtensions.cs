using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.AspNet;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// 
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
            services.AddService<IModelBuilder>((sp, next) => new TestModelProducer(StoreModel.Model))
                .AddService<IModelMapper>((sp, next) => new TestModelMapper())
                .AddService<IQueryExpressionSourcer>((sp, next) => new TestQueryExpressionSourcer())
                .AddService<IChangeSetInitializer>((sp, next) => new TestChangeSetInitializer())
                .AddService<ISubmitExecutor>((sp, next) => new TestSubmitExecutor());
            return services;
        }

    }

}