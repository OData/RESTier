using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.WebApi.Model;
using Microsoft.Restier.WebApi.Query;

namespace Microsoft.Restier.WebApi
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebApiServices<T>(this IServiceCollection services)
        {
            RestierModelExtender.ApplyTo(services, typeof(T));
            RestierOperationModelBuilder.ApplyTo(services, typeof(T));
            return
                services.AddScoped<RestierQueryExecutorOptions>()
                    .ChainPrevious<IQueryExecutor, RestierQueryExecutor>();
        }
    }
}
