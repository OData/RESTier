// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore.Options;
using Microsoft.Restier.AspNetCore.Routing;
using System;

namespace Microsoft.Restier.AspNetCore;

/// <summary>
/// Extension Methods on <see cref="IMvcBuilder"/> for Restier.
/// </summary>
public static class RestierIMvcBuilderExtensions
{
    /// <summary>
    /// Registers the Restier core host services shared by every <c>AddRestier</c> overload:
    /// the HttpContextAccessor, the dynamic-route transformer, and the matcher policy that
    /// honors <c>[AllowAnonymous]</c> / <c>[Authorize]</c> on user API classes and operations.
    /// </summary>
    private static void AddRestierServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<Routing.RestierRouteValueTransformer>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<MatcherPolicy, RestierAuthorizationMetadataPolicy>());
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IStartupFilter, RestierBatchingStartupFilter>());
    }

    /// <summary>
    /// Adds the Restier and OData Services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder" /> to add services to.</param>
    /// <param name="setupAction">The OData options to configure the services with. Extension methods for adding Restier APIs are provided.</param>
    /// <returns>A <see cref="IMvcBuilder"/> that can be used to further configure the OData services.</returns>
    /// <example>
    /// <code>
    /// services.AddControllers().AddRestier(options =>
    ///     builder
    ///         .AddRestierApi<SomeApi>(routeServices =>
    ///             routeServices
    ///                 .AddEF6ProviderServices<SomeDbContext>()
    ///                 .AddChainedService<IModelBuilder, SomeDbContextModelBuilder>(),
    ///             bag =>
    ///             {
    ///                 bag.Validation.MaxAnyAllExpressionDepth = 3;
    ///                 bag.Validation.MaxExpansionDepth = 3;
    ///             }
    ///         )
    ///
    ///         .AddRestierApi<AnotherApi>(routeServices =>
    ///             routeServices
    ///                 .AddEF6ProviderServices<AnotherDbContext>()
    ///                 .AddChainedService<IModelBuilder, AnotherDbContextModelBuilder>(),
    ///             bag =>
    ///             {
    ///                 bag.Validation.MaxAnyAllExpressionDepth = 3;
    ///                 bag.Validation.MaxExpansionDepth = 3;
    ///             }
    ///         );
    ///    );
    ///
    ///    // @robertmclaws: Since AddControllers calls .AddAuthorization(), you can use the line below if you want every request to be authenticated.
    ///    services.Configure<AuthorizationOptions>(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    /// </code>
    /// </example>
    public static IMvcBuilder AddRestier(this IMvcBuilder builder, Action<ODataOptions> setupAction)
    {
        Ensure.NotNull(builder, nameof(builder));
        AddRestierServices(builder.Services);
        builder.AddOData(setupAction);
        return builder;
    }

    /// <summary>
    /// Adds the Restier and OData Services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder" /> to add services to.</param>
    /// <param name="setupAction">The OData options to configure the services with,
    /// including access to a service provider which you can resolve services from. Extension methods for adding Restier APIs are provided.</param>
    /// <returns>A <see cref="IMvcBuilder"/> that can be used to further configure the OData services.</returns>
    public static IMvcBuilder AddRestier(this IMvcBuilder builder, Action<ODataOptions, IServiceProvider> setupAction)
    {
        Ensure.NotNull(builder, nameof(builder));
        AddRestierServices(builder.Services);
        builder.AddOData(setupAction);
        return builder;
    }

    /// <summary>
    /// Adds the Restier and OData Services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder" /> to add services to.</param>
    /// <param name="alternateBaseUri">In reverse-proxy situations, provides for an alternate base URI that can be specified in the odata.context fields.</param>
    /// <param name="setupAction">The OData options to configure the services with. Extension methods for adding Restier APIs are provided.</param>
    /// <returns>A <see cref="IMvcBuilder"/> that can be used to further configure the OData services.</returns>
    public static IMvcBuilder AddRestier(this IMvcBuilder builder, Uri alternateBaseUri, Action<ODataOptions> setupAction)
    {
        Ensure.NotNull(builder, nameof(builder));
        AddRestierServices(builder.Services);
        builder.AddOData(setupAction);
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, RestierMvcOptionsSetup>(sp => new RestierMvcOptionsSetup(alternateBaseUri)));
        return builder;
    }

    /// <summary>
    /// Adds the Restier and OData Services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder" /> to add services to.</param>
    /// <param name="alternateBaseUri">In reverse-proxy situations, provides for an alternate base URI that can be specified in the odata.context fields.</param>
    /// <param name="setupAction">The OData options to configure the services with,
    /// including access to a service provider which you can resolve services from. Extension methods for adding Restier APIs are provided.</param>
    /// <returns>A <see cref="IMvcBuilder"/> that can be used to further configure the OData services.</returns>
    public static IMvcBuilder AddRestier(this IMvcBuilder builder, Uri alternateBaseUri, Action<ODataOptions, IServiceProvider> setupAction)
    {
        Ensure.NotNull(builder, nameof(builder));
        AddRestierServices(builder.Services);
        builder.AddOData(setupAction);
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, RestierMvcOptionsSetup>(sp => new RestierMvcOptionsSetup(alternateBaseUri)));
        return builder;
    }
}