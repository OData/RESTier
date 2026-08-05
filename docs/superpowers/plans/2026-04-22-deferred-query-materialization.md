# Deferred Query Materialization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate unnecessary `ToList()` / `ToArrayAsync()` materializations in RESTier's query executors so `IQueryable` flows through to the OData serializer for collection responses, and move 404 detection from the query handler to the controller.

**Architecture:** Query executors pass `IQueryable` through instead of materializing. `DefaultQueryHandler.CheckSubExpressionResult` is removed entirely. The controller gains 404-vs-204 detection based on OData path segments. The submit path explicitly materializes where it needs multi-enumeration consistency.

**Tech Stack:** .NET 8/9, ASP.NET Core OData 9.x, Entity Framework Core, Entity Framework 6, xUnit v3, FluentAssertions

**Spec:** `docs/superpowers/specs/2026-04-22-deferred-query-materialization-design.md`

---

### Task 1: Defer Materialization in `DefaultQueryExecutor`

**Files:**
- Modify: `src/Microsoft.Restier.Core/Query/DefaultQueryExecutor.cs:29`
- Test: `test/Microsoft.Restier.Tests.Core/Query/DefaultQueryExecutorTests.cs`

- [ ] **Step 1: Update the existing test to verify deferred execution**

The existing `CanCallExecuteQueryAsync` test at `test/Microsoft.Restier.Tests.Core/Query/DefaultQueryExecutorTests.cs:67` asserts `result.Results.Should().BeEquivalentTo(queryable)`. After the change, `Results` will BE the `IQueryable` itself (not a materialized copy). Add a test that verifies the result is the same reference — proving deferred execution:

Add this test after the existing `CanCallExecuteQueryAsync` test (after line 79):

```csharp
/// <summary>
/// Verifies that ExecuteQueryAsync returns the IQueryable without materializing it.
/// </summary>
/// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
[Fact]
public async Task ExecuteQueryAsync_ReturnsDeferredQueryable()
{
    var context = new QueryContext(
        new TestApi(model, queryHandler, submitHandler),
        new QueryRequest(new QueryableSource<Test>(Expression.Constant(queryable))));

    var result = await testClass.ExecuteQueryAsync(
        context,
        queryable,
        CancellationToken.None);

    result.Results.Should().BeSameAs(queryable);
}
```

- [ ] **Step 2: Run the new test to verify it fails**

Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~ExecuteQueryAsync_ReturnsDeferredQueryable" -v n`

Expected: FAIL — currently `Results` is a `List<Test>` (materialized copy), not the original `IQueryable`.

- [ ] **Step 3: Change `DefaultQueryExecutor` to defer materialization**

In `src/Microsoft.Restier.Core/Query/DefaultQueryExecutor.cs`, change line 29 from:

```csharp
            var result = new QueryResult(query.ToList());
```

to:

```csharp
            var result = new QueryResult(query);
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~DefaultQueryExecutorTests" -v n`

Expected: All `DefaultQueryExecutorTests` pass, including the new `ExecuteQueryAsync_ReturnsDeferredQueryable`.

- [ ] **Step 5: Remove unused `using System.Linq` if no longer needed**

Check if `System.Linq` is still needed in `DefaultQueryExecutor.cs`. The `ToList()` call was the only LINQ usage — but `IQueryable<T>` comes from `System.Linq`, so the using is still needed. No change required.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.Core/Query/DefaultQueryExecutor.cs test/Microsoft.Restier.Tests.Core/Query/DefaultQueryExecutorTests.cs
git commit -m "fix: defer materialization in DefaultQueryExecutor (#614)"
```

---

### Task 2: Defer Materialization in `EFQueryExecutor`

**Files:**
- Modify: `src/Microsoft.Restier.EntityFramework.Shared/Query/EFQueryExecutor.cs:84`

- [ ] **Step 1: Change `EFQueryExecutor` to defer materialization**

In `src/Microsoft.Restier.EntityFramework.Shared/Query/EFQueryExecutor.cs`, change line 84 from:

```csharp
                return new QueryResult(await query.ToArrayAsync(cancellationToken).ConfigureAwait(false));
