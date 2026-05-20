# Keyless EF Views — Follow-ups A, B, D Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** promote V1 keyless-view function imports from "they work" to "they participate in the normal RESTier query pipeline".

- **A —** keyless-view function imports flow through `api.QueryAsync(...)`, so the normal query pipeline runs: `IQueryExpressionSourcer` → `IQueryExpressionAuthorizer` → `IQueryExpressionExpander` → `IQueryExpressionProcessor`. This makes `OnFiltering<View>` and any custom `IQueryExpressionAuthorizer` implementation apply to keyless-view requests.
- **B —** `RestierEFOptions.TrackingBehavior` applies to keyless-view requests the same way it applies to entity-set requests.
- **D —** Swagger / NSwag OpenAPI generation is covered by assertion-based tests, and the Postgres sample includes a worked keyless-view example with committed migration artifacts.

**Non-goal:** do not bolt a separate convention path onto `RestierOperationExecutor`. That preserves deferred execution, but it does **not** run the query authorizer chain and therefore does not satisfy Follow-up A.

**Spec:** `docs/superpowers/specs/2026-05-19-keyless-views-design.md` — Follow-ups A, B, D.

**V1 plan (already shipped):** `docs/superpowers/plans/2026-05-19-keyless-views.md`.

---

## Architecture

### Current V1 flow

`GET /odata/BooksByPublisher()` currently goes:

1. `RestierController.Get`
2. `OperationImportSegment` branch
3. `RestierOperationExecutor.ExecuteOperationAsync`
4. keyless-view fallback returns `entry.SourceFactory(api)` directly
5. controller applies OData query options to that returned `IQueryable`

This preserves deferred EF translation, but it bypasses `api.QueryAsync(...)`, so query authorizers and `ConventionBasedQueryExpressionProcessor` never run.

### Target flow

For keyless-view function imports only, `RestierController.Get` must switch to the normal query path:

1. Detect that the unbound `OperationImportSegment` name matches a `KeylessViewRegistry` entry.
2. Build a typed `QueryableSource` using `api.GetQueryableSource<T>(name)` via reflection on `KeylessViewEntry.ClrType`.
3. Wrap that in `new QueryRequest(queryableSource)`.
4. Apply OData query options to that `QueryRequest` **before** execution.
5. Call `api.QueryAsync(queryRequest, ct)`.
6. Let the normal query pipeline run:
   - sourcer replaces the `DataSourceStub`
   - authorizer inspects the query
   - expander runs if applicable
   - processor resolves `OnFiltering<View>`
   - executor executes the final composed query
7. Return `QueryResult.Results.AsQueryable()` to the OData response layer.

This is the only design in this codebase that satisfies both:

- deferred provider-backed composition, and
- query-pipeline participation.

### Design rule

`RestierOperationExecutor` remains responsible for real operation methods and for V1 keyless-view fallback only until the controller is updated. After Follow-up A lands, the controller should route keyless-view function imports into the query pipeline directly, instead of asking the executor to materialize them.

---

## Constraints

- Targets: `net8.0`, `net9.0`, `net10.0`
- Warnings as errors: yes
- Implicit usings: disabled
- Test framework: xUnit v3, AwesomeAssertions (`using FluentAssertions;`), NSubstitute
- Tabs for indentation in touched C# files
- Full-suite gate: `dotnet test RESTier.slnx`

---

## File Inventory

