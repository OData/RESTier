// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.OData.Formatter.Deserialization;
using Microsoft.AspNet.OData.Formatter.Serialization;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Microsoft.Restier.AspNet.Formatter;
using Microsoft.Restier.AspNet.Model;
using Microsoft.Restier.AspNet.Operation;
using Microsoft.Restier.AspNet.Query;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;

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
        public static IServiceCollection AddRestierDefaultServices<T>(this IServiceCollection services)
        {
            if (services.HasService<RestierQueryExecutor>())
            {
                // Avoid applying multiple times to a same service collection.
                return services;
            }

            if (!services.HasService<RestierModelBuilder>())
            {
                services.AddChainedService<IModelBuilder, RestierModelBuilder>();
            }

            if (!services.HasService<RestierModelExtender>())
            {
                AddRestierModelExtender(services, typeof(T));
            }

            // RWM: I'm not sure if we shuld wrap this call, because it is chained.
            //if (!services.HasService<RestierOperationModelBuilder>())
            //{
                AddOperationModelBuilder(services, typeof(T));
            //}

            // RWM: OData already registers the default settings, so if we have 2, either the developer
            //      added one, or we already did.
            if (services.HasServiceCount<ODataQuerySettings>() < 2)
            {
                services.AddSingleton(new ODataQuerySettings
                {
                    HandleNullPropagation = HandleNullPropagationOption.False,
                    PageSize = null,  // no support for server enforced PageSize, yet
                });
            }

            if (!services.HasService<ODataValidationSettings>())
            {
                services.AddSingleton<ODataValidationSettings>();
            }

            // RWM: Override the default OData serializer with Restier's.
            if (!services.HasService<DefaultRestierSerializerProvider>())
            {
                services.AddSingleton<ODataSerializerProvider, DefaultRestierSerializerProvider>();
            }

            // RWM: Override the default OData deserializer with Restier's.
            if (!services.HasService<DefaultRestierDeserializerProvider>())
            {
                services.AddSingleton<ODataDeserializerProvider, DefaultRestierDeserializerProvider>();
            }

            if (!services.HasService<RestierOperationExecutor>())
            {
                services.TryAddSingleton<IOperationExecutor, RestierOperationExecutor>();
            }

            if (!services.HasService<RestierPayloadValueConverter>())
            {
                services.AddSingleton<ODataPayloadValueConverter, RestierPayloadValueConverter>();
            }

            if (!services.HasService<RestierModelMapper>())
            {
                services.AddChainedService<IModelMapper, RestierModelMapper>();
            }

            if (!services.HasService<RestierQueryExecutorOptions>())
            {
                services.AddScoped<RestierQueryExecutorOptions>();
            }

            if (!services.HasService<RestierQueryExecutor>())
            {
                services.AddChainedService<IQueryExecutor, RestierQueryExecutor>();
            }

            return services;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="targetType"></param>
        internal static void AddRestierModelExtender(IServiceCollection services, Type targetType)
        {
            Ensure.NotNull(services, nameof(services));
            Ensure.NotNull(targetType, nameof(targetType));

            // The model builder must maintain a singleton life time, for holding states and being injected into
            // some other services.
            services.AddSingleton(new RestierModelExtender(targetType));

            services.AddChainedService<IModelBuilder, RestierModelExtender.ModelBuilder>();
            services.AddChainedService<IModelMapper, RestierModelExtender.ModelMapper>();
            services.AddChainedService<IQueryExpressionExpander, RestierModelExtender.QueryExpressionExpander>();
            services.AddChainedService<IQueryExpressionSourcer, RestierModelExtender.QueryExpressionSourcer>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="targetType"></param>
        internal static void AddOperationModelBuilder(IServiceCollection services, Type targetType)
        {
            services.AddChainedService<IModelBuilder>((sp, next) => new RestierOperationModelBuilder(targetType, next));
        }

        #endregion

    }

}