```

to:

```csharp
                return new QueryResult(query);
```

The EF6 `SelectExpandHelper` path (line 80) is unchanged — it must materialize.

- [ ] **Step 2: Clean up unused usings if applicable**

The `ToArrayAsync` call came from `Microsoft.EntityFrameworkCore` (EFCore) or `System.Data.Entity` (EF6). These usings are still needed for the `IAsyncQueryProvider`/`IDbAsyncQueryProvider` type checks and the `SelectExpandHelper` path. No changes needed.

- [ ] **Step 3: Run the EFCore integration tests to verify nothing breaks**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~EFCore.QueryTests" -v n`

Expected: All pass. The tests make HTTP requests that go through the full pipeline — deferred execution is transparent because the serializer still enumerates the results.

- [ ] **Step 4: Run the full test suite to check for regressions**

Run: `dotnet test RESTier.slnx -v n`

Expected: All tests pass. If any test fails, investigate — the failure likely means that consumer code was relying on materialized results and needs the fix from a later task (Task 5 for `EFChangeSetInitializer`).

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.EntityFramework.Shared/Query/EFQueryExecutor.cs
git commit -m "fix: defer materialization in EFQueryExecutor (#614)"
```

---

### Task 3: Remove `CheckSubExpressionResult` from `DefaultQueryHandler`

**Files:**
- Modify: `src/Microsoft.Restier.Core/Query/DefaultQueryHandler.cs:25-27,127-128,181-304`

- [ ] **Step 1: Remove the call to `CheckSubExpressionResult` in `QueryAsync`**

In `src/Microsoft.Restier.Core/Query/DefaultQueryHandler.cs`, remove lines 127-128:

```csharp
                await CheckSubExpressionResult(
                    context, result.Results, visitor, executor, expression, cancellationToken).ConfigureAwait(false);
```

- [ ] **Step 2: Remove the three private const string fields**

Remove lines 25-27:

```csharp
        private const string ExpressionMethodNameOfWhere = "Where";
        private const string ExpressionMethodNameOfSelect = "Select";
        private const string ExpressionMethodNameOfSelectMany = "SelectMany";
```

- [ ] **Step 3: Remove the three private methods**

Remove the entire `CheckSubExpressionResult` method (lines 181-235), `ExecuteSubExpression` method (lines 237-276), and `CheckWhereCondition` method (lines 278-304).

- [ ] **Step 4: Clean up unused usings**

After removing these methods, the following usings may no longer be needed in `DefaultQueryHandler.cs`. Check each:
- `System.Collections` — still used by `IEnumerable` in other parts? No, remove it.
- `System.Collections.Generic` — still used by `IDictionary` in `QueryExpressionVisitor`. Keep.
- `System.Net` — was used by `HttpStatusCode.NotFound` in `CheckWhereCondition`. Remove it.

- [ ] **Step 5: Run the core unit tests**

Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj -v n`

Expected: All pass. The `DefaultQueryHandlerTests` should still work since they test `QueryAsync` which no longer calls the removed methods.

- [ ] **Step 6: Run the full test suite**

Run: `dotnet test RESTier.slnx -v n`

Expected: The `GetNonExistingEntityTest` in `RestierControllerTests.cs:37` may now FAIL — it expects 404 for `/Products(-1)`, but with `CheckSubExpressionResult` removed, the controller returns 204 instead. This is expected and will be fixed in Task 4.

Note which tests fail. They should only be tests that expect 404 for nonexistent entities by key.

- [ ] **Step 7: Commit (even with known failures)**

```bash
git add src/Microsoft.Restier.Core/Query/DefaultQueryHandler.cs
git commit -m "refactor: remove CheckSubExpressionResult from DefaultQueryHandler (#614)

The 404 detection for key-based requests moves to RestierController
in the next commit. Tests expecting 404 on nonexistent entities
will temporarily fail."
```

---

