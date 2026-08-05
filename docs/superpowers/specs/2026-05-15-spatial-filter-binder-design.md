# Spatial `$filter` Translation in Restier (Spec B)

**Date:** 2026-05-15
**Status:** Design draft — second revision after review (awaiting confirmation)
**Predecessor:** [Spec A — Spatial Types Round-Trip](./2026-05-06-spatial-types-roundtrip-design.md)
**Issue:** [OData/RESTier#673](https://github.com/OData/RESTier/issues/673) (filtering portion)

## Goal

Translate the three OData v4 spatial query functions — `geo.distance`, `geo.length`, `geo.intersects` — into server-side LINQ so they execute as native SQL spatial operators (T-SQL on SQL Server, PostGIS on Npgsql, etc.) rather than 4xx-ing at the OData filter binder. Both Entity Framework 6 (`DbGeography` / `DbGeometry`) and Entity Framework Core (NetTopologySuite) are covered symmetrically. The `$filter=geo.distance(...) lt N` negative integration test shipped in Spec A flips to a positive test, and matching positive coverage is added for `geo.length` and `geo.intersects`.

This is the second of three planned specs. Source-generator/`[SpatialProperty]` sugar for users who prefer Microsoft.Spatial-typed entity properties is deferred to Spec C. Later items — `geo.*` in `$orderby`, default-SRID configuration, `Edm.Stream` for very-large geometries — are explicitly out of scope.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Function coverage | `geo.distance`, `geo.length`, `geo.intersects` | The three v4-core spatial functions. Non-core (`geo.coveredby`, `geo.contains`, ...) fall through to the base `FilterBinder` (HTTP 400) — out of scope. |
| EF flavor symmetry | Both EF6 and EF Core | Mirrors Spec A. EF6's `DbGeography.Distance` and EF Core's NTS `Geometry.Distance` both translate natively under their respective providers, so a single binder works for both via Spec A's `ISpatialTypeConverter` indirection. |
| Mechanism | Custom `IFilterBinder` subclass | AspNetCoreOData's official extension point. Alternatives (post-bind tree rewrite, ODL-level visitor) don't survive: the default `FilterBinder` throws on `geo.*` before any downstream processor sees the expression, and ODL has no node kind to express "call CLR method X". |
| Opt-in surface | `AddRestierSpatial()` activates filtering by registering `ISpatialTypeConverter`; the binder itself is registered unconditionally by `Microsoft.Restier.AspNetCore` inside `AddRouteComponents`. | One-line user opt-in (the same `AddRestierSpatial()` call that lights up round-trip lights up filtering), matching Spec A's UX, *without* forcing the host-agnostic `.Spatial` packages to depend on `Microsoft.Restier.AspNetCore`. The binder is a near-identity passthrough when no converters are registered (overrides only three function names; everything else falls through to `base`), so always-registering it has zero behavioral impact on non-spatial Restier APIs. Registration site is inside the `AddRouteComponents` services lambda *before* `configureRouteServices.Invoke(services)`, so consumers who register their own custom `IFilterBinder` in their route-services delegate win. Uses `RemoveAll<IFilterBinder>() + AddSingleton<...>` (idempotent regardless of whether AspNetCoreOData's default is already present). |
| Error policy on bad input | Hybrid — happy-path translates, unknown function names fall through, genus / CRS mismatches throw `ODataException` | Translates the three supported functions; preserves AspNetCoreOData's stock "unknown function" error for forward compat with future OData additions; surfaces user errors as `400 Bad Request` with property/function context. **Genus validation source:** the EDM-side `IEdmTypeReference` on each ODL argument node — *not* the `ISpatialTypeConverter` contract, which is genus-agnostic by design (`NtsSpatialConverter.CanConvert` only checks `Geometry`-assignability; both converters' `ToStorage` accept either Microsoft.Spatial genus). Note (added 2026-05-15 during implementation): ODL's parser already rejects cross-genus calls at parse time via function signature matching, so the planned Step 0 `ValidateGenus` is unreachable through any URL-driven query and is skipped. The `SpatialFilter_GenusMismatch` resource string is retained as a placeholder for future programmatic-FilterClause callers but is not exercised today. |
| Path-segment `$filter` coverage | Fixed too | `RestierQueryBuilder.HandleFilterPathSegment` currently `new FilterBinder()`s with no DI access. Spec B widens the QueryBuilder ctor to accept an optional `IFilterBinder` (the controller resolves it from `HttpContext.Request.GetRouteServices()` and passes it in); `HandleFilterPathSegment` uses it instead of constructing a fresh `FilterBinder`. One mental model for both `?$filter=` and `/$filter(...)` URL shapes. |
| `geo.length` argument validation | Delegated to the provider | OData v4 specifies the input must be `Edm.GeographyLineString` / `Edm.GeometryLineString`. EF6 `DbGeography.Length` returns `null` for non-LineString inputs; NTS `Geometry.Length` returns the boundary length (perimeter for polygons). We don't duplicate the validation at bind time — would require ODL EDM-type plumbing the binder doesn't have. |
| Provider awareness | None | EF6 SQL Server, EF Core SQL Server NTS plugin, and Npgsql PostGIS each provide their own LINQ-to-SQL translation for the storage CLR members. Spec B is a pure binding/expression-shaping concern. |
| CRS handling on literals | Mirror Spec A — fail-fast on non-EPSG | Spec A's `ISpatialTypeConverter.ToStorage` throws `InvalidOperationException` for `CoordinateSystem.EpsgId == null`. The binder catches that specific exception and rewraps as `ODataException` so it flows out as a 400. Consistent posture across read, write, and filter. Note (added 2026-05-15 during implementation): Microsoft.Spatial treats any integer SRID as a valid EpsgId, so the spec-A non-EPSG fail-fast path (`EpsgId == null` → `InvalidOperationException`) is unreachable from OData literal syntax (`geography'SRID=N;…'` only accepts integer N). The wrap-as-ODataException rewrap remains in `LowerSpatialLiteralIfNeeded` as defense-in-depth for programmatic FilterClause callers but has no Spec B test. |

