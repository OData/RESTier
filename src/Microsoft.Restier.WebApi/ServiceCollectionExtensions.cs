using System;
using System.Web.OData.Formatter.Deserialization;
using System.Web.OData.Formatter.Serialization;
using System.Web.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.WebApi.Formatter.Deserialization;
using Microsoft.Restier.WebApi.Formatter.Serialization;
using Microsoft.Restier.WebApi.Model;
using Microsoft.Restier.WebApi.Query;

namespace Microsoft.Restier.WebApi
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebApiServices<T>(this IServiceCollection services)
        {
            if (services.HasService<RestierQueryExecutor>())
            {
                // Avoid applying multiple times to a same service collection.
                return services;
            }

            RestierModelExtender.ApplyTo(services, typeof(T));
            RestierOperationModelBuilder.ApplyTo(services, typeof(T));

            // Add OData Query Settings and valiadtion settings
            Func<IServiceProvider, ODataQuerySettings> querySettingFactory = (sp) => new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False,
                PageSize = null,  // no support for server enforced PageSize, yet
            };

            services.TryAddSingleton(typeof(ODataQuerySettings), querySettingFactory);
            services.TryAddSingleton<ODataValidationSettings>();

            // Make serializer and deserializer provider as DI services
            services.TryAddSingleton<ODataSerializerProvider, DefaultRestierSerializerProvider>();
            services.TryAddSingleton<ODataDeserializerProvider, DefaultRestierDeserializerProvider>();

            return
                services.AddScoped<RestierQueryExecutorOptions>()
                    .ChainPrevious<IQueryExecutor, RestierQueryExecutor>();
        }
    }
}
