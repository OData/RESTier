// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Extensions;
using Microsoft.OData;
using Microsoft.Restier.Core;
using System;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Restier-specific extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks
    public static partial class RestierApiServiceCollectionExtensions
    {

        /// <summary>
        /// Adds the Restier and OData Services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="configureApisAction">An <see cref="Action{RestierApiBuilder}" /> that allows you to add APIs to the <see cref="RestierApiBuilder"/>.</param>
        /// <returns>An <see cref="IODataBuilder"/> that can be used to further configure the OData services.</returns>
        /// <example>
        /// <code>
        /// config.UseRestier(builder =>
        ///     builder
        ///         .AddRestierApi<SomeApi>(services =>
        ///             services
        ///                 .AddEF6ProviderServices<SomeDbContext>()
        ///                 .AddChainedService<IModelBuilder, SomeDbContextModelBuilder>()
        ///                 .AddSingleton(new ODataValidationSettings
        ///                 {
        ///                     MaxAnyAllExpressionDepth = 3,
        ///                     MaxExpansionDepth = 3,
        ///                 })
        ///         )
        ///  
        ///         .AddRestierApi<AnotherApi>(services =>
        ///             services
        ///                 .AddEF6ProviderServices<AnotherDbContext>()
        ///                 .AddChainedService<IModelBuilder, AnotherDbContextModelBuilder>()
        ///                 .AddSingleton(new ODataValidationSettings
        ///                 {
        ///                     MaxAnyAllExpressionDepth = 3,
        ///                     MaxExpansionDepth = 3,
        ///                 })
        ///         );
        ///    );
        /// </code>
        /// </example>
        public static IMvcBuilder AddRestier(this IServiceCollection services, Action<RestierApiBuilder> configureApisAction)
        {
            Ensure.NotNull(services, nameof(services));
            Ensure.NotNull(configureApisAction, nameof(configureApisAction));

            services.AddOData();

            // @robertmclaws: We're going to store this in the core DI container so we can grab it later and configure the APIs.
            services.AddSingleton(sp => configureApisAction);

            //RWM: Make sure that Restier works in any situation without needing additional knowledge.
            return services.AddControllers(options => options.EnableEndpointRouting = false);
        }

    }

}