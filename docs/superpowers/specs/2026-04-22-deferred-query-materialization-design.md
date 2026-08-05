# Deferred Query Materialization

**Date:** 2026-04-22
**Issue:** [OData/RESTier#614](https://github.com/OData/RESTier/issues/614)

## Problem

RESTier's query pipeline materializes the entire result set into memory (via `ToList()` / `ToArrayAsync()`) inside query executors before returning results. This means every query — regardless of size — is fully buffered before the OData serializer sees it. For large collection queries this causes unnecessary memory pressure.

## Goals

- Eliminate executor-level `ToList()` / `ToArrayAsync()` allocation for **collection responses**, allowing the OData serializer to enumerate the `IQueryable` directly without buffering the full result set
- Remove the `CheckSubExpressionResult` method that forces early enumeration to detect empty results
- Move single-entity 404 detection from the query handler to the controller, where HTTP semantics belong
- Document the intentional EF6 `SelectExpandHelper` materialization

## Scope and Constraints

The primary benefit is for **collection responses** (`GET /EntitySet`). These are the queries where full-buffer allocation is costly.

**Single-entity paths** (primitive, complex, enum, raw `$value`, ETag, and the entity-by-key branch in `CreateQueryResponse`) still enumerate eagerly in the controller and result class constructors (`BaseSingleResult:26` calls `query.SingleOrDefault()`). These are 1-row queries where the memory overhead is negligible and the eager enumeration is acceptable.

**Async/cancellation trade-off:** The current `ToArrayAsync(cancellationToken)` provides async execution and explicit cancellation. After the change, the OData serializer enumerates `IQueryable` synchronously via `IEnumerable` — ASP.NET Core OData 9.x does not support `IAsyncEnumerable`. This is the standard pattern used by non-RESTier ASP.NET Core OData controllers (which return `IQueryable<T>` directly). Cancellation still works via connection-drop detection. For single-entity paths, the sync execution is on 1-row queries and the thread-blocking is trivial.

## Non-Goals

- Changing `QueryResult` or `IQueryExecutor` contracts
- Fixing the EF6 `SelectExpandHelper` materialization (intentional workaround, documented instead)
- True async streaming (would require `IAsyncEnumerable` support in the OData serializer)
- Changing submit-path materializations in `EFChangeSetInitializer` (see section 7)

## Design

### 1. Defer Materialization in `EFQueryExecutor`

**File:** `src/Microsoft.Restier.EntityFramework.Shared/Query/EFQueryExecutor.cs`

Change `ExecuteQueryAsync` to pass the `IQueryable` through without materializing:

```csharp
// Before
return new QueryResult(await query.ToArrayAsync(cancellationToken).ConfigureAwait(false));

// After
return new QueryResult(query);
```

`QueryResult` accepts `IEnumerable`, and `IQueryable` implements `IEnumerable`, so this is a compatible change. The query will be executed when the OData serializer enumerates the results (for collections) or when the controller calls `SingleOrDefault()` (for single entities).

The EF6 `SelectExpandHelper` path is unchanged — it must materialize to work around the OData/EF6 expression tree incompatibility.

### 2. Defer Materialization in `DefaultQueryExecutor`

**File:** `src/Microsoft.Restier.Core/Query/DefaultQueryExecutor.cs`

Same change for the fallback (non-EF) executor:

```csharp
// Before
var result = new QueryResult(query.ToList());

// After
var result = new QueryResult(query);
```

The `IQueryable` contract guarantees deferred execution. Custom `IQueryable` sources are expected to handle this.

### 3. Remove `CheckSubExpressionResult` from `DefaultQueryHandler`

**File:** `src/Microsoft.Restier.Core/Query/DefaultQueryHandler.cs`

Remove three methods entirely:
- `CheckSubExpressionResult` — forces enumeration of results just to check emptiness
- `ExecuteSubExpression` — re-executes stripped sub-queries (the 404 check at lines 264-275 is already commented out)
- `CheckWhereCondition` — detects key-predicate Where clauses to throw 404

Remove the call site in `QueryAsync` (lines 127-128):

```csharp
// Remove this block
await CheckSubExpressionResult(
    context, result.Results, visitor, executor, expression, cancellationToken).ConfigureAwait(false);
```

Also remove the three `const string` fields (`ExpressionMethodNameOfWhere`, `ExpressionMethodNameOfSelect`, `ExpressionMethodNameOfSelectMany`) that are only used by the removed methods.

**Why this is safe:** `CheckSubExpressionResult` has two behaviors:
1. Key-predicate 404 detection (via `CheckWhereCondition`) — moved to controller (see section 4)
2. Sub-expression re-execution (via `ExecuteSubExpression`) — the actual 404 throw is commented out, so this currently does nothing useful except waste a database round-trip

### 4. Add 404 Detection to `RestierController`

**File:** `src/Microsoft.Restier.AspNetCore/RestierController.cs`

404 detection covers two cases: direct key requests and property/navigation paths on nonexistent parents.

#### Case A: Direct key request returns nothing

In `CreateQueryResponse`, the single-entity path (line 527) calls `query.SingleOrDefault()`. When the last segment is a `KeySegment` (or `TypeSegment` after `KeySegment`) and the result is null, return 404:

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

    // ...
}
```

This handles:
- `GET /Products(999)` → 404
- `GET /Products(999)/MyNamespace.SpecialProduct` → 404

#### Case B: Property/navigation path on nonexistent parent

For paths like `GET /Products(999)/Publisher` or `GET /Products(999)/Name`, the last segment is `NavigationPropertySegment` or `PropertySegment`, NOT `KeySegment`. If the result is null, we cannot tell from the result alone whether the parent entity doesn't exist (404) or the property is genuinely null (204).

When the path contains a `KeySegment` that is NOT the terminal segment and the result is null, execute a lightweight parent-existence query:

```csharp
if (entityResult is null && !isKeyRequest)
{
    // Check if the path has a keyed parent whose existence we need to verify
    if (path.OfType<KeySegment>().Any())
    {
        var parentExists = await ParentEntityExistsAsync(path, cancellationToken)
            .ConfigureAwait(false);
        if (!parentExists)
        {
            return NotFound(Resources.ResourceNotFound);
        }
    }

    return NoContent();
}
```

The `ParentEntityExistsAsync` helper truncates the OData path at the last `KeySegment` (including any trailing `TypeSegment`), builds a query via `RestierQueryBuilder` for just the parent entity, and checks if it returns any results:

```csharp
private async Task<bool> ParentEntityExistsAsync(ODataPath fullPath, CancellationToken cancellationToken)
{
    // Build a path containing only segments up to and including the KeySegment
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
    var queryRequest = new QueryRequest(parentQuery);
    var result = await api.QueryAsync(queryRequest, cancellationToken).ConfigureAwait(false);
    return result.Results.GetEnumerator().MoveNext();
}
```

This only runs when a result is null AND the path has a keyed parent — the common case (entity exists, property has value) has zero overhead. The extra query is one `SELECT ... WHERE key = @key LIMIT 1` — comparable to what `CheckSubExpressionResult` did before.

This also handles the `BaseSingleResult` paths (primitive, complex, enum, raw) since those return `NoContent` in `CreateQueryResponse` (line 504-511) when `Result` is null. The same pattern applies: thread `ODataPath` through and check parent existence before deciding 204 vs 404.

#### `CreateQueryResponse` signature change

Add `ODataPath path` and `CancellationToken cancellationToken` parameters. All call sites already have both available.

### 5. Downstream Auto-Fix: `RestierController.ExecuteQuery`

**File:** `src/Microsoft.Restier.AspNetCore/RestierController.cs`

The `.AsQueryable()` call at line 625:

```csharp
var result = queryResult.Results.AsQueryable();
```

When `Results` holds a live `IQueryable`, `AsQueryable()` returns it unchanged (the extension method checks `is IQueryable<T>` first). No code change needed — this becomes a no-op passthrough automatically.

### 6. Document EF6 `SelectExpandHelper` Materialization

**File:** `docs/msdocs/` (new or existing performance/known-issues page)

Add documentation explaining that when using Entity Framework 6 with `$expand`/`$select`, results are materialized in memory before serialization. This is an intentional workaround for EF6 not being able to translate OData's `SelectExpand` expression trees to SQL. EF Core is not affected.

### 7. Explicitly Preserve `EFChangeSetInitializer` Materialization

**Files:** `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs`, `src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs`

No changes. The submit path's `FindResource` method:
1. Calls `result.Results.SingleOrDefault()` (first enumeration)
2. Passes `result.Results.AsQueryable()` to `ValidateEtag` (second enumeration)
3. `ValidateEtag` may enumerate again on failure (`ChangeSetItem.cs:284`)

With the deferred `IQueryable`, each enumeration would be a separate database query with a wider concurrency window. This is unacceptable — the submit path must see a consistent snapshot.

The submit path calls `api.QueryAsync` → executor → `QueryResult`. Since the executor no longer materializes, the submit path would receive a live `IQueryable`. To preserve the current behavior, `FindResource` should explicitly materialize before consuming:

```csharp
// In FindResource, after getting the query result:
var result = await apiBase.QueryAsync(new QueryRequest(query), cancellationToken).ConfigureAwait(false);