### Task 4: Add 404 Detection to `RestierController`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs:155,372,459-554`
- Test: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/QueryTests.cs`
- Test: `test/Microsoft.Restier.Tests.AspNetCore/RestierControllerTests.cs`

- [ ] **Step 1: Write integration tests for 404/204 behavior**

Add tests to the base `QueryTests` class at `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/QueryTests.cs`. Add after the existing `ObservableCollectionsAsCollectionNavigationProperties` test (after line 77):

```csharp
[Fact]
public async Task NonExistentEntityByKeyReturns404()
{
    var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        HttpMethod.Get,
        resource: "/Books(00000000-0000-0000-0000-000000000000)",
        serviceCollection: ConfigureServices);
    _ = await TraceListener.LogAndReturnMessageContentAsync(response);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}

[Fact]
public async Task NonExistentParentEntityNavigationPropertyReturns404()
{
    var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        HttpMethod.Get,
        resource: "/Books(00000000-0000-0000-0000-000000000000)/Publisher",
        serviceCollection: ConfigureServices);
    _ = await TraceListener.LogAndReturnMessageContentAsync(response);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

- [ ] **Step 2: Run the new tests to verify they fail**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~NonExistentEntityByKeyReturns404 | FullyQualifiedName~NonExistentParentEntityNavigationPropertyReturns404" -v n`

Expected: FAIL — the controller currently returns 204 for these cases (since we removed `CheckSubExpressionResult` in Task 3).

- [ ] **Step 3: Change `CreateQueryResponse` signature to accept path and cancellation token**

In `src/Microsoft.Restier.AspNetCore/RestierController.cs`, change the method signature at line 459 from:

```csharp
        private IActionResult CreateQueryResponse(IQueryable query, IEdmType edmType, ETag etag)
```

to:

```csharp
        private async Task<IActionResult> CreateQueryResponse(IQueryable query, IEdmType edmType, ETag etag, ODataPath path, CancellationToken cancellationToken)
```

- [ ] **Step 4: Add `ParentEntityExistsAsync` helper method**

Add this private method to `RestierController`, after the `CreateQueryResponse` method:

```csharp
        private async Task<bool> ParentEntityExistsAsync(ODataPath fullPath, CancellationToken cancellationToken)
        {
            var parentSegments = new List<ODataPathSegment>();
            foreach (var segment in fullPath)
            {
                parentSegments.Add(segment);
                if (segment is KeySegment)
                {
                    break;
                }
            }

            var parentPath = new ODataPath(parentSegments);
            var parentQuery = new RestierQueryBuilder(api, parentPath).BuildQuery();
            if (parentQuery is null)
            {
                return false;
            }

            var queryRequest = new QueryRequest(parentQuery);
            var result = await api.QueryAsync(queryRequest, cancellationToken).ConfigureAwait(false);
            var enumerator = result.Results.GetEnumerator();
            return enumerator.MoveNext();
        }
```

- [ ] **Step 5: Add 404 detection for `BaseSingleResult` null path**

In `CreateQueryResponse`, replace the `singleResult` null check block (lines 504-514) with parent-existence-aware logic:

Replace:

```csharp
            if (singleResult is not null)
            {
                if (singleResult.Result is null)
                {
                    // Per specification, If the property is single-valued and has the null value,
                    // the service responds with 204 No Content.
                    return NoContent();
                }

                return response;
            }
```

with:

```csharp
            if (singleResult is not null)
            {
                if (singleResult.Result is null)
                {
                    // Check if parent entity doesn't exist (404) vs property is null (204)
                    if (path.OfType<KeySegment>().Any())
                    {
                        var parentExists = await ParentEntityExistsAsync(path, cancellationToken).ConfigureAwait(false);
                        if (!parentExists)
                        {
                            return NotFound(Resources.ResourceNotFound);
                        }
                    }

                    // Per specification, If the property is single-valued and has the null value,
                    // the service responds with 204 No Content.
                    return NoContent();
                }

                return response;
            }
```

- [ ] **Step 6: Add 404 detection for entity result null path**

Replace the entity result null check (lines 527-531):

