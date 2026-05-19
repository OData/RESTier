# Keyless EF Views as Read-Only RESTier Resources

**Date:** 2026-05-19
**Status:** Design draft — second revision after code-review pushback (awaiting confirmation)
**Issue:** [OData/RESTier#741](https://github.com/OData/RESTier/issues/741) (predecessor: [#692](https://github.com/OData/RESTier/issues/692))

## Goal

Expose EF Core `[Keyless]` / `HasNoKey()` and EF6 keyless `DbSet<T>` / `DbQuery<T>` entities — typically database views — as read-only RESTier resources, so a single Restier API can serve both tables and views without forcing users to hand-author `[UnboundOperation]` complex-type wrappers. The current behaviour (throw `InvalidOperationException` at model-build time with a message that tells the user to do exactly that wrapping themselves) is replaced with automatic complex-type + function-import wiring through `EFModelBuilder`. Both EF flavours behave identically from the consumer's perspective.

`GET /odata/BooksByPublisher()` (function-call URL, parens required) returns the rows; `$filter`, `$select`, `$orderby`, `$top`, `$skip` work as normal OData query options applied by AspNetCore.OData over the returned `IQueryable`; **all four write verbs (POST, PUT, PATCH, DELETE) return HTTP 405**. POST already has a function-import branch in `RestierController.Post`; we add a parallel branch to `RestierController.Delete` and the private `Update` method so PUT/PATCH/DELETE return 405 instead of throwing `NotImplementedException` (HTTP 500). Convention interceptors (`OnFiltering<View>` etc.) **do not fire** in v1 — that requires widening `ConventionBasedQueryExpressionProcessor` to function-import model references and is deferred to a follow-up spec.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| URL shape | Function-import call: `GET /odata/BooksByPublisher()` (parens required) | OData v4 entity sets require keyed entity types per spec — this is unchanged in OData 8 / AspNetCore.OData 9 (Microsoft Learn: *"abstract entity type without keys can't be used to define any navigation sources"*). Function imports over `Collection(ComplexType)` are the spec-aligned shape and what the original RESTier error message already pointed users to. |
| EDM modelling | `ComplexType<T>` + unbound `FunctionImport` named after the DbSet/EntitySet returning `Collection(<ComplexType>)` | Smallest spec-aligned surface. No synthetic keys (would lie about the data model and expose insert/update/delete URLs we'd then have to hand-block). No singleton (singletons return one entity, not a collection). |
| Dispatch | Registry-based fallback inside `RestierOperationExecutor` — no EDM annotations | A single shared `KeylessViewRegistry` (defined in `Microsoft.Restier.Core`, populated by EF model builders, lifetime-bridged into the route container) maps function-import name → CLR type + source factory. The executor's existing "method by name" lookup falls through to the registry when no API method matches. Avoids leaking RESTier-private vocabulary terms into `$metadata` and keeps OData-Core unaware of the feature. |
| Source factory | Captured at model-build time, EF-flavour-specific | EF Core: reflection on the DbSet property. EF6 DbSet/DbQuery-backed: reflection on the property. EF6 EDMX-only (no CLR property): `((IObjectContextAdapter)ctx).ObjectContext.CreateQuery<T>("[Container].[EntitySet]")`. The executor stays EF-agnostic — it only ever invokes `Func<object api, IQueryable>`. |
| Query pipeline integration (v1) | Executor returns the factory's `IQueryable` *directly*, bypassing `api.QueryAsync` | `ApiBase.QueryAsync` only accepts `QueryableSource<T>` requests (`ApiBase.cs:77-80`), produced via `api.GetQueryableSource<T>(name)`, which in turn requires the name to resolve through `IModelMapper` — and the mapper currently maps only entity sets/singletons, not function imports (`RestierModelMapper.cs:40-67`; the second overload has an explicit `TODO GitHubIssue#39` for composable function imports). Wiring keyless views into the query pipeline would require new mapper + sourcer entries *and* convention-processor changes. Out of scope for v1. AspNetCore.OData's query-option layer still applies `$filter`/`$select`/`$orderby`/`$top`/`$skip` to the returned `IQueryable` at the OData layer — that path is independent of `api.QueryAsync`. |
| Convention interceptors (`OnFiltering<View>` etc.) | **Not fired in v1** | `ConventionBasedQueryExpressionProcessor.Process` returns null unless `context.ModelReference.Element is IEdmEntitySet` whose element type is `IEdmEntityType` (`ConventionBasedQueryExpressionProcessor.cs:51-66`). A function-import-with-Collection-of-ComplexType return doesn't satisfy either condition. v1 documents this as a limitation and points users at `[Authorize]` on the function import, or row-filtering in the view SQL, for security. A follow-up spec can widen the convention processor to recognise function-import model references. |
| Writes | 405 Method Not Allowed for all four verbs | POST hits the existing `OperationImportSegment + IsFunctionImport` branch at `RestierController.cs:178-182` and returns `MethodNotAllowed()`. PUT/PATCH/DELETE today throw `NotImplementedException` (HTTP 500) for non-entity-set paths (`RestierController.cs:315, :441`). v1 adds a matching guard at the top of `Delete` and the private `Update` method so all four verbs return 405. No submit-pipeline plumbing. |
| EF flavour parity | EF6 + EF Core both ship in this spec | EF6 keyless detection is currently a *silent-bug* path (empty `keyProperties` list, not `null`, so the existing throw at `EFModelBuilder.cs:141` doesn't fire, and downstream OData chokes on a zero-key entity type). Normalising both flavours unblocks EF6 and EFCore at once. |
| EF6 EDMX-only fallback | Included in v1 (not deferred) | EF6 customers are heavily EDMX-first; restricting the feature to property-backed views would miss a large slice of the audience. The fallback is small (one `ObjectContext.CreateQuery<T>` call) and isolated to the EF6 partial. |
| Detection criterion (both flavours) | Key collection is `null` OR empty | EFCore reports `null` for `FindPrimaryKey()` on keyless; EF6's `efEntityType.KeyProperties` returns an empty `ReadOnlyMetadataCollection<EdmMember>`. Normalising to "missing-or-empty ⇒ keyless" makes the shared `BuildEdmModelFromEntitySetMaps` symmetric. |

## Background

### Issue history

`#692` (closed 2022) opened with a `NullReferenceException` when an EF Core `[Keyless]` view hit `EFModelBuilder`. The fix at that time was to convert the NRE into the more informative `InvalidOperationException` that exists today at `src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs:139-144`:

```csharp
if (pair.Value is null)
{
    throw new InvalidOperationException(
        $"The entity '{pair.Key}' does not have a key specified. "
        + $"Entities tagged with the [Keyless] attribute (or otherwise do not have a key specified) "
        + $"are not supported in either OData or Restier. "
        + $"Please map the object as a ComplexType and implement as an [UnboundOperation] on your API instead.");
}
```

The existing `EFModelBuilder_Should_HandleViews` test in `Microsoft.Restier.Tests.EntityFrameworkCore/EFModelBuilderTests.cs` asserts that throw. `#741` reopens the design question: with the current OData/AspNetCoreOData stack, can RESTier do that complex-type-plus-unbound-operation wrapping *automatically* so views feel like first-class resources?

### Why function imports, not entity sets

OData v4 `§3.4` and the Microsoft Learn ["Abstract entity types"](https://learn.microsoft.com/odata/webapi/abstract-entity-types) page agree: an entity type that has no key cannot back a navigation source (entity set or singleton). Calling `ODataConventionModelBuilder.EntitySet<T>("X")` with a keyless `T` either fails at `GetEdmModel()` (the documented case) or produces invalid metadata that breaks ODL routing/parsing downstream. AspNetCore.OData 9 has not relaxed this — the spec hasn't changed.

The spec-aligned shape for "callable collection of values that aren't entities" is a `ComplexType` exposed via an unbound `FunctionImport` whose return type is `Collection(<ComplexType>)`. ODL supports `$filter`/`$select`/`$orderby`/`$top`/`$skip` on function imports the same way it does on entity sets, and AspNetCore.OData's `OperationImportRoutingConvention` already wires the URL `GET /odata/Foo()` to controller dispatch via `OperationImportSegment`.

### Why the registry, not annotations

The `RestierOperationExecutor` (`src/Microsoft.Restier.AspNetCore/Operation/RestierOperationExecutor.cs:78-85`) discovers operation implementations by reflective method lookup on the API class:

```csharp
var method = context.Api.GetType().GetMethod(
    restierOperationContext.OperationName,
    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

if (method is null)
{
    throw new NotImplementedException(AspNetResources.OperationNotImplemented);
}
```

Auto-generated keyless-view function imports have no backing API method by definition. Two options were considered:

1. **EDM annotation marker** — tag each generated import with a RESTier-private vocabulary term, dispatch on the annotation. Leaks an internal concern into `$metadata` consumed by clients, adds a dependency on AspNetCore.OData's annotation surface from the executor.
2. **Registry fallback** *(chosen)* — a `KeylessViewRegistry` (constructor-injected into `RestierOperationExecutor`) holds `{name → (clrType, sourceFactory)}`. The executor falls through to the registry on a null method lookup. Zero metadata pollution; one new class; localised change.

### Why the registry lives in Core, with a manual lifetime bridge

`RestierODataOptionsExtensions.AddRestierRoute` (`src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs:111-141`) builds the EDM model in a temporary `modelBuildingServiceProvider` that is *disposed* before `oDataOptions.AddRouteComponents(...)` constructs the per-route service container at line 148. A registry registered as a `Singleton` in `modelBuildingServices` and populated during `modelBuilder.GetEdmModel()` is on a different container than the one the request-time `RestierOperationExecutor` resolves from — the populated instance would be GC'd along with the model-building SP.

The existing precedent is `RestierWebApiModelExtender`: registered into `modelBuildingServices` (line 117), captured into a local `modelExtender` variable *before* the `finally`-clause disposal (line 132), then re-registered into the route services as `AddSingleton(modelExtender)` (line 181). The keyless-view registry follows the same shape — three local captures across the dispose boundary instead of two.

This dictates two design choices: (1) the registry class lives in `Microsoft.Restier.Core` (no EF dependency, so `AddRestierRoute` can reference it without leaking layering), and (2) it's constructor-injected into `RestierOperationExecutor`, since `ApiBase` doesn't expose a service provider for ad-hoc resolution.

## Design

### Component overview

```text
┌──────────────────────────────────┐
│ EF Core DbContext                │
│   DbSet<KeylessT> (HasNoKey)     │
└───────────────┬──────────────────┘
                │
                ▼
┌──────────────────────────────────────────────────────────┐
│ EFModelBuilder (shared partial)                          │
│   • detect: key collection null OR empty                 │
│   • register T as ComplexType<T>                         │
│   • add unbound FunctionImport Foo() : Collection(T)     │
│   • record {Foo → (T, sourceFactory)} in registry        │
└───────────────┬──────────────────────────────────────────┘
                │  HTTP GET /odata/Foo()
                ▼
┌──────────────────────────────────────────────────────────┐
│ RestierController.Get                                    │
│   OperationImportSegment branch (already in place)       │
└───────────────┬──────────────────────────────────────────┘
                │
                ▼
┌──────────────────────────────────────────────────────────┐
│ RestierOperationExecutor.ExecuteOperationAsync           │
│   1. reflective method lookup → null                     │
│   2. NEW fallback: consult constructor-injected registry │
│   3. sourceFactory(api) → IQueryable                     │
│   4. return that IQueryable directly                     │
│      (AspNetCore.OData applies $filter / $select etc.    │
│       at the OData query-options layer)                  │
└──────────────────────────────────────────────────────────┘
```

### New / modified components

| Component | Change | Path |
|---|---|---|
| `KeylessViewRegistry` (new) | Plain class (not a DI service in the EF DI block — see lifetime-bridge component below). Members: `Register(string functionImportName, Type clrType, Func<object, IQueryable> sourceFactory)`, `TryGet(string name, out KeylessViewEntry entry)`. Entry stores name, CLR type, factory. Throws on duplicate name registration. **Lives in `Microsoft.Restier.Core` so the AspNetCore layer's `AddRestierRoute` can reference it without depending on EF.** | `src/Microsoft.Restier.Core/Model/KeylessViewRegistry.cs` |
| Lifetime bridge in `AddRestierRoute` | Add a third locally-captured object alongside `model` and `modelExtender`. Register `KeylessViewRegistry` into `modelBuildingServices`, capture the populated instance from the SP *before* the `finally` disposal, then `services.AddSingleton(keylessViewRegistry)` inside the `AddRouteComponents` lambda. Mirrors the `RestierWebApiModelExtender` bridge exactly. | `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs:111-181` |
| `EFModelBuilder<TDbContext>` shared partial — `BuildEdmModelFromEntitySetMaps` | Replace the `throw` at line 141. New branch: when `pair.Value` is null OR empty, demote to complex (split `entitySetMap` into `keyedEntitySets` and `keylessViewSets` *before* the convention builder iterates — see Implementation note below), call `builder.ComplexType<T>()`, add a function import on the container post-`GetEdmModel`, register in the `KeylessViewRegistry` resolved from the model-building SP. Takes `KeylessViewRegistry` as a constructor dependency (or passed through the partial-class signature). | `src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs` |
| `EFModelBuilder<TDbContext>` EF Core partial — `EntityFrameworkCoreGetEntities` | Already produces `null` for the keyless case. Additionally produce a `Dictionary<string, Func<object, IQueryable>>` of source factories keyed by DbSet property / entity-set name (reflection on the DbSet property captured at model-build time). Wire into the shared method's signature. | `src/Microsoft.Restier.EntityFrameworkCore/Model/EFModelBuilder.cs` |
| `EFModelBuilder<TDbContext>` EF6 partial — `EntityFramework6GetEntitySets` | Same factory dictionary. Discovery still iterates `efEntityContainer.EntitySets` (unchanged). Source-factory selection: prefer reflection on a context property whose type is assignable to `IQueryable<T>` (covers `DbSet<T>`, `IDbSet<T>`, `DbQuery<T>`); fall back to `((IObjectContextAdapter)ctx).ObjectContext.CreateQuery<T>("[Container].[EntitySet]")` when no such property exists (EDMX-only case). Also normalises *empty* `KeyProperties` lists to "keyless" so the shared throw-or-demote logic fires. | `src/Microsoft.Restier.EntityFramework/Model/EfModelBuilder.cs` |
| `RestierOperationExecutor` | Add a `KeylessViewRegistry` constructor parameter (route-DI resolves it; the lifetime bridge above guarantees it's the populated instance). In `ExecuteOperationAsync`: after the existing reflective method lookup, if `method is null`, try `registry.TryGet(OperationName, out var entry)`. On hit: `var iq = entry.SourceFactory(restierOperationContext.Api); return iq;` — return directly, no `api.QueryAsync` (see "v1 pipeline simplification" decision row). On miss: existing `throw new NotImplementedException`. | `src/Microsoft.Restier.AspNetCore/Operation/RestierOperationExecutor.cs` |
| `RestierController.Delete` + private `Update` method | Add the same `OperationSegment IsFunction()` / `OperationImportSegment IsFunctionImport()` early-return guard that `Post` already has, returning `MethodNotAllowed()`. Without this, PUT/PATCH/DELETE on a function-import URL throw `NotImplementedException` (HTTP 500). | `src/Microsoft.Restier.AspNetCore/RestierController.cs` |
| `AddEF6ProviderServices` / `AddEFCoreProviderServices` | **No direct registration** — the registry is registered by the AspNetCore-layer lifetime bridge described above. EF DI extension files are unchanged on this axis. | (no change) |

Registration site (verified against worktree):

- `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs:AddRestierRoute` — single location for the lifetime-bridge dance. Registers `KeylessViewRegistry` into `modelBuildingServices` (line ~117), captures the populated instance after `modelBuilder.GetEdmModel()` (around line ~132), re-registers the same instance into the route services lambda (around line ~181). No registration in the EF DI extension files — the registry is host-agnostic and the host (AspNetCore.OData route construction) owns the lifetime.

### Implementation note — "demote before iteration" vs "EntitySet then ignore"

The current shared `BuildEdmModelFromEntitySetMaps` iterates `entitySetMap` calling `builder.EntitySet<T>(name)` *before* it iterates `entitySetKeyMap`. If we call `EntitySet<T>` on a keyless type and only later realise we should have used `ComplexType<T>`, the convention builder has already inferred T as an entity type — the subsequent `ComplexType<T>` call is a no-op or throws. So the demote decision must happen before the first iteration.

Approach: pre-process `entitySetMap`. Split it into two dictionaries — `keyedEntitySets` and `keylessViewSets` — driven by the EF-flavour-specific code that builds the maps. The shared builder iterates `keyedEntitySets` for `EntitySet<T>` and `keylessViewSets` for `ComplexType<T>` + function-import addition. This keeps the shared file's structure recognisable and isolates the EF-flavour-specific keyless detection in the partials where it already belongs.

### Source-factory shape (locked-in)

```csharp
// EF Core (captured per CLR type inside EntityFrameworkCoreGetEntities):
Func<object, IQueryable> sourceFactory = api =>
{
    var dbContext = ((IEntityFrameworkApi)api).DbContext;
    var prop = dbContext.GetType().GetProperty(dbSetPropertyName);
    return (IQueryable)prop.GetValue(dbContext);
};

// EF6 — DbSet/DbQuery property path (preferred):
Func<object, IQueryable> sourceFactory = api =>
{
    var dbContext = ((IEntityFrameworkApi)api).DbContext;
    var prop = dbContext.GetType().GetProperty(propertyName);
    return (IQueryable)prop.GetValue(dbContext);
};

// EF6 — EDMX-only fallback (no CLR property):
Func<object, IQueryable> sourceFactory = api =>
{
    var dbContext = (DbContext)((IEntityFrameworkApi)api).DbContext;
    var oc = ((IObjectContextAdapter)dbContext).ObjectContext;
    return oc.CreateQuery<TClr>($"[{containerName}].[{entitySetName}]");
};
```

### Data flow — `GET /odata/BooksByPublisher()?$filter=PublisherId eq 'Publisher1'`

1. AspNetCore.OData `OperationImportRoutingConvention` matches `BooksByPublisher` against the function import in `$metadata`.
2. Routes to `RestierController.Get`; the path's last segment is an `OperationImportSegment` (handled at `RestierController.cs:106-110`).
3. `RestierController` calls `ExecuteOperationAsync` → `IOperationExecutor.ExecuteOperationAsync` (resolved as `RestierOperationExecutor`).
4. Reflective method lookup against `LibraryWithViewsApi` returns null (no API method named `BooksByPublisher`).
5. **NEW:** Executor consults its constructor-injected `KeylessViewRegistry.TryGet("BooksByPublisher", out var entry)` → hit. (The registry was populated during model build and bridged into the route container by `AddRestierRoute`.)
6. `entry.SourceFactory(restierOperationContext.Api)` produces the underlying `IQueryable<BooksByPublisher>` (from `DbContext.BooksByPublisher`).
7. Executor returns that `IQueryable` directly. **No `api.QueryAsync` call** — `ApiBase.QueryAsync` would reject the request because the query is not a `QueryableSource<T>` (the only type it accepts; `ApiBase.cs:77-80`). Wiring through the RESTier query pipeline would require new `IModelMapper` + `IQueryExpressionSourcer` entries for function-import names; that's deferred.
8. The returned `IQueryable` flows back to `RestierController` and then to AspNetCore.OData, which applies `$filter=PublisherId eq 'Publisher1'` (and any other OData query options) at the OData layer just as it does for any function-import result.
9. Serialisation: the function returns `Collection(<ComplexType>)`, so OData's complex-type serializer handles output.

**What's NOT in this flow** (intentional v1 limitations):

- `IQueryExpressionAuthorizer` does not run for these requests. Use `[Authorize]` on the EDM function import (or row-filter inside the view's SQL) for security.
- `ConventionBasedQueryExpressionProcessor` does not fire — its early-return at `ConventionBasedQueryExpressionProcessor.cs:51-66` rejects anything that isn't an `IEdmEntitySet` of an `IEdmEntityType`. `OnFiltering<View>` / `OnExecuting<View>` therefore do not run for v1.
- `EFQueryExpressionSourcer` is not invoked — the leaf `IQueryable` comes from the source factory, not from a sourcer chain.
- `RestierEFOptions.NoTracking` is *not* applied here. The source factory returns whatever `DbSet<T>` exposes by default (tracking, in EFCore). Out of scope for v1; if real-world usage shows this is wrong, lift no-tracking into the source factory (one extra `AsNoTracking` call per EFCore view).

### Edge cases

| Case | Behaviour |
|---|---|
| Keyless type that isn't a DbSet (e.g. EFCore query type only) | Not in `entitySetMap` → not iterated → unaffected. Same as today. |
| Two keyless DbSets with the same name (impossible in EF, but defended anyway) | Registry throws `InvalidOperationException` on the second `Register`. Caught at startup, not at request time. |
| `OnFiltering<View>` / `OnExecuting<View>` conventions on the view | **Do not fire** in v1. Use `[Authorize]` on the function import or pre-filter in the view SQL. Follow-up spec to extend `ConventionBasedQueryExpressionProcessor` to recognise function-import model references. |
| POST / PATCH / PUT / DELETE on the view URL | Returns **HTTP 405 Method Not Allowed**. POST already had the function-import branch (`RestierController.cs:178-182`); v1 adds a matching guard to `Delete` (line ~311) and the private `Update` method (line ~435 — handles PUT and PATCH) so all four verbs respond 405 instead of throwing `NotImplementedException` (HTTP 500). |
| Mixed model — regular entity sets and keyless views in the same DbContext | Both paths coexist; `keyedEntitySets` vs `keylessViewSets` split is per-DbContext-instance, no cross-talk. |
| Versioning / Swagger | Function imports already appear in `$metadata`; `Microsoft.Restier.AspNetCore.Swagger` generates them via the OpenAPI converter as paths under `/odata/<name>()`. Verify; no code change planned unless a regression appears. |
| `RestierEFOptions` no-tracking | **Not applied in v1.** The source factory returns the raw `DbSet<T>` (EFCore default: tracking). Out of scope. If real-world usage shows this is wrong, the source factory in `EntityFrameworkCoreGetEntities` can call `AsNoTracking()` on the returned queryable — one extra line per view. |
| EF6 property-type variation | The source-factory probe selects any property assignable to `IQueryable<T>` — that covers `DbSet<T>`, `IDbSet<T>`, and `DbQuery<T>` in one check (they all implement `IQueryable<T>`). |
| EF6 `DbQuery<T>` not configured in the EDM | **Not supported.** Discovery iterates `efEntityContainer.EntitySets` from the metadata workspace. A `DbQuery<T>` (or `DbSet<T>`) that isn't configured via `modelBuilder.Entity<T>()` / EDMX won't appear in `EntitySets` and so never reaches the keyless branch. Users who want a `DbQuery<T>` as a view must configure it as an entity in the model (the typical EF6 pattern) — at which point the regular detection path picks it up. |

## Testing

Mirrors the existing dual-EF pattern (`docs/superpowers/specs/2026-04-15-dual-ef-testing-design.md`).

### Model-shape tests

| Test | Project | Assertion |
|---|---|---|
| `EFModelBuilder_Should_HandleViews` (existing) | `Microsoft.Restier.Tests.EntityFrameworkCore` | Flip from "throws InvalidOperationException" to "produces ComplexType + FunctionImport". Specifically: `$metadata` contains `<ComplexType Name="BooksByPublisher">` and `<FunctionImport Name="BooksByPublisher" Function="...BooksByPublisher" />` with an unbound function returning `Collection(<ns>.BooksByPublisher)`. |
| `EFModelBuilder_Should_HandleViews` (new, EF6) | `Microsoft.Restier.Tests.EntityFramework` | Same assertions against EF6 `LibraryWithViewsContext`. |
| `EFModelBuilder_Should_HandleMixedModel` | both flavours | Regular entity sets (`Books`, `Publishers`, …) AND `BooksByPublisher` view coexist in the same `$metadata`. |

### End-to-end query tests

Per-flavour, against real SQL Server using the existing user-secrets / `AddEntityFrameworkServices<T>` pattern. Skip cleanly when no connection string is configured (CI on a fresh box):

| Test | Project | Coverage |
|---|---|---|
| `GET /BooksByPublisher() returns rows` | `Microsoft.Restier.Tests.AspNetCore` (both EF6 and EFCore under `RegressionTests/EF6` and `RegressionTests/EFCore`, named `Issue741_KeylessViews.cs`) | Basic happy path. |
| `GET /BooksByPublisher()?$filter=PublisherId eq 'Publisher1'` filters | both flavours | OData query option works on the result. |
| `OnFilteringBooksByPublisher` convention does **NOT** fire | both flavours | Hook a counting interceptor on the API and assert it was *not* invoked. Pins the v1 limitation; flipping this test to "did fire" is the entry condition for the convention-processor follow-up. |
| `POST /BooksByPublisher()` returns **HTTP 405** | both flavours | Verifies `RestierController.Post`'s function-import branch. |

### Documentation

A new user-facing MDX page is part of v1, not a follow-up. The docs project (`src/Microsoft.Restier.Docs/`, DotNetDocs SDK) generates Mintlify-flavoured MDX; hand-authored content lives under `guides/`.

Required:

- **New page:** `src/Microsoft.Restier.Docs/guides/server/keyless-views.mdx`. Covers:
  - When the feature applies (EF Core `[Keyless]` / `HasNoKey()` / `ToView`, EF6 keyless `DbSet<T>` / `DbQuery<T>` / EDMX-only entity sets).
  - The auto-generated EDM shape: `ComplexType<T>` + unbound `FunctionImport` returning `Collection(<ComplexType>)`. Sample `$metadata` snippet.
  - URL shape: `GET /odata/<ViewName>()` (parens required) with `$filter` / `$select` / `$orderby` / `$top` / `$skip` examples.
  - **v1 limitations callout** (`<Warning>` Mintlify component): no `OnFiltering<View>` interceptor, no `IQueryExpressionAuthorizer`, no `RestierEFOptions.NoTracking`. Security: use `[Authorize]` on the function import or pre-filter inside the view SQL. Link to the follow-up tracking issue when filed.
  - Write attempts return HTTP 405 — show the response shape.
  - End-to-end EF Core sample (DbContext + Api class + view CLR type + cURL request/response).
  - Brief EF6 sample (DbContext + DbSet-backed view + EDMX-only ESQL-fallback note).
- **Navigation:** add the new page to the `<MintlifyTemplate>` block in `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`. Place under the existing "Server" group, near `model-building.mdx`. The SDK regenerates `docs.json` on build — commit the regenerated `docs.json` alongside the template change but do not hand-edit it.
- **Cross-links from existing pages:**
  - `guides/server/model-building.mdx` — short paragraph in the "What can RESTier model?" section pointing to the new keyless-views page.
  - `guides/server/operations.mdx` — note that keyless views appear as unbound function imports but are auto-generated, not user-authored.
- **Release notes:** add an entry to `src/Microsoft.Restier.Docs/release-notes/` (matching the existing release-notes folder structure for the current vnext release) summarising the new capability and the v1 limitations.

### Test infrastructure changes

- `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/` — add `LibraryWithViewsContext.cs` (EF6 + EFCore via the existing `#if EF6 / EFCore` pattern) plus `BooksByPublisher.cs` view CLR type (already exists for EFCore; mirror for EF6).
- The existing `LibraryWithViewsContext` in `test/Microsoft.Restier.Tests.EntityFrameworkCore/Scenarios/Views/` uses `UseInMemoryDatabase`. For the model-shape tests this is fine (no DB calls). For the end-to-end tests, route through `AddEntityFrameworkServices<LibraryWithViewsContext>` (real SQL) and create the view in the seeded DB via the test initialiser:
  - EFCore — `dbContext.Database.ExecuteSqlRaw("CREATE OR ALTER VIEW BooksByPublisher AS SELECT p.Id AS PublisherId, b.Title AS BookName, CAST(COUNT(b.Id) OVER(PARTITION BY p.Id) AS INT) AS BookCount FROM Publishers p INNER JOIN Books b ON b.PublisherId = p.Id")` inside `LibraryWithViewsTestInitializer.Seed` (which delegates to `LibraryTestInitializer.SeedLibraryData` for the underlying publishers/books).
  - EF6 — same SQL via `context.Database.ExecuteSqlCommand(...)` inside the EF6 initialiser.

### Out of scope (call out, don't ship)

- **Function imports with parameters** (e.g. `BooksByPublisher(publisherId=1)`). v1 always returns the unfiltered collection; users compose with `$filter`. Parameterised function imports would shadow hand-written `[UnboundOperation]` methods, so the cost/benefit shifts.
- **Parens-free URL** (`GET /odata/BooksByPublisher`). Function-import semantics with parens were explicitly chosen in the brainstorm.
- **Submit-pipeline plumbing** — read-only by construction; 405 from `RestierController.Post`'s existing branch is the desired UX.
- **EF6 stored-procedure result sets** that share the same shape as a view. Out of scope; the user can hand-author `[UnboundOperation]` today.

## Follow-ups (deferred work, must be tracked)

**These are not optional eventually — they're the gaps between "the feature works" and "the feature feels like a first-class RESTier resource." File a follow-up issue (or two) at the end of v1 implementation; link it from the docs `<Warning>` callout described in the Documentation section.**

### Follow-up A — Convention hooks and query-pipeline integration for keyless views

Goal: `OnFiltering<View>`, `OnExecuting<View>`, and the `IQueryExpressionAuthorizer` chain run for keyless-view function imports the same way they run for entity sets.

Required code changes:

1. **`IModelMapper.TryGetRelevantType`** — extend `RestierModelMapper` (`src/Microsoft.Restier.AspNetCore/Model/RestierModelMapper.cs:40-67`) to also resolve `IEdmFunctionImport` names returning collections (currently it filters to `IEdmEntitySet` and `IEdmSingleton`). The second overload at line 82 has a pre-existing `TODO GitHubIssue#39` for composable function imports and is the natural home.
2. **`ConventionBasedQueryExpressionProcessor.Process`** — widen the first early-return at `src/Microsoft.Restier.Core/Conventions/ConventionBasedQueryExpressionProcessor.cs:51-66` so a `DataSourceStubModelReference` whose `Element` is an `IEdmFunctionImport` (or whose return is `IEdmCollectionType` over `IEdmComplexType`) also routes to `AppendOnFilterExpression`. The method-name convention (`OnFiltering<ViewName>`) is the same.
3. **`KeylessViewQueryExpressionSourcer`** (new) — chained `IQueryExpressionSourcer` that recognises `DataSourceStub.GetQueryableSource<T>(viewName)` calls where `viewName` is in `KeylessViewRegistry` and returns `Expression.Constant(entry.SourceFactory(api))`. Mirrors `EFQueryExpressionSourcer` for entity sets.
4. **`RestierOperationExecutor` switch** — once the above are in, the executor's keyless-view branch swaps from "return factory IQueryable directly" to `var qs = api.GetQueryableSource<T>(name); var result = await api.QueryAsync(new QueryRequest(qs), ct); return result.Results.AsQueryable();`. The `T` is reflectively obtained from `entry.ClrType`.
5. **Test flip** — the v1 test `OnFilteringBooksByPublisher does NOT fire` flips to `does fire`. The docs `<Warning>` callout is removed and replaced with the standard interceptor docs cross-link.

### Follow-up B — `RestierEFOptions` no-tracking for keyless views

Goal: keyless-view queries respect the `NoTracking` setting on `RestierEFOptions`.

Required code changes:

- Either the EFCore-specific source factory in `EntityFrameworkCoreGetEntities` reads `RestierEFOptions` and calls `AsNoTracking()` when the option is set, **or** (preferred) Follow-up A's `KeylessViewQueryExpressionSourcer` lives in the EF layer (one per flavour) and applies the existing EF-layer no-tracking pass uniformly. Either way, B falls out of A more or less for free — list as a *sub-task* of A if filed as a single follow-up issue.

### Follow-up C — Spec-time open questions to verify

1. **EF6 `EntityContainer` name in the ESQL fallback string.** `efEntityContainer.Name` is already in scope of `EntityFramework6GetEntitySets`; capture and pass through to the factory closure. Trivial; resolve during v1 implementation, mention here for completeness.
2. **`Microsoft.Restier.AspNetCore.Swagger` / NSwag OpenAPI generation** for function imports returning `Collection(<ComplexType>)`. Verify with the existing Postgres / Northwind samples once a view is wired in. If the OpenAPI output is malformed, file as a separate Swagger-side issue — out of scope of v1's EDM work.
