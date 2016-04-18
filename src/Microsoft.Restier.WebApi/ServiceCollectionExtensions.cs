using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.WebApi.Model;
using Microsoft.Restier.WebApi.Query;

namespace Microsoft.Restier.WebApi
{
    [CLSCompliant(false)]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebApiServices<T>(this IServiceCollection services)
        {
            ApiModelBuilder.ApplyTo(services, typeof(T));
            OperationModelBuilder.ApplyTo(services, typeof(T));
            return
                services.AddScoped<RestierQueryExecutorOptions>()
                    .ChainPrevious<IQueryExecutor, RestierQueryExecutor>();
        }
    }
}
