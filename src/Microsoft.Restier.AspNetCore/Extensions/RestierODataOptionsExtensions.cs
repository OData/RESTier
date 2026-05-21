// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Batch;
using Microsoft.Restier.AspNetCore.Formatter;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.AspNetCore.Operation;
using Microsoft.Restier.AspNetCore.Query;
using Microsoft.Restier.AspNetCore.Routing;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using System;
using System.Collections.Generic;

namespace Microsoft.Restier.AspNetCore;

/// <summary>
/// Extension Methods on <see cref="ODataOptions"/> for Restier.
/// </summary>
public static class RestierODataOptionsExtensions
{
    /// <summary>
    /// Adds a Restier route for <typeparamref name="TApi"/> using default per-route options.
    /// </summary>
    /// <typeparam name="TApi">The Restier API type.</typeparam>
    /// <param name="oDataOptions">The <see cref="ODataOptions"/> to add a route to.</param>
    /// <param name="routePrefix">The route prefix. Pass <see cref="string.Empty"/> for an unprefixed route.</param>
    /// <param name="configureRouteServices">Per-route DI configuration delegate.</param>
    /// <returns>The same <see cref="ODataOptions"/> for chaining.</returns>
    public static ODataOptions AddRestierRoute<TApi>(
        this ODataOptions oDataOptions,
        string routePrefix,
        Action<IServiceCollection> configureRouteServices)
        where TApi : ApiBase
        => oDataOptions.AddRestierRoute<TApi>(routePrefix, configureRouteServices, configureOptions: null);

    /// <summary>
    /// Adds a Restier route with full per-route configuration.
    /// </summary>
    /// <typeparam name="TApi">The Restier API type.</typeparam>
    /// <param name="oDataOptions">The <see cref="ODataOptions"/> to add a route to.</param>
    /// <param name="routePrefix">The route prefix. Pass <see cref="string.Empty"/> for an unprefixed route.</param>
    /// <param name="configureRouteServices">Per-route DI configuration delegate.</param>
    /// <param name="configureOptions">Optional callback to mutate the <see cref="RestierRouteOptions"/> bag. The bag's settings are authoritative — see remarks on DI precedence.</param>
    /// <returns>The same <see cref="ODataOptions"/> for chaining.</returns>
    /// <remarks>
    /// <paramref name="configureOptions"/> is the single canonical channel for configuring
    /// <see cref="DeepOperationSettings"/>, <see cref="RestierConformanceOptions"/>,
    /// <c>UseRestierBatching</c>, and <see cref="RestierNamingConvention"/>. Any
    /// registrations of <see cref="DeepOperationSettings"/> or
    /// <see cref="RestierConformanceOptions"/> made inside
    /// <paramref name="configureRouteServices"/> are silently replaced by the bag's instances.
    /// </remarks>
    public static ODataOptions AddRestierRoute<TApi>(
        this ODataOptions oDataOptions,
        string routePrefix,
        Action<IServiceCollection> configureRouteServices,
        Action<RestierRouteOptions> configureOptions)
        where TApi : ApiBase
    {
        var options = new RestierRouteOptions();
        configureOptions?.Invoke(options);
        return AddRestierRoute(oDataOptions, typeof(TApi), routePrefix, configureRouteServices, options);
    }


    /// <summary>
    /// Gets the route prefixes for all registered Restier APIs.
    /// </summary>
    /// <param name="odataOptions">The <see cref="ODataOptions"/> to enumerate.</param>
    /// <returns>An enumerable of route prefix strings for Restier routes.</returns>
    public static IEnumerable<string> GetRestierRoutePrefixes(this ODataOptions odataOptions)
    {
        Ensure.NotNull(odataOptions, nameof(odataOptions));

        foreach (var (prefix, _) in odataOptions.RouteComponents)
        {
            var routeServices = odataOptions.GetRouteServices(prefix);
            if (routeServices.GetService(typeof(RestierRouteMarker)) is not null)
            {
                yield return prefix;
            }
        }
    }

