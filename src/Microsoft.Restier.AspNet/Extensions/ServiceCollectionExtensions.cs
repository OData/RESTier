// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.OData.Formatter.Deserialization;
using Microsoft.AspNet.OData.Formatter.Serialization;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.AspNet.Formatter;
using Microsoft.Restier.AspNet.Model;
using Microsoft.Restier.AspNet.Operation;
using Microsoft.Restier.AspNet.Query;

namespace Microsoft.Restier.AspNet
{
    /// <summary>
    /// Contains extension methods of <see cref="IServiceCollection"/>.
    /// This method is used to add odata publisher service into container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        #region Public Methods

        /// <summary>
        /// This method is used to add odata publisher service into container.
        /// </summary>
        /// <typeparam name="T">The Api type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>Current <see cref="IServiceCollection"/></returns>
        public static IServiceCollection AddRestierServices<T>(this IServiceCollection services)
        {
            if (services.HasService<RestierQueryExecutor>())
            {
                // Avoid applying multiple times to a same service collection.
                return services;
            }

            services.AddService<IModelBuilder, RestierModelBuilder>();
            AddRestierModelExtender(services, typeof(T));
            AddOperationModelBuilder(services, typeof(T));

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

            services.TryAddSingleton<IOperationExecutor, RestierOperationExecutor>();
            services.AddSingleton<ODataPayloadValueConverter, RestierPayloadValueConverter>();

            services.AddService<IModelMapper, RestierModelMapper>();

            services.AddScoped<RestierQueryExecutorOptions>();
            services.AddService<IQueryExecutor, RestierQueryExecutor>();
            return services;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="targetType"></param>
        internal static void AddRestierModelExtender(
            IServiceCollection services,
            Type targetType)
        {
            Ensure.NotNull(services, nameof(services));
            Ensure.NotNull(targetType, nameof(targetType));

            // The model builder must maintain a singleton life time, for holding states and being injected into
            // some other services.
            services.AddSingleton(new RestierModelExtender(targetType));

            services.AddService<IModelBuilder, RestierModelExtender.ModelBuilder>();
            services.AddService<IModelMapper, RestierModelExtender.ModelMapper>();
            services.AddService<IQueryExpressionExpander, RestierModelExtender.QueryExpressionExpander>();
            services.AddService<IQueryExpressionSourcer, RestierModelExtender.QueryExpressionSourcer>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="targetType"></param>
        internal static void AddOperationModelBuilder(IServiceCollection services, Type targetType)
        {
            services.AddService<IModelBuilder>((sp, next) => new RestierOperationModelBuilder(targetType, next));
        }

        #endregion

    }
}