| File | Action | Purpose |
|------|--------|---------|
| `src/Microsoft.Restier.AspNetCore/RestierController.cs` | Modify | Route keyless-view operation imports into the normal query pipeline instead of the executor shortcut. |
| `src/Microsoft.Restier.AspNetCore/Model/RestierModelMapper.cs` | Modify | Resolve unbound keyless-view function-import names to CLR element types so `api.GetQueryableSource<T>(name)` succeeds. |
| `src/Microsoft.Restier.Core/Conventions/ConventionBasedMethodNameFactory.cs` | Modify | Add `GetFunctionImportMethodName(string, RestierPipelineState, RestierEntitySetOperation)` for function-import filter conventions. |
| `src/Microsoft.Restier.Core/Conventions/ConventionBasedQueryExpressionProcessor.cs` | Modify | Extend `Process(...)` so `DataSourceStubModelReference` over an `IEdmFunctionImport` returning `Collection(<ComplexType>)` resolves `OnFiltering<View>`. |
| `src/Microsoft.Restier.EntityFramework.Shared/Query/KeylessViewQueryExpressionSourcer.cs` | Create | Source `DataSourceStub.GetQueryableSource<T>(viewName)` for keyless views via the registry and apply EF tracking behavior. |
| `src/Microsoft.Restier.EntityFramework.Shared/Extensions/ServiceCollectionExtensions.cs` | Modify | Register `KeylessViewQueryExpressionSourcer` into the chained sourcer pipeline ahead of `EFQueryExpressionSourcer`. |
| `src/Microsoft.Restier.AspNetCore/Operation/RestierOperationExecutor.cs` | Modify | Remove keyless-view responsibility from the GET path assumptions; keep fallback semantics only where still needed. |
| `test/Microsoft.Restier.Tests.AspNetCore/Model/RestierModelMapperTests.cs` | Modify | Cover function-import type resolution. |
| `test/Microsoft.Restier.Tests.Core/Conventions/ConventionBasedMethodNameFactoryTests.cs` | Modify | Pin function-import convention naming. |
| `test/Microsoft.Restier.Tests.Core/Conventions/ConventionBasedQueryExpressionProcessorTests.cs` | Modify | Assert `OnFiltering<View>` resolves on function-import references. |
| `test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/KeylessViewQueryExpressionSourcerTests.cs` | Create | Cover source hits/misses and tracking-behavior matrix. |
| `test/Microsoft.Restier.Tests.AspNetCore/Operation/RestierOperationExecutorTests.cs` | Modify | Update assertions to reflect that GET-path keyless views no longer depend on executor-side convention dispatch. |
| `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/EFCore/Issue741_KeylessViews.cs` | Modify | Flip the v1 limitation tests and add authorizer/convention assertions. |
| `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsApi.cs` | Modify | Add observable `OnFilteringBooksByPublisher(...)` behavior and an authorizer probe if needed. |
| `src/Microsoft.Restier.Docs/guides/server/keyless-views.mdx` | Modify | Document `OnFiltering<View>` and tracking behavior for keyless views. |
| `src/Microsoft.Restier.Docs/release-notes/<latest>.mdx` | Modify | Summarize Follow-ups A/B/D. |
| `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Infrastructure/KeylessViewTestApi.cs` | Create | Self-contained OpenAPI fixture. |
| `test/Microsoft.Restier.Tests.AspNetCore.NSwag/IntegrationTests/KeylessViewOpenApiTests.cs` | Create | Assert NSwag output for keyless-view function import + complex type. |
| `test/Microsoft.Restier.Tests.AspNetCore.Swagger/Infrastructure/KeylessViewTestApi.cs` | Create | Self-contained Swagger fixture. |
| `test/Microsoft.Restier.Tests.AspNetCore.Swagger/IntegrationTests/KeylessViewOpenApiTests.cs` | Create | Assert Swagger output for keyless-view function import + complex type. |
| `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Models/UsersByType.cs` | Create | Sample keyless-view POCO. |
| `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Models/RestierTestContext.UsersByType.cs` | Create | Partial context hook-up for the sample view. |
| `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Migrations/<timestamp>_AddUsersByTypeView.cs` | Create | Migration code file. |
| `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Migrations/<timestamp>_AddUsersByTypeView.Designer.cs` | Create/Update | Migration designer file. |
| `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Migrations/RestierTestContextModelSnapshot.cs` | Update | Required EF snapshot update for the sample migration. |

---

## Phase 1 — Follow-up A

### Task 1: Add function-import convention naming

**Files:**
- Modify: `src/Microsoft.Restier.Core/Conventions/ConventionBasedMethodNameFactory.cs`
- Modify: `test/Microsoft.Restier.Tests.Core/Conventions/ConventionBasedMethodNameFactoryTests.cs`