```csharp
            var entityResult = query.SingleOrDefault();
            if (entityResult is null)
            {
                return NoContent();
            }
```

with:

```csharp
            var entityResult = query.SingleOrDefault();
            if (entityResult is null)
            {
                var lastSegment = path.LastOrDefault();
                var isKeyRequest = lastSegment is KeySegment
                    || (lastSegment is TypeSegment && path.Count >= 2 && path[path.Count - 2] is KeySegment);

                if (isKeyRequest)
                {
                    return NotFound(Resources.ResourceNotFound);
                }

                // Parent entity might not exist — check before returning 204
                if (path.OfType<KeySegment>().Any())
                {
                    var parentExists = await ParentEntityExistsAsync(path, cancellationToken).ConfigureAwait(false);
                    if (!parentExists)
                    {
                        return NotFound(Resources.ResourceNotFound);
                    }
                }

                return NoContent();
            }
```

- [ ] **Step 7: Update call sites to pass path and cancellation token**

In `Get()` method, change line 155 from:

```csharp
            return CreateQueryResponse(result, path.GetEdmType(), etag);
```

to:

```csharp
            return await CreateQueryResponse(result, path.GetEdmType(), etag, path, cancellationToken).ConfigureAwait(false);
```

In `PostAction()` method, change line 372 from:

```csharp
            return CreateQueryResponse(result, path.GetEdmType(), null);
```

to:

```csharp
            return await CreateQueryResponse(result, path.GetEdmType(), null, path, cancellationToken).ConfigureAwait(false);
```

- [ ] **Step 8: Add missing using for `List<>` if needed**

`System.Collections.Generic` should already be imported (line 6). Verify `ODataPathSegment` and `KeySegment` etc. are available — they come from `Microsoft.OData.UriParser` which is already imported (line 23). No new usings needed.

- [ ] **Step 9: Run the new tests**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~NonExistentEntityByKeyReturns404 | FullyQualifiedName~NonExistentParentEntityNavigationPropertyReturns404" -v n`

Expected: PASS.

- [ ] **Step 10: Run the full test suite**

Run: `dotnet test RESTier.slnx -v n`

Expected: All tests pass, including the previously-failing `GetNonExistingEntityTest` and `EmptyEntitySetQueryReturns200Not404`.

- [ ] **Step 11: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/RestierController.cs test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/QueryTests.cs
git commit -m "fix: add 404 detection for key-based requests in RestierController (#614)

Replaces the removed CheckSubExpressionResult logic. Distinguishes:
- Entity by key not found -> 404
- Nonexistent parent entity on nav/property path -> 404
- Null-valued property on existing entity -> 204"
```

---

### Task 5: Preserve Materialization in `EFChangeSetInitializer`

**Files:**
- Modify: `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs:127-149`
- Modify: `src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs:151-173`

- [ ] **Step 1: Run the update/delete tests to see if they fail**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~UpdateTests | FullyQualifiedName~RestierControllerTests" -v n`

Expected: These may fail if `FindResource` receives a live `IQueryable` and enumerates it multiple times. Check the output.

- [ ] **Step 2: Fix `FindResource` in EFCore `EFChangeSetInitializer`**

In `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs`, replace the `FindResource` method body (lines 127-149):

Replace:

```csharp
        private static async Task<object> FindResource(SubmitContext context, DataModificationItem item, CancellationToken cancellationToken)
        {
            var apiBase = context.Api;
            var query = apiBase.GetQueryableSource(item.ResourceSetName);
            query = item.ApplyTo(query);

            var result = await apiBase.QueryAsync(new QueryRequest(query), cancellationToken).ConfigureAwait(false);

            var resource = result.Results.SingleOrDefault();
            if (resource is null)
            {
                throw new StatusCodeException(HttpStatusCode.NotFound, Resources.ResourceNotFound);
            }

            // This means no If-Match or If-None-Match header
            if (item.OriginalValues is null || item.OriginalValues.Count == 0)
            {
                return resource;
            }

            resource = item.ValidateEtag(result.Results.AsQueryable());
            return resource;
        }
```

with:

