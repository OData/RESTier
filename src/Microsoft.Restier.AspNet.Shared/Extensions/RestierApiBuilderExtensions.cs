// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.Restier.AspNetCore.Model;
#else
using Microsoft.Restier.AspNet.Model;
#endif
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Extension methods for the Restier API Builder.
    /// </summary>
    public static class RestierApiBuilderExtensions
    {
#region Public Methods

        /// <summary>
        /// Adds a Restier Api.
        /// </summary>
        /// <typeparam name="TApi">The type of the Api.</typeparam>
        /// <param name="builder">The restier api builder.</param>
        /// <returns>The <see cref="RestierApiBuilder"/> instance to allow for fluent method chaining.</returns>
        public static RestierApiBuilder AddRestierApi<TApi>(this RestierApiBuilder builder) where TApi : ApiBase
        {
            return AddRestierApi<TApi>(builder, services => { });
        }

        /// <summary>
        /// Adds a restier Api and allows for service registration on the route container.
        /// </summary>
        /// <typeparam name="TApi">The type of the Api.</typeparam>
        /// <param name="builder">The restier api builder.</param>
        /// <param name="services">The action to configure the services.</param>
        public static RestierApiBuilder AddRestierApi<TApi>(this RestierApiBuilder builder, Action<IServiceCollection> services) where TApi : ApiBase
        {
            Ensure.NotNull(builder, nameof(builder));
            Ensure.NotNull(services, nameof(services));

            if (builder.Apis.ContainsKey(typeof(TApi))) return builder;

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
