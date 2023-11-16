// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Restier.Core;
using System;
using System.Linq;

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
        /// services.AddRestier(builder =>
        ///     builder
        ///         .AddRestierApi<SomeApi>(routeServices =>
        ///             routeServices
        ///                 .AddEF6ProviderServices<SomeDbContext>()
        ///                 .AddChainedService<IModelBuilder, SomeDbContextModelBuilder>()
        ///                 .AddSingleton(new ODataValidationSettings
        ///                 {
        ///                     MaxAnyAllExpressionDepth = 3,
        ///                     MaxExpansionDepth = 3,
        ///                 })
        ///         )
        ///  
        ///         .AddRestierApi<AnotherApi>(routeServices =>
        ///             routeServices
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
            //RWM: Make sure that Restier works in any situation without needing additional knowledge.
            return AddRestier(services, options => options.EnableEndpointRouting = false, configureApisAction);
        }

        /// <summary>
        /// Adds the Restier and OData Services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="mvcOptions">
        /// An <see cref="Action{MvcOptions}" /> that allows you to configure additional ASP.NET options, such as adding <see cref="AuthorizeFilter "/> implementations.</param>
        /// <param name="configureApisAction">An <see cref="Action{RestierApiBuilder}" /> that allows you to add APIs to the <see cref="RestierApiBuilder"/>.</param>
        /// <returns>An <see cref="IODataBuilder"/> that can be used to further configure the OData services.</returns>
        /// <example>
        /// <code>
        /// services.AddRestier(
        ///     options =>
        ///     {
        ///         // @robertmclaws: Until we have endpoint routing support, please don't forget this line... it is normally set by default on other overloads of this method.
        ///         options.EnableEndpointRouting = false;
        ///         options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
        ///     },
        ///     builder =>
        ///     {
        ///         builder.AddRestierApi<SomeApi>(routeServices =>
        ///             routeServices
        ///                 .AddEF6ProviderServices<SomeDbContext>()
        ///                 .AddChainedService<IModelBuilder, SomeDbContextModelBuilder>()
        ///                 .AddSingleton(new ODataValidationSettings
        ///                 {
        ///                     MaxAnyAllExpressionDepth = 3,
        ///                     MaxExpansionDepth = 3,
        ///                 });
        ///         );
        ///  
        ///         builder.AddRestierApi<AnotherApi>(routeServices =>
        ///             routeServices
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
        public static IMvcBuilder AddRestier(this IServiceCollection services, Action<MvcOptions> mvcOptions, Action<RestierApiBuilder> configureApisAction)
        {
            Ensure.NotNull(services, nameof(services));
            Ensure.NotNull(configureApisAction, nameof(configureApisAction));

            services.AddHttpContextAccessor();
            services.AddOData();

            // @robertmclaws: We're going to store this in the core DI container so we can grab it later and configure the APIs.
            services.AddSingleton(sp => configureApisAction);

            //RWM: Make sure that Restier works in any situation without needing additional knowledge.
            return services.AddControllers(mvcOptions);
        }

        /// <summary>
        /// Adds the Restier and OData Services to the specified <see cref="IServiceCollection"/>.
        /// This will setup the container for future endpoint routing as opposed to legacy routing.
        /// This method will call AddRouting internally, but will not add support for any other MVC
        /// components, like controllers, views or pages.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="configureApisAction">An <see cref="Action{RestierApiBuilder}" /> that allows you to add APIs to the <see cref="RestierApiBuilder"/>.</param>
        /// <returns>An <see cref="IODataBuilder"/> that can be used to further configure the OData services.</returns>
        /// <example>
        /// <code>
        /// services.AddEndpointRestier(builder =>
        ///     builder
        ///         .AddRestierApi<SomeApi>(routeServices =>
        ///             routeServices
        ///                 .AddEF6ProviderServices<SomeDbContext>()
        ///                 .AddChainedService<IModelBuilder, SomeDbContextModelBuilder>()
        ///                 .AddSingleton(new ODataValidationSettings
        ///                 {
        ///                     MaxAnyAllExpressionDepth = 3,
        ///                     MaxExpansionDepth = 3,
        ///                 })
        ///         )
        ///  
        ///         .AddRestierApi<AnotherApi>(routeServices =>
        ///             routeServices
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
        public static IServiceCollection AddEndpointRestier(this IServiceCollection services, Action<RestierApiBuilder> configureApisAction)
        {
            Ensure.NotNull(services, nameof(services));
            Ensure.NotNull(configureApisAction, nameof(configureApisAction));

            services.AddHttpContextAccessor();
            services.AddOData();

            // @robertmclaws: We're going to store this in the core DI container so we can grab it later and configure the APIs.
            services.AddSingleton(sp => configureApisAction);

            //RWM: Make sure that Restier works in any situation without needing additional knowledge.
            return services.AddRouting();
        }

        /// Adds the Restier and OData Services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="alternateBaseUri">In reverse-proxy situations, provides for an alternate base URI that can be specified in the odata.context fields.</param>
        /// <param name="configureApisAction">An <see cref="Action{RestierApiBuilder}" /> that allows you to add APIs to the <see cref="RestierApiBuilder"/>.</param>
        /// <returns>An <see cref="IODataBuilder"/> that can be used to further configure the OData services.</returns>
        /// <example>
        /// <code>
        /// services.AddRestier(builder =>
        ///     builder
        ///         .AddRestierApi<SomeApi>(routeServices =>
        ///             routeServices
        ///                 .AddEF6ProviderServices<SomeDbContext>()
        ///                 .AddChainedService<IModelBuilder, SomeDbContextModelBuilder>()
        ///                 .AddSingleton(new ODataValidationSettings
        ///                 {
        ///                     MaxAnyAllExpressionDepth = 3,
        ///                     MaxExpansionDepth = 3,
        ///                 })
        ///         )
        ///  
        ///         .AddRestierApi<AnotherApi>(routeServices =>
        ///             routeServices
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
        public static IMvcBuilder AddRestier(this IServiceCollection services, Uri alternateBaseUri, Action<RestierApiBuilder> configureApisAction)
        {
            Ensure.NotNull(services, nameof(services));
            Ensure.NotNull(configureApisAction, nameof(configureApisAction));

            services.AddHttpContextAccessor();
            services.AddOData();

            // @robertmclaws: We're going to store this in the core DI container so we can grab it later and configure the APIs.
            services.AddSingleton(sp => configureApisAction);

            //RWM: Make sure that Restier works in any situation without needing additional knowledge.
            return services.AddControllers(options => 
            {
                options.EnableEndpointRouting = false;

                // Read formatters
                Uri inputBaseAddressFactory(HttpRequest request) =>
                        new(alternateBaseUri, ODataInputFormatter.GetDefaultBaseAddress(request).AbsolutePath);

                foreach (var inputFormatter in ODataInputFormatterFactory.Create().Reverse())
                {
                    inputFormatter.BaseAddressFactory = inputBaseAddressFactory;
                    options.InputFormatters.Insert(0, inputFormatter);
                }

                // Write formatters
                Uri outputBaseAddressFactory(HttpRequest request) =>
                        new(alternateBaseUri, ODataOutputFormatter.GetDefaultBaseAddress(request).AbsolutePath);

                foreach (var outputFormatter in ODataOutputFormatterFactory.Create().Reverse())
                {
                    outputFormatter.BaseAddressFactory = outputBaseAddressFactory;
                    options.OutputFormatters.Insert(0, outputFormatter);
                }
            });
        }

    }

}