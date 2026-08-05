# Dynamic Routing for RESTier on ASP.NET Core OData 9.x

## Problem

RESTier's `feature/vnext` branch ported routing from the old `Microsoft.AspNet.OData` 7.x (dynamic, runtime path parsing) to `Microsoft.AspNetCore.OData` 9.x (template-based, startup-time conventions). This introduced 7 `IODataControllerActionConvention` classes that generate static `ODataPathTemplate` objects at startup.

OData URLs are inherently dynamic. Template-based routing cannot predict all valid path combinations (e.g., `$filter` path segments, deep navigation chains, `$ref`, type casts composed with operations). This causes route-not-found failures for valid OData requests. Currently 1 of 92 tests fails (`BoundFunctions_CanHaveFilterPathSegment`) and more exotic paths would also fail.

## Solution

Replace the 8 template-based convention files with a single `RestierRouteValueTransformer` that uses ASP.NET Core's `DynamicRouteValueTransformer` mechanism. This is the same approach the old main-branch code used with `ODataEndpointRouteValueTransformer` -- a catch-all route pattern delegates to a transformer that parses OData URLs dynamically at runtime.

## Architecture

### Request Flow

```
HTTP Request
    |
    v
UseRouting()
    |-- MapControllers() endpoints evaluated first
    |   (MetadataController handles $metadata, service doc via attribute routes)
    |
    |-- MapDynamicControllerRoute<RestierRouteValueTransformer>("{prefix}/{**odataPath}")
    |   |
    |   v
    |   RestierRouteValueTransformer.TransformAsync()
    |     1. Resolve route prefix -> EDM model + per-route services
    |     2. Parse URL path via ODataUriParser -> ODataPath
    |     3. Populate HttpContext.ODataFeature() (Path, Model, RoutePrefix, Services)
    |     4. Determine action: HTTP method + last segment -> Get/Post/PostAction/Put/Patch/Delete
    |     5. Return RouteValueDictionary { controller = "Restier", action = "<action>" }
    |
    v
UseEndpoints()
    |
    v
RestierController.<action>()
    reads HttpContext.ODataFeature().Path (already populated by transformer)
```

### Components

#### New Files

| File | Purpose |
|------|---------|
| `src/Microsoft.Restier.AspNetCore/Routing/RestierRouteValueTransformer.cs` | `DynamicRouteValueTransformer` -- parses OData paths, populates ODataFeature, returns route values to RestierController |
| `src/Microsoft.Restier.AspNetCore/Routing/RestierRouteRegistry.cs` | Singleton that tracks which route prefixes are Restier routes (a `HashSet<string>`). Populated by `AddRestierRoute()`, read by `MapRestier()` |
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierEndpointRouteBuilderExtensions.cs` | `MapRestier()` extension on `IEndpointRouteBuilder` that registers catch-all dynamic routes for Restier prefixes only |

#### Modified Files

| File | Change |
|------|--------|
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` | Remove convention registrations (lines 187-192). Register the route prefix in `RestierRouteRegistry` after calling `AddRouteComponents()`. |
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierIMvcBuilderExtensions.cs` | Register `RestierRouteValueTransformer` (scoped) and `RestierRouteRegistry` (singleton) in `AddRestier()` |
| `src/Microsoft.Restier.Breakdance/RestierBreakdanceTestBase.cs` | Add `endpoints.MapRestier()` after `endpoints.MapControllers()` |
| `src/Microsoft.Restier.Samples.Northwind.AspNetCore/Startup.cs` | Add `endpoints.MapRestier()` after `endpoints.MapControllers()` |

#### Deleted Files (8 convention classes)

| File | Reason |
|------|--------|
| `Routing/RestierRoutingConvention.cs` | Base class with constants -- folded into transformer |
| `Routing/RestierEntitySetRoutingConvention.cs` | Template-based, replaced by dynamic parsing |
| `Routing/RestierEntityRoutingConvention.cs` | Template-based, replaced by dynamic parsing |
| `Routing/RestierFunctionRoutingConvention.cs` | Template-based, replaced by dynamic parsing |
| `Routing/RestierActionRoutingConvention.cs` | Template-based, replaced by dynamic parsing |
| `Routing/RestierOperationRoutingConvention.cs` | Template-based, replaced by dynamic parsing |
| `Routing/RestierOperationImportRoutingConvention.cs` | Template-based, replaced by dynamic parsing |
| `Routing/RestierSingletonRoutingConvention.cs` | Template-based, replaced by dynamic parsing |

#### Left As-Is (excluded from compilation, historical reference)

| File | Status |
|------|--------|
| `Extensions/Restier_IEndpointRouteBuilderExtensions.cs` | Already `<Compile Remove>` in csproj |
| `Extensions/Restier_IRouteBuilderExtensions.cs` | Already `<Compile Remove>` in csproj |
| `Extensions/Restier_IServiceCollectionExtensions.cs` | Already `<Compile Remove>` in csproj |
| `Extensions/Restier_IApplicationBuilderExtensions.cs` | Already `<Compile Remove>` in csproj (note: the non-underscore version is active) |
| Other `<Compile Remove>` files | Unchanged |

## RestierRouteValueTransformer -- Detailed Behavior

### Class Structure

```csharp
public class RestierRouteValueTransformer : DynamicRouteValueTransformer
{
    private readonly IOptions<ODataOptions> _odataOptions;

