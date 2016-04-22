using System;
using System.Web.OData.Query;
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

            // Add OData Query Settings and valiadtion settings
            Func<IServiceProvider, ODataQuerySettings> querySettingFactory = (sp) => new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False,
                PageSize = null,  // no support for server enforced PageSize, yet
            };

            services.AddSingleton<ODataQuerySettings>(querySettingFactory);
            services.AddSingleton<ODataValidationSettings>();

            return
                services.AddScoped<RestierQueryExecutorOptions>()
                    .ChainPrevious<IQueryExecutor, RestierQueryExecutor>();
        }
    }
}
