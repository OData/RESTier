using System;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.AspNet.Model;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Core
{

    /// <summary>
    /// 
    /// </summary>
    public static class RestierApiBuilderExtensions
    {

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TApi"></typeparam>
        /// <param name="builder"></param>
        /// <returns>The <see cref="RestierApiBuilder"/> instance to allow for fluent method chaining.</returns>
        public static RestierApiBuilder AddRestierApi<TApi>(this RestierApiBuilder builder) where TApi : ApiBase
        {
            return AddRestierApi<TApi>(builder, services => { });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TApi"></typeparam>
        /// <param name="builder"></param>
        /// <param name="services"></param>
        public static RestierApiBuilder AddRestierApi<TApi>(this RestierApiBuilder builder, Action<IServiceCollection> services) where TApi : ApiBase
        {
            Ensure.NotNull(builder, nameof(builder));
            Ensure.NotNull(services, nameof(services));

            builder.Apis.Add(typeof(TApi), (serviceCollection) =>
            {

                //RWM: Add the API as the specifc API type first, then if an ApiBase instance is requested from the container,
                //     get the existing instance.
                serviceCollection
                    .AddScoped(typeof(TApi), typeof(TApi))
                    .AddScoped(sp => (ApiBase)sp.GetService(typeof(TApi)));

                serviceCollection.RemoveAll<ODataQuerySettings>()
                    .AddRestierCoreServices()
                    .AddRestierConventionBasedServices(typeof(TApi));

                services.Invoke(serviceCollection);

                serviceCollection.AddChainedService<IModelBuilder, RestierWebApiModelBuilder>();

                // The model builder must maintain a singleton life time, for holding states and being injected into
                // some other services.
                serviceCollection.AddSingleton(new RestierWebApiModelExtender(typeof(TApi)))
                    .AddChainedService<IModelBuilder, RestierWebApiModelExtender.ModelBuilder>()
                    .AddChainedService<IModelBuilder>((sp, next) => new RestierWebApiOperationModelBuilder(typeof(TApi), next))

                    .AddChainedService<IModelMapper, RestierWebApiModelExtender.ModelMapper>()
                    .AddChainedService<IQueryExpressionExpander, RestierWebApiModelExtender.QueryExpressionExpander>()
                    .AddChainedService<IQueryExpressionSourcer, RestierWebApiModelExtender.QueryExpressionSourcer>();

                serviceCollection.AddRestierDefaultServices();
            });

            return builder;
        }

        #endregion

    }

}