// Materialize to ensure consistent snapshot for multi-enumeration
var materialized = result.Results.Cast<object>().ToArray();
var resource = materialized.SingleOrDefault();
```

This is a targeted materialization at the consumption site (where multi-enumeration is needed), not in the executor (where it was unnecessarily broad).

## Impact on Existing Consumers

| Consumer | Current behavior | After change | Breaking? |
|----------|-----------------|-------------|-----------|
| `RestierController.ExecuteQuery` | `.AsQueryable()` wraps materialized list | `.AsQueryable()` passes through live `IQueryable` | No |
| `RestierController.CreateQueryResponse` (collections) | Serializer iterates in-memory list | Serializer iterates live `IQueryable` | No |
| `RestierController.CreateQueryResponse` (single entity) | `SingleOrDefault()` on in-memory list | `SingleOrDefault()` on live `IQueryable` (1 row) | No |
| `BaseSingleResult` | `SingleOrDefault()` on in-memory list | `SingleOrDefault()` on live `IQueryable` (1 row) | No |
| `EFChangeSetInitializer.FindResource` | Multi-enumeration on in-memory list | Explicit materialization added (see section 7) | No |
| `RestierQueryExecutor` | Delegates to inner, no materialization | Unchanged | No |
| Custom `IQueryExecutor` implementations | N/A — their behavior is their own | N/A | No |

## Testing

- Existing integration tests should continue to pass (query results are the same, just deferred)
- Add/verify tests for 404 on `GET /EntitySet(nonexistent-key)`
- Add/verify tests for 404 on `GET /EntitySet(nonexistent-key)/NavigationProperty`
- Add/verify tests for 404 on `GET /EntitySet(nonexistent-key)/PrimitiveProperty`
- Add/verify tests for 204 on null single-valued navigation properties (parent exists)
- Verify that `$expand`/`$select` still works on both EF6 and EF Core paths
- Verify `$count` still works (goes through `ExecuteExpressionAsync`, not affected)
- Verify submit operations (PUT, PATCH, DELETE) with ETag validation still work correctly
- Verify batch requests still work correctly