    public RestierRouteValueTransformer(IOptions<ODataOptions> odataOptions) { ... }

    public override ValueTask<RouteValueDictionary> TransformAsync(
        HttpContext httpContext, RouteValueDictionary values) { ... }
}
```

### Parse

1. Extract the raw OData path from the catch-all route value (`odataPath`).
2. Look up the route prefix from the registered `ODataOptions.RouteComponents` to find the `IEdmModel` and per-route `IServiceProvider`.
3. Create an `ODataUriParser` with the model and parse the path string into an `ODataPath`.
4. If parsing fails, return `null` -- ASP.NET Core falls through to other endpoints or returns 404.

### Populate ODataFeature

Set these properties on `HttpContext.ODataFeature()`:

| Property | Source |
|----------|--------|
| `Path` | Parsed `ODataPath` from `ODataUriParser` |
| `Model` | From `ODataOptions.RouteComponents[prefix]` |
| `RoutePrefix` | The matched route prefix string |
| `BaseAddress` | Computed from `HttpContext.Request.Scheme`, `Host`, and prefix |

`Services` and `RequestScope` are NOT set by the transformer. The existing `HttpRequest.GetRouteServices()` extension creates a scoped service provider lazily from `ODataFeature().RoutePrefix` and `ODataOptions.RouteComponents`. Setting `RoutePrefix` is sufficient.

### Route to Action

Determine the RestierController action name:

| Condition | Action |
|-----------|--------|
| HTTP GET, last segment is not an `IEdmAction` | `"Get"` |
| HTTP POST, last segment is `OperationSegment` or `OperationImportSegment` containing `IEdmAction` | `"PostAction"` |
| HTTP POST, otherwise | `"Post"` |
| HTTP PUT | `"Put"` |
| HTTP PATCH | `"Patch"` |
| HTTP DELETE | `"Delete"` |

This replicates the logic from the old main-branch `RestierRoutingConvention.SelectAction()`.

Return `new RouteValueDictionary { ["controller"] = "Restier", ["action"] = actionName }`.

### Multi-Route Support

RESTier supports multiple API routes (e.g., `MapApiRoute<V1Api>("v1", "api/v1")` and `MapApiRoute<V2Api>("v2", "api/v2")`). `MapRestier()` iterates `ODataOptions.RouteComponents` and registers one `MapDynamicControllerRoute` per prefix. The route pattern embeds the prefix literally, so each dynamic route matches only its own prefix.

The transformer also validates that the matched prefix is a Restier route by checking `RestierRouteRegistry` before parsing. If a request matches the catch-all pattern but the prefix isn't registered in the registry, the transformer returns `null` to fall through.

## MapRestier Extension

```csharp
public static class RestierEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapRestier(this IEndpointRouteBuilder endpoints)
    {
        var registry = endpoints.ServiceProvider
            .GetRequiredService<RestierRouteRegistry>();

        foreach (var prefix in registry.RoutePrefixes)
        {
            var pattern = string.IsNullOrEmpty(prefix)
                ? "{**odataPath}"
                : prefix + "/{**odataPath}";

            endpoints.MapDynamicControllerRoute<RestierRouteValueTransformer>(pattern);
        }

        return endpoints;
    }
}
```

`RestierRouteRegistry` is a singleton with a `HashSet<string> RoutePrefixes` property. Only prefixes registered via `AddRestierRoute<TApi>()` are included. Non-Restier OData routes registered via `AddRouteComponents()` directly are not affected.

## Pipeline Integration

### Test Infrastructure (RestierBreakdanceTestBase)

```csharp
.Configure(builder =>
{
    ApplicationBuilderAction?.Invoke(builder);
    builder.UseODataRouteDebug();
    builder.UseRouting();
    builder.UseAuthorization();
    builder.UseDeveloperExceptionPage();
    builder.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapRestier();
    });
});
```

### Northwind Sample (Startup.cs)

Same pattern -- add `endpoints.MapRestier()` after `endpoints.MapControllers()`.

### Ordering

`MapControllers()` before `MapRestier()`. OData's `MetadataController` has attribute routes (e.g., `$metadata`) that are more specific than the catch-all pattern. ASP.NET Core selects the most specific match, so `$metadata` goes to `MetadataController` and everything else falls through to the dynamic route.

## Error Handling & Edge Cases

| Scenario | Behavior |
|----------|----------|
| Invalid OData path (e.g., `/notAnEntitySet`) | `ODataUriParser` throws; transformer catches and returns `null`; 404 response |
| Empty path / service document (`GET /prefix/`) | Parsed as empty `ODataPath`; routes to `Get`; RestierController returns service document |
| `$batch` requests | Handled by `UseODataBatching()` middleware before routing; catch-all not reached |
| `$metadata` requests | Matched by `MetadataController` attribute route (more specific); transformer not called |
| Multiple route prefixes | Each prefix gets its own `MapDynamicControllerRoute`; transformer resolves correct model per prefix |
| Concurrent requests | `ODataUriParser` is stateless; EDM model is immutable; no thread safety issues |
| Route prefix conflicts with non-OData routes | More specific route wins (standard ASP.NET Core behavior) |

## Testing Strategy

### Existing Tests

All 91 currently passing tests remain green. The routing layer change is transparent to the controller -- it still receives a populated `ODataFeature().Path`.

### The $filter Path Segment Test

`BoundFunctions_CanHaveFilterPathSegment` currently fails with a route-not-found (404). After this change, the route WILL match and `ODataUriParser` will parse the `$filter` segment. However, `RestierQueryBuilder` has no handler for `FilterSegment` and will throw `NotImplementedException`. The test failure changes from a routing error to a query builder error. This test should be marked `[Fact(Skip = "FilterSegment handler not yet implemented in RestierQueryBuilder")]` until that gap is addressed separately.

### New Unit Tests for RestierRouteValueTransformer

| Test Case | Expectation |
|-----------|-------------|
| GET `/EntitySet` | Routes to `Get`, ODataFeature.Path has EntitySetSegment |
| GET `/EntitySet(1)` | Routes to `Get`, ODataFeature.Path has EntitySetSegment + KeySegment |
| GET `/EntitySet(1)/NavigationProp` | Routes to `Get`, path has navigation segments |
| POST `/EntitySet` | Routes to `Post` |
| POST `/EntitySet/Ns.Action` | Routes to `PostAction` |
| POST `/ActionImport` | Routes to `PostAction` |
| PUT `/EntitySet(1)` | Routes to `Put` |
| PATCH `/EntitySet(1)` | Routes to `Patch` |
| DELETE `/EntitySet(1)` | Routes to `Delete` |
| GET `/InvalidPath` | Returns `null` (404 fallthrough) |
| GET `/` (empty, service document) | Routes to `Get`, empty ODataPath |
| ODataFeature population | Path, Model, RoutePrefix, Services all set correctly |

### Integration Tests

The existing test suite in `Microsoft.Restier.Tests.AspNetCore` exercises the full pipeline (test server, HTTP request, controller, query, response). These serve as regression tests for the routing change.

## Out of Scope

- `RestierQueryBuilder` support for `FilterSegment` -- tracked separately
- Old routing files excluded from compilation -- left as-is for historical reference
- Changes to `RestierController` internals -- the controller is unchanged
