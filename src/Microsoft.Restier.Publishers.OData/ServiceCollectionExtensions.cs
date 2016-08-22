// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.OData.Formatter.Deserialization;
using System.Web.OData.Formatter.Serialization;
using System.Web.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Publishers.OData.Formatter;
using Microsoft.Restier.Publishers.OData.Model;
using Microsoft.Restier.Publishers.OData.Operation;
using Microsoft.Restier.Publishers.OData.Query;

namespace Microsoft.Restier.Publishers.OData
{
     /// <summary>
     /// Contains extension methods of <see cref="IServiceCollection"/>.
     /// This method is used to add odata publisher service into container.
     /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// This method is used to add odata publisher service into container.
        /// </summary>
        /// <typeparam name="T">The Api type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        [CLSCompliant(false)]
        public static IServiceCollection AddODataServices<T>(this IServiceCollection services)
        {
            if (services.HasService<RestierQueryExecutor>())
            {
                // Avoid applying multiple times to a same service collection.
                return services;
            }

            services.AddService<IModelBuilder, RestierModelBuilder>();
            RestierModelExtender.ApplyTo(services, typeof(T));
            RestierOperationModelBuilder.ApplyTo(services, typeof(T));

            // Add OData Query Settings and validation settings
            Func<IServiceProvider, ODataQuerySettings> querySettingFactory = (sp) => new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False,
                PageSize = null,  // no support for server enforced PageSize, yet
            };

            services.AddSingleton(typeof(ODataQuerySettings), querySettingFactory);
            services.AddSingleton<ODataValidationSettings>();

            // Make serializer and deserializer provider as DI services
            // WebApi OData service provider will be added first, need to overwrite.
            services.AddSingleton<ODataSerializerProvider, DefaultRestierSerializerProvider>();
            services.AddSingleton<ODataDeserializerProvider, DefaultRestierDeserializerProvider>();

            services.TryAddSingleton<IOperationExecutor, OperationExecutor>();
            services.AddSingleton<ODataPayloadValueConverter, RestierPayloadValueConverter>();

            services.AddService<IModelMapper, ModelMapper>();

            return services.AddScoped<RestierQueryExecutorOptions>()
                    .AddService<IQueryExecutor, RestierQueryExecutor>();
        }
    }
}
