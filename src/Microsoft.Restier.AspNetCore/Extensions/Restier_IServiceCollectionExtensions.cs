// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Restier.Core;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Restier-specific extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks
    public static partial class Restier_IServiceCollectionExtensions
    {

        /// <summary>
        /// Adds the Restier and OData Services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="configureApisAction">An <see cref="Action{RestierApiBuilder}" /> that allows you to add APIs to the <see cref="RestierApiBuilder"/>.</param>
        /// <param name="useEndpointRouting">Specifies whether or not to use Endpoint Routing. Defaults to false for backwards compatibility, but will change in Restier 2.0.</param>
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
        ///    
        ///    // @robertmclaws: Since AddRestier calls .AddAuthorization(), you can use the line below if you want every request to be authenticated.
        ///    services.Configure<AuthorizationOptions>(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        /// </code>
        /// </example>
        public static IMvcBuilder AddRestier(this IServiceCollection services, Action<RestierApiBuilder> configureApisAction, bool useEndpointRouting = false)
        {
            //RWM: Make sure that Restier works in any situation without needing additional knowledge.
            return AddRestier(services, configureApisAction, options => options.EnableEndpointRouting = useEndpointRouting, useEndpointRouting);
        }

        /// <summary>
        /// Adds the Restier and OData Services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="configureApisAction">An <see cref="Action{RestierApiBuilder}" /> that allows you to add APIs to the <see cref="RestierApiBuilder"/>.</param>
        /// <param name="mvcOptions">
        /// An <see cref="Action{MvcOptions}" /> that allows you to configure additional ASP.NET options, such as adding <see cref="AuthorizeFilter"/> implementations.</param>
        /// <param name="useEndpointRouting">Specifies whether or not to use Endpoint Routing. Defaults to false for backwards compatibility, but will change in Restier 2.0.</param>
        /// <returns>An <see cref="IODataBuilder"/> that can be used to further configure the OData services.</returns>
        /// <example>
        /// <code>
        /// services.AddRestier(
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
        ///    },
        ///    options =>
        ///    {
        ///        // @robertmclaws: Until we have endpoint routing support, please don't forget this line... it is normally set by default on other overloads of this method.
        ///        options.EnableEndpointRouting = false;
        ///         
        ///        // @robertmclaws: This is one way to make requests require authentication, but is not recommended since it will only work for MVC routes.
        ///        options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
        ///    });
        ///    
        ///    // @robertmclaws: Since AddRestier calls .AddAuthorization(), you can use the line below if you want every request to be authenticated.
        ///    services.Configure<AuthorizationOptions>(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        /// </code>
        /// </example>
        public static IMvcBuilder AddRestier(this IServiceCollection services, Action<RestierApiBuilder> configureApisAction, Action<MvcOptions> mvcOptions, bool useEndpointRouting = false)
        {
            Ensure.NotNull(services, nameof(services));
            Ensure.NotNull(configureApisAction, nameof(configureApisAction));

            services.AddHttpContextAccessor();
            services.AddOData();

            // @robertmclaws: We're going to store this in the core DI container so we can grab it later and configure the APIs.
            services.AddSingleton(sp => configureApisAction);
            services.AddSingleton<RestierRouteBuilder>();

            if (useEndpointRouting)
            {
                // @robertmclaws: This is SUPER expensive, so don't do it unless we need it.
                //      https://github.com/dotnet/aspnetcore/blob/release/8.0/src/Http/Routing/src/DependencyInjection/RoutingServiceCollectionExtensions.cs
                services.AddRouting();
            }

            // @robertmclaws: Make sure that Restier works in any situation without needing additional knowledge.
            //                This is the equivalent of services.AddMvcCore().AddApiExplorer().AddAuthorization().AddCors().AddDataAnnotations().AddFormatterMappings();
            return services.AddControllers(mvcOptions);
        }

        /// Adds the Restier and OData Services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="alternateBaseUri">In reverse-proxy situations, provides for an alternate base URI that can be specified in the odata.context fields.</param>
        /// <param name="configureApisAction">An <see cref="Action{RestierApiBuilder}" /> that allows you to add APIs to the <see cref="RestierApiBuilder"/>.</param>
        /// <param name="useEndpointRouting">Specifies whether or not to use Endpoint Routing. Defaults to false for backwards compatibility, but will change in Restier 2.0.</param>
        /// <returns>An <see cref="IODataBuilder"/> that can be used to further configure the OData services.</returns>
        /// <example>
        /// <code>
        /// services.AddRestier("https://someotherwebsite.com/someapp", builder =>
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
        ///    
        ///    // @robertmclaws: Since AddRestier calls .AddAuthorization(), you can use the line below if you want every request to be authenticated.
        ///    services.Configure<AuthorizationOptions>(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        /// </code>
        /// </example>
        public static IMvcBuilder AddRestier(this IServiceCollection services, Uri alternateBaseUri, Action<RestierApiBuilder> configureApisAction, bool useEndpointRouting = false)
        {
            Ensure.NotNull(services, nameof(services));
            Ensure.NotNull(configureApisAction, nameof(configureApisAction));

            services.AddHttpContextAccessor();
            services.AddOData();

            // @robertmclaws: We're going to store this in the core DI container so we can grab it later and configure the APIs.
            services.AddSingleton(sp => configureApisAction);

            if (useEndpointRouting)
            {
                // @robertmclaws: This is SUPER expensive, so don't do it unless we need it.
                //      https://github.com/dotnet/aspnetcore/blob/release/8.0/src/Http/Routing/src/DependencyInjection/RoutingServiceCollectionExtensions.cs
                services.AddRouting();
            }

            //RWM: Make sure that Restier works in any situation without needing additional knowledge.
            return services.AddControllers(options => 
            {
                options.EnableEndpointRouting = useEndpointRouting;

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