```csharp
        private static async Task<object> FindResource(SubmitContext context, DataModificationItem item, CancellationToken cancellationToken)
        {
            var apiBase = context.Api;
            var query = apiBase.GetQueryableSource(item.ResourceSetName);
            query = item.ApplyTo(query);

            var result = await apiBase.QueryAsync(new QueryRequest(query), cancellationToken).ConfigureAwait(false);

            // Materialize to ensure consistent snapshot for multi-enumeration (ETag validation
            // may re-enumerate). The executor no longer materializes, so we do it here.
            var materialized = result.Results.Cast<object>().ToArray();

            var resource = materialized.Length == 1 ? materialized[0] : null;
            if (resource is null)
            {
                if (materialized.Length > 1)
                {
                    throw new InvalidOperationException(Core.Resources.QueryShouldGetSingleRecord);
                }

                throw new StatusCodeException(HttpStatusCode.NotFound, Resources.ResourceNotFound);
            }

            // This means no If-Match or If-None-Match header
            if (item.OriginalValues is null || item.OriginalValues.Count == 0)
            {
                return resource;
            }

            resource = item.ValidateEtag(materialized.AsQueryable());
            return resource;
        }
```

- [ ] **Step 3: Add required using for `System.Linq`**

Check if `System.Linq` is already imported in the EFCore `EFChangeSetInitializer.cs`. It should be — the file uses `item.ApplyTo(query)` and other LINQ methods. Verify and add if missing.

- [ ] **Step 4: Fix `FindResource` in EF6 `EFChangeSetInitializer`**

In `src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs`, apply the identical change to the `FindResource` method (lines 151-173):

Replace:

```csharp
        private static async Task<object> FindResource(SubmitContext context, DataModificationItem item, CancellationToken cancellationToken)
        {
            var apiBase = context.Api;
            var query = apiBase.GetQueryableSource(item.ResourceSetName);
            query = item.ApplyTo(query);

            var result = await apiBase.QueryAsync(new QueryRequest(query), cancellationToken).ConfigureAwait(false);

            var resource = result.Results.SingleOrDefault();
            if (resource is null)
            {
                throw new StatusCodeException(HttpStatusCode.NotFound, Resources.ResourceNotFound);
            }

            // This means no If-Match or If-None-Match header
            if (item.OriginalValues is null || item.OriginalValues.Count == 0)
            {
                return resource;
            }

            resource = item.ValidateEtag(result.Results.AsQueryable());
            return resource;
        }
```

with:

```csharp
        private static async Task<object> FindResource(SubmitContext context, DataModificationItem item, CancellationToken cancellationToken)
        {
            var apiBase = context.Api;
            var query = apiBase.GetQueryableSource(item.ResourceSetName);
            query = item.ApplyTo(query);

            var result = await apiBase.QueryAsync(new QueryRequest(query), cancellationToken).ConfigureAwait(false);

            // Materialize to ensure consistent snapshot for multi-enumeration (ETag validation
            // may re-enumerate). The executor no longer materializes, so we do it here.
            var materialized = result.Results.Cast<object>().ToArray();

            var resource = materialized.Length == 1 ? materialized[0] : null;
            if (resource is null)
            {
                if (materialized.Length > 1)
                {
                    throw new InvalidOperationException(Core.Resources.QueryShouldGetSingleRecord);
                }

                throw new StatusCodeException(HttpStatusCode.NotFound, Resources.ResourceNotFound);
            }

            // This means no If-Match or If-None-Match header
            if (item.OriginalValues is null || item.OriginalValues.Count == 0)
            {
                return resource;
            }

            resource = item.ValidateEtag(materialized.AsQueryable());
            return resource;
        }
```

- [ ] **Step 5: Run update/delete tests**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~UpdateTests | FullyQualifiedName~RestierControllerTests" -v n`

Expected: All pass.

- [ ] **Step 6: Run full test suite**

Run: `dotnet test RESTier.slnx -v n`

Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs
git commit -m "fix: materialize explicitly in EFChangeSetInitializer.FindResource (#614)

