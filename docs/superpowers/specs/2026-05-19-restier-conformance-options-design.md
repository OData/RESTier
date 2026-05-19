# RestierRouteOptions and Opt-In OData Conformance

**Date:** 2026-05-19
**Status:** Design approved
**GitHub Issue:** https://github.com/OData/RESTier/issues/735

## Goal

Close the last OData-spec gap from issue #735 — `GET /Entities(missing)/CollectionNav` currently returns `200 OK { "value": [] }` instead of `404 Not Found` — without forcing the change on everyone, while at the same time **consolidating per-route configuration into a single `RestierRouteOptions` object** that replaces the growing list of positional arguments on `AddRestierRoute`.

The conformance toggle is the user-visible deliverable. The options-object refactor is the API-surface housekeeping that makes future toggles cheap to add.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Default conformance behavior | Off (200 empty) | Preserves historical behavior; matches what the original #735 reporter wanted; no perf cost unless opted in |
| Scope of the toggle | Collection-valued nav from missing parent *only* | Single-entity-by-key cases already return 404 unconditionally per PR #614; toggle does not relax those |
| Settings class name | `RestierConformanceOptions` | Single-knob name leaves room for future spec-conformance toggles without renaming |
| Property name | `StrictMissingParentForCollections` | Descriptive over short; mentions both the trigger (missing parent) and the affected request shape (collection) |
| Route-level options container | `RestierRouteOptions` | Single bag for all per-route configuration; sub-objects for the existing `DeepOperationSettings` and the new `RestierConformanceOptions`; flat properties for `UseRestierBatching` and `NamingConvention` |
| Public API surface | Exactly two `AddRestierRoute` overloads | Existing four overloads are removed (breaking change, acceptable on `feature/vnext`) |
| `routePrefix` placement | Stays as a positional argument | Route identity, not configuration; forcing it through options hurts ergonomics |
| `ConfigureServices` placement | Stays as its own `Action<IServiceCollection>` parameter | DI registration is a different concern from settings; mixing them in one object muddies both |
| `ODataOptions` membership | *Not* folded into `RestierRouteOptions` | Owned by `AddOData()`, lives at a different scope (one container, many routes) |
| DI precedence (options vs. `configureRouteServices`) | Options bag wins | `AddSingleton` (not `TryAdd`) applied *after* `configureRouteServices` runs; the bag is the single canonical source for these settings, no silent dual paths |
| Versioning layer migration | Replace positional `useRestierBatching` + `namingConvention` on `AddVersion` with `Action<RestierRouteOptions>` | `Microsoft.Restier.AspNetCore.Versioning` reflects into the core overload; spec must carry the change through to keep that package compiling |

## Background

PR #614 (commit `45a9e1dd`, Apr 2026) brought RESTier into OData v4 compliance for the single-entity-by-key case: `GET /Books(missing)` now returns 404 instead of 204, and `ParentEntityExistsAsync` covers nested cases such as `GET /Books(missing)/Publisher` and `GET /Publishers('P1')/Books(missing)`. See `src/Microsoft.Restier.AspNetCore/RestierController.cs:680`.

One case remained: collection-valued navigation from a missing parent (`GET /Books(missing)/Reviews`). The current `CreateQueryResponse` collection branch (line 621–630) constructs an empty `ResourceSetResult` without consulting parent existence. Per OData v4 Protocol Part 1 §9.1.5 and §11.2.6, this should be 404 because the addressed resource (the collection-of-Reviews-belonging-to-Books(missing)) does not exist.

The cost of strict checking is one extra parent-existence query per collection-nav request whose path contains a key segment. We can't tell from a deferred `IQueryable` whether a collection is empty without materializing it, and even if we could, "empty because parent missing" and "empty because no related items" are indistinguishable from the query result alone — so the parent check has to run unconditionally whenever strict mode is on. For APIs that don't need spec strictness, paying that cost on every such request is unwanted — hence opt-in.

