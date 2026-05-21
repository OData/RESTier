---
title: '2.0.0-beta'
description: 'Pre-release notes for Restier 2.0 — new packages, options-bag configuration, magical operations, keyless views, multi-tenancy, spatial types, deep operations, OpenAPI annotations, and more.'
sidebarTitle: '2.0.0-beta'
---

# 2.0.0-beta

Restier 2.0 is a substantial rework on top of the 1.2 baseline. The headline themes are:

- A **single options-bag** (`RestierRouteOptions`) for all per-route configuration.
- **Endpoint routing** is now the only routing model — the legacy convention-based pipeline is gone.
- **Magical operations** — `[BoundOperation]` / `[UnboundOperation]` methods register themselves.
- **Keyless EF views** become first-class read-only resources with full pipeline integration.
- **OpenAPI annotations** are emitted automatically from standard .NET attributes.
- New optional packages for **API versioning**, **NSwag/ReDoc**, and **spatial types**.
- **Deep insert / deep update / `@odata.bind`**, **multi-tenancy**, and **lower-camelCase JSON**.

The sections below highlight the breaking changes, then walk through the new features. Each one links to a dedicated guide.

## Platform updates

- **Target frameworks:** `net8.0`, `net9.0`, `net10.0`. **.NET Framework 4.8 is no longer supported.** Restier 1.x is still maintained for .NET Framework consumers.
- **OData stack:** `Microsoft.OData.Core` / `Microsoft.OData.Edm` 8.x, `Microsoft.AspNetCore.OData` 9.x, `Microsoft.OData.ModelBuilder` 2.x.
- **EF Core:** 8.x, 9.x, and 10.x. **EF6:** 6.5.x.
- **Test stack:** xUnit v3 + FluentAssertions (AwesomeAssertions) + NSubstitute. The legacy MSTest projects have been removed.

## Breaking changes

### `AddRestierRoute` overloads collapsed to the options-bag form

The old `AddRestierRoute` overload set (taking individual `Action<IServiceCollection>` and validation knobs) has been replaced with a single options-bag form:

```csharp
options.AddRestierRoute<NorthwindApi>(
    "api",
    routeServices => routeServices.AddEFCoreProviderServices<NorthwindContext>(...),
    bag =>
    {
        bag.NamingConvention = RestierNamingConvention.CamelCase;
        bag.Validation.MaxExpansionDepth = 3;
        bag.Conformance.StrictMissingParentForCollections = true;
        bag.DeepOperations.MaxDepth = 4;
    });
```

The bag exposes:

| Property | Type | Purpose |
|---|---|---|
| `DeepOperations` | `DeepOperationSettings` | Maximum nesting depth for deep insert / deep update. |
| `Conformance` | `RestierConformanceOptions` | Opt-in OData v4 spec strictness toggles. |
| `Validation` | `RestierValidationOptions` | Per-route `$top` / `$expand` / `$filter` / `$orderby` limits. |
| `UseRestierBatching` | `bool` | Whether the Restier batch handler is registered (default `true`). |
| `NamingConvention` | `RestierNamingConvention` | EDM-to-JSON property naming (`PascalCase` by default). |

The versioning package's `AddVersion` was updated to the same shape — it now takes an `Action<RestierRouteOptions>` instead of the old standalone parameters.

See the [OData Conformance Options](/guides/server/conformance-options) guide.

### Endpoint routing only — `MapRestier()` replaces legacy conventions

The old convention-based routing infrastructure (`RestierRouteConvention`, `RestierControllerRouteConvention`, …) has been deleted. Routes are now registered through ASP.NET Core endpoint routing:

```csharp
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(endpoints =>
{
    endpoints.MapRestier();
    endpoints.MapControllers();
});
```

`MapRestier()` wires a `RestierRouteValueTransformer` that dynamically parses OData paths and dispatches into `RestierController`. The marker type `RestierRouteMarker` identifies route containers as Restier-owned.

### Query validation: bag-only, no more DI registration of `ODataValidationSettings`

Restier's per-route query validation knobs (`MaxTop`, `MaxSkip`, `MaxExpansionDepth`, `MaxAnyAllExpressionDepth`, `MaxOrderByNodeCount`, `MaxNodeCount`) now live on `RestierRouteOptions.Validation` — and the bag is now the **only** configuration channel.