The submit path needs multi-enumeration (SingleOrDefault + ETag
validation). Since executors no longer materialize, FindResource
materializes to an array for a consistent snapshot."
```

---

### Task 6: Document EF6 `SelectExpandHelper` Materialization

**Files:**
- Create: `docs/msdocs/server/performance.md`
- Modify: `docs/msdocs/docfx.json` (only if toc changes are needed — docfx auto-discovers md files, so likely not needed)

- [ ] **Step 1: Create the performance documentation page**

Create `docs/msdocs/server/performance.md`:

```markdown
---
title: Performance Considerations
description: Performance notes and known limitations for RESTier.
---

# Performance Considerations

## Query Execution and Streaming

RESTier passes `IQueryable` results from Entity Framework through to the OData serializer without buffering the entire result set in memory. For collection queries (e.g., `GET /Products`), the OData serializer enumerates the `IQueryable` directly, which means:

- Results are not fully loaded into memory before serialization begins
- Memory usage is proportional to the serialization buffer, not the full result set
- This is the same pattern used by standard ASP.NET Core OData controllers

For single-entity queries (e.g., `GET /Products(1)`), the result is a single row and is evaluated eagerly in the controller.

## Entity Framework 6: `$expand` and `$select` Materialization

When using **Entity Framework 6** (not EF Core) with `$expand` or `$select` query options, RESTier must materialize the full result set in memory before serialization. This is because OData v9's `SelectExpandBinder` generates LINQ expression trees that contain `IEdmModel` constants, which EF6 cannot translate to SQL.

RESTier works around this by:

1. Stripping the `$expand`/`$select` projection from the LINQ expression tree
2. Adding `Include()` calls for navigation properties referenced by `$expand`
3. Executing the stripped query against EF6 to load entities
4. Re-applying the projection in memory

This workaround does not affect **Entity Framework Core**, which handles these expression trees natively.

If you are using EF6 and working with large result sets combined with `$expand`/`$select`, consider:

- Using server-side paging (`$top` / `$skip`) to limit result sizes
- Migrating to Entity Framework Core, which does not have this limitation
```

- [ ] **Step 2: Verify the docs build**

Run: `docs/msdocs/build.sh`

Expected: Build succeeds without errors. The new page should appear in the output.

- [ ] **Step 3: Commit**

```bash
git add docs/msdocs/server/performance.md
git commit -m "docs: add performance page documenting EF6 materialization (#614)"
```

---

### Task 7: Final Verification

- [ ] **Step 1: Run the complete test suite**

Run: `dotnet test RESTier.slnx -v n`

Expected: All tests pass with zero failures.

- [ ] **Step 2: Run the complete test suite with code coverage**

Run:

```bash
rm -rf TestResults/Coverage
dotnet test RESTier.slnx --collect:"XPlat Code Coverage" --results-directory TestResults/Coverage
~/.dotnet/tools/reportgenerator "-reports:TestResults/Coverage/*/coverage.cobertura.xml" "-targetdir:TestResults/CoverageReport" -reporttypes:TextSummary
cat TestResults/CoverageReport/Summary.txt
```

Expected: Coverage should be comparable to the baseline. The removed `CheckSubExpressionResult` code reduces the denominator, so coverage percentage may slightly increase.

- [ ] **Step 3: Verify key scenarios manually with the test requests**

Run these targeted test filters to confirm each scenario from the spec:

```bash
# Empty entity set returns 200 (not 404)
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~EmptyEntitySetQueryReturns200Not404" -v n

# Empty filter returns 200 (not 404)
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~EmptyFilterQueryReturns200Not404" -v n

# Nonexistent entity by key returns 404
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~NonExistentEntityByKeyReturns404 | FullyQualifiedName~GetNonExistingEntityTest" -v n

# Navigation properties work
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~ObservableCollections" -v n

# Update/delete with ETag still works
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~UpdateTests" -v n

# Batch requests still work
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~BatchTests" -v n
```

Expected: All pass.

- [ ] **Step 4: Commit any remaining fixes**

If any tests failed and required fixes, commit them now.