While we're touching the registration surface, the existing pattern of growing positional arguments on `AddRestierRoute` (currently `useRestierBatching` and `namingConvention`, soon also `DeepOperationSettings` and `RestierConformanceOptions`) does not scale. Consolidating into a single options object stabilizes the signature against future additions.

## Architecture

### New types

**`RestierConformanceOptions`** in `Microsoft.Restier.Core`:

```csharp
namespace Microsoft.Restier.Core;

/// <summary>
/// Opt-in toggles for stricter OData-spec conformance. Defaults preserve
/// RESTier's existing pragmatic behavior.
/// </summary>
public class RestierConformanceOptions
{
    /// <summary>
    /// When <c>true</c>, requests to a collection-valued navigation property
    /// whose parent entity does not exist (e.g. <c>/Books(missing)/Reviews</c>)
    /// return <c>404 Not Found</c> per OData v4 Part 1 §9.1.5 / §11.2.6.
    /// When <c>false</c> (default), an empty collection
    /// (<c>200 OK { "value": [] }</c>) is returned, matching RESTier's
    /// historical behavior. Setting this to <c>true</c> incurs one extra
    /// parent-existence query per collection-nav request whose path
    /// includes a key segment.
    /// </summary>
    public bool StrictMissingParentForCollections { get; set; }
}
```

**`RestierRouteOptions`** in `Microsoft.Restier.Core`:

```csharp
namespace Microsoft.Restier.Core;

/// <summary>
/// Per-route configuration for a Restier route. Pass an
/// <c>Action&lt;RestierRouteOptions&gt;</c> to <c>AddRestierRoute</c> to
/// customize batching, naming convention, deep-operation depth, and
/// OData-spec conformance.
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
    /// Naming convention applied to EDM property names and the resulting JSON.
    /// </summary>
    public RestierNamingConvention NamingConvention { get; set; }
        = RestierNamingConvention.PascalCase;
}
```

Both classes are mutable; `RestierRouteOptions` exposes `DeepOperations` and `Conformance` as get-only properties initialized to fresh instances, so callers tweak them in-place rather than reassigning.

### Replaced API surface

The four existing `AddRestierRoute` overloads (two with `routePrefix`, two without; each with positional `useRestierBatching` and `namingConvention`) are **removed**. They are replaced by exactly two overloads:

```csharp
public static ODataOptions AddRestierRoute<TApi>(
    this ODataOptions oDataOptions,
    string routePrefix,
    Action<IServiceCollection> configureRouteServices)
    where TApi : ApiBase;

public static ODataOptions AddRestierRoute<TApi>(
    this ODataOptions oDataOptions,
    string routePrefix,
    Action<IServiceCollection> configureRouteServices,
    Action<RestierRouteOptions> configureOptions)
    where TApi : ApiBase;
```

`routePrefix` is required positionally. Pass `string.Empty` for an unprefixed route. The unprefixed convenience overloads from before are dropped — `""` is two extra characters and removes the ambiguity of which overload you're hitting.

The two-overload variant builds a `RestierRouteOptions`, invokes the user's `configureOptions` callback against it, then forwards into a single internal registration helper. The one-overload variant defers to the two-overload form with a `null` `configureOptions`, which produces all-default settings.

### Versioning layer impact

`Microsoft.Restier.AspNetCore.Versioning` depends on the removed `AddRestierRoute` shape in three places, all of which must move with the core surface:

1. **`IRestierApiVersioningBuilder`** (`src/Microsoft.Restier.AspNetCore.Versioning/IRestierApiVersioningBuilder.cs`) currently exposes two `AddVersion` overloads with positional `useRestierBatching` and `namingConvention`. These two parameters are replaced by a single `Action<RestierRouteOptions> configureOptions` (defaulting to `null`), mirroring the new core overload:

   ```csharp
   IRestierApiVersioningBuilder AddVersion<TApi>(
       string basePrefix,
       Action<IServiceCollection> configureRouteServices,
       Action<RestierVersioningOptions> configureVersioning = null,
       Action<RestierRouteOptions> configureOptions = null)
       where TApi : ApiBase;

   IRestierApiVersioningBuilder AddVersion<TApi>(
       ApiVersion apiVersion,
       bool deprecated,
       string basePrefix,
       Action<IServiceCollection> configureRouteServices,
       Action<RestierVersioningOptions> configureVersioning = null,
       Action<RestierRouteOptions> configureOptions = null)
       where TApi : ApiBase;
   ```

