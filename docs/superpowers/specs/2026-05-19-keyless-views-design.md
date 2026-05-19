# Keyless EF Views as Read-Only RESTier Resources

**Date:** 2026-05-19
**Status:** Design draft — awaiting confirmation
**Issue:** [OData/RESTier#741](https://github.com/OData/RESTier/issues/741) (predecessor: [#692](https://github.com/OData/RESTier/issues/692))

## Goal

Expose EF Core `[Keyless]` / `HasNoKey()` and EF6 keyless `DbSet<T>` / `DbQuery<T>` entities — typically database views — as read-only RESTier resources, so a single Restier API can serve both tables and views without forcing users to hand-author `[UnboundOperation]` complex-type wrappers. The current behaviour (throw `InvalidOperationException` at model-build time with a message that tells the user to do exactly that wrapping themselves) is replaced with automatic complex-type + function-import wiring through `EFModelBuilder`. Both EF flavours behave identically from the consumer's perspective.

`GET /odata/BooksByPublisher()` (function-call URL, parens required) returns the rows; `$filter`, `$select`, `$orderby`, `$top`, `$skip` work as normal OData query options; convention interceptors (`OnFiltering<View>`) fire just like on a sourced query; writes are unavailable by construction (no entity set ⇒ no `EntitySetRoutingConvention` match ⇒ 404 from AspNetCore.OData).

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| URL shape | Function-import call: `GET /odata/BooksByPublisher()` (parens required) | OData v4 entity sets require keyed entity types per spec — this is unchanged in OData 8 / AspNetCore.OData 9 (Microsoft Learn: *"abstract entity type without keys can't be used to define any navigation sources"*). Function imports over `Collection(ComplexType)` are the spec-aligned shape and what the original RESTier error message already pointed users to. |
| EDM modelling | `ComplexType<T>` + unbound `FunctionImport` named after the DbSet/EntitySet returning `Collection(<ComplexType>)` | Smallest spec-aligned surface. No synthetic keys (would lie about the data model and expose insert/update/delete URLs we'd then have to hand-block). No singleton (singletons return one entity, not a collection). |
| Dispatch | Registry-based fallback inside `RestierOperationExecutor` — no EDM annotations | A single shared `KeylessViewRegistry` (DI singleton, per-API) maps function-import name → CLR type + source factory. The executor's existing "method by name" lookup falls through to the registry when no API method matches. Avoids leaking RESTier-private vocabulary terms into `$metadata` and keeps OData-Core unaware of the feature. |
| Source factory | Captured at model-build time, EF-flavour-specific | EF Core: reflection on the DbSet property. EF6 DbSet/DbQuery-backed: reflection on the property. EF6 EDMX-only (no CLR property): `((IObjectContextAdapter)ctx).ObjectContext.CreateQuery<T>("[Container].[EntitySet]")`. The executor stays EF-agnostic — it only ever invokes `Func<object api, IQueryable>`. |
| Query pipeline integration | Route through `api.QueryAsync(new QueryRequest(...))` | Conventions (`OnFiltering<View>`), authorizers, processors, and the no-tracking handling already wired by `RestierEFOptions` apply uniformly. `EFQueryExpressionSourcer` is *not* invoked (there is no `EntitySet` model reference); the factory produces the leaf `IQueryable` instead. |
| Writes | Not supported, no special handling needed | No entity set means AspNetCore.OData's `EntitySetRoutingConvention` never matches POST/PATCH/PUT/DELETE on the view URL. Default response is 404 — that's the desired UX. No submit-pipeline plumbing. |
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
2. **Registry fallback** *(chosen)* — a `KeylessViewRegistry` (per-API DI singleton) holds `{name → (clrType, sourceFactory)}`. The executor falls through to the registry on a null method lookup. Zero metadata pollution; one new class; localised change.

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
│   2. NEW fallback: consult KeylessViewRegistry           │
│   3. sourceFactory(api) → IQueryable                     │
│   4. api.QueryAsync(new QueryRequest(...))               │
│   5. return composed IQueryable                          │
└──────────────────────────────────────────────────────────┘
```

### New / modified components

| Component | Change | Path |
|---|---|---|
| `KeylessViewRegistry` (new) | Per-API singleton. Members: `Register(string functionImportName, Type clrType, Func<object, IQueryable> sourceFactory)`, `TryGet(string name, out KeylessViewEntry entry)`. Entry stores name, CLR type, factory. Throws on duplicate name registration. | `src/Microsoft.Restier.EntityFramework.Shared/Model/KeylessViewRegistry.cs` |
| `EFModelBuilder<TDbContext>` shared partial — `BuildEdmModelFromEntitySetMaps` | Replace the `throw` at line 141. New branch: when `pair.Value` is null OR empty, demote to complex (skip the `EntitySet<T>` call by removing the type from `entitySetMap` *before* the convention builder iterates it — see Implementation note below), call `builder.ComplexType<T>()`, add a function import on the container post-`GetEdmModel`, register in `KeylessViewRegistry`. | `src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs` |
| `EFModelBuilder<TDbContext>` EF Core partial — `EntityFrameworkCoreGetEntities` | Normalise the keyless case (already produces `null`, leave alone). Also produce a new `Dictionary<Type, Func<object, IQueryable>>` of source factories: reflection on the DbSet property captured at model-build time. Wire into the shared method's signature. | `src/Microsoft.Restier.EntityFrameworkCore/Model/EFModelBuilder.cs` |
| `EFModelBuilder<TDbContext>` EF6 partial — `EntityFramework6GetEntitySets` | Same factory dictionary. Logic: prefer reflection on a matching `DbSet<T>` / `DbQuery<T>` property; fall back to `((IObjectContextAdapter)ctx).ObjectContext.CreateQuery<T>("[Container].[EntitySet]")` when no property exists (EDMX-only case). | `src/Microsoft.Restier.EntityFramework/Model/EfModelBuilder.cs` |
| `RestierOperationExecutor.ExecuteOperationAsync` | After the existing reflective method lookup, if `method is null`, resolve `KeylessViewRegistry` from `restierOperationContext.Api`'s service provider and try `TryGet(OperationName, ...)`. On hit: invoke `sourceFactory(api)`, wrap in `QueryRequest`, call `api.QueryAsync`, return `result.Results.AsQueryable()`. On miss: existing `throw new NotImplementedException`. | `src/Microsoft.Restier.AspNetCore/Operation/RestierOperationExecutor.cs` |
| `AddEF6ProviderServices` / `AddEFCoreProviderServices` | Register `KeylessViewRegistry` as singleton in the shared DI block. Same lifetime/scope as the model. | EF DI extension files (paths below) |

DI registration sites (verified against worktree):

- `src/Microsoft.Restier.EntityFramework/Extensions/ServiceCollectionExtensions.cs` (EF6 `AddEF6ProviderServices`)
- `src/Microsoft.Restier.EntityFrameworkCore/Extensions/ServiceCollectionExtensions.cs` (EFCore `AddEFCoreProviderServices`)
- `src/Microsoft.Restier.EntityFramework.Shared/Extensions/ServiceCollectionExtensions.cs` if it already centralises a shared DI block; otherwise register independently in each EF flavour's file. Singleton lifetime — same scope as the EDM model.

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

### Data flow — `GET /odata/BooksByPublisher()?$filter=PublisherId eq 1`

1. AspNetCore.OData `OperationImportRoutingConvention` matches `BooksByPublisher` against the function import in `$metadata`.
2. Routes to `RestierController.Get`; the path's last segment is an `OperationImportSegment` (handled at `RestierController.cs:106-110`).
3. `RestierController` calls `ExecuteOperationAsync` → `IOperationExecutor.ExecuteOperationAsync` (resolved as `RestierOperationExecutor`).
4. Reflective method lookup against `LibraryWithViewsApi` returns null (no API method named `BooksByPublisher`).
5. **NEW:** Executor consults `KeylessViewRegistry.TryGet("BooksByPublisher", out var entry)` → hit.
6. `entry.SourceFactory(api)` produces the underlying `IQueryable<BooksByPublisher>` (from `DbContext.BooksByPublisher`).
7. Executor wraps in `QueryRequest(queryable)`, calls `api.QueryAsync(request, ct)`. The `IQueryExpressionAuthorizer`, `ConventionBasedQueryExpressionProcessor` (which fires `OnFiltering<BooksByPublisher>`), and `DefaultQueryExecutor` run normally. `EFQueryExpressionSourcer` does not insert a source — the leaf is already the `IQueryable` from the factory.
8. Result `IQueryable` returns to `RestierController`, which lets AspNetCore.OData apply the `$filter=PublisherId eq 1` query option as usual for a function-import result.
9. Wire serialisation: the function returns `Collection(<ComplexType>)`, so OData's complex-type serializer handles output.

### Edge cases

| Case | Behaviour |
|---|---|
| Keyless type that isn't a DbSet (e.g. EFCore query type only) | Not in `entitySetMap` → not iterated → unaffected. Same as today. |
| Two keyless DbSets with the same name (impossible in EF, but defended anyway) | Registry throws `InvalidOperationException` on the second `Register`. Caught at startup, not at request time. |
| `OnFiltering<View>` / `OnExecuting<View>` conventions on the view | Fire normally (step 7 above). Documented as the row-level-security integration point. |
| POST/PATCH/PUT/DELETE on the view URL | No `EntitySetRoutingConvention` match → 404 from AspNetCore.OData. No custom handling added. |
| Mixed model — regular entity sets and keyless views in the same DbContext | Both paths coexist; `keyedEntitySets` vs `keylessViewSets` split is per-DbContext-instance, no cross-talk. |
| Versioning / Swagger | Function imports already appear in `$metadata`; `Microsoft.Restier.AspNetCore.Swagger` generates them via the OpenAPI converter as paths under `/odata/<name>()`. Verify; no code change planned unless a regression appears. |
| `RestierEFOptions` no-tracking | EF Core honours `AsNoTracking` set on `DbSet<T>` regardless of whether the query was sourced via `EFQueryExpressionSourcer`. The factory returns the raw `DbSet` `IQueryable` — `EFQueryExecutor` (or whatever applies the no-tracking pass on the EF chain) sees it as it would any other queryable. Confirm during implementation; if no-tracking is *only* applied inside `EFQueryExpressionSourcer`, lift that into a stage that runs unconditionally on every EF-sourced query. |
| EF6 reflection on `IDbSet<T>` properties | `IDbSet<T>` inherits from `IQueryable<T>`, so the factory's `(IQueryable)prop.GetValue(dbContext)` works without casting through `DbSet<T>`. Tested as part of EF6 coverage. |
| EF6 DbQuery<T> obsolete? | `DbQuery<T>` is the EF6 read-only sibling of `DbSet<T>` and is *not* deprecated in EF6 (it was removed in EFCore 3.0+). Including it in the property scan keeps the EF6 path complete. |

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
| `GET /BooksByPublisher()?$filter=PublisherId eq 1` filters | both flavours | OData query option works on the result. |
| `OnFilteringBooksByPublisher` convention fires | both flavours | Hook a counting interceptor on the API; assert it ran. |
| `POST /BooksByPublisher` returns 404 | both flavours | Read-only-by-construction sanity check. |

### Test infrastructure changes

- `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/` — add `LibraryWithViewsContext.cs` (EF6 + EFCore via the existing `#if EF6 / EFCore` pattern) plus `BooksByPublisher.cs` view CLR type (already exists for EFCore; mirror for EF6).
- The existing `LibraryWithViewsContext` in `test/Microsoft.Restier.Tests.EntityFrameworkCore/Scenarios/Views/` uses `UseInMemoryDatabase`. For the model-shape tests this is fine (no DB calls). For the end-to-end tests, route through `AddEntityFrameworkServices<LibraryWithViewsContext>` (real SQL) and create the view in the seeded DB via the test initialiser:
  - EFCore — `dbContext.Database.ExecuteSqlRaw("CREATE OR ALTER VIEW BooksByPublisher AS SELECT p.Id AS PublisherId, p.Name AS PublisherName, b.Title AS BookName, COUNT(b.Id) OVER(PARTITION BY p.Id) AS BookCount FROM Publishers p JOIN Books b ON b.PublisherId = p.Id")` inside `LibraryTestInitializer.Seed`.
  - EF6 — same SQL via `context.Database.ExecuteSqlCommand(...)` inside the EF6 initialiser.

### Out of scope (call out, don't ship)

- Function imports with parameters (e.g. `BooksByPublisher(publisherId=1)`). v1 always returns the unfiltered collection; users compose with `$filter`. Parameterised function imports are a natural follow-up but require the model builder to pick up parameter conventions (and become almost indistinguishable from hand-written `[UnboundOperation]` methods, so the cost/benefit shifts).
- Make the parens-free URL (`GET /odata/BooksByPublisher`) work. Function-import semantics with parens were explicitly chosen in the brainstorm.
- Submit-pipeline plumbing — read-only by construction.
- EF6 stored-procedure result sets that share the same shape as a view. Out of scope; the user can hand-author `[UnboundOperation]` today.

## Open questions

1. Where does the EF6 `EntityContainer` name come from in the ESQL fallback string? `efEntityContainer.Name` is already in scope of `EntityFramework6GetEntitySets`; capture and pass through.
2. Does `RestierEFOptions`-driven no-tracking get applied to the factory's `IQueryable` automatically, or only inside `EFQueryExpressionSourcer`? Quick spike during implementation; if the latter, lift the no-tracking pass into a stage that runs on every EF-sourced query.
3. Does `Microsoft.Restier.AspNetCore.Swagger` generate sensible OpenAPI paths for function imports returning `Collection(<ComplexType>)`? Verify with the existing Postgres / Northwind samples once a view is wired in.

These don't block the spec — they're flagged for the implementation plan.