- [ ] Add `GetFunctionImportMethodName(string importName, RestierPipelineState state, RestierEntitySetOperation operation)`.
- [ ] Match existing entity-set filter semantics exactly:
  - `Submit + Filter` => `OnFilterBooksByPublisher` (no `-ing` suffix). The entity-set helper at `ConventionBasedMethodNameFactory.cs:78` forces the suffix to empty for `Filter`, so the actual entity-set convention is `OnFilter<EntitySet>` (confirmed by `LibraryApi.cs:186` — `OnFilterBooks`). The V1 keyless-views fixture used the gerund form `OnFilteringBooksByPublisher`, but the V1 convention never actually fired so the wrong name was never observable. Task 7 of this plan renames the probe to the correct `OnFilterBooksByPublisher`.
  - `Authorization + Filter` => `string.Empty` (entity sets suppress this combo via `ExcludedFilterStates`; the function-import helper must do the same so no `CanFilter<View>` surface is invented for a pipeline state that has no backing convention)
- [ ] Do **not** invent a different accessibility or naming contract than the entity-set path. The function-import helper's body must mirror `GetEntitySetMethodName`'s logic line-for-line, swapping the trailing entity-set name for the supplied `functionImportName`.
- [ ] Add tests for the two names above.

### Task 2: Extend `ConventionBasedQueryExpressionProcessor` for function imports

**Files:**
- Modify: `src/Microsoft.Restier.Core/Conventions/ConventionBasedQueryExpressionProcessor.cs`
- Modify: `test/Microsoft.Restier.Tests.Core/Conventions/ConventionBasedQueryExpressionProcessorTests.cs`

- [ ] Extend the `DataSourceStubModelReference` branch to accept:
  - `IEdmEntitySet` returning `Collection(EntityType)` (existing behavior)
  - `IEdmFunctionImport` returning `Collection(ComplexType)` for keyless views
- [ ] Refactor `AppendOnFilterExpression(...)` to take:
  - the already-computed method name
  - the source name used in diagnostics
  - the EDM element type
- [ ] Keep the accessibility rule exactly aligned with the existing processor:
  - accepted: `protected`, `protected internal`
  - not accepted: `public`, plain `internal`
- [ ] Add a unit test that feeds a function-import-shaped `QueryExpressionContext` and verifies `OnFilterBooksByPublisher` is resolved.

### Task 3: Resolve keyless-view function imports in `RestierModelMapper`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Model/RestierModelMapper.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Model/RestierModelMapperTests.cs`

- [ ] Extend `TryGetRelevantType(InvocationContext, string, out Type)` to resolve:
  - entity sets
  - singletons
  - unbound function imports representing registered keyless views
- [ ] Use the registry-backed CLR type, not EDM annotation inference from entity sets.
- [ ] Add tests for:
  - known entity set
  - known keyless-view function import
  - unknown name delegating to inner mapper

### Task 4: Add `KeylessViewQueryExpressionSourcer`

**Files:**
- Create: `src/Microsoft.Restier.EntityFramework.Shared/Query/KeylessViewQueryExpressionSourcer.cs`
- Modify: `src/Microsoft.Restier.EntityFramework.Shared/Extensions/ServiceCollectionExtensions.cs`
- Create: `test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/KeylessViewQueryExpressionSourcerTests.cs`

- [ ] Implement a chained sourcer that recognizes `DataSourceStubModelReference` where:
  - `Element` is an `IEdmFunctionImport`
  - the import name exists in `KeylessViewRegistry`
- [ ] Source the query by calling `KeylessViewEntry.SourceFactory(api)`.
- [ ] Apply `EFQueryExpressionSourcer.ApplyTracking(...)` before returning `Expression.Constant(...)`.
- [ ] Register it so it gets first crack at keyless-view function-import references, ahead of the generic EF sourcer.
- [ ] Add tests for:
  - registry hit
  - registry miss
  - tracking behavior permutations

