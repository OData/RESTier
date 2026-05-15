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
    /// Adds a Restier route for the specified API type to the OData options.
    /// </summary>
    /// <typeparam name="TApi">The type of the API to add.</typeparam>
    /// <param name="oDataOptions">The <see cref="ODataOptions"/> to add a route to.</param>
    /// <param name="configureRouteServices">Action to configure the Restier Route services.</param>
    /// <param name="useRestierBatching">Use the default Restier Batching Handler</param>
    /// <param name="namingConvention">The naming convention to use for OData JSON property names.</param>
    /// <returns>The <see cref="ODataOptions"/>.</returns>
    public static ODataOptions AddRestierRoute<TApi>
    (this ODataOptions oDataOptions,
            Action<IServiceCollection> configureRouteServices, bool useRestierBatching = true,
            RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
    where TApi : ApiBase
        => oDataOptions.AddRestierRoute<TApi>(string.Empty, configureRouteServices, useRestierBatching, namingConvention);

    /// <summary>
    /// Adds a Restier route for the specified API type to the OData options.
    /// </summary>
    /// <typeparam name="TApi">The type of the API to add.</typeparam>
    /// <param name="oDataOptions">The <see cref="ODataOptions"/> to add a route to.</param>
    /// <param name="routePrefix">The route prefix to use.</param>
    /// <param name="configureRouteServices">Action to configure the Restier Route services.</param>
    /// <param name="useRestierBatching">Use the default Restier Batching Handler</param>
    /// <param name="namingConvention">The naming convention to use for OData JSON property names.</param>
    /// <returns>The <see cref="ODataOptions"/>.</returns>
    public static ODataOptions AddRestierRoute<TApi>(
        this ODataOptions oDataOptions,
        string routePrefix,
        Action<IServiceCollection> configureRouteServices,
        bool useRestierBatching = true,
        RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
    where TApi : ApiBase
    => AddRestierRoute(oDataOptions, typeof(TApi), routePrefix , configureRouteServices, useRestierBatching, namingConvention);


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
        Type type, string routePrefix,
        Action<IServiceCollection> configureRouteServices,
        bool useRestierBatching,
        RestierNamingConvention namingConvention)
    {
        Ensure.NotNull(oDataOptions, nameof(oDataOptions));
        Ensure.NotNull(type, nameof(type));
        Ensure.NotNull(routePrefix, nameof(routePrefix));

        // Restier does not support qualified operation calls.
        oDataOptions.RouteOptions.EnableQualifiedOperationCall = false;

        // We have to do some trickery here. The model building process in OData is now separate from the route building process,
        // but Restier is not really expecting that. So we have to build the model first and then add the model and the model extender
        // to the route services. That also means that we have to invoke the service configuring action twice: once for the model building container
        // and once for the route container.
        // It might make sense to redesign the model builder to 
        var modelBuildingServices = new ServiceCollection();
        modelBuildingServices.TryAddSingleton<IChainOfResponsibilityFactory<IModelBuilder>, DefaultChainOfResponsibilityFactory<IModelBuilder>>();
        modelBuildingServices.TryAddSingleton<ModelMerger>();
        configureRouteServices.Invoke(modelBuildingServices);
        modelBuildingServices.AddSingleton(typeof(RestierNamingConvention), (object)namingConvention);
        modelBuildingServices.AddSingleton< IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
            .AddSingleton(new RestierWebApiModelExtender(type))
            .AddSingleton<IChainedService<IModelBuilder>>(sp => new RestierWebApiOperationModelBuilder(type, sp.GetRequiredService<RestierWebApiModelExtender>()))
            .AddSingleton<IChainedService<IModelBuilder>>(sp => new ConventionBasedAnnotationModelBuilder(type));

        IEdmModel model;
        RestierWebApiModelExtender modelExtender;
        ServiceProvider modelBuildingServiceProvider = null;

        try
        {
            modelBuildingServiceProvider = modelBuildingServices.BuildServiceProvider();
            var modelBuilderFactory = modelBuildingServiceProvider
                .GetRequiredService<IChainOfResponsibilityFactory<IModelBuilder>>();
            var modelBuilder = modelBuilderFactory.Create();
            model = modelBuilder.GetEdmModel();
            modelExtender = modelBuildingServiceProvider.GetRequiredService<RestierWebApiModelExtender>();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Model building failed with exception {exception.Message}", exception);
        }
        finally
        {
            modelBuildingServiceProvider?.Dispose();
        }

//        var extType = Type.GetType("Microsoft.AspNetCore.OData.Edm.EdmModelExtensions, Microsoft.AspNetCore.OData");
//;
//        var method = extType.GetMethod("ResolveNavigationSource", BindingFlags.Static | BindingFlags.Public, new[] { typeof(IEdmModel), typeof(string), typeof(bool) });
//        method.Invoke(null, [model, "Test", true]);

        oDataOptions.AddRouteComponents(routePrefix, model, services =>
        {
            // Register the Restier route marker so MapRestier() can identify this as a Restier route.
            services.AddSingleton(new RestierRouteMarker(type));

            //RWM: Add the API as the specific API type first, then if an ApiBase instance is requested from the container,
            //     get the existing instance.
            services
                .AddScoped(type, type)
                .AddScoped(sp => (ApiBase)sp.GetService(type));

            services.AddSingleton(typeof(RestierNamingConvention), (object)namingConvention);
            // RemoveAll is required: AspNetCore.OData's AddOData() registers ODataQuerySettings
            // in the outer service collection (and the route container inherits from it), so
            // without this our TryAddScoped below silently no-ops and the route ends up with the
            // default (TimeZone=null) settings — re-opening issue #704.
            services.RemoveAll<ODataQuerySettings>()
                .AddRestierCoreServices()
                .AddRestierConventionBasedServices(type);

            // Replace AspNetCoreOData's default IFilterBinder with the spatial-aware subclass.
            // The binder falls through to base for every non-geo.* call and for geo.* calls when
            // no ISpatialTypeConverter is registered, so this has zero behavioral impact on
            // non-spatial Restier APIs. Inserted BEFORE configureRouteServices.Invoke so consumers
            // who register their own IFilterBinder in their route-services delegate still win.
            services.RemoveAll<IFilterBinder>();
            services.AddSingleton<IFilterBinder, RestierSpatialFilterBinder>();

            configureRouteServices.Invoke(services);

            services.TryAddSingleton(new DeepOperationSettings());

            services.AddSingleton<IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
                .AddSingleton(modelExtender)
                .AddSingleton<IChainedService<IModelBuilder>>(sp => new RestierWebApiOperationModelBuilder(type, sp.GetRequiredService<RestierWebApiModelExtender>()))
                .AddSingleton<IChainedService<IModelBuilder>>(sp => new ConventionBasedAnnotationModelBuilder(type))
                .AddSingleton<IChainedService<IModelMapper>, RestierWebApiModelMapper>()
                .AddSingleton<IChainedService<IQueryExpressionExpander>, RestierQueryExpressionExpander>()
                .AddSingleton<IChainedService<IQueryExpressionSourcer>, RestierQueryExpressionSourcer>();

            // Stock AspNetCore.OData does not register ODataQuerySettings in DI — it constructs
            // one on-demand from EnableQueryAttribute defaults. Restier registers one here so the
            // RestierController and RestierQueryBuilder share a single, route-scoped instance.
            // Propagate ODataOptions.TimeZone so the AspNetCore.OData filter binder converts
            // DateTimeOffset literals into DateTime constants with the right DateTimeKind. Without
            // this, the binder falls back to TimeZoneInfo.Local and emits Kind=Local, which
            // Npgsql 6+ then rejects against "timestamp with time zone" columns. See
            // https://github.com/OData/RESTier/issues/704.
            services.TryAddScoped((sp) => new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.False,
                PageSize = null,  // no support for server enforced PageSize, yet
                TimeZone = oDataOptions.TimeZone,
            });

            // default registration, same as OData. Should not be necesary but just in case.
            services.TryAddSingleton<ODataValidationSettings>();

            // OData already registers the ODataSerializerProvider, so if we have 2, either the developer
            // added one, or we already did. OData resolves the right one so multiple can be registered.
            if (services.HasServiceCount<IODataSerializerProvider>() < 2)
            {
                services.AddSingleton<IODataSerializerProvider, DefaultRestierSerializerProvider>();
            }

            // OData already registers the ODataDeserializerProvider, so if we have 2, either the developer
            // added one, or we already did. OData resolves the right one so multiple can be registered.
            if (services.HasServiceCount<IODataDeserializerProvider>() < 2)
            {
                services.AddSingleton<IODataDeserializerProvider, DefaultRestierDeserializerProvider>();
            }

            services.TryAddSingleton<IOperationExecutor, RestierOperationExecutor>();

            // OData already registers the ODataPayloadValueConverter, so if we have 2, either the developer
            // added one, or we already did. OData resolves the right one so multiple can be registered.
            if (services.HasServiceCount<ODataPayloadValueConverter>() < 2)
            {
                services.AddSingleton<ODataPayloadValueConverter, RestierPayloadValueConverter>();
            }

            services.AddSingleton<IChainedService<IModelMapper>, RestierModelMapper>();
            services.AddSingleton<IChainedService<IQueryExecutor>, RestierQueryExecutor>();

            if (useRestierBatching)
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