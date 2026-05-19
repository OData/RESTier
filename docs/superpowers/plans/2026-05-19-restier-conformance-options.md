# RestierRouteOptions and Opt-In OData Conformance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship `RestierConformanceOptions.StrictMissingParentForCollections` (opt-in 404 for `/Entity(missing)/CollectionNav` per OData v4 §11.2.6) while consolidating per-route configuration into a single `RestierRouteOptions` bag passed via `Action<RestierRouteOptions>`, collapsing the two existing positional-default `AddRestierRoute` overloads down to two cleaner ones.

**Architecture:** New `RestierConformanceOptions` and `RestierRouteOptions` types live in `Microsoft.Restier.Core`. `RestierODataOptionsExtensions.AddRestierRoute` keeps its `routePrefix` + `configureRouteServices` shape but drops the positional `useRestierBatching` / `namingConvention` parameters in favor of an optional `Action<RestierRouteOptions>`. The bag is registered into route DI via `AddSingleton` *after* `configureRouteServices` runs — bag wins, single canonical source. The controller gains one guarded block in `CreateQueryResponse` that calls the existing `ParentEntityExistsAsync` only when strict mode is enabled. The Versioning package (`IRestierApiVersioningBuilder`, `PendingVersionRegistration`, `RestierApiVersioningOptionsConfigurator`) is updated in lockstep because it reflects into the core overload.

**Tech Stack:** .NET 8/9/10, ASP.NET Core OData 9.x, xUnit v3, FluentAssertions (AwesomeAssertions), Microsoft.Restier.Breakdance test harness, Mintlify (DotNetDocs SDK) for docs.

**Reference spec:** `docs/superpowers/specs/2026-05-19-restier-conformance-options-design.md`.

---

## File Structure

**New files:**
- `src/Microsoft.Restier.Core/RestierConformanceOptions.cs` — opt-in conformance toggles (single property today).
- `src/Microsoft.Restier.Core/RestierRouteOptions.cs` — per-route configuration bag holding `DeepOperations`, `Conformance`, `UseRestierBatching`, `NamingConvention`.
- `src/Microsoft.Restier.Docs/guides/server/conformance-options.mdx` — user-facing docs and migration guide.

**Modified files (core surface):**
- `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` — replace public overloads; change DI registration ordering and use `AddSingleton`.
- `src/Microsoft.Restier.AspNetCore/RestierController.cs` — add strict-mode guard before the existing `typeReference.IsCollection()` block in `CreateQueryResponse`.

**Modified files (versioning):**
- `src/Microsoft.Restier.AspNetCore.Versioning/IRestierApiVersioningBuilder.cs` — replace positional batching/naming with `Action<RestierRouteOptions>`.
- `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningBuilder.cs` — match interface signature.
- `src/Microsoft.Restier.AspNetCore.Versioning/Internal/PendingVersionRegistration.cs` — replace fields.
- `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningOptionsConfigurator.cs` — reflection target moves from 5-parameter to 4-parameter overload.

**Modified files (test infrastructure):**
- `src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs` — `GetTestBaseInstance`, `GetTestableRestierServer`, `ExecuteTestRequest`, `GetTestableInjectedService` gain optional `configureOptions` parameters and migrate internal `AddRestierRoute` calls.

**Modified files (call sites — tests):**
- `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/Issue671_MultipleContexts.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/Issue541_CountPlusParametersFails.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/Issue519_SingleNavPropertyFilter.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/EFCore/Issue714_ComplexTypes.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/EFCore/Issue704_DateTimeFilterKind.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/EF6/Issue714_ComplexTypes.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/MultiTenancy/MultiTenancyTests.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/FallbackTests/ODataControllerFallbackTests.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/ClaimsPrincipalAccessorTests/ClaimsPrincipalAccessorTests.cs`
- `test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/NSwagIntegrationTests.cs`
- `test/Microsoft.Restier.Tests.AspNetCore.NSwag/IntegrationTests/CombinedAppTests.cs`
- `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensionsTests.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/QueryTests.cs` — *adds* the three new conformance tests.

**Modified files (call sites — samples):**
- `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Program.cs`
- `src/Microsoft.Restier.Samples.Northwind.AspNetCore/Startup.cs`

**Modified files (docs):**
- `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` — add the new page to `<MintlifyTemplate>` nav.
- Existing pages that show `AddRestierRoute` (or `AddVersion`) with positional `useRestierBatching` / `namingConvention`:
  - `src/Microsoft.Restier.Docs/quickstart.mdx`
  - `src/Microsoft.Restier.Docs/guides/server/api-versioning.mdx`
  - `src/Microsoft.Restier.Docs/guides/server/naming-conventions.mdx`
  - `src/Microsoft.Restier.Docs/guides/server/testing.mdx`
  - `src/Microsoft.Restier.Docs/guides/server/swagger.mdx`
  - `src/Microsoft.Restier.Docs/guides/server/operations.mdx`
  - `src/Microsoft.Restier.Docs/guides/server/openapi-annotations.mdx`
  - `src/Microsoft.Restier.Docs/guides/server/nswag.mdx`
  - `src/Microsoft.Restier.Docs/guides/server/multi-tenancy.mdx`
  - `src/Microsoft.Restier.Docs/guides/server/model-building.mdx`
  - `src/Microsoft.Restier.Docs/guides/server/method-authorization.mdx`
  - `src/Microsoft.Restier.Docs/guides/server/interceptors.mdx`
  - `src/Microsoft.Restier.Docs/guides/server/filters.mdx`
  - `src/Microsoft.Restier.Docs/guides/extending-restier/in-memory-provider.mdx`

---

### Task 1: Add `RestierConformanceOptions`

**Files:**
- Create: `src/Microsoft.Restier.Core/RestierConformanceOptions.cs`