### Task 5: Move keyless-view GET handling into the controller query path

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/EFCore/Issue741_KeylessViews.cs`

This is the critical task.

- [ ] In `RestierController.Get`, special-case unbound `OperationImportSegment` requests whose operation name matches a keyless-view registry entry.
- [ ] Resolve `KeylessViewRegistry` via `HttpContext.Request.GetRouteServices().GetService<KeylessViewRegistry>()`. Do **not** add a constructor parameter to `RestierController`; the registry is bridged into the route service container by `AddRestierRoute`'s V1 lifetime bridge, and the route-services accessor is the standard pattern in this controller (see the existing `IExpandCycleDetector` resolution around `RestierController.cs:824`).
- [ ] For that branch:
  - build `api.GetQueryableSource<T>(operation.Name)` reflectively against `entry.ClrType` from the registry
  - wrap it in `QueryRequest`
  - set `ShouldReturnCount` as today
  - call `ApplyQueryOptions(...)` on that `QueryRequest`
  - execute with `api.QueryAsync(...)`
  - return `queryResult.Results.AsQueryable()`
- [ ] Keep the existing executor path for real user-authored operations.
- [ ] Do **not** materialize the keyless-view query before `ApplyQueryOptions(...)`.
- [ ] Do **not** use an executor-side bespoke `OnFiltering<View>` invoker.

**Expected outcome:**

- `OnFilteringBooksByPublisher(...)` now fires through the normal processor.
- any custom `IQueryExpressionAuthorizer` now sees the query.
- OData query options still compose provider-side.

### Task 6: Remove the executor's keyless-view branch

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Operation/RestierOperationExecutor.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Operation/RestierOperationExecutorTests.cs`

The keyless-view fallback inside `ExecuteOperationAsync` (V1, lines ~92–107 — `keylessViewRegistry.TryGet`, `SourceFactory(api)`, `PerformPre/PostEvent`, return) is reachable **only** from `RestierController.Get`'s `OperationImportSegment` branch. After Task 5 the controller handles keyless views directly and never delegates them to the executor, so the branch is unreachable.

- [ ] Delete the `if (keylessViewRegistry.TryGet(...))` block from `ExecuteOperationAsync`. The path falls through to the existing `throw new NotImplementedException(AspNetResources.OperationNotImplemented)` — correct: a hand-authored operation by the same name as a view would still be picked up by the reflective method lookup above.
- [ ] Keep the `KeylessViewRegistry` constructor parameter — it's now unused by the executor body but removing it is a behavior-neutral churn that's better handled as a separate cleanup commit (or deferred until after this PR merges). Note the unused field with a `// retained for ctor-injection compat; see Follow-up A` comment so reviewers don't flag it.
- [ ] In `RestierOperationExecutorTests.cs`, delete the tests that asserted the keyless-view executor branch's behavior (operation-filter pre/post fires, `ParameterValues = Array.Empty<object>()`, `SourceFactory` is invoked). The corresponding contracts are now covered end-to-end by `Issue741_KeylessViews` (controller path).