2. **`PendingVersionRegistration`** (`Internal/PendingVersionRegistration.cs`) drops the `UseRestierBatching` and `NamingConvention` properties and gains an `Action<RestierRouteOptions> ConfigureOptions` property carrying the per-version callback through to the configurator.

3. **`RestierApiVersioningOptionsConfigurator.ApplyOne`** (`Internal/RestierApiVersioningOptionsConfigurator.cs`, line ~109) uses reflection to find the 5-parameter `AddRestierRoute` overload. That reflection is rewritten to find the new 4-parameter overload (`ODataOptions`, `string`, `Action<IServiceCollection>`, `Action<RestierRouteOptions>`) and invoke it with `pending.ConfigureOptions`:

   ```csharp
   var addRestierRoute = typeof(RestierODataOptionsExtensions)
       .GetMethods(BindingFlags.Public | BindingFlags.Static)
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

The versioning unit and integration tests are updated alongside these changes. Any sample app or doc page that calls `AddVersion<...>` with positional `useRestierBatching` / `namingConvention` is updated to use `configureOptions` in the same PR.

### Controller change

`RestierController.CreateQueryResponse` (`src/Microsoft.Restier.AspNetCore/RestierController.cs`) gains one block immediately before the existing `if (typeReference.IsCollection())` at line 621:

```csharp
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

`ParentEntityExistsAsync` is the existing helper introduced by PR #614 (line 680). No changes to it.

### DI registration and precedence

**Decision:** the options bag is authoritative for `DeepOperationSettings` and `RestierConformanceOptions`. Registrations of those types inside `configureRouteServices` are *not* a supported way to override them — the new `configureOptions` callback is.

Implementation: register the bag's instances via `AddSingleton` (not `TryAddSingleton`) **after** `configureRouteServices` has run. Last writer wins, and the bag writes last:

```csharp
services.AddSingleton(typeof(RestierNamingConvention), (object)options.NamingConvention);
// ... other Restier core services ...
configureRouteServices?.Invoke(services);
services.AddSingleton(options.DeepOperations);    // authoritative
services.AddSingleton(options.Conformance);       // authoritative
// ... rest of the body ...
```

This is a behavior change vs. today's `TryAddSingleton(new DeepOperationSettings())`: callers who previously registered `DeepOperationSettings` themselves from inside `configureRouteServices` will silently lose that registration. The migration story in the documentation calls this out and points them at `configureOptions` instead.

### Configuration flow

```
AddRestierRoute<TApi>(routePrefix, configureRouteServices, configureOptions)
    |
    v
new RestierRouteOptions() — defaults
    |
    v
configureOptions?.Invoke(options) — caller mutates the bag
    |
    v
AddRouteComponents(routePrefix, model, services => {
    // Restier core services registered first
    ...
    configureRouteServices?.Invoke(services);     // user DI
    services.AddSingleton(options.DeepOperations); // bag wins
    services.AddSingleton(options.Conformance);
    ...
})
    |
    v
At request time: RestierController resolves RestierConformanceOptions
                 from route DI; reads StrictMissingParentForCollections
                 in CreateQueryResponse before returning 200 empty.
```

## Tests

**Test-helper change.** `RestierTestHelpers.ExecuteTestRequest` and the related setup helpers gain a new optional `Action<RestierRouteOptions> configureOptions = null` parameter and pass it through to `AddRestierRoute(prefix, services, options)`. This is the channel by which feature tests configure `RestierRouteOptions` on the public API path — not by registering settings via the existing `serviceCollection` parameter. Without this, the new tests would not actually exercise the new `configureOptions` overload.

