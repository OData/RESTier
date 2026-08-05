# API Versioning for RESTier — Design Spec

**Date:** 2026-05-03
**Status:** Draft
**Tracks:** [OData/RESTier#662](https://github.com/OData/RESTier/issues/662)
**Related external work:** [Asp.Versioning.OData](https://github.com/dotnet/aspnet-api-versioning)

## Goal

Make RESTier a first-class consumer of [`Asp.Versioning`](https://github.com/dotnet/aspnet-api-versioning) for **URL-segment** API versioning, so users can register multiple versions of a Restier API in the same host with versioned `$metadata`, versioned OpenAPI documents, and standard version-discovery response headers — with no changes to the existing `Microsoft.Restier.AspNetCore` request pipeline.

## Non-goals (initial release)

- **Header / query-string / media-type versioning.** RESTier's dynamic route transformer keys off the URL prefix; supporting other version readers requires a deeper rewrite of route resolution and is deferred.
- **EDM-level deprecation annotations.** Emitting `OData-Deprecation` annotations on entity sets/properties overlaps with the in-flight OpenAPI annotation work and deserves its own design.
- **Versioned `Microsoft.Restier.EntityFramework` (EF6) bindings.** The same patterns work without special glue; documented as such.
- **Auto-default-version routing** at the bare prefix (e.g., serving `/api` from V2 implicitly). Users opt in by registering a non-versioned route at the bare prefix themselves.
- **Automatic 410/Gone after sunset.** Sunset header is emitted; enforcement is a future enhancement.

## Background

### What RESTier currently does

`RestierODataOptionsExtensions.AddRestierRoute<TApi>(prefix, configureRouteServices, …)` registers an OData route at `prefix`:
- builds the EDM from `RestierWebApiModelBuilder` + `RestierWebApiModelExtender` + `RestierWebApiOperationModelBuilder` + `ConventionBasedAnnotationModelBuilder`, all driven by `TApi : ApiBase`;
- registers the per-route DI container via `ODataOptions.AddRouteComponents(prefix, model, services => …)`;
- marks the container with `RestierRouteMarker` so `MapRestier()` can identify Restier routes among all OData routes.

`MapRestier()` then iterates `ODataOptions.RouteComponents` and registers a `DynamicRouteValueTransformer` (`RestierRouteValueTransformer`) per Restier prefix. The transformer parses OData URLs at request time, fills the OData feature on `HttpContext`, and dispatches to the single generic `RestierController`. There is no user-written, attribute-decorated controller.

### Implication for versioning

The existing per-prefix model already accommodates URL-segment versioning mechanically — `NorthwindApiV1` at `api/v1`, `NorthwindApiV2` at `api/v2` works today without code changes. What's missing is integration with the `Asp.Versioning` ecosystem:
- `IApiVersionDescriptionProvider` doesn't know about RESTier routes
- `[ApiVersion]` attributes on `ApiBase` types are ignored
- NSwag/Swagger documents are named by full route prefix instead of by version
- Standard `api-supported-versions`, `api-deprecated-versions`, `Sunset` headers aren't emitted

This design fills that gap with an opt-in package.

## High-level architecture

```
ASP.NET Core MVC + Asp.Versioning  (user-configured AddApiVersioning())
                                                                       │
Microsoft.Restier.AspNetCore.Versioning  (NEW, opt-in package)         │
  • services.AddRestierApiVersioning(builder => builder                │
        .AddVersion<TApi>(basePrefix, configureRouteServices, ...))    │
  • Registers RestierApiVersionRegistry (singleton, IRestierApiVersionRegistry)
  • Registers IConfigureOptions<ODataOptions> that, when ODataOptions  │
    is materialized, iterates collected versions and calls             │
    oDataOptions.AddRestierRoute<TApi>(composedPrefix, ...)            │
  • Registers IApiVersionDescriptionProvider adapter                   ▼
  • Provides UseRestierVersionHeaders() middleware

Microsoft.Restier.AspNetCore  (BEHAVIOR UNCHANGED — adds two type-only contracts)
  • AddRestierRoute<TApi>(prefix, ...)  — works as today
  • IRestierApiVersionRegistry, RestierApiVersionDescriptor (read-only contracts)
```

Two existing packages get registry-aware updates:

- **`Microsoft.Restier.AspNetCore.NSwag`** — when the registry is in DI, OpenAPI documents are looked up by version group name (`v1`, `v2`) instead of route prefix; the **UI helpers** `UseRestierReDoc` and `UseRestierNSwagUI` enumerate versions from the registry instead of route prefixes. Falls back to prefix-based behavior when the registry is absent (full back-compat).
- **`Microsoft.Restier.AspNetCore.Swagger`** — mirrored change to the document generator and any UI helpers that enumerate prefixes.

A new sample, **`Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore`**, demonstrates end-to-end wiring with a real V1/V2 delta.

## Public API

### Registration

```csharp
// Attribute-driven (canonical path)
[ApiVersion("1.0", Deprecated = true)]
public class NorthwindApiV1 : EntityFrameworkApi<NorthwindContextV1> { }

[ApiVersion("2.0")]
public class NorthwindApiV2 : EntityFrameworkApi<NorthwindContextV2> { }

services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(2, 0);
    o.ReportApiVersions = true;
    o.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer();

services
    .AddControllers()
    .AddRestier(options =>
    {
        options.Select().Expand().Filter().OrderBy().SetMaxTop(100).Count();
        // No version-specific calls here.
    });

services.AddRestierApiVersioning(builder => builder
    .AddVersion<NorthwindApiV1>("api", restierServices =>
    {
        restierServices.AddEFCoreProviderServices<NorthwindContextV1>(...);
    })
    .AddVersion<NorthwindApiV2>("api", restierServices =>
    {
        restierServices.AddEFCoreProviderServices<NorthwindContextV2>(...);
    },
    options => options.SunsetDate = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero)));

// in Configure(...)
app.UseRouting();
app.UseRestierVersionHeaders();   // before MapRestier
app.UseEndpoints(e =>
{
    e.MapControllers();
    e.MapRestier();
});
```

**Why `services.AddRestierApiVersioning(...)` rather than calls inside the `AddRestier` lambda?** RESTier passes the `AddRestier` lambda through `IMvcBuilder.AddOData(setupAction)`, which OData registers as `services.Configure<ODataOptions>(setupAction)`. That lambda runs **lazily**, when `IOptions<ODataOptions>.Value` is first materialized — typically inside `MapRestier()`, well after `ConfigureServices` finishes. Registering versioned routes from inside that lambda would force any registry-population mechanism to either keep a global static alive past Startup (fragile) or pass an `IServiceProvider` parameter through the lambda. Putting versioning on `IServiceCollection` and using `IConfigureOptions<ODataOptions>` with constructor-injected `IServiceProvider` resolves both problems cleanly.

### Extension surface

```csharp
namespace Microsoft.Restier.AspNetCore.Versioning;

public static class RestierApiVersioningServiceCollectionExtensions
{
    /// <summary>
    /// Registers Restier API versioning: the <see cref="IRestierApiVersionRegistry"/> singleton,
    /// the <see cref="IApiVersionDescriptionProvider"/> adapter, and an
    /// <see cref="IConfigureOptions{ODataOptions}"/> that adds versioned Restier routes when
    /// <c>ODataOptions</c> is materialized.
    /// </summary>
    public static IServiceCollection AddRestierApiVersioning(
        this IServiceCollection services,
        Action<IRestierApiVersioningBuilder> configure);
}

public interface IRestierApiVersioningBuilder
{
    // Attribute-driven: reads every [ApiVersion] attribute on TApi.
    // Because [ApiVersion] supports AllowMultiple, a single class can declare more than one
    // version this way — covering the "one ApiBase serves multiple versions" case without
    // a dedicated overload.
    IRestierApiVersioningBuilder AddVersion<TApi>(
        string basePrefix,
        Action<IServiceCollection> configureRouteServices,
        Action<RestierVersioningOptions> configureVersioning = null,
        bool useRestierBatching = true,
        RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
        where TApi : ApiBase;

    // Imperative — for users who don't want [ApiVersion] on their class.
    // Carries an explicit `deprecated` flag because no attribute is read.
    IRestierApiVersioningBuilder AddVersion<TApi>(
        ApiVersion apiVersion,
        bool deprecated,
        string basePrefix,
        Action<IServiceCollection> configureRouteServices,
        Action<RestierVersioningOptions> configureVersioning = null,
        bool useRestierBatching = true,
        RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
        where TApi : ApiBase;
}

public sealed class RestierVersioningOptions
{
    /// <summary>How to render an ApiVersion as a URL segment. Defaults to <see cref="ApiVersionSegmentFormatters.Major"/>.</summary>
    public Func<ApiVersion, string> SegmentFormatter { get; set; } = ApiVersionSegmentFormatters.Major;

    /// <summary>Override the composed route prefix entirely (skips SegmentFormatter and basePrefix composition).</summary>
    public string ExplicitRoutePrefix { get; set; }

    /// <summary>
    /// Optional sunset date for this version, surfaced as the <c>Sunset</c> response header
    /// by <c>UseRestierVersionHeaders</c>. <c>[ApiVersion]</c> does not carry sunset metadata,
    /// so it must be configured here per call. (Future: integrate with
    /// <c>Asp.Versioning.IPolicyManager</c> for policy-driven sunset.)
    /// </summary>
    public DateTimeOffset? SunsetDate { get; set; }

    /// <summary>
    /// Optional formatter that produces the OpenAPI document <c>GroupName</c> for this version.
    /// When null (default), <see cref="SegmentFormatter"/> is used (so a v1 segment also produces
    /// the "v1" group name). When you register multiple logical APIs at different
    /// <c>basePrefix</c>es that share a version, set this on each call to disambiguate
    /// (e.g., <c>opts.GroupNameFormatter = v =&gt; $"orders-v{v.MajorVersion}"</c>); the
    /// configurator throws <see cref="InvalidOperationException"/> if two descriptors would
    /// have the same GroupName.
    /// </summary>
    public Func<ApiVersion, string> GroupNameFormatter { get; set; }
}

public static class ApiVersionSegmentFormatters
{
    public static Func<ApiVersion, string> Major      { get; } = v => $"v{v.MajorVersion}";
    public static Func<ApiVersion, string> MajorMinor { get; } = v => $"v{v.MajorVersion}.{v.MinorVersion}";
}

public static class RestierVersionedApplicationBuilderExtensions
{
    /// <summary>Adds a middleware that emits api-supported-versions / api-deprecated-versions / Sunset headers on Restier responses.</summary>
    public static IApplicationBuilder UseRestierVersionHeaders(this IApplicationBuilder app);
}
```

### Registry types

The **read-only contract** lives in **`Microsoft.Restier.AspNetCore`** so NSwag/Swagger consume it without picking up the `Asp.Versioning` dependency. The descriptor exposes only primitive metadata (string-formatted version), not the typed `ApiVersion` — keeping the base package free of `Asp.Versioning.Abstractions`.

```csharp
namespace Microsoft.Restier.AspNetCore.Versioning;   // namespace alias inside the AspNetCore package

public interface IRestierApiVersionRegistry
{
    IReadOnlyList<RestierApiVersionDescriptor> Descriptors { get; }
    RestierApiVersionDescriptor FindByPrefix(string routePrefix);
    RestierApiVersionDescriptor FindByGroupName(string groupName);

    /// <summary>
    /// Returns descriptors that share the supplied logical API group key
    /// (the <c>basePrefix</c> passed to <c>AddVersion</c>). Used by the
    /// headers middleware so api-supported-versions / api-deprecated-versions
    /// reflect only the API the request belongs to, not unrelated APIs at
    /// other prefixes.
    /// </summary>
    IReadOnlyList<RestierApiVersionDescriptor> FindByBasePrefix(string basePrefix);
}

public sealed class RestierApiVersionDescriptor
{
    public string Version { get; }           // "1.0", "2.0", etc.
    public string BasePrefix { get; }        // logical API group, e.g. "api" or "orders"
    public string RoutePrefix { get; }       // composed (e.g., "api/v1")
    public Type ApiType { get; }             // e.g., typeof(NorthwindApiV1)
    public bool IsDeprecated { get; }
    public string GroupName { get; }         // e.g., "v1" — used as OpenAPI doc name
    public DateTimeOffset? SunsetDate { get; }
}
```

`BasePrefix` is the logical API key. Two routes registered with the same `basePrefix` belong to the same logical API; headers reported on one are reported across the whole group.

`BasePrefix` is taken from the `basePrefix` argument the user passes to `AddVersion`, regardless of whether `RestierVersioningOptions.ExplicitRoutePrefix` is set. The configurator **normalizes** it by trimming a trailing `/` so `AddVersion(..., "api")` and `AddVersion(..., "api/")` produce the same `BasePrefix` and group together for header reporting. The user's explicit declaration of the logical API key is what matters; the trailing-slash normalization keeps it unambiguous.

`GroupName` defaults to `RestierVersioningOptions.SegmentFormatter(version)` (e.g., `"v1"` for the major-only formatter). It can be overridden per-version via `RestierVersioningOptions.GroupNameFormatter`. The configurator detects collisions: if two descriptors would share a `GroupName`, it throws `InvalidOperationException` with guidance pointing to `GroupNameFormatter`. Multi-API setups that share a version segment (e.g., `orders/v1` and `inventory/v1`) MUST disambiguate via `GroupNameFormatter` (e.g., `v => $"orders-v{v.MajorVersion}"`).

The **concrete implementation** and a strongly-typed view live in **`Microsoft.Restier.AspNetCore.Versioning`** (which references `Asp.Versioning.Mvc` / `Asp.Versioning.Mvc.ApiExplorer`, NOT `Asp.Versioning.OData` — see Tech Stack note below):

```csharp
namespace Microsoft.Restier.AspNetCore.Versioning;

internal sealed class RestierApiVersionRegistry : IRestierApiVersionRegistry
{
    public RestierApiVersionDescriptor Add(
        ApiVersion apiVersion, string routePrefix, Type apiType,
        bool deprecated, string groupName, DateTimeOffset? sunset);

    // IRestierApiVersionRegistry members

    public RestierApiVersionDescriptor FindByVersion(ApiVersion apiVersion);  // typed extension
}
```

NSwag and Swagger consume `IRestierApiVersionRegistry` (resolved via `IServiceProvider.GetService`, null-tolerant). The Versioning package registers the concrete `RestierApiVersionRegistry` against both the interface and itself.

## Internal components

### `Microsoft.Restier.AspNetCore.Versioning` (new project)

| File | Purpose |
|------|---------|
| `Extensions/RestierApiVersioningServiceCollectionExtensions.cs` | `services.AddRestierApiVersioning(configure)` — entry point; registers the registry, the `IApiVersionDescriptionProvider` adapter, and the `IConfigureOptions<ODataOptions>` that adds versioned routes. |
| `Extensions/RestierVersionedApplicationBuilderExtensions.cs` | `UseRestierVersionHeaders()`. |
| `IRestierApiVersioningBuilder.cs` | Public builder contract used by the `configure` delegate. |
| `Internal/RestierApiVersioningBuilder.cs` | Concrete builder; accumulates pending version registrations and a `Configure(ODataOptions, IServiceProvider)` method invoked from `IConfigureOptions<ODataOptions>`. |
| `Internal/RestierApiVersioningOptionsConfigurator.cs` | `IConfigureOptions<ODataOptions>` that resolves the registry and the builder from DI and applies all pending registrations to the materialized `ODataOptions`. |
| `RestierApiVersionRegistry.cs` | Internal concrete `IRestierApiVersionRegistry` implementation; mutable from inside the package. |
| `RestierVersioningOptions.cs` | Per-route options (segment formatter, explicit prefix, sunset date). |
| `ApiVersionSegmentFormatters.cs` | Built-in formatters. |
| `Internal/ApiVersionAttributeReader.cs` | Reads `[ApiVersion]` (version + deprecated). Sunset is **not** read here — it's per-call via `RestierVersioningOptions.SunsetDate`. |
| `Internal/RestierApiVersionDescriptionProvider.cs` | `IApiVersionDescriptionProvider` adapter sourced from the registry. **Depends on `IOptions<ODataOptions>`** and reads `.Value` before reading the registry, forcing the `IConfigureOptions<ODataOptions>` pipeline to run (and thereby populating the registry) on the first description-provider read. This avoids returning an empty list when ApiExplorer or Swashbuckle resolves the provider during host startup, before any HTTP request has touched `MapRestier`. |
| `Middleware/RestierVersionHeadersMiddleware.cs` | Adds version-discovery headers based on the matched prefix using `PathString.StartsWithSegments` (segment-boundary safe; `PathBase`-aware). |

Targets: `net8.0;net9.0;net10.0` (matching `Microsoft.Restier.AspNetCore` and the rest of the AspNetCore-family packages — no `net48`, since these are ASP.NET Core packages).

Dependencies: `Microsoft.Restier.AspNetCore`, `Asp.Versioning.Mvc`, `Asp.Versioning.Mvc.ApiExplorer`.

> **Tech-stack note (rationale for the dependency choice):** the spec deliberately does **not** depend on `Asp.Versioning.OData`. That package pins `Microsoft.AspNetCore.OData` 8.x, which conflicts with RESTier's OData 9.x. RESTier also doesn't need its types: we don't use `ODataModelBuilder` or `VersionedODataModelBuilder` (RESTier builds EDMs from `ApiBase` conventions). All version-reading and ApiExplorer integration we need lives in the AspNetCore-version-agnostic `Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer` packages.

### `Microsoft.Restier.AspNetCore.NSwag` (changes)

Three integration points must be made registry-aware. All three resolve `IRestierApiVersionRegistry` via `IServiceProvider.GetService<IRestierApiVersionRegistry>()` (null-tolerant).

**"Registry effectively absent" rule.** The fallback to existing prefix-based behavior fires when the registry is **null OR has no descriptors** — not only when the service is unregistered. This handles the case where the user added the Versioning package but never registered any versions; the helpers continue to behave as today instead of producing empty UI dropdowns.

**Mixed versioned + unversioned routes (UI enumeration only).** When the registry has descriptors AND the user also has unversioned Restier routes registered via `AddRestierRoute<TApi>`, UI helpers MUST emit one entry per registry descriptor (using `GroupName` as the doc name) AND one entry per route prefix in `ODataOptions.GetRestierRoutePrefixes()` that does not match any descriptor's `RoutePrefix`. Result: in an app with `api/v1`, `api/v2`, and an unversioned route at `legacy`, the dropdown shows `v1`, `v2`, and `legacy` (or `default` if the unversioned prefix is empty). Document and middleware paths still resolve correctly because `RestierOpenApiDocumentGenerator` already accepts both group-name and route-prefix lookups.

**Materialization invariant (mandatory for all integration points that read the registry directly):** before reading any descriptor, the integration point MUST first read `IOptions<ODataOptions>.Value` (resolved from the same scope). This deterministically runs the `IConfigureOptions<ODataOptions>` pipeline that populates the registry, so registry reads can't observe an empty list. This invariant applies to UI helpers running during middleware setup (before any HTTP request), to the description provider, and to the headers middleware. It is not duplicate work — `IOptions<T>.Value` caches.

| File | Change |
|------|--------|
| `RestierOpenApiDocumentGenerator.cs` | Accept optional registry. Document name `"v1"` → look up via `registry.FindByGroupName("v1")` to get the route prefix; generate from that EDM. Falls back to prefix-based lookup when the registry is absent or has no descriptors, AND for any document name that doesn't resolve via the registry (i.e., unversioned routes intermingled with versioned ones). The generator is invoked from `RestierOpenApiMiddleware`, which already resolves `IOptions<ODataOptions>.Value` for its own work, so the materialization invariant is satisfied without an extra touch here. |
| `RestierOpenApiMiddleware.cs` | Path pattern unchanged (`/openapi/{documentName}/openapi.json`). The `documentName` may now be a version group (`"v1"`) or a route prefix; the generator handles both. |
| `Extensions/IApplicationBuilderExtensions.cs` (`UseRestierReDoc`, `UseRestierNSwagUI`) | These run during `Configure(...)`, **before any HTTP request**. They MUST resolve `IOptions<ODataOptions>.Value` first to satisfy the materialization invariant (the existing code already does so to enumerate prefixes — keep that line and read the registry afterward). When the registry has descriptors, enumerate (a) every version from the registry as `GroupName` → `/openapi/{GroupName}/openapi.json`, plus (b) every prefix in `ODataOptions.GetRestierRoutePrefixes()` that is NOT covered by any descriptor's `RoutePrefix`, mapped to the existing prefix-or-`default` document name. When the registry is absent or empty, the existing prefix-enumeration path is used unchanged. |

### `Microsoft.Restier.AspNetCore.Swagger` (mirrored changes)

Same integration points: document generator, middleware, and any UI helpers that enumerate prefixes — all subject to the same materialization invariant. Exact files to be inventoried during plan-writing.

### `Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore` (new sample)

Two API classes with a real surface delta:
- `NorthwindApiV1` — `[ApiVersion("1.0", Deprecated = true)]`. Customers (no `Email`), Orders.
- `NorthwindApiV2` — `[ApiVersion("2.0")]`. Customers (with `Email`), Orders, new `OrderShipments` set, deprecates the legacy `GetTopCustomers` function.

Two `DbContext` subclasses (`NorthwindContextV1`, `NorthwindContextV2`) hide V2-only members from V1's model via `OnModelCreating` `Ignore(...)` calls — the simplest pattern that matches RESTier's "the type defines the surface" philosophy.

`Startup.cs` wires `AddApiVersioning` → `AddControllers().AddRestier(options => …)` → `AddRestierApiVersioning(builder => builder.AddVersion<NorthwindApiV1>(...).AddVersion<NorthwindApiV2>(...))` → `UseRestierVersionHeaders` → `MapRestier`. NSwag UI dropdown lists `v1` and `v2`.

### `Microsoft.Restier.AspNetCore` — minimal additions

Two type-only additions, no behavior changes:

- `IRestierApiVersionRegistry` (interface)
- `RestierApiVersionDescriptor` (sealed class with read-only properties)

Both live in a new `Microsoft.Restier.AspNetCore.Versioning` namespace within the package. No new dependencies — they reference only `System.*`.

**Why here, not in the Versioning package?** NSwag and Swagger need to consume the registry contract without taking a dependency on `Asp.Versioning.Mvc`. Putting the read-only contract in the base package (which both already reference) is the simplest path. The Versioning package owns the mutable concrete implementation.

### Mechanism for populating the registry

`services.AddRestierApiVersioning(configure)` does **not** require modifying or replacing `AddRestier`. It works as follows:

1. **`AddRestierApiVersioning(configure)` runs synchronously during `ConfigureServices`:**
   - **Find or create** the `RestierApiVersioningBuilder` instance: enumerate `services` looking for an existing `ServiceDescriptor` whose `ServiceType` is `RestierApiVersioningBuilder`. If present, retrieve the `ImplementationInstance`. If absent, instantiate a new builder and register it with `services.AddSingleton<RestierApiVersioningBuilder>(builder)` (note: an instance registration so the same instance is observed both during `ConfigureServices` and at resolution time). **Do not** use `TryAddSingleton` for the builder — the find-or-create lookup already enforces single-instance, and `TryAddSingleton` would silently discard subsequent additions.
   - Invoke the user's `configure(builder)` delegate, which calls `builder.AddVersion<TApi>(...)` one or more times. Each call appends a `PendingVersionRegistration` record to the builder's internal list.
   - The remaining service registrations (registry, description provider, options configurator) use `TryAddSingleton` / `TryAddEnumerable` — they're naturally idempotent across multiple `AddRestierApiVersioning` calls because nothing about their identity changes from one call to the next:
     - `RestierApiVersionRegistry` registered as a singleton against itself and `IRestierApiVersionRegistry`.
     - `IApiVersionDescriptionProvider` adapter.
     - `IConfigureOptions<ODataOptions>` (`RestierApiVersioningOptionsConfigurator`) whose `Configure(ODataOptions options)` method resolves the same builder and registry from constructor-injected `IServiceProvider`, iterates the builder's pending registrations once, and for each one composes the prefix, calls `options.AddRestierRoute<TApi>(prefix, configureRouteServices, ...)`, and adds a descriptor to the registry. The configurator guards with a "ran already" flag so a re-materialization (rare but possible) does not double-register.
2. **When `IOptions<ODataOptions>.Value` is first materialized** (from `MapRestier`, `RestierOpenApiMiddleware`, etc.), all `IConfigureOptions<ODataOptions>` instances run. Both the user's `AddRestier` lambda and our `RestierApiVersioningOptionsConfigurator` execute against the same `ODataOptions` instance. Order doesn't matter because they touch independent route prefixes.
3. **No global statics, no Startup-time bridges, no new lambda parameters.** The registry is populated at the same moment `AddRestierRoute` is — when `ODataOptions` materializes — using only DI-resolvable services.
4. **Materialization invariant (mandatory for any consumer that reads `IRestierApiVersionRegistry` directly).** The registry is only populated when `IOptions<ODataOptions>.Value` first materializes. ApiExplorer / Swashbuckle / NSwag may resolve `IApiVersionDescriptionProvider` (or the registry) during host startup, and UI helpers (`UseRestierReDoc`, `UseRestierNSwagUI`, Swagger equivalents) run during `Configure(...)` — both happen **before any HTTP request**. To prevent any consumer from observing an empty registry, the invariant is:

   > **Every component that reads `IRestierApiVersionRegistry` MUST first resolve `IOptions<ODataOptions>.Value` from the same scope.** `IOptions<T>.Value` caches, so the cost is paid once.

   Components in scope (and how they comply):
   - `RestierApiVersionDescriptionProvider` — constructor takes `IOptions<ODataOptions>` and the registry; reads `.Value` before reading the registry.
   - `RestierVersionHeadersMiddleware` — already resolves `IOptions<ODataOptions>.Value` to look up route prefixes; reads the registry afterward.
   - `UseRestierReDoc` / `UseRestierNSwagUI` (NSwag) and Swagger UI helpers — already resolve `IOptions<ODataOptions>.Value` to enumerate prefixes; if a registry is present, read it afterward in the same method.
   - `RestierOpenApiMiddleware` (NSwag) and Swagger middleware equivalents — already resolve `IOptions<ODataOptions>.Value`; registry reads piggyback safely.

   Anything new added in the future that reads the registry directly is required to follow the same pattern. Tests assert the invariant by constructing a host, resolving the description provider (and exercising the UI helper paths) without making any HTTP request, and asserting the descriptors are non-empty.

If the user calls `AddRestierApiVersioning` more than once, each call locates the existing builder via the `ServiceDescriptor` lookup described in step 1 and appends additional `PendingVersionRegistration` records. The supporting service registrations are idempotent. A single `RestierApiVersioningOptionsConfigurator` runs once when `ODataOptions` materializes, applies all accumulated pending registrations, and marks itself complete.

## Data flow

### Registration time

```
ConfigureServices:
    services.AddRestierApiVersioning(b => b
        .AddVersion<NorthwindApiV1>("api", svc => {...})
        .AddVersion<NorthwindApiV2>("api", svc => {...}))
    │
    ├─ Register RestierApiVersionRegistry singleton (idempotent)
    ├─ Register IApiVersionDescriptionProvider adapter
    ├─ Register IConfigureOptions<ODataOptions> (RestierApiVersioningOptionsConfigurator)
    └─ Builder.AddVersion calls just append to a pending-registrations list

ODataOptions materialized (lazy, e.g., from MapRestier):
    IConfigureOptions<ODataOptions>.Configure(options) runs
    │
    └─ For each pending registration:
        ├─ ApiVersionAttributeReader.Read(typeof(TApi))  // attribute path
        │   → ApiVersion(1,0), IsDeprecated=true
        │   (sunset comes from RestierVersioningOptions.SunsetDate, not the attribute)
        │
        ├─ Compose prefix: "api" + "/" + segmentFormatter(v) → "api/v1"
        │
        ├─ Append RestierApiVersionDescriptor to registry
        │
        └─ options.AddRestierRoute<TApi>(
             "api/v1", configureRouteServices, useRestierBatching, namingConvention)
```

### Request time — versioned OData call

```
GET /api/v1/Customers('ALFKI')
    │
    ├─ ASP.NET routing matches the dynamic catch-all registered at "api/v1"
    │   by MapRestier (UNCHANGED)
    │
    ├─ RestierRouteValueTransformer.TransformAsync (UNCHANGED)
    │   parses, dispatches to RestierController.Get
    │
    ├─ RestierVersionHeadersMiddleware (registered via UseRestierVersionHeaders,
    │  before MapRestier; runs on the response side):
    │   - normalize request path: strip PathBase using HttpContext.Request.PathBase
    │   - for each registry descriptor, test
    │       request.Path.StartsWithSegments(new PathString("/" + descriptor.RoutePrefix))
    │     This is segment-boundary safe: "/api/v1" matches "/api/v1/Customers" and
    │     "/api/v1" itself, but does NOT match "/api/v10/anything".
    │   - longest-prefix match wins (so a non-versioned "api" route doesn't shadow "api/v1")
    │   - if matched, look up the matched descriptor's BasePrefix (e.g., "api"). Use
    │     registry.FindByBasePrefix(basePrefix) to compute api-supported-versions /
    │     api-deprecated-versions over ONLY the descriptors in that logical API group.
    │     Versions from unrelated APIs registered at other base prefixes (e.g.,
    │     "orders" vs "inventory") do not leak into each other's headers.
    │   - emit Sunset only if the matched descriptor.SunsetDate is set
    │   - never overwrite headers already present
    │
    └─ Response returned with versioning headers.
```

### Request time — versioned OpenAPI doc

```
GET /openapi/v1/openapi.json
    │
    ├─ RestierOpenApiMiddleware:
    │   - documentName = "v1"
    │   - if registry resolved: descriptor = registry.FindByGroupName("v1")
    │     routePrefix = descriptor.RoutePrefix  // "api/v1"
    │   - else: routePrefix = documentName  // legacy fallback
    │
    └─ Generate OpenAPI from the EDM at that prefix → return JSON
```

### Asp.Versioning header reporting overlap

Asp.Versioning's `ReportApiVersions = true` reports headers via MVC's response filters; RESTier requests are dispatched by the dynamic route transformer in a way Asp.Versioning's filters won't always catch. `RestierVersionHeadersMiddleware` fills that gap on Restier-prefixed responses. The two are not in conflict — the middleware checks for already-set headers and won't duplicate them. Documented in the guide.

## Error handling and edge cases

| Scenario | Behavior |
|----------|----------|
| `services.AddRestierApiVersioning` not called but versioning expected | Versioned routes simply aren't registered (the `IConfigureOptions<ODataOptions>` is absent). Documentation makes this prerequisite explicit. |
| `[ApiVersion]` missing on attribute-driven path | `InvalidOperationException` when the `IConfigureOptions<ODataOptions>` runs (i.e., on first request / first `IOptions<ODataOptions>.Value` access), with the type name and a one-line fix (the imperative overload). Throws are surfaced as InvalidOperationException at startup-equivalent time, not as runtime 500s on user requests, because the options-configurator pipeline propagates these to the host startup path on first materialization. |
| Same `(ApiVersion, basePrefix)` registered twice | `InvalidOperationException` listing both API types. (`basePrefix` is captured on `RestierApiVersionDescriptor.BasePrefix`; the configurator's dedup check compares against existing descriptors.) `basePrefix` is normalized — `"api"` and `"api/"` are treated as the same. |
| Two descriptors would share a `GroupName` (e.g., `orders/v1` + `inventory/v1` both default to `"v1"`) | `InvalidOperationException` naming both descriptors and pointing to `RestierVersioningOptions.GroupNameFormatter` as the resolution. |
| Different versions colliding on the composed prefix (custom formatter collision) | `InvalidOperationException` naming both versions and the colliding prefix. |
| `[ApiVersion]` declares state conflicting with imperative override | Imperative call wins. Documented in XML doc. |
| Multiple versions on one `ApiBase` | Each becomes an independent route registration; descriptors are independent but share `ApiType`. |
| Request to `/api` (no version segment) | 404 unless the user registers a non-versioned `AddRestierRoute<TApi>` at `"api"` themselves. |
| Versioning package present, no versioned routes | Empty registry; NSwag/Swagger glue falls back to existing prefix-based behavior (per the "registry effectively absent" rule — null OR empty triggers fallback); headers middleware no-ops. |
| Versioning package absent, NSwag present | Existing prefix-based behavior preserved. |
| Mixed versioned and unversioned Restier routes in the same app | UI helpers emit one entry per registry descriptor (by `GroupName`) plus one entry per `GetRestierRoutePrefixes()` prefix not represented in the registry. Both versioned and unversioned routes appear in the dropdown. |
| Two logical APIs at different `basePrefix`es (e.g., `orders` and `inventory`) each with multiple versions | Headers middleware reports `api-supported-versions` / `api-deprecated-versions` for the matched logical API only — `/orders/v1` returns Orders' versions; `/inventory/v1` returns Inventory's. No cross-leak. **OpenAPI docs** require disambiguated `GroupNameFormatter` per call (e.g., `orders-v1`/`inventory-v1`); without it the configurator throws (see GroupName-collision row above). |
| Batching at `/api/v1/$batch` | Works automatically: each versioned route gets its own `RestierBatchHandler { PrefixName = "api/v1" }` from the existing `AddRestierRoute`. |
| Sunset date in the past | `Sunset` header still emitted; no automatic 410. Future enhancement. |
| `UseRestierVersionHeaders` not registered | No exception; versioning routes simply don't carry the headers. |
| Request path containing a "look-alike" prefix (e.g., `/api/v10/...` when only `api/v1` is registered) | Middleware does not match (segment-boundary check). No headers attached. The `/api/v10/...` request is handled by the underlying routing in the normal way (404 if no route matches). |
| Application has a non-empty `PathBase` (reverse proxy) | Middleware strips `HttpContext.Request.PathBase` before applying the StartsWithSegments check, so prefixes match against the application-relative path consistent with how `MapRestier` routes are mounted. |

## Testing strategy

### Unit tests — `Microsoft.Restier.Tests.AspNetCore.Versioning` (new project)

- `ApiVersionAttributeReader` — reads major/minor; reads deprecated flag; throws when `[ApiVersion]` is missing; explicitly does NOT read sunset (sunset comes from `RestierVersioningOptions.SunsetDate`).
- `RestierApiVersionRegistry` — add/find by version, prefix, group; collision detection.
- `ApiVersionSegmentFormatters` — `Major`, `MajorMinor`, custom delegate produce expected segments for representative versions (`1.0`, `2.1`, `1-Beta`).
- `RestierApiVersioningServiceCollectionExtensions` / `RestierApiVersioningBuilder` — registers the registry, builder, `IConfigureOptions<ODataOptions>`, and `IApiVersionDescriptionProvider` adapter; multiple `AddRestierApiVersioning` calls are idempotent for service registrations and append for version registrations. **Test:** call `AddRestierApiVersioning` twice with one version each; build the host; assert the registry contains both versions and that exactly one `RestierApiVersioningBuilder` `ServiceDescriptor` exists in the collection. Inverse test: with `TryAddSingleton` semantics naively used for the builder, the second-call version would be lost — guard against regressions.
- `RestierApiVersionDescriptionProvider` — when resolved before any request, reading `Descriptions` populates the registry (via `IOptions<ODataOptions>.Value`) and returns the expected entries. Test: build a host, resolve the provider, read descriptions without making any HTTP request, assert `v1`/`v2` are present.
- `RestierApiVersioningOptionsConfigurator` — when invoked against a real `ODataOptions`, composes correct prefix; calls underlying `AddRestierRoute`; populates registry; rejects duplicate `(ApiVersion, BasePrefix)` pairs; rejects duplicate `GroupName`s with guidance; **normalizes basePrefix** (trims trailing `/`); honors `ExplicitRoutePrefix`; honors `GroupNameFormatter` overrides; imperative overload bypasses attribute reader.
- `RestierApiVersionDescriptionProvider` — surfaces registry entries with correct group names and deprecated flags.
- `RestierVersionHeadersMiddleware` — adds expected headers for matched prefix; no-ops for unmatched paths; emits `Sunset` only when set; doesn't duplicate headers already present; **boundary cases**: `/api/v10/...` does NOT match `api/v1`; `/api/v1` (exact) matches; `/api/v1/$metadata` matches; `PathBase` is stripped before matching. **Group isolation:** with two logical APIs registered (`orders/v1`, `orders/v2`, `inventory/v1`), a request to `/orders/v1` reports only `orders` versions in `api-supported-versions`; a request to `/inventory/v1` reports only `inventory`'s. Verified by direct middleware test.

### Integration tests — same project, `IntegrationTests/`

Breakdance-style in-memory host with two versioned APIs:

| Scenario | Assertion |
|----------|-----------|
| `GET /api/v1/$metadata` | EDM matches V1 surface; V2-only members absent. |
| `GET /api/v2/$metadata` | EDM matches V2 surface. |
| `GET /api/v1/Customers` | 200; uses V1 entity set. |
| `GET /api/v3/Customers` | 404 (no such version). |
| `POST /api/v1/$batch` (one inner GET Customers) | 200; routed to V1. |
| `POST /api/v2/$batch` (one inner GET Customers) | 200; routed to V2. |
| Any 200 from a versioned route | Carries `api-supported-versions` and `api-deprecated-versions` headers. |
| V1 (deprecated) response | Carries `Sunset` if a sunset date is configured. |
| `GET /openapi/v1/openapi.json` | Returns V1-shaped OpenAPI; `info.version == "1.0"`. |
| `GET /openapi/v2/openapi.json` | Returns V2-shaped OpenAPI. |
| `GET /openapi/api/v1/openapi.json` (legacy path) | Falls back to prefix lookup; returns V1 doc. |
| NSwag UI (`/swagger`) listing | Dropdown shows `v1`/`v2` (registry-driven), not `api/v1`/`api/v2`. |
| ReDoc paths | One ReDoc instance per version, mounted at `/redoc/v1`, `/redoc/v2`. |
| Headers boundary | `GET /api/v10/anything` returns 404 with no `api-supported-versions` header (no segment-boundary collision with `/api/v1`). `GET /api/v1` (exact) carries headers. |
| Reverse-proxy `PathBase` | App mounted under `/odata-svc` (`PathBase="/odata-svc"`); `GET /odata-svc/api/v1/Customers` carries headers (PathBase stripped before matching). |

### Existing test projects

- `Microsoft.Restier.Tests.AspNetCore` — one regression test confirming `MapRestier` still works without versioning (no behavior change on the unversioned path).
- `Microsoft.Restier.Tests.AspNetCore.NSwag` and `…Swagger` — tests for the registry-aware paths in **all three** integration points (document generator, middleware, and UI helpers `UseRestierReDoc` / `UseRestierNSwagUI`); registry-absent fallback for each; **registry-empty fallback** (registry registered but contains zero descriptors → existing prefix-based behavior); **mixed routes** (versioned + unversioned in the same app → UI dropdown contains both registry group names AND any `GetRestierRoutePrefixes()` entries not represented by registry descriptors); **multi-group docs** (two logical APIs at different `basePrefix`es with disambiguated `GroupNameFormatter` → both docs independently reachable at `/openapi/{groupName}/openapi.json`).

### Sample-based smoke (manual, documented)

The `NorthwindVersioned` sample doubles as a runnable end-to-end check.

## Documentation

New page **`src/Microsoft.Restier.Docs/guides/server/api-versioning.mdx`**:

- Why versioning
- Quickstart (10–15 line two-version sample)
- The pattern — one `ApiBase` per version; sharing an EF model with per-version `Ignore`s
- Registration API — both overloads; segment formatters; `ExplicitRoutePrefix`
- Versioning headers — what they look like, how to enable, interaction with `Asp.Versioning`'s own reporting
- Versioned OpenAPI — NSwag and Swagger doc-name behavior; URL paths
- Versioned `$metadata` — what to expect
- Limitations — header / query / media-type versioning not supported; pointer to a tracking issue. EDM-level deprecation annotations not yet emitted.
- Migrating from unversioned — short upgrade guide

Cross-links from `nswag.mdx`, `swagger.mdx`, `index.mdx`. Nav update in `Microsoft.Restier.Docs.docsproj`'s `<MintlifyTemplate>` to put the page under "Server guides".

A short release note entry under `release-notes/`.

## Build and packaging

- New csproj `src/Microsoft.Restier.AspNetCore.Versioning/Microsoft.Restier.AspNetCore.Versioning.csproj` — multi-targets `net8.0;net9.0;net10.0` (matching the AspNetCore-family packages — no `net48`), signed with `restier.snk`, warnings-as-errors, implicit usings off, nullable off.
- New csproj `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj` — xUnit v3, FluentAssertions (AwesomeAssertions), NSubstitute.
- New sample `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/...`.
- All three new projects (`Microsoft.Restier.AspNetCore.Versioning`, `Microsoft.Restier.Tests.AspNetCore.Versioning`, `Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore`) added to `RESTier.slnx`.
- Docs project `Microsoft.Restier.Docs.docsproj` includes `Microsoft.Restier.AspNetCore.Versioning` in its source list so the API reference is generated.

## Open questions for the implementation plan

- Whether to expose `IApiVersionDescriptionProvider` as a *replacement* or *additional* provider when the user's MVC controllers also use Asp.Versioning. Default: additional (do not replace), so the user's MVC-controller versions and Restier versions both surface. Tested either way.
- Whether `RestierApiVersionDescriptor` should expose the strongly-typed `ApiVersion` via an extension method on `IRestierApiVersionRegistry` (defined in the Versioning package) for consumers that are willing to take the dependency. Default: yes, as a quality-of-life affordance.
- Whether to integrate sunset propagation with `Asp.Versioning.IPolicyManager` in a follow-up so policy-driven sunset is honored without per-call configuration. Tracked as a deferred enhancement.
- Inventory of UI helpers in `Microsoft.Restier.AspNetCore.Swagger` that enumerate prefixes — to be done at plan-writing time.
