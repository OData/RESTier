// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using Microsoft.AspNet.OData.Extensions;
    using Microsoft.AspNet.OData.Interfaces;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OData;
    using Microsoft.Restier.Core;

    /// <summary>
    /// Contains extension methods of <see cref="IServiceCollection"/>.
    /// This method is used to add odata publisher service into container.
    /// </summary>
    public static partial class RestierApiServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Restier and OData Services to the Service collection.
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

            services.AddSingleton<IContainerBuilder>(s => new RestierContainerBuilder(configureApisAction));

            //RWM: Make sure that Restier works in any situation without needing additional knowledge.
            return services.AddControllers(options => options.EnableEndpointRouting = false);
        }
    }
}