- [ ] **Step 1: Create the file**

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Opt-in toggles for stricter OData v4 spec conformance. Defaults preserve
    /// Restier's existing pragmatic behavior.
    /// </summary>
    public class RestierConformanceOptions
    {
        /// <summary>
        /// When <c>true</c>, requests to a collection-valued navigation property
        /// whose parent entity does not exist (e.g. <c>/Books(missing)/Reviews</c>)
        /// return <c>404 Not Found</c> per OData v4 Part 1 §9.1.5 / §11.2.6.
        /// When <c>false</c> (default), an empty collection
        /// (<c>200 OK { "value": [] }</c>) is returned, matching Restier's
        /// historical behavior. Setting this to <c>true</c> incurs one extra
        /// parent-existence query per collection-nav request whose path
        /// includes a key segment.
        /// </summary>
        public bool StrictMissingParentForCollections { get; set; }
    }
}
```

- [ ] **Step 2: Verify the core project compiles**

Run: `dotnet build src/Microsoft.Restier.Core/Microsoft.Restier.Core.csproj -c Debug --nologo -v q`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.Core/RestierConformanceOptions.cs
git commit -m "feat(core): add RestierConformanceOptions for opt-in spec strictness"
```

---

### Task 2: Add `RestierRouteOptions`

**Files:**
- Create: `src/Microsoft.Restier.Core/RestierRouteOptions.cs`

- [ ] **Step 1: Create the file**

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Per-route configuration for a Restier route. Pass an
    /// <c>Action&lt;RestierRouteOptions&gt;</c> to
    /// <c>ODataOptions.AddRestierRoute</c> to customize batching, naming
    /// convention, deep-operation depth, and OData-spec conformance.
    /// </summary>
    public class RestierRouteOptions
    {
        /// <summary>
        /// Deep insert/update settings (max nesting depth).
        /// </summary>
        public DeepOperationSettings DeepOperations { get; } = new();

        /// <summary>
        /// Opt-in OData-spec conformance toggles.
        /// </summary>
        public RestierConformanceOptions Conformance { get; } = new();

        /// <summary>
        /// When <c>true</c> (default), the Restier batch handler is registered
        /// for the route.
        /// </summary>
        public bool UseRestierBatching { get; set; } = true;

        /// <summary>
        /// Naming convention applied to EDM property names and the resulting
        /// JSON. Defaults to <see cref="RestierNamingConvention.PascalCase"/>.
        /// </summary>
        public RestierNamingConvention NamingConvention { get; set; }
            = RestierNamingConvention.PascalCase;
    }
}
```

- [ ] **Step 2: Verify the core project compiles**

Run: `dotnet build src/Microsoft.Restier.Core/Microsoft.Restier.Core.csproj -c Debug --nologo -v q`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.Core/RestierRouteOptions.cs
git commit -m "feat(core): add RestierRouteOptions bag for per-route configuration"
```

---

### Task 3: Replace the public `AddRestierRoute` overloads