- **`ODataValidationSettings` is no longer a route-DI service.** `RestierController` and the OpenAPI generators (Swagger / NSwag) now resolve `RestierValidationOptions` from the route container and either build settings on demand (the controller) or read fields directly (the generators). Third-party code that previously resolved `ODataValidationSettings` from `HttpRequest.GetRouteServices()` must switch to `RestierValidationOptions`.
- **DI registration of `ODataValidationSettings` is rejected.** Registering it inside the `AddRestierRoute` service callback throws `InvalidOperationException` at startup with a migration message pointing at the bag.

`ODataValidationSettings` is an upstream per-action class designed for `[EnableQuery]` controller methods. Restier has no per-action layer, so the per-action model never applied. Pre-2.0 versions accepted DI-registered instances as a workaround, which produced silent conflicts with the global `ODataOptions.SetMaxTop(...)` (see issues [#684](https://github.com/OData/RESTier/issues/684) and [#719](https://github.com/OData/RESTier/issues/719)). The 2.0 bag makes the route-level scope explicit, and the only place `MaxTop` can still appear twice — bag and global — emits a loud `Trace.TraceWarning` if the two values disagree.

See [Query Validation Options](/guides/server/validation-options) and issue [#751](https://github.com/OData/RESTier/issues/751).

#### Migration

Before:

```csharp
options.AddRestierRoute<NorthwindApi>(string.Empty, restierServices =>
{
    restierServices
        .AddEFCoreProviderServices<NorthwindContext>(...)
        .AddSingleton(new ODataValidationSettings
        {
            MaxTop = 5,
            MaxExpansionDepth = 3,
            MaxAnyAllExpressionDepth = 3,
        });
});
```

After:

```csharp
options.Select().Expand().Filter().OrderBy().SetMaxTop(5).Count();

options.AddRestierRoute<NorthwindApi>(
    string.Empty,
    restierServices => restierServices
        .AddEFCoreProviderServices<NorthwindContext>(...),
    bag =>
    {
        bag.Validation.MaxExpansionDepth = 3;
        bag.Validation.MaxAnyAllExpressionDepth = 3;
    });
```

### `OperationContext.GetParameterValueFunc` is now presence-aware

`Microsoft.Restier.Core.Operation.OperationContext.GetParameterValueFunc` changed from `Func<string, object>` to `Func<string, (bool Present, object Value)>`. The `Present` flag is `true` when the parameter name appears in the request, even if the supplied value is `null`. This is required to distinguish "URL omitted the parameter" from "URL supplied `p=null`" — necessary for both default substitution and explicit-null semantics on the same parameter.

**Affected:** custom `RestierController` subclasses that construct their own `getParaValueFunc`, and any code that constructs `OperationContext` directly.

**Migration:** replace `Func<string, object>` with `Func<string, (bool Present, object Value)>`. For URL/segment parameters, build the delegate as:

```csharp
Func<string, (bool Present, object Value)> getParaValueFunc = p =>
{
    var match = segment.Parameters.FirstOrDefault(c => c.Name == p);
    return (match is not null, match?.Value);
};
```

### GET queries no longer change-track entities (rolled forward from 1.2)

GET queries execute with change tracking disabled by default (EF Core: `AsNoTrackingWithIdentityResolution`; EF6: `AsNoTracking` with a cycle-aware fallback). The submit pipeline and internal lookups are unaffected.

Opt back into tracking when needed:

```csharp
services.AddEFCoreProviderServices<MyContext>(
    dbOpts => dbOpts.UseSqlServer(...),
    restierOpts => restierOpts.TrackingBehavior = RestierEFTrackingBehavior.TrackAll);
```

`IExpandCycleDetector` is now a first-class core service and the controller computes a `HasRecursiveExpand` hint on `QueryRequest`. See the [Tracking behavior](/guides/server/performance#tracking-behavior) section.

## Magical operations

`[BoundOperation]` / `[UnboundOperation]`-decorated methods are now fully self-registering. See [Operations → Auto-registration, optional parameters, and annotations](/guides/server/operations#auto-registration-optional-parameters-and-annotations).

Highlights:

- **Complex types are auto-registered** (issue [#651](https://github.com/OData/RESTier/issues/651)). Operation parameter and return types that aren't already in the model are registered as `ComplexType`, `EntityType` (when keyed), or `EnumType` without any manual model-builder work.
- **Optional parameters** (issue [#656](https://github.com/OData/RESTier/issues/656)). Four signal sources — `Nullable<T>`, compiler defaults, `[DefaultValue]`, and the new `[Optional]` attribute — produce the correct `EdmOptionalParameter` shape with the right default literal. The runtime executor substitutes declared defaults on URL-omitted parameters; explicit `?p=null` on a nullable parameter passes null. Non-nullable value-type parameters without `[Optional]` or `[DefaultValue]` are rejected at startup with a clear message.
- **Duplicate-name detection** (issue [#652](https://github.com/OData/RESTier/issues/652)). Declaring the same operation both manually and via `[Operation]` no longer creates a duplicate in the EDM model; the manual registration wins and a `Trace.TraceWarning` surfaces the duplicate.
- **`[Obsolete]` annotation.** Method-level `[Obsolete]` now emits `Core.V1.Revisions` with `Kind = Deprecated`, round-tripping into OpenAPI's `deprecated` field.
- **Parameter-level `[Description]`.** Annotates `EdmOperationParameter` with `Core.V1.Description`.

Closes the operation-related items of [#750](https://github.com/OData/RESTier/issues/750).

## Keyless views

Auto-generated keyless-view function imports flow through the normal RESTier query pipeline.

- Keyless EF Core entities are demoted to `EdmComplexType` and surfaced as a `FunctionImport` (`GET /Service/MyView()`).
- The function-import handler dispatches through a registry built at model time, with a per-route `IQueryExpressionSourcer` projecting the keyless type.
- `OnFilter<View>` convention methods fire. Visibility is `protected` or `protected internal`, matching the entity-set `OnFilter<EntitySet>` contract.
- Custom `IQueryExpressionAuthorizer` registrations see view GET requests.
- `RestierEFOptions.TrackingBehavior` applies to keyless-view reads.
- `DELETE` / `PUT` / `PATCH` on a function import returns `405 Method Not Allowed`.

The convention name was corrected: V1 docs and the V1 test fixture used the gerund form `OnFiltering<View>`; the actual convention is `OnFilter<View>` (no gerund — matches the entity-set convention via `ConventionBasedMethodNameFactory.GetEntitySetMethodName`). The V1 convention name never produced an observable call because V1 explicitly documented that the convention did not fire.

The operation-filter pipeline (`IOperationFilter`) does **not** fire for view requests — the controller routes views through the query pipeline rather than the operation executor. Use `IQueryExpressionProcessor` or the `OnFilter<View>` convention for pre/post hooks on view reads.

See the [Keyless Views](/guides/server/keyless-views) guide. Closes [#741](https://github.com/OData/RESTier/issues/741).

## Authorization metadata on API surfaces

`[AllowAnonymous]`, `[Authorize]`, `[Authorize(Policy = "…")]`, `[Authorize(Roles = "…")]`, and `[Authorize(AuthenticationSchemes = "…")]` are now honored on:

- The `ApiBase` subclass itself (scoping every route the API serves).
- Individual operation methods on the API.

A new `RestierAuthorizationMetadataPolicy` propagates the discovered attributes onto the matched endpoint so `AuthorizationMiddleware` runs against them before the request reaches RESTier. RESTier-level convention authorization (`Can{Operation}{Target}`, `IChangeSetItemAuthorizer`) continues to layer on top.

See the [Method Authorization](/guides/server/method-authorization) guide.

## API versioning — new package: `Microsoft.Restier.AspNetCore.Versioning`

URL-segment API versioning built on `Asp.Versioning`. Each version is a distinct `ApiBase` subclass at its own route prefix, with its own EDM, `$metadata`, and OpenAPI document.

```csharp
builder.Services
    .AddRestierApiVersioning(api =>
    {
        api.AddVersion<NorthwindV1Api>("api", services => ...)
           .AddVersion<NorthwindV2Api>("api", services => ...);
    });

app.UseRestierVersionHeaders();
```

Features:

- `RestierApiVersionSegmentFormatters` (`Major`, `MajorMinor`) control how `ApiVersion` becomes a URL segment.
- `[ApiVersion]` attributes on each API class are discovered by `ApiVersionAttributeReader`.
- `UseRestierVersionHeaders` middleware emits `api-supported-versions` and `api-deprecated-versions` response headers.
- NSwag and Swagger doc resolution is registry-aware — each version gets its own OpenAPI document and dropdown entry.
- Versioned `$batch` routing is supported.
- Optional `Sunset` and explicit base-prefix overrides.

See the [API Versioning](/guides/server/api-versioning) guide.

## NSwag integration — new package: `Microsoft.Restier.AspNetCore.NSwag`

A first-class NSwag integration that ports the Restier OpenAPI document generator onto the NSwag pipeline and adds:

- `AddRestierNSwag(settings => …)` service registration.
- `UseRestierOpenApi()` middleware serving the OpenAPI document at `/openapi/v1.json` (path configurable; honors `Sunset`).
- `UseRestierReDoc()` and `UseRestierNSwagUI()` for ReDoc and the NSwag UI, both registry-aware and listing user-registered NSwag documents in the dropdown.
- `RestierControllerApiExplorerConvention` so `RestierController` is excluded from plain MVC `ApiExplorer` discovery — your hand-written controllers stay isolated from the Restier doc.

Doc generation honors the same OpenAPI annotation attributes as the Swagger package (see below).

See the [NSwag](/guides/server/nswag) guide. NSwag is the recommended integration for new projects; the Swashbuckle-based `Microsoft.Restier.AspNetCore.Swagger` package remains supported.

## OpenAPI annotations from .NET attributes

Restier scans your CLR types for standard .NET attributes and emits the matching OData vocabulary annotations into `$metadata`. NSwag and Swagger then surface them in the generated OpenAPI document — no extra configuration:

| Attribute | EDM annotation | OpenAPI effect |
|---|---|---|
| `[Description]` (entity, complex, property, operation, parameter) | `Core.V1.Description` | Schema / property / operation description |
| `[DatabaseGenerated]` | `Core.V1.Computed` | Property dropped from POST/PATCH/PUT bodies; `readOnly` in OpenAPI |
| `[ReadOnly(true)]` | `Core.V1.Immutable` | Property dropped from PATCH/PUT bodies (POST still accepts) |
| `[Obsolete]` (operation) | `Core.V1.Revisions { Kind = Deprecated }` | OpenAPI `deprecated: true` |
| `[Range]` | `Validation.Min` / `Validation.Max` typed by EDM kind | `minimum` / `maximum` |
| `[RegularExpression]` | `Validation.Pattern` | `pattern` |

`Core.V1.Computed` and `Core.V1.Immutable` are not metadata-only — Restier's submit pipeline reads them to drop request-body properties before the change set is applied.

See the [OpenAPI Annotation Attributes](/guides/server/openapi-annotations) guide.

## Spatial types — new packages

Round-tripping `Microsoft.Spatial` types through EF6 and EF Core, plus server-side translation of OData `geo.*` filter functions.

- **`Microsoft.Restier.EntityFramework.Spatial`** wires `Microsoft.Spatial` to `System.Data.Entity.Spatial.DbGeography`/`DbGeometry` via `DbSpatialConverter` and `DbSpatialModelMetadataProvider`. Register with `services.AddRestierSpatial()`.
- **`Microsoft.Restier.EntityFrameworkCore.Spatial`** wires `Microsoft.Spatial` to NetTopologySuite via `NtsSpatialConverter` and column-type inference (`NtsSpatialModelMetadataProvider`).
- The `[Spatial]` attribute opts CLR properties into the EDM `Geography`/`Geometry` primitive types.
- `RestierSpatialFilterBinder` translates `geo.intersects`, `geo.distance`, and `geo.length` to provider methods/properties so EF Core can push them to the database.
- `RestierPayloadValueConverter` and the change-set initializers dispatch spatial branches via DI.
- `SridPrefixHelpers` mediates the SQL Server WKT dialect (`SRID=4326;…`) when needed.
- `ODataOptions.TimeZone` is propagated to the filter binder (fixes [#704](https://github.com/OData/RESTier/issues/704)).

See the [Spatial Types](/guides/extending-restier/spatial-types) guide. Closes [#673](https://github.com/OData/RESTier/issues/673).

## Multi-tenancy guide and middleware support

Restier's per-route scoped DI plus EF Core's runtime `DbContextOptions` configuration are enough to build a DB-per-tenant SaaS service from one `ApiBase` subclass:

- A `PathSegmentTenantResolutionMiddleware` reads the tenant id from the URL, validates it against an `IConnectionStringProvider`, and populates a scoped `ITenantContext`.
- The route's `AddDbContext` factory bridges back via `IHttpContextAccessor` to pick the right connection string at request time.
- `@odata.context` preserves the tenant prefix via `PathBase`.

No changes to RESTier itself are required — this is documented as a guide with an end-to-end integration test fixture.

See the [Multi-Tenancy](/guides/server/multi-tenancy) guide.

## Deep insert / deep update / `@odata.bind`

POST and PATCH bodies can now express nested writes:

- `DeepOperationExtractor` walks the request payload into a `DataModificationItem` tree (with `BindReference` nodes for `@odata.bind`).
- `DeepUpdateClassifier` decides — per nested property — whether to insert, update, link, or unlink (with FK update / relationship removal where applicable).
- `DefaultChangeSetInitializer` and the EF6/EFCore initializers handle the new tree shape and emit `400` for relationship constraint violations (`DbUpdateException`).
- `RestierRouteOptions.DeepOperations.MaxDepth` caps nesting depth (default `5`; set to `0` to disable).
- OData-Version `4.01` is required for nested PATCH bodies; non-4.01 requests get a clear error.
- `$ContentId` references in `$batch` change sets are resolved via the new `ChangeSetDependencyResolver`, with the whole batch enlisted in a `TransactionScope` ([#762](https://github.com/OData/RESTier/issues/762)).

## Lower-camelCase JSON

`RestierRouteOptions.NamingConvention = RestierNamingConvention.CamelCase` (or `bag.NamingConvention = CamelCase`) transforms property names end-to-end:

- `$metadata` and query response payloads use camelCase property names.
- Request bodies, ETags, and `If-Match` / `If-None-Match` headers normalize to CLR names before reaching the submit pipeline (`EdmClrPropertyMapper`).
- The `RestierResourceDeserializer` accepts camelCase enum literals and validates them against the CLR enum.

See the [Naming Conventions](/guides/server/naming-conventions) guide. Closes [#549](https://github.com/OData/RESTier/issues/549).

## Conformance toggle: `StrictMissingParentForCollections`

A collection-valued navigation property whose parent entity doesn't exist (for example `/Books(missing)/Reviews`) historically returned `200 OK { "value": [] }`. With `bag.Conformance.StrictMissingParentForCollections = true` it returns `404 Not Found` per OData v4 Part 1 §9.1.5 / §11.2.6, at the cost of one extra parent-existence query per request. The toggle also extends to `$count` ([#735](https://github.com/OData/RESTier/issues/735)).

## Other notable changes

- **Deferred query materialization** ([#614](https://github.com/OData/RESTier/issues/614)). `DefaultQueryExecutor` and `EFQueryExecutor` no longer materialize collections eagerly; the controller adds 404 detection for key-based requests against missing resources, and `EFChangeSetInitializer.FindResource` materializes explicitly when needed. Closes a class of bugs around deferred `IQueryable` and missing-entity 404s.
- **`OnFilter` for single navigation properties.** `OnFilter` interceptors now fire for single navigation references inside `$expand` ([#519](https://github.com/OData/RESTier/issues/519)).
- **`OData-Version: 4.01` gating** with a clear error message when a request uses a 4.01-only construct on a 4.0 endpoint.
- **`$filter` path segment.** `RestierQueryBuilder` handles `FilterSegment` so `/Books/$filter(Year gt 2000)/$count` works end-to-end.
- **DateOnly / TimeOnly** support added to the Restier type mapping pipeline, including provider-specific EFCore metadata baselines. See [Temporal Types](/guides/extending-restier/temporal-types).
- **PostgreSQL sample** (`Microsoft.Restier.Samples.Postgres.AspNetCore`) — a vnext-style sample wired to PostgreSQL via EF Core, including a keyless-view example.
- **Documentation** rebuilt on top of the [DotNetDocs SDK](https://github.com/CloudNimble/DotNetDocs) and Mintlify (MDX). The `api-reference/` tree is regenerated from XML doc comments on build.

## Source

Full diff: [`v1.2.0...feature/vnext`](https://github.com/OData/RESTier/compare/v1.2.0...feature/vnext).
