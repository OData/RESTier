// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.OData.Formatter.Deserialization;
using Microsoft.AspNet.OData.Formatter.Serialization;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Microsoft.Restier.AspNet;
using Microsoft.Restier.AspNet.Formatter;
using Microsoft.Restier.AspNet.Model;
using Microsoft.Restier.AspNet.Operation;
using Microsoft.Restier.AspNet.Query;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// A set of <see cref="IServiceCollection"/> extension methods to help register required Restier services for a given Route.
    /// </summary>
    public static class ServiceCollectionExtensions
    {

        #region Internal Members

        /// <summary>
        /// Adds any missing Restier default services to the <see cref="IServiceCollection"/>. Should be called last in the service registration process.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> instance to allow for fluent method chaining.</returns>
        internal static IServiceCollection AddRestierDefaultServices(this IServiceCollection services)
        {
            Ensure.NotNull(services, nameof(services));

            if (services.HasService<DefaultRestierServicesDetectionDummy>())
            {
                // Avoid applying multiple times to a same service collection.
                return services;
            }
            services.AddSingleton<DefaultRestierServicesDetectionDummy>();

            // Only add if none are there. We have removed the default OData one before.
            services.TryAddScoped((sp) => new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False,
                PageSize = null,  // no support for server enforced PageSize, yet
            });

            // default registration, same as OData. Should not be neccesary but just in case.
            services.TryAddSingleton<ODataValidationSettings>();

            // OData already registers the ODataSerializerProvider, so if we have 2, either the developer
            // added one, or we already did. OData resolves the right one so multiple can be registered.
            if (services.HasServiceCount<ODataSerializerProvider>() < 2)
            {
                services.AddSingleton<ODataSerializerProvider, DefaultRestierSerializerProvider>();
            }

            // OData already registers the ODataDeserializerProvider, so if we have 2, either the developer
            // added one, or we already did. OData resolves the right one so multiple can be registered.
            if (services.HasServiceCount<ODataDeserializerProvider>() < 2)
            {
                services.AddSingleton<ODataDeserializerProvider, DefaultRestierDeserializerProvider>();
            }

            // TryAdd only adds if no other implementation is already registered.
            services.TryAddSingleton<IOperationExecutor, RestierOperationExecutor>();

            // OData already registers the ODataPayloadValueConverter, so if we have 2, either the developer
            // added one, or we already did. OData resolves the right one so multiple can be registered.
            if (services.HasServiceCount<ODataPayloadValueConverter>() < 2)
            {
                services.AddSingleton<ODataPayloadValueConverter, RestierPayloadValueConverter>();
            }

            // Do not add Restier implementation of chained service inside the container twice.
            if (!services.HasService<RestierWebApiModelMapper>())
            {
                services.AddChainedService<IModelMapper, RestierWebApiModelMapper>();
            }

            services.TryAddScoped<RestierQueryExecutorOptions>();

            // Do not add Restier implementation of chained service inside the container twice.
            if (!services.HasService<RestierQueryExecutor>())
            {
                services.AddChainedService<IQueryExecutor, RestierQueryExecutor>();
            }

            return services;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Dummy class to detect double registration of Default restier services inside a container.
        /// </summary>
        private sealed class DefaultRestierServicesDetectionDummy
        {

        }

        #endregion
    }

}