### Task 7: Flip regression coverage

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/EFCore/Issue741_KeylessViews.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsApi.cs`

- [ ] Rename the V1 probe from `OnFilteringBooksByPublisher` to `OnFilterBooksByPublisher` so it matches the actual convention name produced by `GetFunctionImportMethodName` (and the existing entity-set convention `OnFilter<EntitySet>` — see Task 1). Rename `OnFilteringBooksByPublisherCallCount` accordingly. Update all references in `Issue741_KeylessViews.cs`, in the LibraryWithViews docs, and anywhere else the gerund spelling appears.
- [ ] Replace the v1 limitation test with positive assertions:
  - `Get_KeylessView_InvokesOnFilterConvention`
  - `Get_KeylessView_OnFilterFilterReachesResponse`
  - `Get_KeylessView_QueryAuthorizerFires`
- [ ] Make `OnFilterBooksByPublisher(...)` observably alter the result, for example:
  - `return entitySet.Where(b => b.PublisherId != "Publisher3");`
- [ ] If needed, add a minimal query authorizer probe in the test services so the regression test can prove the authorizer chain was reached.
- [ ] Do not use a `$top=1` smoke test as proof of provider-side composition; it does not distinguish deferred provider execution from in-memory LINQ.

---

## Phase 2 — Follow-up B

Task 4 already produces a `KeylessViewQueryExpressionSourcer` that calls `EFQueryExpressionSourcer.ApplyTracking(...)`, and Task 4's tracking-behavior permutations cover the unit-level matrix. Follow-up B therefore reduces to one end-to-end assertion that the real `DbContext.ChangeTracker` is empty after a keyless-view GET when `RestierEFOptions.TrackingBehavior = NoTracking` — the EFCore behaviour that user-visible Follow-up B exists to deliver.

### Task 8: EFCore end-to-end assertion for `NoTracking` on keyless views

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/KeylessViewQueryExpressionSourcerTests.cs` (or a sibling test file under the same folder if a separate file fits the project's conventions better)

- [ ] With `RestierEFOptions.TrackingBehavior = NoTracking` wired through `AddEFCoreProviderServices`, fire a real GET against the LibraryWithViewsApi keyless view, then assert `((IEntityFrameworkApi)api).DbContext.ChangeTracker.Entries().Count() == 0`.
- [ ] If a `TrackAll` variant adds confidence cheaply (one extra `[Theory]` row, same fixture), include it; otherwise leave the per-behaviour matrix at the unit level in Task 4.

### Task 9: Update docs for A + B

**Files:**
- Modify: `src/Microsoft.Restier.Docs/guides/server/keyless-views.mdx`
- Modify: `src/Microsoft.Restier.Docs/release-notes/<latest>.mdx`

- [ ] Remove the v1 warning that `OnFiltering<View>` does not fire.
- [ ] Add a concise interceptor section for keyless views.
- [ ] Document that keyless views now participate in the normal query pipeline.
- [ ] Document that `RestierEFOptions.TrackingBehavior` applies to keyless-view reads.
- [ ] Keep claims about authorizers accurate: after controller/query-path integration lands, custom `IQueryExpressionAuthorizer` implementations do run for keyless-view GET requests.

---

## Phase 3 — Follow-up D

### Task 10: Add NSwag verification

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Infrastructure/KeylessViewTestApi.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/IntegrationTests/KeylessViewOpenApiTests.cs`

- [ ] Use a self-contained API/model-builder fixture in the NSwag test project.
- [ ] Assert:
  - the function-import path exists
  - the complex-type schema exists

### Task 11: Add Swagger verification

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Swagger/Infrastructure/KeylessViewTestApi.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Swagger/IntegrationTests/KeylessViewOpenApiTests.cs`

- [ ] Mirror Task 10 for Swagger.

### Task 12: Add worked Postgres sample

**Files:**
- Create: `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Models/UsersByType.cs`
- Create: `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Models/RestierTestContext.UsersByType.cs`
- Create/Update:
  - `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Migrations/<timestamp>_AddUsersByTypeView.cs`
  - `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Migrations/<timestamp>_AddUsersByTypeView.Designer.cs`
  - `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Migrations/RestierTestContextModelSnapshot.cs`

- [ ] Add the POCO and `DbSet<UsersByType>`.
- [ ] Hook up `HasNoKey().ToView("UsersByType")` via `OnModelCreatingPartial(...)`.
- [ ] Generate the migration with `dotnet ef migrations add ...` and commit **all** EF artifacts.
- [ ] Treat the migration as a three-file unit in this repo:
  - migration code
  - migration designer
  - model snapshot
- [ ] Do **not** hand-author only the `.cs` migration file. If local infrastructure is unavailable, the task is blocked until the migration can be generated or all three artifacts are updated consistently.

---

## Verification

### Phase 1 checks

- [ ] `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~ConventionBasedMethodNameFactory"`
- [ ] `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~ConventionBasedQueryExpressionProcessor"`
- [ ] `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierModelMapper"`
- [ ] `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~Issue741_KeylessViews"`

### Phase 2 checks

- [ ] `dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj --filter "FullyQualifiedName~KeylessViewQueryExpressionSourcer"`
- [ ] `dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`

### Phase 3 checks

- [ ] `dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~KeylessViewOpenApiTests"`
- [ ] `dotnet test test/Microsoft.Restier.Tests.AspNetCore.Swagger/Microsoft.Restier.Tests.AspNetCore.Swagger.csproj --filter "FullyQualifiedName~KeylessViewOpenApiTests"`
- [ ] `dotnet build src/Microsoft.Restier.Samples.Postgres.AspNetCore/Microsoft.Restier.Samples.Postgres.AspNetCore.csproj`

### Final gate

- [ ] `dotnet test RESTier.slnx`
- [ ] Warning-clean across touched projects

---

## Notes For Implementers

- If a proposed change preserves deferred execution but bypasses `api.QueryAsync(...)`, it is not a valid implementation of Follow-up A.
- If a proposed change introduces a new visibility rule for `OnFiltering<View>`, it is wrong unless the entity-set convention contract is updated to match.
- If a proposed sample migration commit omits the designer or snapshot file, it is incomplete for this repository.