Three new `[Fact]`s in `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/QueryTests.cs`:

1. **`CollectionNavFromMissingParentReturns200ByDefault`** — `GET /Books(00000000-...)/Reviews` with no `configureOptions` argument, asserts `200 OK`. Locks in the default behavior and proves the one-overload variant still produces a working route.

2. **`CollectionNavFromMissingParentReturns404WhenStrict`** — same request, but with `configureOptions: o => o.Conformance.StrictMissingParentForCollections = true` passed through `ExecuteTestRequest`. Asserts `404 Not Found`. This test must fail if the new `AddRestierRoute(prefix, services, options)` overload is removed or its wiring is broken, so it verifies the full public path: callback → bag → DI → controller.

3. **`CollectionNavFromExistingParentReturns200EmptyWhenStrict`** — `GET /Publishers('Publisher1')/Books` (existing publisher) with the same `configureOptions` strict toggle. Asserts `200 OK` — confirms strict mode doesn't false-positive on empty-but-valid collections, and again exercises the public callback path.

Plus call-site updates throughout the test suite to migrate from the removed overloads to the new two-overload surface, and parallel updates to the versioning test helpers and tests so they go through `configureOptions` rather than the removed positional arguments.

## Documentation

A new Mintlify guide page `src/Microsoft.Restier.Docs/guides/conformance-options.mdx` covering:

- What `RestierRouteOptions` is and how it replaces the older positional parameters.
- The `Conformance.StrictMissingParentForCollections` toggle: what it does, when to enable it (strict OData clients, full v4 spec compliance), and the performance trade-off.
- A migration example showing old vs. new `AddRestierRoute` call shapes for users upgrading from earlier `feature/vnext` snapshots.

The page is added to `<MintlifyTemplate>` in `Microsoft.Restier.Docs.docsproj` so the SDK regenerates `docs.json` with the new entry on the next build.

Existing pages that show `AddRestierRoute` call samples (any quickstart/guide using `useRestierBatching` or `namingConvention` positionally) are updated to the new form in the same change.

## Out of scope

- Folding `ODataQuerySettings`, `ODataValidationSettings`, or `ODataOptions` itself into `RestierRouteOptions`. Those are owned by `AddOData()` or by the OData library and have their own configuration entry points.
- Adding a second conformance toggle (e.g., strict `$expand` handling, strict null-property semantics). The class is named to allow it, but no second toggle is added now.
- Touching the AspNet (legacy) controller. That project was removed in commit `70fa1ae1`; only the AspNetCore controller remains.
- Changing the single-entity-by-key 404 behavior from PR #614. That stays unconditional.

## Breaking changes

`feature/vnext` is pre-release, so breaking changes are acceptable. The cleanup is:

- **Core (`Microsoft.Restier.AspNetCore`):** four existing `AddRestierRoute` overloads removed. Replaced by two new overloads with the same `routePrefix` + `configureRouteServices` shape, plus an optional `configureOptions` action. `useRestierBatching` and `namingConvention` no longer take positional arguments — they move onto `RestierRouteOptions`. Call sites that omitted `routePrefix` (relying on the unprefixed convenience overload) must now pass `string.Empty` explicitly.
- **Versioning (`Microsoft.Restier.AspNetCore.Versioning`):** both `AddVersion<TApi>` overloads on `IRestierApiVersioningBuilder` lose their positional `useRestierBatching` and `namingConvention` parameters; they gain an optional `Action<RestierRouteOptions> configureOptions`. `PendingVersionRegistration` and `RestierApiVersioningOptionsConfigurator` are updated to carry and forward the callback.
- **DI behavior:** users who previously registered `DeepOperationSettings` directly inside `configureRouteServices` will find that registration silently replaced by the bag's instance (`AddSingleton` after `configureRouteServices`). The supported path is now `configureOptions`.

A migration note will live alongside the conformance-options doc page covering all of the above.