This task breaks compilation of `Microsoft.Restier.Breakdance`, the versioning package, the samples, and ~15 test files. Tasks 4-8 repair them in sequence.

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs`

- [ ] **Step 1: Replace the two public overloads and update the private body**

Open `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs`.

Delete the two public `AddRestierRoute<TApi>` methods at lines 46-70 (the no-prefix one and the prefixed one, both with positional `useRestierBatching` / `namingConvention`). Replace them with the following:

```csharp
    /// <summary>
    /// Adds a Restier route at the empty (root) prefix.
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
```

- [ ] **Step 2: Update the private `AddRestierRoute` body**

Find the existing private method `private static ODataOptions AddRestierRoute(ODataOptions oDataOptions, Type type, string routePrefix, Action<IServiceCollection> configureRouteServices, bool useRestierBatching, RestierNamingConvention namingConvention)` (it currently starts near line 92).

Replace its signature and body. The new signature accepts a `RestierRouteOptions options` instead of `bool useRestierBatching` and `RestierNamingConvention namingConvention`. Inside the body:

1. The model-building services block keeps using `options.NamingConvention` instead of the old `namingConvention` local.
2. The route services block keeps using `options.NamingConvention` instead of `namingConvention`.
3. The `useRestierBatching` check at the end becomes `options.UseRestierBatching`.
4. The existing `services.TryAddSingleton(new DeepOperationSettings());` line is **removed**; replaced by `services.AddSingleton(options.DeepOperations);` and `services.AddSingleton(options.Conformance);` placed **after** `configureRouteServices.Invoke(services);`.

The complete new body:

```csharp
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
        modelBuildingServices.AddSingleton<IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
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

        oDataOptions.AddRouteComponents(routePrefix, model, services =>
        {
            services.AddSingleton(new RestierRouteMarker(type));

            services
                .AddScoped(type, type)
                .AddScoped(sp => (ApiBase)sp.GetService(type));

            services.AddSingleton(typeof(RestierNamingConvention), (object)options.NamingConvention);
            services.RemoveAll<ODataQuerySettings>()
                .AddRestierCoreServices()
                .AddRestierConventionBasedServices(type);

            services.RemoveAll<IFilterBinder>();
            services.AddSingleton<IFilterBinder, RestierSpatialFilterBinder>();

            configureRouteServices?.Invoke(services);

            // Bag wins: applied *after* configureRouteServices so it overrides any
            // registrations of these types the caller may have made in DI.
            services.AddSingleton(options.DeepOperations);
            services.AddSingleton(options.Conformance);

            services.AddSingleton<IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
                .AddSingleton(modelExtender)
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

            services.TryAddSingleton<ODataValidationSettings>();

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
```

The visible changes versus the previous body:
- Parameter list ends with `RestierRouteOptions options` instead of `bool useRestierBatching, RestierNamingConvention namingConvention`.
- The `services.TryAddSingleton(new DeepOperationSettings());` line is removed.
- `services.AddSingleton(options.DeepOperations);` and `services.AddSingleton(options.Conformance);` are placed *after* `configureRouteServices?.Invoke(services);`.
- `useRestierBatching` → `options.UseRestierBatching`.
- `namingConvention` → `options.NamingConvention` (two call sites).
- An `Ensure.NotNull(options, ...)` guard is added.
- `configureRouteServices?.Invoke(...)` is now null-safe (existing code didn't tolerate null callbacks; the new one-arg overload passes `configureOptions: null` but `configureRouteServices` is still required from the public surface, so this is defensive but not strictly necessary — keep the `?.` for safety).

- [ ] **Step 3: Verify the AspNetCore project compiles**

Run: `dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj -c Debug --nologo -v q`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

Downstream projects (Breakdance, Versioning, Samples, all test projects) will *not* compile until Task 4-8 complete. That is expected.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs
git commit -m "feat(aspnetcore)!: replace AddRestierRoute overloads with options-bag form

BREAKING CHANGE: positional useRestierBatching and namingConvention removed.
Use Action<RestierRouteOptions> configureOptions instead. The bag is now the
authoritative source for these settings — DI registrations of
DeepOperationSettings or RestierConformanceOptions inside configureRouteServices
are silently replaced by the bag's instances."
```

---

### Task 4: Update `RestierTestHelpers` to flow `configureOptions` through

**Files:**
- Modify: `src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs`

The current helpers take a `RestierNamingConvention namingConvention` parameter and forward it positionally to `AddRestierRoute`. They must:
1. Add an optional `Action<RestierRouteOptions> configureOptions = null` parameter to every public entry point (`ExecuteTestRequest`, `GetTestableInjectedService`, `GetTestableRestierServer`, `GetTestBaseInstance`, and any other `<TApi>` helpers that build the host).
2. Keep `namingConvention`'s named-argument signature for callers who pass it (it still maps onto `options.NamingConvention`), but apply it by composing into the same `configureOptions` action.
3. Pass `configureOptions` through to `AddRestierRoute<TApi>(routePrefix, services, configureOptions)`.

- [ ] **Step 1: Update `GetTestBaseInstance<TApi>` to accept and forward `configureOptions`**

Open `src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs`. Find `GetTestBaseInstance<TApi>` near line 396.

Replace the method with the version below. The composition trick: build a single `Action<RestierRouteOptions>` that first applies the helper's own `namingConvention` (so existing callers keep working) and then the caller's `configureOptions` (so it can override or extend).

```csharp
        public static RestierBreakdanceTestBase<TApi> GetTestBaseInstance<TApi>(
            string routeName = WebApiConstants.RouteName,
            string routePrefix = WebApiConstants.RoutePrefix,
            Action<IServiceCollection> apiServiceCollection = default,
            RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase,
            Action<RestierRouteOptions> configureOptions = null)
            where TApi : ApiBase
        {
            using var restierTests = new RestierBreakdanceTestBase<TApi>();

            restierTests.AddRestierAction = (odataOptions) =>
            {
                odataOptions.AddRestierRoute<TApi>(routeName, restierServices =>
                {
                    restierServices
                        .AddSingleton(new ODataValidationSettings
                        {
                            MaxTop = 5,
                            MaxAnyAllExpressionDepth = 3,
                            MaxExpansionDepth = 3,
                        });
                    apiServiceCollection?.Invoke(restierServices);
                },
                options =>
                {
                    options.NamingConvention = namingConvention;
                    configureOptions?.Invoke(options);
                });
            };

            restierTests.TestSetup();

            return restierTests;
        }
```

- [ ] **Step 2: Mirror the parameter into `GetTestableRestierServer<TApi>`**

Locate `GetTestableRestierServer<TApi>` (just above `GetTestBaseInstance`, near line 382) and add the same `configureOptions` parameter, forwarding it through:

```csharp
        public static TestServer GetTestableRestierServer<TApi>(
            string routeName = WebApiConstants.RouteName,
            string routePrefix = WebApiConstants.RoutePrefix,
            Action<IServiceCollection> apiServiceCollection = default,
            RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase,
            Action<RestierRouteOptions> configureOptions = null)
            where TApi : ApiBase
            => GetTestBaseInstance<TApi>(routeName, routePrefix, apiServiceCollection, namingConvention, configureOptions).TestServer;
```

- [ ] **Step 3: Add `configureOptions` to every other helper that takes `namingConvention` or `serviceCollection`**

Search the file for every public method whose signature mentions `RestierNamingConvention namingConvention` or whose body calls `GetTestBaseInstance` / `GetTestableRestierServer`. Run:

```bash
grep -n "namingConvention\|GetTestBaseInstance\|GetTestableRestierServer" src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs
```

For each match, add the same optional `Action<RestierRouteOptions> configureOptions = null` parameter to the public signature and forward it. The common pattern looks like:

```csharp
// Before
public static async Task<HttpResponseMessage> ExecuteTestRequest<TApi>(
    HttpMethod method,
    string host = WebApiConstants.HostName,
    string routePrefix = WebApiConstants.RoutePrefix,
    string resource = null,
    object payload = null,
    string acceptHeader = WebApiConstants.DefaultAcceptHeader,
    JsonSerializerSettings jsonSerializerSettings = null,
    Action<IServiceCollection> serviceCollection = default,
    RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase,
    ... )
    where TApi : ApiBase
{
    ...
    var server = GetTestableRestierServer<TApi>(routeName, routePrefix, serviceCollection, namingConvention);
    ...
}

// After
public static async Task<HttpResponseMessage> ExecuteTestRequest<TApi>(
    HttpMethod method,
    string host = WebApiConstants.HostName,
    string routePrefix = WebApiConstants.RoutePrefix,
    string resource = null,
    object payload = null,
    string acceptHeader = WebApiConstants.DefaultAcceptHeader,
    JsonSerializerSettings jsonSerializerSettings = null,
    Action<IServiceCollection> serviceCollection = default,
    RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase,
    Action<RestierRouteOptions> configureOptions = null,
    ... )
    where TApi : ApiBase
{
    ...
    var server = GetTestableRestierServer<TApi>(routeName, routePrefix, serviceCollection, namingConvention, configureOptions);
    ...
}
```

Place `Action<RestierRouteOptions> configureOptions = null` immediately *after* `namingConvention` in every signature so existing positional/named call sites keep compiling — the new parameter is purely additive at the end of the options group.

- [ ] **Step 4: Add the `using Microsoft.Restier.Core;` import if missing**

`RestierRouteOptions` lives in `Microsoft.Restier.Core`. If the file already imports it (most likely it does, for `RestierNamingConvention`), skip this step.

- [ ] **Step 5: Verify Breakdance compiles**

Run: `dotnet build src/Microsoft.Restier.Breakdance/Microsoft.Restier.Breakdance.csproj -c Debug --nologo -v q`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs
git commit -m "feat(breakdance): flow Action<RestierRouteOptions> through test helpers"
```

---

### Task 5: Update non-helper test call sites

These tests call `AddRestierRoute` directly inside their own host setup rather than going through `RestierTestHelpers`. None of them currently pass positional `useRestierBatching` or `namingConvention` (I checked — they all use the bare two-argument prefixed form), so the mechanical change is purely to verify they still compile against the new signature and to introduce a `configureOptions` callback if any of them want to use the new conformance toggle.

**Files (all in `test/Microsoft.Restier.Tests.AspNetCore/`):**
- `RegressionTests/Issue671_MultipleContexts.cs`
- `RegressionTests/Issue541_CountPlusParametersFails.cs`
- `RegressionTests/Issue519_SingleNavPropertyFilter.cs`
- `RegressionTests/EFCore/Issue714_ComplexTypes.cs`
- `RegressionTests/EFCore/Issue704_DateTimeFilterKind.cs`
- `RegressionTests/EF6/Issue714_ComplexTypes.cs`
- `FeatureTests/EFCore/MultiTenancy/MultiTenancyTests.cs`
- `FeatureTests/AnonymousAccessTests.cs`
- `FallbackTests/ODataControllerFallbackTests.cs`
- `ClaimsPrincipalAccessorTests/ClaimsPrincipalAccessorTests.cs`

Also in `test/Microsoft.Restier.Tests.AspNetCore.Versioning/` and `test/Microsoft.Restier.Tests.AspNetCore.NSwag/`:
- `IntegrationTests/NSwagIntegrationTests.cs`
- `IntegrationTests/CombinedAppTests.cs`
- `Extensions/IApplicationBuilderExtensionsTests.cs`

- [ ] **Step 1: Inventory the call shapes**

Run:

```bash
grep -rn "AddRestierRoute<" test/ | grep -v "QueryTests.cs"
```

For every match, inspect the surrounding lines. If a call has positional `true`/`false` (batching) or `RestierNamingConvention.*` as the third or later argument, those must move into a `configureOptions` block. If the call is just `options.AddRestierRoute<TApi>(prefix, services => { ... })`, no change is required — it now binds to the new two-parameter public overload.

- [ ] **Step 2: Migrate any call that uses positional batching or naming arguments**

For each affected file, replace the call shape:

```csharp
// Before
options.AddRestierRoute<MyApi>(prefix, services => { ... }, useRestierBatching: false, namingConvention: RestierNamingConvention.LowerCamelCase);

// After
options.AddRestierRoute<MyApi>(prefix, services => { ... }, options =>
{
    options.UseRestierBatching = false;
    options.NamingConvention = RestierNamingConvention.LowerCamelCase;
});
```

Based on the inventory in Step 1, the test files listed above only use the bare two-argument form, so most edits in this step will be no-ops. Verify by re-running the grep after edits — if any positional `bool` or `RestierNamingConvention.*` argument remains on an `AddRestierRoute` line, fix it.

- [ ] **Step 3: Verify each affected test project compiles**

Run:

```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj -c Debug --nologo -v q
dotnet build test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj -c Debug --nologo -v q
dotnet build test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj -c Debug --nologo -v q
```

Expected: each `Build succeeded. 0 Warning(s) 0 Error(s)`. Failures here mean either an unmigrated positional argument or a missing `using Microsoft.Restier.Core;` import.

- [ ] **Step 4: Commit**

```bash
git add test/
git commit -m "test: migrate non-helper AddRestierRoute call sites to options-bag form"
```

---

### Task 6: Update sample projects

**Files:**
- Modify: `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Program.cs`
- Modify: `src/Microsoft.Restier.Samples.Northwind.AspNetCore/Startup.cs`

- [ ] **Step 1: Inspect each call**

```bash
grep -B 1 -A 8 "AddRestierRoute" src/Microsoft.Restier.Samples.Postgres.AspNetCore/Program.cs
grep -B 1 -A 8 "AddRestierRoute" src/Microsoft.Restier.Samples.Northwind.AspNetCore/Startup.cs
```

- [ ] **Step 2: Migrate any positional batching/naming args**

If either sample passes `useRestierBatching` or `namingConvention` positionally, fold them into a `configureOptions` block as in Task 5 Step 2. If they use only the bare form, no change is required.

The Northwind sample's call is `options.AddRestierRoute<NorthwindApi>(restierServices => ...)` (no prefix). Since the unprefixed convenience overload is gone, change this to `options.AddRestierRoute<NorthwindApi>(string.Empty, restierServices => ...)`.

The Postgres sample's call is `options.AddRestierRoute<RestierTestContextApi>("v3", restierServices => ...)` — already in the new shape; no change.

- [ ] **Step 3: Verify samples compile**

Run:

```bash
dotnet build src/Microsoft.Restier.Samples.Postgres.AspNetCore/Microsoft.Restier.Samples.Postgres.AspNetCore.csproj -c Debug --nologo -v q
dotnet build src/Microsoft.Restier.Samples.Northwind.AspNetCore/Microsoft.Restier.Samples.Northwind.AspNetCore.csproj -c Debug --nologo -v q
```

Expected: both `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Samples.Postgres.AspNetCore/ src/Microsoft.Restier.Samples.Northwind.AspNetCore/
git commit -m "sample: migrate AddRestierRoute calls to new options-bag form"
```

---

### Task 7: Update the versioning layer

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore.Versioning/IRestierApiVersioningBuilder.cs`
- Modify: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningBuilder.cs`
- Modify: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/PendingVersionRegistration.cs`
- Modify: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningOptionsConfigurator.cs`

- [ ] **Step 1: Update `IRestierApiVersioningBuilder.AddVersion` signatures**

Open `src/Microsoft.Restier.AspNetCore.Versioning/IRestierApiVersioningBuilder.cs`. Replace both `AddVersion` overload declarations with:

```csharp
        /// <summary>
        /// Registers one or more versions for <typeparamref name="TApi"/>, reading every
        /// <c>[ApiVersion]</c> attribute on the type.
        /// </summary>
        /// <typeparam name="TApi">The <see cref="ApiBase"/>-derived type for these versions.</typeparam>
        /// <param name="basePrefix">The logical API prefix; the version segment is appended to it.</param>
        /// <param name="configureRouteServices">Per-route DI configuration delegate.</param>
        /// <param name="configureVersioning">Optional per-call versioning options (segment formatter, sunset, explicit prefix).</param>
        /// <param name="configureOptions">Optional callback to mutate the per-route <see cref="RestierRouteOptions"/> bag.</param>
        IRestierApiVersioningBuilder AddVersion<TApi>(
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> configureVersioning = null,
            Action<RestierRouteOptions> configureOptions = null)
            where TApi : ApiBase;

        /// <summary>
        /// Registers a specific <paramref name="apiVersion"/> for <typeparamref name="TApi"/>,
        /// without reading any <c>[ApiVersion]</c> attribute.
        /// </summary>
        /// <typeparam name="TApi">The <see cref="ApiBase"/>-derived type for this version.</typeparam>
        /// <param name="apiVersion">The version to register.</param>
        /// <param name="deprecated">Whether this version is deprecated.</param>
        /// <param name="basePrefix">The logical API prefix; the version segment is appended to it.</param>
        /// <param name="configureRouteServices">Per-route DI configuration delegate.</param>
        /// <param name="configureVersioning">Optional per-call versioning options (segment formatter, sunset, explicit prefix).</param>
        /// <param name="configureOptions">Optional callback to mutate the per-route <see cref="RestierRouteOptions"/> bag.</param>
        IRestierApiVersioningBuilder AddVersion<TApi>(
            ApiVersion apiVersion,
            bool deprecated,
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> configureVersioning = null,
            Action<RestierRouteOptions> configureOptions = null)
            where TApi : ApiBase;
```

- [ ] **Step 2: Update the concrete `RestierApiVersioningBuilder` implementations to match**

Open `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningBuilder.cs`. Replace both `AddVersion` overloads so their parameter lists match the interface (drop `useRestierBatching` and `namingConvention`, add `configureOptions`). Pass `configureOptions` through to `PendingVersionRegistration` instead of the old two values:

```csharp
        public IRestierApiVersioningBuilder AddVersion<TApi>(
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> configureVersioning = null,
            Action<RestierRouteOptions> configureOptions = null)
            where TApi : ApiBase
        {
            if (basePrefix is null)
            {
                throw new ArgumentNullException(nameof(basePrefix));
            }

            if (configureRouteServices is null)
            {
                throw new ArgumentNullException(nameof(configureRouteServices));
            }

            foreach (var read in ApiVersionAttributeReader.Read(typeof(TApi)))
            {
                lock (_lock)
                {
                    _pending.Add(new PendingVersionRegistration(
                        typeof(TApi),
                        read.ApiVersion,
                        read.IsDeprecated,
                        basePrefix,
                        configureRouteServices,
                        configureVersioning,
                        configureOptions));
                }
            }

            return this;
        }

        public IRestierApiVersioningBuilder AddVersion<TApi>(
            ApiVersion apiVersion,
            bool deprecated,
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> configureVersioning = null,
            Action<RestierRouteOptions> configureOptions = null)
            where TApi : ApiBase
        {
            if (apiVersion is null)
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            if (basePrefix is null)
            {
                throw new ArgumentNullException(nameof(basePrefix));
            }

            if (configureRouteServices is null)
            {
                throw new ArgumentNullException(nameof(configureRouteServices));
            }

            lock (_lock)
            {
                _pending.Add(new PendingVersionRegistration(
                    typeof(TApi),
                    apiVersion,
                    deprecated,
                    basePrefix,
                    configureRouteServices,
                    configureVersioning,
                    configureOptions));
            }

            return this;
        }
```

- [ ] **Step 3: Update `PendingVersionRegistration`**

Open `src/Microsoft.Restier.AspNetCore.Versioning/Internal/PendingVersionRegistration.cs`. Replace the constructor parameter list and the `UseRestierBatching` / `NamingConvention` properties with a single `ConfigureOptions` property:

```csharp
    internal sealed class PendingVersionRegistration
    {

        public PendingVersionRegistration(
            Type apiType,
            ApiVersion apiVersion,
            bool isDeprecated,
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> applyVersioningOptions,
            Action<RestierRouteOptions> configureOptions)
        {
            ApiType = apiType;
            ApiVersion = apiVersion;
            IsDeprecated = isDeprecated;
            BasePrefix = basePrefix;
            ConfigureRouteServices = configureRouteServices;
            ApplyVersioningOptions = applyVersioningOptions;
            ConfigureOptions = configureOptions;
        }

        public Type ApiType { get; }

        public ApiVersion ApiVersion { get; }

        public bool IsDeprecated { get; }

        public string BasePrefix { get; }

        public Action<IServiceCollection> ConfigureRouteServices { get; }

        public Action<RestierVersioningOptions> ApplyVersioningOptions { get; }

        public Action<RestierRouteOptions> ConfigureOptions { get; }

    }
```

- [ ] **Step 4: Update the reflection in `RestierApiVersioningOptionsConfigurator.ApplyOne`**

Open `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningOptionsConfigurator.cs`. Replace the reflection block at the bottom of `ApplyOne` (lines ~106-122) with the four-parameter version of the target overload:

```csharp
            // Reflect into the AddRestierRoute extension. The generic constraint makes this
            // a one-time cost per host boot.
            var addRestierRoute = typeof(RestierODataOptionsExtensions)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .First(m => m.Name == nameof(RestierODataOptionsExtensions.AddRestierRoute)
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 4);
            var closed = addRestierRoute.MakeGenericMethod(pending.ApiType);
            closed.Invoke(null, new object[]
            {
                options,
                routePrefix,
                pending.ConfigureRouteServices,
                pending.ConfigureOptions,
            });
```

`GetParameters().Length == 4` because the new options-form public overload has four parameters: `this ODataOptions`, `string routePrefix`, `Action<IServiceCollection> configureRouteServices`, `Action<RestierRouteOptions> configureOptions`. The `this` parameter counts in `MethodInfo.GetParameters()` for extension methods, so 4 is correct (the previous code used 5 for the same reason).

- [ ] **Step 5: Add `using Microsoft.Restier.Core;` where needed**

`RestierRouteOptions` and `RestierConformanceOptions` live in `Microsoft.Restier.Core`. Confirm the relevant files import it:

```bash
grep -L "using Microsoft.Restier.Core" src/Microsoft.Restier.AspNetCore.Versioning/IRestierApiVersioningBuilder.cs src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningBuilder.cs src/Microsoft.Restier.AspNetCore.Versioning/Internal/PendingVersionRegistration.cs
```

For any file the command lists, add `using Microsoft.Restier.Core;` at the top.

- [ ] **Step 6: Verify the versioning project compiles**

Run: `dotnet build src/Microsoft.Restier.AspNetCore.Versioning/Microsoft.Restier.AspNetCore.Versioning.csproj -c Debug --nologo -v q`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 7: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Versioning/
git commit -m "feat(versioning)!: switch AddVersion to Action<RestierRouteOptions>

Mirrors the AddRestierRoute breaking change. PendingVersionRegistration
carries the new callback; the configurator reflects into the 4-parameter
options-form overload."
```

---

### Task 8: Add the controller strict-mode guard

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

- [ ] **Step 1: Add the guard block before the collection branch**

Open `src/Microsoft.Restier.AspNetCore/RestierController.cs`. Locate the existing collection branch in `CreateQueryResponse`:

```csharp
            if (typeReference.IsCollection())
            {
                var elementType = typeReference.AsCollection().ElementType();
                if (elementType.IsPrimitive() || elementType.IsEnum())
                {
                    return Ok(new NonResourceCollectionResult(query, typeReference));
                }

                return Ok(new ResourceSetResult(query, typeReference));
            }
```

Immediately *before* this `if`, insert:

```csharp
            // Opt-in OData v4 §11.2.6 strictness: when a collection-valued nav segment
            // sits below a key segment whose parent does not exist, the addressed
            // resource doesn't exist, so 404 is required by the spec. Off by default —
            // see RestierConformanceOptions.StrictMissingParentForCollections.
            if (typeReference.IsCollection() && path.OfType<KeySegment>().Any())
            {
                var conformance = HttpContext.Request.GetRouteServices()
                    .GetService<RestierConformanceOptions>();
                if (conformance?.StrictMissingParentForCollections == true)
                {
                    var parentExists = await ParentEntityExistsAsync(path, cancellationToken)
                        .ConfigureAwait(false);
                    if (!parentExists)
                    {
                        return NotFound(Resources.ResourceNotFound);
                    }
                }
            }
```

The new block lives *inside* the same `async Task<IActionResult>` method, just before the existing `if (typeReference.IsCollection())`. The `ParentEntityExistsAsync` helper and the `path` parameter are already in scope (introduced by PR #614).

- [ ] **Step 2: Add the `using Microsoft.Restier.Core;` import if missing**

```bash
grep -n "using Microsoft.Restier.Core" src/Microsoft.Restier.AspNetCore/RestierController.cs
```

If no match, add `using Microsoft.Restier.Core;` to the using block at the top of the file.

- [ ] **Step 3: Verify the AspNetCore project compiles**

Run: `dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj -c Debug --nologo -v q`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/RestierController.cs
git commit -m "feat(controller): opt-in 404 for collection nav from missing parent

Guarded by RestierConformanceOptions.StrictMissingParentForCollections.
Off by default; preserves the historical 200 + empty-collection response.
Closes the last OData v4 §11.2.6 gap from #735."
```

---

### Task 9: Add the three new conformance tests

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/QueryTests.cs`

These tests exercise the new public API path end-to-end: the `configureOptions` callback flows into `RestierTestHelpers.ExecuteTestRequest` (extended in Task 4), into the new `AddRestierRoute` overload, into route DI, and finally into the controller guard added in Task 8.

- [ ] **Step 1: Write the default-behavior test**

Open `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/QueryTests.cs` and add the following `[Fact]` to the existing `QueryTests` class:

```csharp
    [Fact]
    public async Task CollectionNavFromMissingParentReturns200ByDefault()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books(00000000-0000-0000-0000-000000000000)/Reviews",
            serviceCollection: ConfigureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
```

- [ ] **Step 2: Run the default-behavior test to confirm it passes**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
    --filter "FullyQualifiedName~CollectionNavFromMissingParentReturns200ByDefault" \
    --nologo -v q
```

Expected: 1 test passed across each TFM. (The behavior was already correct — this test locks it in.)

- [ ] **Step 3: Write the strict-mode 404 test**

Add the following `[Fact]`. This is the load-bearing test: it must fail if the controller guard is broken *or* if the new `AddRestierRoute(prefix, services, options)` overload's wiring breaks.

```csharp
    [Fact]
    public async Task CollectionNavFromMissingParentReturns404WhenStrict()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books(00000000-0000-0000-0000-000000000000)/Reviews",
            serviceCollection: ConfigureServices,
            configureOptions: options => options.Conformance.StrictMissingParentForCollections = true);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
```

- [ ] **Step 4: Run the strict-mode test to confirm it passes**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
    --filter "FullyQualifiedName~CollectionNavFromMissingParentReturns404WhenStrict" \
    --nologo -v q
```

Expected: 1 test passed across each TFM.

- [ ] **Step 5: Write the strict-mode + existing-parent test**

```csharp
    [Fact]
    public async Task CollectionNavFromExistingParentReturns200EmptyWhenStrict()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Publishers('Publisher1')/Books",
            serviceCollection: ConfigureServices,
            configureOptions: options => options.Conformance.StrictMissingParentForCollections = true);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
```

- [ ] **Step 6: Run the existing-parent test to confirm it passes**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
    --filter "FullyQualifiedName~CollectionNavFromExistingParentReturns200EmptyWhenStrict" \
    --nologo -v q
```

Expected: 1 test passed across each TFM.

- [ ] **Step 7: Run the full QueryTests file to confirm no regression**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
    --filter "FullyQualifiedName~Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore.QueryTests" \
    --nologo -v q
```

Expected: 11 tests passed across each TFM (the previous 8 + the 3 new ones). EF6 tests in the suite may fail if no SQL Server is reachable from the dev machine; those failures are environmental and unrelated.

- [ ] **Step 8: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/QueryTests.cs
git commit -m "test: cover opt-in collection-nav 404 conformance toggle (#735)"
```

---

### Task 10: Add the conformance documentation page

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/conformance-options.mdx`
- Modify: `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`

- [ ] **Step 1: Create the docs page**

Write the file with the following content:

````mdx
---
title: 'OData Conformance Options'
description: 'Opt-in toggles for stricter OData v4 spec conformance, plus the per-route RestierRouteOptions configuration bag.'
---

Restier exposes per-route configuration through a single `RestierRouteOptions` bag passed to `AddRestierRoute` (or `AddVersion`, when using the versioning package). The bag groups four sets of knobs:

| Property | Type | Default | Purpose |
|---|---|---|---|
| `DeepOperations` | `DeepOperationSettings` | `new() { MaxDepth = 5 }` | Maximum nesting depth for deep insert / deep update. |
| `Conformance` | `RestierConformanceOptions` | `new()` | Opt-in OData v4 spec strictness toggles. |
| `UseRestierBatching` | `bool` | `true` | Whether the Restier batch handler is registered. |
| `NamingConvention` | `RestierNamingConvention` | `PascalCase` | EDM-to-JSON property naming. |

## Configuring a route

```csharp
builder.Services.AddOData(options =>
{
    options.AddRestierRoute<NorthwindApi>(
        "api",
        services => services.AddEntityFrameworkServices<NorthwindContext>(),
        options =>
        {
            options.Conformance.StrictMissingParentForCollections = true;
            options.DeepOperations.MaxDepth = 10;
            options.UseRestierBatching = false;
            options.NamingConvention = RestierNamingConvention.LowerCamelCase;
        });
});
```

The first argument is the route prefix — pass `""` for an unprefixed route. The second is the per-route DI delegate. The third is the optional `RestierRouteOptions` callback.

## `RestierConformanceOptions.StrictMissingParentForCollections`

When `true`, requests to a collection-valued navigation property whose parent entity does not exist — for example `GET /Books(missing-guid)/Reviews` — return `404 Not Found` per [OData v4 Part 1 §11.2.6](https://docs.oasis-open.org/odata/odata/v4.0/odata-v4.0-part1-protocol.html#_Toc31358950).

When `false` (the default), the same request returns `200 OK` with an empty value array. That matches Restier's historical behavior and keeps the wire format friendly for clients that expect a collection shape regardless of parent state.

### When to enable it

- Your clients are strict OData v4 implementations that distinguish between "no related entities" (200 empty) and "parent doesn't exist" (404).
- You're publishing an interop surface that's validated against the OData v4 spec.

### Trade-off

Strict mode runs one extra parent-existence query per collection-nav request whose path includes a key segment. We can't tell from a deferred `IQueryable` whether a collection is empty without materializing it, so the parent check has to run unconditionally whenever strict mode is on. Don't enable this on hot read paths if you don't need it.

<Note>
Single-entity-by-key requests (e.g. `GET /Books(missing)`, `GET /Books(missing)/Publisher`, `GET /Publishers('P1')/Books(missing)`) already return `404 Not Found` unconditionally — they don't go through this toggle. Only the collection-from-missing-parent case was previously lenient.
</Note>

## DI precedence

`configureOptions` is the canonical channel for `DeepOperationSettings` and `RestierConformanceOptions`. Inside `AddRestierRoute`, the bag's instances are registered via `AddSingleton` *after* `configureRouteServices` runs, so they override any registrations of those types made from the per-route DI delegate. If you've been wiring `DeepOperationSettings` through DI in earlier Restier versions, move that configuration into `configureOptions`.

## Migration from earlier `feature/vnext` snapshots

Earlier snapshots of `feature/vnext` exposed two `AddRestierRoute` overloads with positional `useRestierBatching` and `namingConvention` parameters, plus an unprefixed convenience overload. Those are removed.

```csharp
// Old
options.AddRestierRoute<MyApi>(services => { ... });
options.AddRestierRoute<MyApi>("api", services => { ... }, useRestierBatching: false, namingConvention: RestierNamingConvention.LowerCamelCase);

// New
options.AddRestierRoute<MyApi>("", services => { ... });
options.AddRestierRoute<MyApi>("api", services => { ... }, opts =>
{
    opts.UseRestierBatching = false;
    opts.NamingConvention = RestierNamingConvention.LowerCamelCase;
});
```

The same shape applies to `IRestierApiVersioningBuilder.AddVersion` in the `Microsoft.Restier.AspNetCore.Versioning` package: the old `useRestierBatching` / `namingConvention` positional parameters are replaced by an optional `Action<RestierRouteOptions>`.
````

- [ ] **Step 2: Add the page to the Mintlify nav template**

Open `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` and find the `<MintlifyTemplate>` element. Locate the existing entries under the `guides/server/` group and insert a new item for `conformance-options` alphabetically. The exact XML shape varies — match the surrounding entries' format. The intent is that the SDK regenerates `docs.json` and includes the new page.

After the edit, regenerate:

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj -c Debug --nologo -v q
```

Expected: `Build succeeded.`. The build regenerates `docs.json`.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/conformance-options.mdx src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj src/Microsoft.Restier.Docs/docs.json
git commit -m "docs: add conformance-options guide and RestierRouteOptions migration notes"
```

---

### Task 11: Update existing docs that show old `AddRestierRoute` signatures

The doc pages that show `AddRestierRoute` (or `AddVersion`) call samples need their code blocks updated to the new shape. Most pages use the bare two-argument form and require no edit; the ones that pass `useRestierBatching` or `namingConvention` positionally do.

**Files:**
- Modify (only where the call shape requires it): the 14 `.mdx` files listed in the **File Structure** section above.

- [ ] **Step 1: Inventory which pages need edits**

Run:

```bash
grep -rn "useRestierBatching\|namingConvention:\|namingConvention," src/Microsoft.Restier.Docs/
```

The output lists every doc page that needs updating. Pages not in the output use the bare form and need no edit.

- [ ] **Step 2: For each match, migrate the call sample**

Apply the same mechanical transformation as Task 5 Step 2:

```mdx
<!-- Old -->
```csharp
options.AddRestierRoute<MyApi>("api", services => { ... }, namingConvention: RestierNamingConvention.LowerCamelCase);
```

<!-- New -->
```csharp
options.AddRestierRoute<MyApi>("api", services => { ... }, opts =>
{
    opts.NamingConvention = RestierNamingConvention.LowerCamelCase;
});
```
```

Also check `api-versioning.mdx` — if any sample shows `AddVersion(...)` with positional `useRestierBatching` or `namingConvention`, migrate it the same way (the parameters move onto `RestierRouteOptions` via the new `configureOptions` callback).

- [ ] **Step 3: Verify docs build**

Run: `dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj -c Debug --nologo -v q`
Expected: `Build succeeded.`. The DotNetDocs SDK regenerates `docs.json`.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Docs/
git commit -m "docs: migrate AddRestierRoute samples to options-bag form"
```

---

### Task 12: Final solution-wide build and integration check

This task confirms nothing was missed. If any project fails to compile, the test suite refuses to run, or the new tests fail, return to the relevant earlier task and fix.

- [ ] **Step 1: Solution-wide build**

Run: `dotnet build RESTier.slnx -c Debug --nologo`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 2: Full AspNetCore test suite (EFCore only — EF6 needs SQL Server)**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
    --filter "FullyQualifiedName~EFCore" \
    --nologo -v q
```

Expected: all tests pass across net8/net9/net10.

- [ ] **Step 3: Versioning test suite**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj \
    --nologo -v q
```

Expected: all tests pass.

- [ ] **Step 4: NSwag test suite**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj \
    --nologo -v q
```

Expected: all tests pass.

- [ ] **Step 5: Commit-message cleanup (optional)**

If interactive rebase or fixup was needed during the run, squash trivial fix-up commits into their parent tasks. Otherwise, no commit is needed for this step.

---

## Self-review checklist (already run)

- **Spec coverage:** every spec section maps to a task — new types (Tasks 1-2), API replacement (Task 3), DI precedence + bag wiring (Task 3 step 2), controller guard (Task 8), versioning migration (Task 7), test-helper refactor + new tests (Tasks 4, 9), docs (Tasks 10-11), call-site migration (Tasks 5-6), solution build (Task 12).
- **Placeholders:** none.
- **Type consistency:** `RestierConformanceOptions` and `RestierRouteOptions` names and property names match across the spec, the controller change, the test, and the docs page. `PendingVersionRegistration.ConfigureOptions` and the reflection's `pending.ConfigureOptions` lookup line up. The `4`-parameter reflection target matches the new public overload's actual parameter count (extension `this` + `routePrefix` + `configureRouteServices` + `configureOptions`).