    private static ODataOptions AddRestierRoute(
        ODataOptions oDataOptions,
        Type type,
        string routePrefix,
        Action<IServiceCollection> configureRouteServices,
        RestierRouteOptions options)
    {
        Ensure.NotNull(oDataOptions, nameof(oDataOptions));
        Ensure.NotNull(type, nameof(type));
        Ensure.NotNull(routePrefix, nameof(routePrefix));
        Ensure.NotNull(options, nameof(options));

        // Restier does not support qualified operation calls.
        oDataOptions.RouteOptions.EnableQualifiedOperationCall = false;

        var modelBuildingServices = new ServiceCollection();
        modelBuildingServices.TryAddSingleton<IChainOfResponsibilityFactory<IModelBuilder>, DefaultChainOfResponsibilityFactory<IModelBuilder>>();
        modelBuildingServices.TryAddSingleton<ModelMerger>();
        configureRouteServices?.Invoke(modelBuildingServices);
        modelBuildingServices.AddSingleton(typeof(RestierNamingConvention), (object)options.NamingConvention);
        modelBuildingServices.AddSingleton<KeylessViewRegistry>();
        modelBuildingServices.AddSingleton<IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
            .AddSingleton(new RestierWebApiModelExtender(type))
            .AddSingleton<IChainedService<IModelBuilder>>(sp => new OperationTypeRegistrationModelBuilder(type))
            .AddSingleton<IChainedService<IModelBuilder>>(sp => new RestierWebApiOperationModelBuilder(type, sp.GetRequiredService<RestierWebApiModelExtender>()))
            .AddSingleton<IChainedService<IModelBuilder>>(sp => new ConventionBasedAnnotationModelBuilder(type));

        IEdmModel model;
        RestierWebApiModelExtender modelExtender;
        KeylessViewRegistry keylessViewRegistry;
        ServiceProvider modelBuildingServiceProvider = null;

        try
        {
            modelBuildingServiceProvider = modelBuildingServices.BuildServiceProvider();
            var modelBuilderFactory = modelBuildingServiceProvider
                .GetRequiredService<IChainOfResponsibilityFactory<IModelBuilder>>();
            var modelBuilder = modelBuilderFactory.Create();
            model = modelBuilder.GetEdmModel();
            modelExtender = modelBuildingServiceProvider.GetRequiredService<RestierWebApiModelExtender>();
            keylessViewRegistry = modelBuildingServiceProvider.GetRequiredService<KeylessViewRegistry>();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Model building failed with exception {exception.Message}", exception);
        }
        finally
        {
            modelBuildingServiceProvider?.Dispose();
        }

        oDataOptions.AddRouteComponents(routePrefix, model, services =>
        {
            services.AddSingleton(new RestierRouteMarker(type));

            services
                .AddScoped(type, type)
                .AddScoped(sp => (ApiBase)sp.GetService(type));

            services.RemoveAll<ODataQuerySettings>()
                .AddRestierCoreServices()
                .AddRestierConventionBasedServices(type);

            services.RemoveAll<IFilterBinder>();
            services.AddSingleton<IFilterBinder, RestierSpatialFilterBinder>();

            configureRouteServices?.Invoke(services);

            // Bag wins: applied *after* configureRouteServices so it overrides any
            // registrations of these types the caller may have made in DI.
            services.AddSingleton(typeof(RestierNamingConvention), (object)options.NamingConvention);
            services.AddSingleton(options.DeepOperations);
            services.AddSingleton(options.Conformance);
            services.AddSingleton(options.Validation);

            // ODataValidationSettings is a per-action object in upstream OData
            // (designed for use inside an [EnableQuery] controller method).
            // Restier has no per-action layer, so DI-registering it in route
            // services is meaningless. Reject the legacy pattern with a clear
            // migration message — the bag is the only supported channel.
            for (var i = 0; i < services.Count; i++)
            {
                if (services[i].ServiceType == typeof(ODataValidationSettings))
                {
                    throw new InvalidOperationException(
                        $"Route '{routePrefix}': registering ODataValidationSettings directly in route services " +
                        $"is not supported. Restier has no per-query/per-action layer for this upstream OData class to attach to. " +
                        $"Configure query validation limits via the RestierRouteOptions.Validation bag on AddRestierRoute instead.");
                }
            }

            // Call Resolve once for its conflict-warning side effect; we don't
            // store the result. RestierController and the OpenAPI generators
            // build/read settings on demand from the bag at request time.
            Routing.RestierValidationOptionsResolver.Resolve(
                options.Validation, oDataOptions, routePrefix);

            services.AddSingleton<IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
                .AddSingleton(modelExtender)
                .AddSingleton(keylessViewRegistry)
                .AddSingleton<IChainedService<IModelBuilder>>(sp => new OperationTypeRegistrationModelBuilder(type))
                .AddSingleton<IChainedService<IModelBuilder>>(sp => new RestierWebApiOperationModelBuilder(type, sp.GetRequiredService<RestierWebApiModelExtender>()))
                .AddSingleton<IChainedService<IModelBuilder>>(sp => new ConventionBasedAnnotationModelBuilder(type))
                .AddSingleton<IChainedService<IModelMapper>, RestierWebApiModelMapper>()
                .AddSingleton<IChainedService<IQueryExpressionExpander>, RestierQueryExpressionExpander>()
                .AddSingleton<IChainedService<IQueryExpressionSourcer>, RestierQueryExpressionSourcer>();

            services.TryAddScoped((sp) => new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False,
                PageSize = null,
                TimeZone = oDataOptions.TimeZone,
            });

            if (services.HasServiceCount<IODataSerializerProvider>() < 2)
            {
                services.AddSingleton<IODataSerializerProvider, DefaultRestierSerializerProvider>();
            }

            if (services.HasServiceCount<IODataDeserializerProvider>() < 2)
            {
                services.AddSingleton<IODataDeserializerProvider, DefaultRestierDeserializerProvider>();
            }

            services.TryAddSingleton<IOperationExecutor, RestierOperationExecutor>();

            if (services.HasServiceCount<ODataPayloadValueConverter>() < 2)
            {
                services.AddSingleton<ODataPayloadValueConverter, RestierPayloadValueConverter>();
            }

            services.AddSingleton<IChainedService<IModelMapper>, RestierModelMapper>();
            services.AddSingleton<IChainedService<IQueryExecutor>, RestierQueryExecutor>();

            if (options.UseRestierBatching)
            {
                services.AddSingleton<ODataBatchHandler>(sp => new RestierBatchHandler()
                {
                    PrefixName = routePrefix,
                });
            }
        });

        return oDataOptions;
    }
}