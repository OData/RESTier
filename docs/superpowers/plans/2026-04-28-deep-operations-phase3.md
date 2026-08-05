# Deep Operations Phase 3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete remaining contract gaps from the deep operations spec: full single-nav classification, OData-Version documentation/gating, and remaining test matrix coverage.

**Architecture:** Phase 1 built the extraction + flatten + nav-prop-wiring pipeline. Phase 2 fixed bugs, added the DeepUpdateClassifier, response expansion, and error mapping. Phase 3 completes the single-nav deep update contract and closes test coverage gaps.

**Tech Stack:** .NET 8/9/10, Microsoft.AspNetCore.OData 9.x, Microsoft.OData.Core 8.x, EF 6 + EF Core, xUnit v3, FluentAssertions

**Spec:** `docs/superpowers/specs/2026-04-22-deep-operations-design.md`

---

## Context: Phase 1+2 State

### What works:
- Deep insert with collection nav props (Publisher + inline Books) — both EF6 and EFCore
- Multi-level deep insert (Publisher → Books → Reviews)
- Server-generated key propagation via nav prop assignment
- Convention methods fire for nested entities (OnInsertingBook, OnInsertingReview)
- `@odata.bind` detection via key-subset heuristic (validated by batch tests and inline tests)
- Bind reference validation (404 → 400 for non-existent referenced entities)
- Response expansion (201 response includes expanded nested entities, multi-level)
- Deep update: PATCH/PUT with inline new children (Insert classification)
- Deep update: reclassification of keyed children (Insert → Update via EntityExistsByKey)
- Deep update: PUT omitted children unlinked (RelationshipRemoval with FK nulling)
- Deep update: move existing child to new parent
- Deep update: null FK unlink (PATCH with PublisherId: null)
- Null nav prop detection and FK-based unlink (Book.Publisher = null → PublisherId = null)
- MaxDepth enforcement with correct boundary behavior
- DbUpdateException → 400 mapping for relationship constraint violations
- Non-nullable FK → 400 with descriptive message
- Unsupported relationships → 501 Not Implemented
- DeepOperationSettings configurable via DI

### Known limitations (documented in spec):
- OData-Version: 4.01 header breaks EdmEntityObject deserialization entirely (ASP.NET Core OData 9.x upstream limitation). All entity reference formats work under default/4.0 semantics.
- Nested delta payloads not supported (would require 501)
- Many-to-many, shadow FK, nav-only models not supported (501)
- Response expansion for bound entities (only inline nested entities are expanded)

### Remaining gaps (this plan):
1. Single-nav deep update classification incomplete — no current-entity loading, no same-vs-replace distinction
2. OData-Version not read or gated in controller
3. Test coverage: no 4.01-header tests, no @odata.bind wire-format tests in DeepInsertTests (covered by BatchTests), no single-nav deep update tests

---

## Task 1: Full Single-Nav Deep Update Classification

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepUpdateClassifier.cs`
- Add tests to: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs`

### What the plan requires (docs/superpowers/specs/2026-04-22-deep-operations-design.md:197-202)

| Payload | Action |
|---------|--------|
| Full nested entity with matching key | `Update` the related entity |
| Full nested entity with new/no key | `Insert` new entity; unlink previous if FK is nullable |
| Entity reference (`@odata.bind` / `@id`) | Already handled as NavigationBinding |
| `null` | Set FK to null — **implemented in Phase 2** |
| Absent from payload | No action (PATCH) |

### What's currently implemented

`ClassifySingleNavProp` only does `EntityExistsByKey` — it never loads the current related entity or distinguishes:
- "same entity being updated" (key matches current → Update)
- "replacing with a different existing entity" (key exists but differs from current → Update + unlink old)
- "replacing with new entity" (no key → Insert + unlink old)

### Step 1.1: Write failing tests first

- [ ] Add to `DeepUpdateTests.cs`:

```csharp
[Fact]
public async Task DeepUpdate_SingleNavProperty_ReplaceWithExisting()
{
    // Create a Book linked to Publisher1
    // PATCH the Book with an inline Publisher2 (by key, full entity)
    // Assert: Book is now linked to Publisher2, Publisher2 was Updated (not inserted)
    var bookPayload = new { Isbn = "3030303030303", Title = "NavProp Replace Test", IsActive = true };
    var createResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        HttpMethod.Post,
        resource: "/Publishers('Publisher1')/Books",
        payload: bookPayload,
        acceptHeader: WebApiConstants.DefaultAcceptHeader,
        serviceCollection: ConfigureServices);
    createResponse.IsSuccessStatusCode.Should().BeTrue();
    var (createdBook, _) = await createResponse.DeserializeResponseAsync<Book>();

    // PATCH with Publisher2 inline (has key + non-key props → classified as Update+link)
    // NOTE: Must include at least one non-key property; key-only payloads are treated
    // as entity references (@odata.bind) by IsEntityReference and never reach the classifier.
    var patchPayload = new
    {
        Publisher = new { Id = "Publisher2", Addr = new { Street = "456 Oak Ave", Zip = "54321" } },
    };
    var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        new HttpMethod("PATCH"),
        resource: $"/Books({createdBook.Id})",
        payload: patchPayload,
        acceptHeader: WebApiConstants.DefaultAcceptHeader,
        serviceCollection: ConfigureServices);

    var content = await patchResponse.Content.ReadAsStringAsync(TestContext.CancellationToken);
    patchResponse.IsSuccessStatusCode.Should().BeTrue(
        because: $"replacing Publisher via inline nested entity should succeed. Response: {content}");

    // Verify book is now linked to Publisher2
    var verifyResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
        HttpMethod.Get,
        resource: $"/Books({createdBook.Id})?$expand=Publisher",
        acceptHeader: ODataConstants.DefaultAcceptHeader,
        serviceCollection: ConfigureServices);
    var (updatedBook, _) = await verifyResponse.DeserializeResponseAsync<Book>();
    updatedBook.PublisherId.Should().Be("Publisher2");
}
```

### Step 1.2: Implement full single-nav classification

- [ ] Modify `ClassifySingleNavProp` in `DeepUpdateClassifier.cs`:

```csharp
private async Task ClassifySingleNavProp(
    DataModificationItem rootItem,
    string navPropName,
    DataModificationItem nestedItem,
    IEdmNavigationProperty edmNavProp,
    IEdmEntitySet entitySet,
    CancellationToken cancellationToken)
{
    var targetEntitySetName = FindTargetEntitySetName(edmNavProp);
    var fkPropertyName = FindFkPropertyName(edmNavProp);

    if (nestedItem.ResourceKey is not null && nestedItem.ResourceKey.Count > 0)
    {
        // Has key — check if entity exists globally
        var exists = await EntityExistsByKey(
            targetEntitySetName, nestedItem.ResourceKey, cancellationToken).ConfigureAwait(false);

        if (exists)
        {
            ReclassifyAsUpdate(nestedItem);
        }

        // If the FK is on the root entity (dependent side), update the FK
        // to point to the new target entity. This handles both "same entity"
        // and "replace with different entity" cases.
        if (fkPropertyName is not null)
        {
            // Get the target entity's key value (for the FK)
            var targetKeyValue = nestedItem.ResourceKey.Values.First();
            var updatedValues = new Dictionary<string, object>(rootItem.LocalValues ?? new Dictionary<string, object>())
            {
                [fkPropertyName] = targetKeyValue,
            };
            rootItem.LocalValues = updatedValues;
        }
    }
    else
    {
        // No key — new entity to Insert.
        // If FK is on root entity and currently set, we might want to unlink the old one.
        // But since the new entity will be wired via nav prop assignment by the initializer,
        // EF will handle the FK update automatically. No explicit unlink needed.
    }
}
```

**Key insight 1:** When a single nav prop has an FK on the root entity (e.g., `Book.PublisherId`), setting the FK value in `LocalValues` handles both "same entity" (no change) and "replace" (FK changes) cases. EF's `SetValues` will apply the FK during initialization.

**Key insight 2:** The FK update must happen for ALL keyed payloads, not just existing entities. When a client supplies a key for a new entity (Insert with client-supplied key), the root's FK still needs updating. Therefore the FK-update block is **outside** the `if (exists)` branch — it runs whenever a key is present, regardless of Insert vs Update classification.