## Background

Spec A made Microsoft.Spatial-typed values round-trip through entity properties typed in the storage library (`DbGeography` / `DbGeometry` for EF6, `NetTopologySuite.Geometries.Geometry`-subclass for EF Core). It deliberately stopped short of any `$filter` translation, asserting the limitation via a negative integration test:

```csharp
// test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs:122-134
[Fact]
public async Task EFCore_Filter_GeoDistance_IsNotTranslatable_ReturnsError()
{
    var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
        HttpMethod.Get,
        resource: "/SpatialPlaces?$filter=geo.distance(HeadquartersLocation,geography'POINT(0 0)') lt 10000",
        serviceCollection: _configureServices);

    response.IsSuccessStatusCode.Should().BeFalse(
        "geo.distance translation is not supported by EF Core + NTS (spec-A limitation); " +
        "the server must return a 4xx or 5xx error, not a successful response");
}
```

The failure today is AspNetCoreOData's default `FilterBinder.BindSingleValueFunctionCallNode` throwing on the `geo.distance` `SingleValueFunctionCallNode` (no handler is registered for the `geo.*` function namespace). Spec B replaces the binder with one that recognises the three v4-core spatial functions and rewrites them as LINQ method/property access against the storage CLR type. EF6's SQL Server provider and EF Core's NTS plugin then translate the LINQ to native SQL spatial operators.

The current main-path `$filter` ingestion goes through `RestierController.ApplyQueryOptions` → `ODataQueryOptions.ApplyTo`, which resolves `IFilterBinder` from the per-request service container — so registering a custom binder in route services is sufficient for the common case. A second code path, `RestierQueryBuilder.HandleFilterPathSegment` (`src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs:307-319`), handles path-segment `$filter` syntax (`/SpatialPlaces/$filter(...)`) and currently `new FilterBinder()`s with no DI lookup. Spec B fixes that path too, since leaving it would create a "works in one URL shape, fails in the other" inconsistency for the same query.

## Architecture

### Components

| # | Component | Assembly | Notes |
|---|-----------|----------|-------|
| 1 | `RestierSpatialFilterBinder` (subclass of `Microsoft.AspNetCore.OData.Query.Expressions.FilterBinder`) | `Microsoft.Restier.AspNetCore` | Stateless. Ctor accepts `IEnumerable<ISpatialTypeConverter>` (Spec A primitive). Overrides `BindSingleValueFunctionCallNode`. |
| 2 | `RestierQueryBuilder` ctor widening + `HandleFilterPathSegment` refactor | `Microsoft.Restier.AspNetCore` | New optional ctor parameter `IFilterBinder filterBinder = null`. `HandleFilterPathSegment` uses the injected binder when present, falls back to `new FilterBinder()` when null. Both call sites in `RestierController.cs:704, 717` are updated to resolve `IFilterBinder` from `HttpContext.Request.GetRouteServices()` and pass it through. |
| 3 | `RestierODataOptionsExtensions` registers `RestierSpatialFilterBinder` inside `AddRouteComponents` | `Microsoft.Restier.AspNetCore` | One added line inside the services lambda, before `configureRouteServices.Invoke(services)`: `services.RemoveAll<IFilterBinder>(); services.AddSingleton<IFilterBinder, RestierSpatialFilterBinder>();`. The flavor `.Spatial` packages stay host-agnostic — they keep their existing dependency surface (`Microsoft.Restier.Core` + the flavor's EF package only) and do not gain a reference to `Microsoft.Restier.AspNetCore`. |
| 4 | Two new resource strings | `Microsoft.Restier.AspNetCore` | `SpatialFilter_GenusMismatch`, `SpatialFilter_NoConverterForStorageType`. Both used only inside Spec B's dispatch arms. |

### Why one binder for both flavors

The binder doesn't need to know whether the storage type is `DbGeography`, `DbGeometry`, or an NTS subclass. The three things it cares about are:

1. **What storage CLR type does the bound property argument have?** This comes straight off the bound `Expression.Type` after `base.Bind(arg)` — Spec A's model-builder convention preserves the storage-typed CLR property and only substitutes EDM-side, so `Expression.Type` is `DbGeography` / `DbGeometry` / `NTS.Point` / etc. depending on the flavor.
2. **What's the storage value for the spatial literal?** Resolved by asking each registered `ISpatialTypeConverter` whose `CanConvert(storageType)` is true to `ToStorage(storageType, edmValue)`. Spec A registers exactly one converter per flavor, so the answer is unambiguous.
3. **What CLR member do I call?** `Distance` / `Length` / `Intersects` are present (with matching signatures) on every supported storage type — looked up reflectively via `Expression.Call` / `Expression.Property` against the bound argument's runtime CLR type. No flavor switch needed.

### Component placement

`RestierSpatialFilterBinder` lives in `Microsoft.Restier.AspNetCore` even though both `.Spatial` packages register it. Rationale:

- The binder references `Microsoft.AspNetCore.OData.Query.Expressions.FilterBinder` and `ISpatialTypeConverter` only — no EF6 or EF Core types. Putting it in `Microsoft.Restier.AspNetCore` (which already references AspNetCoreOData) avoids any EF flavor leakage in core wiring.
- DI registration stays in the `.Spatial` packages. Consumers who don't reference either package don't have the class in their composition root and the default `FilterBinder` is used as before — no behavior change for non-spatial APIs.

### `RestierSpatialFilterBinder` shape

```csharp
namespace Microsoft.Restier.AspNetCore.Query
{
    public class RestierSpatialFilterBinder : FilterBinder
    {
        private readonly ISpatialTypeConverter[] converters;

        public RestierSpatialFilterBinder(IEnumerable<ISpatialTypeConverter> converters = null)
        {
            this.converters = converters?.ToArray() ?? Array.Empty<ISpatialTypeConverter>();
        }

        public override Expression BindSingleValueFunctionCallNode(
            SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            switch (node.Name)
            {
                case "geo.distance":   return BindGeoDistance(node, context);
                case "geo.length":     return BindGeoLength(node, context);
                case "geo.intersects": return BindGeoIntersects(node, context);
                default:               return base.BindSingleValueFunctionCallNode(node, context);
            }
        }

        // BindGeo* helpers: (0) validate genus on the ODL nodes' IEdmTypeReferences,
        // (1) bind each child via base.Bind, (2) lower spatial-literal ConstantExpressions
        // to storage values via the converters, (3) Expression.Call / Expression.Property
        // on the storage CLR type using inheritance-walking method resolution.

        // Reflection helper used by Distance / Intersects dispatch.
        // We can't call sourceType.GetMethod("Distance", new[] { argType }) because:
        //   - NTS's Geometry.Distance(Geometry) is inherited by Point/LineString/etc.
        //     but exact-parameter-type lookup returns null for derived arg types.
        //   - DbGeography.Distance(DbGeography) is fine, but using one strategy for
        //     both flavors keeps the binder flavor-free.
        // Instead, walk methods named X with arity 1 and pick the first one whose
        // parameter type is assignable from argType. The instance receiver follows the
        // same rule — inherited members surface through GetMethods() on the derived type.
        private static MethodInfo ResolveSpatialInstanceMethod(
            Type sourceType, string methodName, Type argType)
        {
            foreach (var m in sourceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (m.Name != methodName) { continue; }
                var ps = m.GetParameters();
                if (ps.Length != 1) { continue; }
                if (ps[0].ParameterType.IsAssignableFrom(argType)) { return m; }
            }
            return null;
        }
    }
}
```

The `converters = null` default keeps the parameterless construction working for the (rare) case where someone hand-builds the binder outside DI — the base behavior is preserved.

**Why `ResolveSpatialInstanceMethod` is necessary.** NTS's `WKTReader.Read(body)` returns the concrete subclass (`Point`, `LineString`, `Polygon`, …) for the parsed WKT, and `NtsSpatialConverter.ToStorage` validates assignability but doesn't widen the runtime type — so a lowered literal whose target storage type is `Point` will have `Expression.Type == typeof(Point)`. `Geometry.Distance(Geometry other)` is declared on the abstract base, and `typeof(Point).GetMethod("Distance", new[] { typeof(Point) })` returns null because exact parameter-type lookup doesn't walk inheritance for parameters. The helper walks `GetMethods()` (which already includes inherited members on the derived type) and matches by name + assignability. `Expression.Call(pointInstance, geometryDistanceMethod, pointArg)` then succeeds because the LINQ expression API accepts derived-type arguments for an assignable parameter slot. `Length` is unaffected — properties are looked up via `GetProperty("Length")` which is already inheritance-aware (single name, no parameter list to mismatch).

### `RestierQueryBuilder` ctor widening + `HandleFilterPathSegment` change

`RestierQueryBuilder` today (`src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs:23-60`) is an `internal class` with two private fields (`api`, `path`) plus derived state. It has no `IServiceProvider` and no `IFilterBinder`. The two call sites are inside the controller, where `HttpContext.Request.GetRouteServices()` is already used elsewhere (`RestierController.cs:226, 498`) for route-scoped service resolution.

Spec B widens the ctor with an **optional** binder parameter and threads it from both call sites:

```csharp
// src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs
internal class RestierQueryBuilder
{
    private readonly ApiBase api;
    private readonly ODataPath path;
    private readonly IFilterBinder filterBinder;       // new field
    // ... existing fields ...

    public RestierQueryBuilder(ApiBase api, ODataPath path, IFilterBinder filterBinder = null)
    {
        Ensure.NotNull(api, nameof(api));
        Ensure.NotNull(path, nameof(path));
        this.api = api;
        this.path = path;
        this.filterBinder = filterBinder;              // null is fine; HandleFilterPathSegment defaults
        // ... existing handler-table setup ...
    }

    private void HandleFilterPathSegment(ODataPathSegment segment)
    {
        var filterSegment = (FilterSegment)segment;
        var filterClause = new FilterClause(filterSegment.Expression, filterSegment.RangeVariable);

        var binder = this.filterBinder ?? new FilterBinder();     // <-- change
        var context = new QueryBinderContext(edmModel, new ODataQuerySettings(), currentType);

        queryable = binder.ApplyBind(queryable, filterClause, context);
    }
}
```

Both call sites in `RestierController.cs` are updated to resolve `IFilterBinder` from route services and pass it through:

```csharp
// src/Microsoft.Restier.AspNetCore/RestierController.cs:704
var routeServices = HttpContext.Request.GetRouteServices();
var filterBinder = routeServices?.GetService<IFilterBinder>();
var parentQuery = new RestierQueryBuilder(api, parentPath, filterBinder).BuildQuery();

// src/Microsoft.Restier.AspNetCore/RestierController.cs:717
var routeServices = HttpContext.Request.GetRouteServices();
var filterBinder = routeServices?.GetService<IFilterBinder>();
var builder = new RestierQueryBuilder(api, path, filterBinder);
```

If route services aren't available (a corner-case for direct construction in tests) or no `IFilterBinder` is registered, `HandleFilterPathSegment` falls back to `new FilterBinder()` — observationally identical to today's behavior for non-spatial APIs.

The optional parameter keeps existing direct-construction call sites (none in source today, possibly some in tests) compiling unchanged.

### AspNetCore filter-binder registration

The binder is registered by `Microsoft.Restier.AspNetCore` directly — inside the route-components services lambda in `RestierODataOptionsExtensions.cs` (the file Spec A and earlier work already extends for every Restier route). The flavor `.Spatial` packages stay untouched on this dimension; their `AddRestierSpatial()` extensions only register `ISpatialTypeConverter` and `ISpatialModelMetadataProvider` as they do today.

Why the registration moves up the stack: neither `.Spatial` package references `Microsoft.Restier.AspNetCore` (`src/Microsoft.Restier.EntityFramework.Spatial/Microsoft.Restier.EntityFramework.Spatial.csproj:22-23` references only `Core` + `EntityFramework`; `src/Microsoft.Restier.EntityFrameworkCore.Spatial/Microsoft.Restier.EntityFrameworkCore.Spatial.csproj:24-25` references only `Core` + `EntityFrameworkCore`). They are deliberately host-agnostic. Forcing them to take an AspNetCore dependency just to register a filter binder would invert that design.

The new line goes inside the existing services lambda at `RestierODataOptionsExtensions.cs:147-180`, immediately before `configureRouteServices.Invoke(services)` (i.e. after Restier's own core registrations but before user code):

```csharp
// src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs
oDataOptions.AddRouteComponents(routePrefix, model, services =>
{
    // ... existing setup (RestierRouteMarker, scoped Api, RestierNamingConvention,
    // RemoveAll<ODataQuerySettings>, AddRestierCoreServices, AddRestierConventionBasedServices) ...

    // Override the default OData FilterBinder with the spatial-aware subclass. The
    // binder is a near-identity passthrough — only the three v4-core geo.* functions
    // are intercepted, and even those fall through to base when no ISpatialTypeConverter
    // is registered, so this has zero behavioral impact on non-spatial Restier APIs.
    services.RemoveAll<IFilterBinder>();
    services.AddSingleton<IFilterBinder, RestierSpatialFilterBinder>();

    configureRouteServices.Invoke(services);
    // ... rest of existing setup ...
});
```

**Why `RemoveAll` + `AddSingleton` and not `Replace`.** `Replace` requires the descriptor to already be present; if a future AspNetCoreOData refactor removes the default registration, `Replace` throws. `RemoveAll` is idempotent — works whether or not the default is registered.

**Why before `configureRouteServices.Invoke`.** Consumers' route-services delegate runs *after* this line, so a user who registers their own custom `IFilterBinder` (or who calls `services.Replace<IFilterBinder, MyBinder>()`) wins. The documentation page picks up this note.

**Activation by `AddRestierSpatial()`.** The user-facing UX from Q2 is unchanged: a single `AddRestierSpatial()` call inside `configureRouteServices` registers `ISpatialTypeConverter`s and `ISpatialModelMetadataProvider`. The already-installed binder resolves the converter enumerable from DI lazily (singleton, constructed at first request); by the time the binder is constructed, the user's `AddRestierSpatial()` has run inside `configureRouteServices.Invoke(services)` and the enumerable is populated. APIs that don't call `AddRestierSpatial()` get an empty enumerable; the binder falls through to base on every `geo.*` function name (same behavior as today).

### Data flow per function

Each dispatch arm for the binary functions (`geo.distance`, `geo.intersects`) follows a four-step pattern: validate genus on the EDM nodes → bind children → lower spatial literals → emit storage-typed member access via inheritance-walking method resolution. `geo.length` is a one-step property access on a single bound argument.

#### `geo.distance(arg0, arg1)`

**Step 0 — validate genus from the ODL node tree.** Before binding, inspect each `SingleValueNode` parameter's `TypeReference` (an `IEdmTypeReference`). For each parameter whose primitive kind is in the geography or geometry family, derive its genus from `IEdmPrimitiveTypeReference.PrimitiveKind()` (`Geography*` → Geography family; `Geometry*` → Geometry family). If two parameters disagree on genus, throw `ODataException` (`SpatialFilter_GenusMismatch`, see § Error handling). This validation is **necessary at the binder layer** because Spec A's converter contract is genus-agnostic: `NtsSpatialConverter.CanConvert` checks only `Geometry`-assignability (NTS shares the same `Geometry` base for both genera), and both converters' `ToStorage` happily accept either Microsoft.Spatial genus, dispatching to the storage type by `targetStorageType` — they would silently lower a `GeographyPoint` literal into a `DbGeometry` storage value, which is semantically wrong but the converter alone can't catch.

**Step 1 — bind children.** `boundArg0 = base.Bind(node.Parameters[0], context)`; same for `boundArg1`. After this step, the property-side bound expression has `Expression.Type` equal to the storage CLR type (`DbGeography`, `Point`, `Polygon`, etc. — Spec A's model-builder convention preserves the storage-typed CLR property); the literal-side bound expression is a `ConstantExpression` whose `Value` is a `Microsoft.Spatial.Geography*` / `Geometry*` runtime instance.

**Step 2 — lower spatial literals.** For each bound expression whose `Value`'s runtime type is a `Microsoft.Spatial` subclass:
- Take the "other" argument's `Expression.Type` as the storage target type (e.g., when the property side is `DbGeography`, the literal target is also `DbGeography`).
- Find a `converter` whose `CanConvert(storageTargetType)` returns `true`.
- Replace the `ConstantExpression` with `Expression.Constant(converter.ToStorage(storageTargetType, edmValue), storageTargetType)`.
- If no converter matches, throw `ODataException` (`SpatialFilter_NoConverterForStorageType`).
- If `ToStorage` throws `InvalidOperationException` (Spec A's non-EPSG fail-fast path), wrap it in `ODataException` preserving the original message.

**Step 3 — emit the method call** via inheritance-walking resolution:

```csharp
var method = ResolveSpatialInstanceMethod(storageArg0.Type, "Distance", storageArg1.Type);
// method is e.g. typeof(Geometry).GetMethod("Distance", new[] { typeof(Geometry) })
// even when storageArg0.Type is Point and storageArg1.Type is Point.
return Expression.Call(storageArg0, method, storageArg1);
```

`ResolveSpatialInstanceMethod` (sketch in the previous subsection) walks `GetMethods()` on the source type and picks the first instance method named `Distance` with one parameter whose `ParameterType.IsAssignableFrom(argType)`. This works for both EF6 (`DbGeography.Distance(DbGeography)` lookup against `DbGeography` source and `DbGeography` argument) and NTS (`Geometry.Distance(Geometry)` lookup against a `Point` source and `Point` argument — inherited members surface through `GetMethods()` on the derived type).

The returned `MethodCallExpression`'s type is `double?` (EF6) or `double` (NTS). Base `FilterBinder.BindBinaryOperatorNode` handles the `lt N` wrapper — including nullable-versus-non-nullable comparison — without any further intervention.

#### `geo.length(arg0)`

1. Bind `boundArg0` via base.
2. **Emit** `Expression.Property(boundArg0, "Length")`. `DbGeography.Length` and `DbGeometry.Length` are `double?` instance properties; `NTS.Geometry.Length` is a `double` instance property. `GetProperty("Length")` is already inheritance-aware (no parameter list to mismatch), so a `Point`-typed source resolves to the inherited `Geometry.Length` property without help.

No literal lowering — `geo.length` is unary. No Step 0 either: there's no second argument to compare genus against.

#### `geo.intersects(arg0, arg1)`

Same four-step shape as `geo.distance`:
- **Step 0** validates that both arguments are the same genus (Geography vs Geometry) via their EDM type references.
- **Step 1** binds.
- **Step 2** lowers spatial literals.
- **Step 3** emits `Expression.Call(storageArg0, ResolveSpatialInstanceMethod(storageArg0.Type, "Intersects", storageArg1.Type), storageArg1)`. Return type is `bool?` (EF6) or `bool` (NTS); base bind handles the predicate position.

#### Cross-cutting

- **Literal-on-literal calls** (`geo.distance(geography'...', geography'...')`) are uncommon but legal. Step 0 catches genus mismatches across them; Step 2 lowers both — the first literal's storage type can't be inferred from a property access, so the binder uses the converter's preferred storage type for the literal's genus (`DbGeography` for Geography on EF6, `NetTopologySuite.Geometries.Geometry` on EF Core, etc.). Implementation detail: ask each registered converter for the first storage type it would lower the Geography family into via a sentinel call; the implementation plan will pin this down.
- **Null property values.** EF6 and EF Core both propagate null through instance-method spatial calls — `null.Distance(x) → null` in three-valued SQL logic. The binder relies on that and adds no special-case handling.
- **Provider translation.** EF6's SQL Server provider, EF Core's SQL Server NTS plugin, and Npgsql's PostGIS plugin each translate `DbGeography`/`Geometry`/`NTS.Geometry` instance members to native SQL spatial operators. Spec B contributes zero provider-aware code; if the configured provider can't translate, the failure surfaces from EF as it would for any unsupported LINQ call.

## Error handling

Three concrete cases. All `ODataException`s map to HTTP 400 via AspNetCoreOData's stock exception handler.

### Unknown `geo.*` function name

Cases: `geo.area`, `geo.contains`, `geo.coveredby`, `geo.within`, anything else not in OData v4 core.

- **Action:** fall through to `base.BindSingleValueFunctionCallNode(node, context)`.
- **Surface:** AspNetCoreOData's default produces an `ODataException` of the form *"An unknown function with name 'geo.area' was found. This may also be a function imported in a service…"* → HTTP 400. Existing behavior; not shadowed.

### Genus mismatch

Cases: `geo.distance(GeographyProperty, geometry'...')`, two literals of incompatible families, or any other binary spatial call where the two arguments' EDM type references disagree on family.

- **Detection source:** the EDM `IEdmTypeReference` carried by each parameter's `SingleValueNode`, classified via `IEdmPrimitiveTypeReference.PrimitiveKind()` into the geography family (`Geography`, `GeographyPoint`, `GeographyLineString`, `GeographyPolygon`, `GeographyMultiPoint`, `GeographyMultiLineString`, `GeographyMultiPolygon`, `GeographyCollection`) or the geometry family. **Not** detected via the `ISpatialTypeConverter` contract — that contract is intentionally genus-agnostic (see § Data flow Step 0 rationale) and would silently lower a `GeographyPoint` literal into a `DbGeometry` storage value if asked.
- **Action:** throw `new ODataException(Resources.SpatialFilter_GenusMismatch.FormatWith(functionName, propertyPath, propertyGenus, literalGenus))` from Step 0 of the binary-function dispatch, before any binding occurs.
- **Surface:** HTTP 400. Example: *"Cannot bind 'geo.distance' on 'HeadquartersLocation' (Geography) against a Geometry literal."*

**Implementation note (2026-05-15):** the parser-side discipline supersedes this. `ODataQueryOptionParser.ParseFilter` rejects mixed-genus calls at parse time with `ODataException('No function signature for the function with name '<...>' matches the specified arguments')`, which AspNetCoreOData maps to HTTP 400. The binder's Step 0 check would be unreachable through URL-driven queries, so it is not implemented in this spec. Defense-in-depth against programmatic FilterClause construction is left to a future spec if/when that need arises.

### Non-EPSG / unsupported CRS

Case: parsed `Microsoft.Spatial.CoordinateSystem.EpsgId == null` on a literal.

- **Detection:** Spec A's `ISpatialTypeConverter.ToStorage` already throws `InvalidOperationException` for this — see Spec A § "Non-EPSG coordinate systems".
- **Action:** the binder wraps the `InvalidOperationException` thrown by the converter call in an `ODataException`, preserving the original message and adding the function/property context.
- **Surface:** HTTP 400. Same posture as Spec A's submit-time rejection of non-EPSG CRS, applied at filter-bind time.

**Implementation note (2026-05-15):** the catch-and-rewrap path is unreachable through OData URL queries because Microsoft.Spatial accepts any integer as a valid `EpsgId`, and OData `geography'SRID=N;…'` literals only allow integer N. Reaching `EpsgId == null` requires a programmatic Microsoft.Spatial value constructed with a string-id CoordinateSystem (e.g. `CoordinateSystem.Geography("CRS84")`), which cannot be expressed in `$filter`. The rewrap remains in the binder as defense-in-depth; Spec B does not exercise it.

### Catch scope

The wrap-as-`ODataException` happens only inside the three dispatch arms, around the converter call and the literal-lowering step. The binder never catches around `base.BindSingleValueFunctionCallNode(node, context)` or around `base.Bind(arg, context)` — masking those would swallow legitimate parse errors.

### What is deliberately not validated

- `geo.length`'s argument is not type-checked at bind time. EF6 returns null for non-LineString; NTS returns boundary length. See the decision table.
- The literal's SRID is not validated against an allowlist — the only check is "has non-null `EpsgId`", which is Spec A's contract.

### Resource strings

In `src/Microsoft.Restier.AspNetCore/Properties/Resources.resx` (and the generated `Resources.Designer.cs`):

- `SpatialFilter_GenusMismatch` — placeholders: function name, property name, property genus, literal genus.
- `SpatialFilter_NoConverterForStorageType` — placeholders: function name, property name, storage type. Surfaces when `geo.*` is called against a spatial property but no converter is registered for that storage type (e.g., `AddRestierSpatial()` was forgotten or the wrong-flavor package is referenced).

## Test plan

### Unit tests — new file `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs`

Each test constructs a `FilterClause` via `ODataQueryOptionParser`, calls `binder.ApplyBind(IQueryable, filterClause, context)`, and asserts on the resulting LINQ `Expression`. No DB roundtrip, no HTTP — fast.

Each positive case runs as an xUnit theory across the two flavor converters (`DbSpatialConverter`, `NtsSpatialConverter`) to prove the binder is converter-agnostic.

**Project reference plumbing.** `test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj` does not currently reference either `.Spatial` source package — its integration-test access to spatial behavior is transitive through `Microsoft.Restier.Tests.Shared.EntityFramework[Core]` only. The new unit tests construct `DbSpatialConverter` and `NtsSpatialConverter` directly, so the csproj gains two explicit `<ProjectReference>` items:

```xml
<ProjectReference Include="..\..\src\Microsoft.Restier.EntityFramework.Spatial\Microsoft.Restier.EntityFramework.Spatial.csproj" />
<ProjectReference Include="..\..\src\Microsoft.Restier.EntityFrameworkCore.Spatial\Microsoft.Restier.EntityFrameworkCore.Spatial.csproj" />
```

This mixed-flavor reference set is consistent with the project's existing pattern (it already references both `Tests.Shared.EntityFramework` and `Tests.Shared.EntityFrameworkCore`).

- `BindSingleValueFunctionCallNode_GeoDistance_EmitsStorageDistanceMethodCall` — `geo.distance(prop, literal) lt N` → expression tree shape: `MethodCallExpression(prop, "Distance", [storageLiteral]) < ConstantExpression(N)`.
- `BindSingleValueFunctionCallNode_GeoLength_EmitsStorageLengthProperty` — shape: `MemberExpression(prop, "Length") > ConstantExpression(0)`.
- `BindSingleValueFunctionCallNode_GeoIntersects_EmitsStorageIntersectsMethodCall` — shape: `MethodCallExpression(prop, "Intersects", [storageLiteral])`.
- `BindSingleValueFunctionCallNode_UnknownGeoFunction_FallsThroughToBase` — `geo.unknown(prop, literal)` → `ODataException` whose message contains `"unknown function"`. Proves fall-through preserves AspNetCoreOData's error.
- `BindSingleValueFunctionCallNode_GenusMismatch_ThrowsODataException` — `geo.distance(HeadquartersLocation, geometry'POINT(0 0)')` against a `DbGeography` property → `ODataException` containing both genus names.
- `BindSingleValueFunctionCallNode_NonEpsgLiteral_WrapsInvalidOperationAsODataException` — `geo.distance(prop, geography'SRID=99999;POINT(0 0)')` → `ODataException` whose `InnerException` is `InvalidOperationException` and whose message contains the property name and SRID.
- `Ctor_NoConvertersRegistered_GeoFunctionThrowsNoConverterError` — binder built with an empty converter enumerable + `geo.distance(...)` against a spatial property → `ODataException` matching `SpatialFilter_NoConverterForStorageType`.

### Library scenario extension

`test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/SpatialPlace.cs` gains a `RouteLine` property (under the existing `#if EF6` / `#if EFCore` blocks):

```csharp
#if EF6
public DbGeography RouteLine { get; set; }
#endif
#if EFCore
public NetTopologySuite.Geometries.LineString RouteLine { get; set; }
#endif
```

`LibraryTestInitializer.cs` (both flavor copies) seeds `LINESTRING(0 0, 1 1, 2 2)` with SRID 4326 on each row. This gives `geo.length` a LineString target — neither the existing `HeadquartersLocation` (Point) nor `ServiceArea` (Polygon) is a valid LineString input.

### Integration tests — `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs`

**Flip the existing negative test.** `EFCore_Filter_GeoDistance_IsNotTranslatable_ReturnsError` is renamed to `EFCore_Filter_GeoDistance_TranslatesAndReturnsSeededRow` and changes from `IsSuccessStatusCode.Should().BeFalse()` to asserting 200 OK plus the seeded Amsterdam row in the result set.

**New positive tests — each in both `_EF6` and `_EFCore` flavors** (eight tests total):

- `Filter_GeoDistance_TranslatesAgainstStorageProperty` — `?$filter=geo.distance(HeadquartersLocation,geography'SRID=4326;POINT(0 0)') lt 10000000`. Asserts 200 + body contains the Amsterdam-seeded row.
- `Filter_GeoLength_TranslatesPropertyAccess` — `?$filter=geo.length(RouteLine) gt 0`. Asserts 200 + non-empty result set.
- `Filter_GeoIntersects_TranslatesMethodCall` — `?$filter=geo.intersects(ServiceArea,geography'SRID=4326;POINT(0.5 0.5)')`. Asserts 200 + the row whose polygon contains the test point.
- `Filter_GeoDistance_PathSegmentSyntax_TranslatesToo` — `/SpatialPlaces/$filter(geo.distance(...) lt N)`. Same payload assertion, exercises the `HandleFilterPathSegment` change.

**New negative tests — flavor-agnostic where possible** (four tests):

- `Filter_GeoDistance_GenusMismatch_Returns400` — Geography property vs `geometry'...'` literal. Asserts 400 and that the body mentions the property and both genera.
- `Filter_GeoDistance_NonEpsgSrid_Returns400` — `geography'SRID=99999;POINT(0 0)'` literal. Asserts 400 and that the body preserves the Spec-A non-EPSG message.
- `Filter_GeoArea_UnknownFunction_Returns400` — proves fall-through preserves AspNetCoreOData's error message.
- `Filter_GeoDistance_WithoutAddRestierSpatial_Returns400` — bootstraps an API without `AddRestierSpatial()`; asserts the legacy error mode is unchanged (still 400, original AspNetCoreOData message).

### `RestierQueryBuilder` unit coverage

A new test class in `test/Microsoft.Restier.Tests.AspNetCore/Query/` confirms that `HandleFilterPathSegment` resolves a DI-registered `IFilterBinder` when one is present, and falls back to `new FilterBinder()` when nothing's registered. Regression-protects the change.

### Baselines

No metadata baseline regeneration. Spec B doesn't change the EDM model — only filter semantics. All assertions are on JSON response bodies.

## Documentation

`src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx` updates:

- Remove the `geo.*` translation entry from "What's not yet supported" (line 126 today).
- Add a new top-level section "Server-side filtering with `geo.*` functions" above "How it works", listing the supported function matrix, an example query against the Library scenario, the genus-mismatch and non-EPSG error notes, and a forward pointer to "later" items (`$orderby`, default-SRID config).

The docsproj regenerates `docs.json` on build; commit the regenerated file alongside the `.mdx` change.

## Sample app

Optional. `src/Microsoft.Restier.Samples.Postgres.AspNetCore/README.md` gains a "Try a spatial filter" snippet showing `Users?$filter=geo.distance(HomeLocation,geography'SRID=4326;POINT(4.9 52.4)') lt 500000`. No code change in the sample — the existing `AddRestierSpatial()` call lights up filtering automatically once Spec B ships. If this is too far from Spec B's core surface for your taste, skip it.

EF6 sample is unchanged.

## Scope

### Source files added

- `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs`

### Source files modified

- `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` — inside the existing `AddRouteComponents` services lambda, add `services.RemoveAll<IFilterBinder>(); services.AddSingleton<IFilterBinder, RestierSpatialFilterBinder>();` immediately before `configureRouteServices.Invoke(services)`.
- `src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs` — widen ctor with optional `IFilterBinder filterBinder = null`; `HandleFilterPathSegment` uses the injected binder when present, falls back to `new FilterBinder()` when null.
- `src/Microsoft.Restier.AspNetCore/RestierController.cs` — two call sites (lines 704 and 717) resolve `IFilterBinder` from `HttpContext.Request.GetRouteServices()` and pass it into the new ctor.
- `src/Microsoft.Restier.AspNetCore/Properties/Resources.resx` (+ generated `Resources.Designer.cs`) — two new strings.

The flavor `.Spatial` packages are deliberately **not** modified by Spec B. Their `AddRestierSpatial()` extensions continue to register `ISpatialTypeConverter` + `ISpatialModelMetadataProvider` exactly as Spec A defines, and their csproj dependency surface stays narrow.

### Test files

- `test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj` — add `<ProjectReference>` for `Microsoft.Restier.EntityFramework.Spatial` and `Microsoft.Restier.EntityFrameworkCore.Spatial` (the unit-test file constructs the converters directly).
- `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs` — new, unit tests.
- `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierQueryBuilderFilterBinderResolutionTests.cs` — new, regression test for the ctor-plumbing change.
- `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs` — flip existing negative, add eight positive + four negative cases.
- `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/SpatialPlace.cs` — add `RouteLine`.
- `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs` — seed `RouteLine` (EF6 path).
- `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Library/LibraryTestInitializer.cs` — seed `RouteLine` (EF Core path).

### Documentation files

- `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx` — remove `geo.*` entry from "What's not yet supported", add new "Server-side filtering with `geo.*` functions" section.
- `src/Microsoft.Restier.Docs/docs.json` — regenerated by docsproj build.

### Sample app changes

- `src/Microsoft.Restier.Samples.Postgres.AspNetCore/README.md` — optional example query.

### Solution

- `RESTier.slnx` — no project additions. All work lands in existing projects.

### Not changed in Spec B

- `Microsoft.Restier.Core` — no new abstractions. `ISpatialTypeConverter` is reused as-is from Spec A.
- The model-builder convention from Spec A — Spec B operates entirely on the bound LINQ expression tree; no EDM-model changes.
- `RestierPayloadValueConverter` — read path unchanged.
- `EFChangeSetInitializer` (both flavors) — write path unchanged.
- `Microsoft.Restier.EntityFramework.Spatial.csproj` and `Microsoft.Restier.EntityFrameworkCore.Spatial.csproj` — no new project/package references. Both packages stay host-agnostic (no AspNetCore dependency). The binder registration moves to `Microsoft.Restier.AspNetCore` precisely to preserve this invariant.
- Flavor `.Spatial` packages' `Extensions/ServiceCollectionExtensions.cs` (`AddRestierSpatial`) — Spec A's registrations are sufficient. No Spec B additions.

## Out of scope (deferred)

| Deferred to | Item |
|-------------|------|
| Spec C | Source-generator or convention-driven sugar for users who prefer Microsoft.Spatial-typed entity properties (inverse of Spec A's storage-typed model). |
| Later | `$orderby=geo.distance(...)` translation. |
| Later | Default-SRID configuration (per-API or per-property). The fail-fast non-EPSG behavior from Spec A is preserved by Spec B without a configurable default. |
| Later | `Edm.Stream` for very-large geometries. |
| Later | OData v4 spatial functions beyond the core three (`geo.coveredby`, `geo.contains`, `geo.within`, `geo.area`, ...) — currently fall through to the base binder (HTTP 400). |
| Later | Provider-specific spatial extension methods (NTS `IsValid`, PostGIS `ST_*`, etc.) exposed via OData function imports. |