**Key insight 3:** Test payloads for single-nav classification MUST include at least one non-key property. The `IsEntityReference` heuristic in `DeepOperationExtractor.cs:142` treats any nested entity whose only changed properties are key properties as a bind reference (`@odata.bind`). A key-only payload like `{ Id = "Publisher2" }` will be routed to `NavigationBindings`, never reaching `NestedItems` or `ClassifySingleNavProp`.

### Step 1.3: Run tests, iterate

- [ ] Run: `dotnet test ... --filter "DeepUpdateTests|UpdateTests"`

### Step 1.4: Commit

```bash
git commit -am "feat: full single-nav deep update classification with FK update"
```

---

## Task 2: OData-Version Gating

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`
- Add tests to: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs`

### What to implement

The Phase 1 exploration showed that OData-Version: 4.01 breaks `EdmEntityObject` deserialization entirely — the controller parameter arrives as null. This means:
- No version-based code path is needed in the extractor
- But the controller should read and log the version for diagnostic purposes
- The 4.01 failure produces a generic "A POST requires an object to be present in the request body" 400 error, which isn't helpful

### Step 2.1: Add better error message for 4.01

- [ ] In `RestierController.Post()`, where `edmEntityObject is null` is checked:

```csharp
if (edmEntityObject is null)
{
    var odataVersion = Request.Headers["OData-Version"].FirstOrDefault()?.Trim();
    if (string.Equals(odataVersion, "4.01", StringComparison.Ordinal))
    {
        throw new ODataException(
            "OData-Version 4.01 is not supported for deep operations. " +
            "ASP.NET Core OData 9.x does not support untyped (EdmEntityObject) deserialization with 4.01. " +
            "Remove the OData-Version header or use OData-Version: 4.0.");
    }

    throw new ODataException("A POST requires an object to be present in the request body.");
}
```

Same in `Update()`, but **critically**: the null guard must be placed **before** line 453 of `RestierController.cs`, where `edmEntityObject.ActualEdmType` is first dereferenced. The current code accesses `edmEntityObject.ActualEdmType` and later `CreatePropertyDictionary(...)` with no prior null check. Insert the guard immediately after the etag/precondition check (after the `propertiesInEtag is null` block, around line 443):

```csharp
if (edmEntityObject is null)
{
    var odataVersion = Request.Headers["OData-Version"].FirstOrDefault()?.Trim();
    if (string.Equals(odataVersion, "4.01", StringComparison.Ordinal))
    {
        throw new ODataException(
            "OData-Version 4.01 is not supported for deep operations. " +
            "ASP.NET Core OData 9.x does not support untyped (EdmEntityObject) deserialization with 4.01. " +
            "Remove the OData-Version header or use OData-Version: 4.0.");
    }

    throw new ODataException("An update requires an object to be present in the request body.");
}
```

### Step 2.2: Write test

```csharp
[Fact]
public async Task DeepInsert_ODataVersion401_ReturnsClearErrorMessage()
{
    // This test requires sending a custom OData-Version header.
    // RestierTestHelpers.ExecuteTestRequest doesn't support custom headers,
    // so use the TestServer directly.
    // If this can't be implemented without significant infrastructure,
    // document as a known limitation in the spec.
}
```

Note: If `RestierTestHelpers.ExecuteTestRequest` doesn't support custom headers (confirmed by Task 1 exploration), this test requires `RestierTestHelpers.GetTestableRestierServer<TApi>()` + manual `HttpRequestMessage` construction. Add this test if feasible; if not, document that the 4.01 error message improvement exists but is not directly testable via the standard test helper.

### Step 2.3: Commit

```bash
git commit -am "feat: better error message for OData-Version 4.01 unsupported deserialization"
```

---

## Task 3: Single-Nav Deep Update — Unlink Previous on Insert

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Submit/DeepUpdateClassifier.cs`
- Add tests to: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs`

### What to implement

There are TWO distinct cases here that must be handled separately:

**Case A: No key (server-generated key)**
When a PATCH includes an inline single nav prop with no key at all, a new entity is Inserted. EF wires the FK via nav prop assignment (`book.Publisher = newPublisher`), so the FK update should happen automatically via the change tracker.

**Case B: Client-supplied key for a new entity (unknown key)**
When a PATCH includes an inline single nav prop with a key that doesn't exist in the database (e.g., `Publisher: { Id: "NewPub", Addr: { ... } }`), `EntityExistsByKey` returns false and the item stays as Insert. But if the FK is on the root entity (e.g., `Book.PublisherId`), the FK won't be updated automatically because the key is client-supplied, not server-generated. This case is already handled by Task 1's `ClassifySingleNavProp` refactor — the FK-update block runs for ALL keyed payloads regardless of whether the entity exists.

### Step 3.1: Write tests for BOTH cases

```csharp
[Fact]
public async Task DeepUpdate_SingleNavProperty_InsertNewRelated_NoKey()
{
    // Create a Book linked to Publisher1
    // PATCH with a NEW inline Publisher (no key — server-generated)
    // Assert: new Publisher created, Book linked to it
    // This case relies on EF nav prop wiring (change tracker)
}

[Fact]
public async Task DeepUpdate_SingleNavProperty_InsertNewRelated_ClientSuppliedKey()
{
    // Create a Book linked to Publisher1
    // PATCH with a NEW inline Publisher with a client-supplied key
    // that doesn't exist in the database (e.g., Id = "NewPub123")
    // Must include non-key properties to avoid IsEntityReference heuristic
    // Assert: new Publisher created with the client-supplied key, Book linked to it
}
```

### Step 3.2: Verify or fix

**Case A (no key):** If the test passes (EF handles it via nav prop wiring), no code change needed.
**Case B (client-supplied key):** This should already work after Task 1's `ClassifySingleNavProp` refactor, which moves FK-update logic outside the `if (exists)` block. If it fails, ensure the FK update runs in the `else` (not-exists) branch too.

### Step 3.3: Commit

```bash
git commit -am "test: single-nav deep update with inline new entity"
```

---

## Task 4: Remaining Test Matrix Coverage

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs`

### Tests still needed from spec matrix

**Deep insert:**
- `DeepInsert_BindDoesNotFireConventionMethods` — Verify bind references skip the CUD convention pipeline. This test project does not define `OnInsertingPublisher()`, so validate against an entity that does have an insert convention (`Book`). For example: create a Publisher with a key-only nested existing `Book` and assert the bound Book keeps its existing Id and no new Book is inserted, proving `OnInsertingBook()` did not run for a bind-only relationship change.

**Deep update:**
- `DeepUpdate_FiresConventionMethods` — Verify `OnUpdatingPublisher()` fires for a nested entity update. POST a Book with inline Publisher, then PATCH the Book with an inline Publisher update. Check that `Publisher.LastUpdated` changed (set by `OnUpdatingPublisher`).

### Step 4.1: Implement tests

### Step 4.2: Run full suite

```bash
dotnet test RESTier.slnx
```

### Step 4.3: Commit

```bash
git commit -am "test: complete deep operations spec test matrix coverage"
```

---

## Scope Explicitly NOT in Phase 3

These items were identified in reviews but are deferred beyond Phase 3:

1. **OData-Version 4.01 support**: Requires ASP.NET Core OData to fix EdmEntityObject deserialization with 4.01 headers. This is an upstream dependency.

2. **Real `@odata.bind` wire-format tests in DeepInsertTests**: The existing BatchTests already cover this. Adding separate `@odata.bind` tests requires either raw JSON payloads (C# anonymous objects can't have `@` in property names) or a test helper that supports custom OData annotations. Deferred as the functionality is already tested via BatchTests.

3. **`@id` / `@odata.id` wire-format tests**: These require OData-Version: 4.01 headers, which break EdmEntityObject deserialization. Cannot be tested until the upstream limitation is resolved.

4. **Principal-side 1:1 navigation deep update**: Requires querying via inverse FK on the related entity. Returns 501 currently.

5. **Nested delta payloads**: Returns 501. Requires delta deserialization support.
