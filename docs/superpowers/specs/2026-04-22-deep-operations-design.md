# Deep Operations Design: Deep Insert, Deep Update, and Entity References

**Date**: 2026-04-22
**Issue**: [OData/RESTier#646](https://github.com/OData/RESTier/issues/646)
**Status**: Draft (rev 4)

## Overview

RESTier currently silently ignores navigation properties in POST/PUT/PATCH payloads (`Extensions.cs:122-127`). This design adds support for:

- **Deep insert** (OData 4.0 section 11.4.2.2): Creating related entities inline during POST
- **Deep update** (OData 4.01 section 11.4.3.1): Updating related entities inline during PATCH/PUT
- **Entity references** (OData 4.0 `@odata.bind`, OData 4.01 `@id`/`@odata.id`): Linking to existing entities

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Convention pipeline | Full pipeline for nested **entity** operations only | Preserves RESTier's interception contract; bind/link operations are relationship-only and don't fire entity CUD events |
| Entity references | Modeled as relationship changes on the parent, not as entity CUD items | Bind/link operations link, replace, or add relationships — they don't create or update the referenced entity |
| Relationship wiring | Navigation property object assignment, not FK scalar injection | Works with server-generated keys; EF change tracker infers FKs from nav prop assignments |
| Bind validation | During initialization, before entity materialization | Fails atomically before any entity changes are tracked |
| Nesting depth | Configurable, default 5 | Recursive implementation with safety guard |
| Non-delta collection on PATCH/PUT | Represents the complete relationship set | Per OData 4.01; nested delta payloads are out of scope for initial implementation |
| PUT omitted children | Unlink (non-contained) or delete (contained) | OData 4.01 says omitted entities are unlinked; only containment nav props imply deletion |
| OData version compatibility | Support both `@odata.bind` (4.0) and entity-reference objects (4.01) | Check OData-Version header to select parsing strategy |
| Deep insert response | 201 with response expanded to match request depth | OData 4.01 requires response expanded to at least the level present in the request |

## Architecture

### Approach: Flatten Nested Entities + Parent-Local Binds

The design distinguishes two kinds of nested navigation property values:

1. **Deep entities** (inline entity payloads) — extracted into separate `DataModificationItem` entries that flow through the full submit pipeline (authorization, validation, convention events). Relationships are wired via EF navigation property assignment after materialization.

2. **Entity references** (`@odata.bind` in 4.0, entity-reference objects in 4.01) — stored as `NavigationBindings` metadata on the parent `DataModificationItem`. Resolved during initialization as relationship changes on the parent entity. No CUD pipeline events fire for the referenced entity (it is not being created or updated).

```
HTTP POST /Publishers
{
  "Id": "PUB01",
  "Books": [
    { "Title": "New Book", "Isbn": "1234567890123" }
  ],
  "Books@odata.bind": [ "Books(00000000-0000-0000-0000-000000000001)" ]
}

  Extraction
  ──────────
  Root: Insert Publisher (PUB01)
    ├─ NestedItem: Insert Book ("New Book")     → DataModificationItem in ChangeSet
    └─ NavigationBinding: Books → bind to existing Book(guid)  → parent-local, no CUD item

  ChangeSet Queue                     Bind Resolution (during init)
  ────────────────                    ──────────────────────────────
  1. Insert Publisher (PUB01)         After #1 materialized:
  2. Insert Book (ParentItem → #1)     - Load existing Book(guid)
                                       - Set existingBook.Publisher = publisherEntity
  After #1 and #2 materialized:        (FK inferred by EF change tracker)
    bookEntity.Publisher = publisherEntity
    (FK inferred by EF change tracker)
```

### Why navigation property assignment instead of FK injection?

For deep insert, the parent entity may have a server-generated key (identity column, database sequence). During `InitializeAsync`, the parent is tracked by EF but `SaveChangesAsync` hasn't run yet — the generated key value is not available. By assigning the navigation property object reference (`child.Publisher = parentEntity`), EF's change tracker handles FK propagation internally, including temporary key resolution. This works reliably regardless of key generation strategy.

For entity references (`@odata.bind`), the referenced entity already exists and has a known key. FK assignment would work, but navigation property assignment is used for consistency and because it also updates EF's relationship tracking.

## Component Changes

### 1. Core Data Model (`Microsoft.Restier.Core`)

#### `DataModificationItem` — new properties

```csharp
/// The parent DataModificationItem (null for root/direct operations).
public DataModificationItem ParentItem { get; set; }

/// The CLR navigation property name on the parent entity that this item was nested under.
public string ParentNavigationPropertyName { get; set; }

/// Child DataModificationItems for deep insert/update (full entity operations).
/// Each child flows through the full submit pipeline.
public IList<DataModificationItem> NestedItems { get; }

/// Entity reference bindings: maps CLR navigation property name to bind reference(s).
/// These are relationship-only operations — no CUD pipeline events fire for the target.
public IDictionary<string, IList<BindReference>> NavigationBindings { get; }
```

Note: `IsBindOperation` is removed. Entity references are not modeled as `DataModificationItem` entries.

#### `BindReference` — new class

```csharp
/// Represents a reference to an existing entity for @odata.bind or entity-reference linking.
public class BindReference
{
    /// The target entity set name.
    public string ResourceSetName { get; set; }

    /// The key of the referenced entity.
    public IReadOnlyDictionary<string, object> ResourceKey { get; set; }
}
```

#### `DeepOperationSettings` — new configuration class

```csharp
public class DeepOperationSettings
{
    /// Maximum nesting depth. Default: 5. Set to 0 to disable deep operations.
    public int MaxDepth { get; set; } = 5;
}
```

#### `DefaultChangeSetInitializer` — new protected helpers

Add protected helper methods for relationship wiring that both EF6 and EFCore initializers can call:

- `GetNavigationPropertyInfo(Type entityType, string navigationPropertyName)` — resolves the CLR `PropertyInfo` for a navigation property
- `GetKeyValues(object entity, IEdmEntityType edmType, IEdmModel model)` — reads key property values from a materialized entity via reflection
- `GetContainsTarget(IEdmModel model, IEdmEntityType entityType, string navigationPropertyName)` — checks whether a navigation property has containment semantics

These are provider-agnostic (EDM model and reflection only).

### 2. Nested Entity Extraction (`Microsoft.Restier.AspNetCore`)

#### New class: `DeepOperationExtractor`

Responsible for walking an `EdmEntityObject` and building a tree of `DataModificationItem` entries plus `NavigationBindings`.

**Input**: Root `EdmEntityObject`, `IEdmStructuredType`, `IEdmModel`, `ApiBase`, `DeepOperationSettings`, operation type (insert/update), `isFullReplaceUpdate` flag, and `ODataVersion` (from request header).

**Process**:
1. Call `CreatePropertyDictionary` for scalar/complex properties (existing behavior)
2. Walk changed properties, identify navigation properties via EDM type
3. For each navigation property value:
   - **Entity reference** (`@odata.bind` in 4.0 or entity-reference object with `@id`/`@odata.id` in 4.01): Parse entity set and key, add to parent's `NavigationBindings`. No child `DataModificationItem` is created.
   - **Full nested entity** (deep insert/update): Recursively extract, create child `DataModificationItem` with `ParentItem` set, add to parent's `NestedItems`
   - **Collection**: Process each item individually (may be a mix of entity references and full entities)
4. Track current depth, throw `ODataException` (HTTP 400) if `MaxDepth` is exceeded

**Detecting entity references vs deep entities**:
- **OData 4.0** (`OData-Version: 4.0`): Entity references use `@odata.bind` annotation. AspNetCore.OData's deserializer handles these distinctly from inline resources — the extractor checks whether the nested info wrapper contains `ODataEntityReferenceLink` items vs. full `ODataResource` items.
- **OData 4.01** (`OData-Version: 4.01`): Entity references are inline objects with only `@id` or `@odata.id`. The extractor detects these by checking for the `ODataIdAnnotation` on the `EdmEntityObject` instance annotations.
- **Fallback**: If detection is ambiguous, treat an `EdmEntityObject` that contains only key properties (and no non-key properties) as a potential entity reference, and verify by checking if the entity exists.

#### `Extensions.cs` — `CreatePropertyDictionary` changes

The existing method continues to build `LocalValues` for scalar and complex properties only. The `EdmEntityObject` skip (`continue` on line 126) remains — navigation properties are handled separately by `DeepOperationExtractor`, not mixed into `LocalValues`.

### 3. Controller Changes (`Microsoft.Restier.AspNetCore`)

#### `RestierController.Post()`

After creating the root `DataModificationItem`:
1. Call `DeepOperationExtractor.Extract()` to build the nested item tree and populate `NavigationBindings`
2. Flatten nested entity items (depth-first pre-order, guaranteeing parent before children) into an ordered list
3. Enqueue all entity items into the `ChangeSet` — bindings travel as metadata on the parent item

```csharp
var postItem = new DataModificationItem(...);
var extractor = new DeepOperationExtractor(model, api, deepOperationSettings, odataVersion);
extractor.ExtractNestedItems(edmEntityObject, actualEntityType, postItem, isCreation: true);

var changeSet = new ChangeSet();
foreach (var item in postItem.FlattenDepthFirst())
{
    changeSet.Entries.Enqueue(item);
}
var result = await api.SubmitAsync(changeSet, cancellationToken);
```

Batch support: when `HttpContext.GetChangeSet()` is non-null, items are enqueued into the shared batch changeset in the same order.

#### `RestierController.Update()`

Same extraction, plus deep update logic for determining entity operations:

**Non-delta collection navigation properties** (both PATCH and PUT):

Per OData 4.01, a non-delta nested collection represents the **complete relationship set** for that navigation property. The controller:
1. Queries existing children via `api.QueryAsync()`
2. Matches payload items to existing children by key
3. Creates `Insert` items for new entities, `Update` items for matched entities
4. For entities in the existing set but **not** in the payload:
   - **Non-contained nav prop** (`ContainsTarget = false`): Remove the relationship by clearing the navigation property reference (e.g., remove child from parent's collection, or set child's reference nav prop to null). EF resolves this to the appropriate underlying action: nulling an FK, removing from a join table, or updating a dependent entity. If the relationship is required (non-nullable FK, no cascade), EF will throw a constraint violation during `SaveChangesAsync` in the submit executor. The controller's exception mapping (or a new `DbUpdateException` handler) translates this to HTTP 400 with a descriptive error indicating which relationship could not be removed.
   - **Contained nav prop** (`ContainsTarget = true`): Delete the omitted child entity (creating a `Delete` item).

**Nested delta payloads**: Out of scope for initial implementation. If a nested delta is detected, the server returns 501 Not Implemented.

**Single navigation properties on update**:

| Payload | Action |
|---------|--------|
| Full nested entity with matching key | `Update` the related entity (child DataModificationItem) |
| Full nested entity with new/no key | `Insert` new entity (child DataModificationItem); unlink previous if FK is nullable |
| Entity reference (`@odata.bind` / `@id`) | Add to parent's `NavigationBindings`; resolved during initialization |
| `null` | Remove relationship (set nav prop to null; EF resolves to FK nulling, constraint error, etc.). |
| Absent from payload | No action (PATCH leaves it alone); PUT treats as null |

**Ordering in ChangeSet for deep update**:
1. Root update item
2. Child inserts
3. Child updates
4. Child relationship removals (nav prop clearing for non-contained omitted children)
5. Child deletes (for contained omitted children — last, to avoid FK issues)

### 4. Deep Operation Response Shaping (`Microsoft.Restier.AspNetCore`)

OData 4.01 requires that if a deep insert succeeds with 201 Created, the response body must contain the created entity expanded to at least the depth present in the request. For example, a POST of a Publisher with inline Books must return the Publisher with Books expanded.

#### `RestierController.Post()` — response changes

After the submit completes, the controller builds a `SelectExpandClause` that mirrors the navigation properties present in the deep insert request, then sets it on the `ODataFeature` so the OData serializer includes the expansions in the `CreatedODataResult` response.

```csharp
// After submit succeeds:
// 1. Build SelectExpandClause matching the nested nav props from the request
var selectExpandClause = DeepOperationResponseBuilder.BuildSelectExpandClause(
    postItem, model, entitySet);

// 2. Set it on the OData feature so the serializer picks it up
if (selectExpandClause is not null)
{
    HttpContext.ODataFeature().SelectExpandClause = selectExpandClause;
}

return CreateCreatedODataResult(postItem.Resource);
```

Since we use navigation property assignment during initialization, EF's change tracker has already loaded the related entities in memory on the root entity's navigation properties (via relationship fixup). The serializer can traverse them without additional queries.

#### New helper: `DeepOperationResponseBuilder`

Static helper in `Microsoft.Restier.AspNetCore` that builds a `SelectExpandClause` from a `DataModificationItem` tree:
- For each `NestedItems` entry on the root item, add an `ExpandedNavigationSelectItem` for that navigation property
- Recurse for grandchildren to match multi-level deep inserts
- For `NavigationBindings`, also add expand items (the bound entity should appear in the response)

#### `RestierController.Update()` — response changes

Same approach for deep update responses: build a `SelectExpandClause` and set it on the OData feature before returning `CreateUpdatedODataResult`.

### 5. ChangeSet Initialization (`EntityFramework` / `EntityFrameworkCore`)

Both `EFChangeSetInitializer` implementations process items sequentially from the ChangeSet queue (existing behavior). The additions are a two-phase extension to initialization:

#### Phase 1: Validate and resolve entity references (before entity materialization)

For each `DataModificationItem` that has `NavigationBindings`:
1. For each `BindReference`, query the target entity set by key
2. If the referenced entity does not exist, throw `StatusCodeException(400)` with a descriptive message (e.g., "Referenced entity 'Publishers' with key 'PUB01' does not exist")
3. Store the loaded entity on the `BindReference` for use in Phase 2

This runs before any entities are materialized or tracked, ensuring atomic failure on invalid references. No partial entity changes are applied to the DbContext.

#### Phase 2: Materialize entities and wire relationships

Process items sequentially (existing behavior). After materializing each item:

**For nested entity items** (`entry.ParentItem != null`):
1. The parent entity is already materialized (parent was enqueued first)
2. Set the navigation property on the child or parent entity to establish the relationship:
   - If child has a reference nav prop to parent (e.g., `Book.Publisher`): set `childEntity.Publisher = parentEntity.Resource`
   - If parent has a collection nav prop (e.g., `Publisher.Books`): add `childEntity` to `parentEntity.Books`
3. EF's change tracker infers the FK value from the nav prop assignment — works with server-generated keys

**For entity reference bindings** (`entry.NavigationBindings` is non-empty):
After the current item is materialized, process its bindings:
- **Single nav prop bind** (e.g., `Publisher@odata.bind` on a Book): Set `bookEntity.Publisher = loadedPublisher` (loaded in Phase 1)
- **Collection nav prop bind** (e.g., `Books@odata.bind` on a Publisher): Set `loadedBook.Publisher = publisherEntity` (or add to collection nav prop)

**Provider-specific differences** (why these are not in the shared project):
- EFCore: `dbContext.Entry(resource)` returns `EntityEntry`; navigation set via `EntityEntry.Reference(navProp).CurrentValue` or direct property assignment
- EF6: `dbContext.Entry(resource)` returns `DbEntityEntry`; navigation set via `DbEntityEntry.Reference(navProp).CurrentValue` or direct property assignment

Both rely on EF's change tracker for FK inference — the initializer never directly sets FK scalar values for deep operations.

### 7. DI Registration

#### `RestierODataOptionsExtensions`

Register `DeepOperationSettings` in the route services container. RESTier's configuration uses `ODataOptions.AddRestierRoute<TApi>(Action<IServiceCollection> configureRouteServices, ...)` — there is no `RestierApiBuilder`. `DeepOperationSettings` is registered as a singleton in the route service collection, accessible to both the controller and the initializer.

Default registration (inside `AddRestierRoute`):
```csharp
services.TryAddSingleton(new DeepOperationSettings());
```

User override via the `configureRouteServices` action:
```csharp
options.AddRestierRoute<MyApi>(restierServices =>
{
    restierServices.AddSingleton(new DeepOperationSettings { MaxDepth = 3 });
    restierServices.AddEFCoreProviderServices<MyContext>(...);
});
```

`TryAddSingleton` ensures the default is used only if the user hasn't registered their own.

## Test Strategy

### Test Model Changes

**New entity**: `Review` in `Tests.Shared/Scenarios/Library/`

```csharp
public class Review
{
    public Guid Id { get; set; }
    public string Content { get; set; }
    public int Rating { get; set; }
    public Guid BookId { get; set; }
    public Book Book { get; set; }
}
```

**Modified entity**: `Book` — add explicit FK and Reviews collection:

```csharp
// Add to Book.cs:
public string PublisherId { get; set; }
public virtual ObservableCollection<Review> Reviews { get; set; }
```

**Modified context**: `LibraryContext` — add `DbSet<Review> Reviews`.

**Seed data**: Add sample Reviews in `LibraryTestInitializer`.

No migrations needed — the test database is recreated via `EnsureDeleted()` + `EnsureCreated()`.

### Unit Tests

| Test Class | Project | Covers |
|-----------|---------|--------|
| `DeepOperationExtractorTests` | `Tests.AspNetCore` | Nested entity extraction from EdmEntityObject, entity reference parsing (4.0 and 4.01), depth limit enforcement, collection vs single nav prop, mixed bind+entity collections |
| `DataModificationItemTests` | `Tests.Core` | New properties (ParentItem, NestedItems, NavigationBindings), tree flattening/ordering |
| `BindReferenceTests` | `Tests.Core` | BindReference key parsing, entity set resolution |
| `DeepOperationSettingsTests` | `Tests.Core` | Configuration defaults and validation |
| `EFChangeSetInitializerTests` | `Tests.EntityFramework` + `Tests.AspNetCore` | Nav prop assignment after parent materialization, bind resolution and validation, server-generated key propagation |

### Feature Tests (HTTP Integration)

New base classes `DeepInsertTests<TApi, TContext>` and `DeepUpdateTests<TApi, TContext>` in `Tests.AspNetCore/FeatureTests/`, with EF6 and EFCore subclasses.

#### Deep Insert Tests

| Test | OData-Version | Scenario |
|------|---------------|----------|
| `DeepInsert_SingleNavProperty` | 4.0 | POST Publisher with inline single Book |
| `DeepInsert_CollectionNavProperty` | 4.0 | POST Publisher with inline Books array |
| `DeepInsert_WithBindReference_V40` | 4.0 | POST Book with `Publisher@odata.bind` (OData-Version: 4.0 header) |
| `DeepInsert_WithEntityReference_V401` | 4.01 | POST Book with inline Publisher entity-reference (`@id`) (OData-Version: 4.01 header) |
| `DeepInsert_CollectionWithBind_V40` | 4.0 | POST Publisher with `Books@odata.bind` array (OData-Version: 4.0) |
| `DeepInsert_CollectionWithEntityRef_V401` | 4.01 | POST Publisher with inline Book entity-references (`@id`) (OData-Version: 4.01) |
| `DeepInsert_BindInV401Request_Rejected` | 4.01 | POST with `@odata.bind` under OData-Version: 4.01 — returns 400 (clients must not use @odata.bind in 4.01) |
| `DeepInsert_MixedBindAndCreate_V40` | 4.0 | POST Publisher with some inline Books and some `@odata.bind` (OData-Version: 4.0) |
| `DeepInsert_MixedRefAndCreate_V401` | 4.01 | POST Publisher with some inline Books and some entity-references (OData-Version: 4.01) |
| `DeepInsert_MultiLevel` | 4.0 | POST Publisher with Books containing Reviews (2-level) |
| `DeepInsert_ServerGeneratedKeys` | 4.0 | POST with inline entities where parent has server-generated key (Guid) — verifies FK propagation works |
| `DeepInsert_ExceedsMaxDepth` | 4.0 | Returns 400 when nesting exceeds configured limit |
| `DeepInsert_BindReferenceNotFound` | 4.0 | Returns 400 when entity reference points to non-existent entity — verifies no partial changes applied |
| `DeepInsert_FiresConventionMethods` | 4.0 | Verifies `OnInsertingBook()` fires for nested Book |
| `DeepInsert_BindDoesNotFireConventionMethods` | 4.0 | Verifies `OnInsertingPublisher()` does NOT fire when Publisher is only bound via `@odata.bind` |
| `DeepInsert_ResponseIncludesExpandedEntities` | 4.0 | 201 response includes expanded navigation properties matching request depth |
| `DeepInsert_ResponseIncludesMultiLevelExpand` | 4.0 | 201 response for multi-level deep insert includes nested expansions |

#### Deep Update Tests

| Test | OData-Version | Scenario |
|------|---------------|----------|
| `DeepUpdate_NonDeltaCollection_ReplacesRelationships` | 4.01 | PATCH/PUT Publisher with full Books array — represents complete relationship set |
| `DeepUpdate_Put_OmittedChildrenUnlinked` | 4.01 | PUT Publisher with subset of Books — omitted non-contained children are unlinked (relationship removed; EF resolves to FK nulling or constraint error) |
| `DeepUpdate_Put_ContainedChildrenDeleted` | 4.01 | PUT with contained nav prop — omitted children are deleted (requires containment model) |
| `DeepUpdate_Put_RequiredRelationship_Returns400` | 4.01 | PUT that would remove a required relationship on omitted child — returns 400 |
| `DeepUpdate_SingleNavProperty_V401` | 4.01 | PATCH Book with inline Publisher change (inline deep update is 4.01 only) |
| `DeepUpdate_InlineEntityInV40_Rejected` | 4.0 | PATCH with inline nested entity under OData-Version: 4.0 — returns 400 (4.0 only allows @odata.bind on update) |
| `DeepUpdate_BindOnUpdate_V40` | 4.0 | PATCH Book with `Publisher@odata.bind` (OData-Version: 4.0) |
| `DeepUpdate_EntityRefOnUpdate_V401` | 4.01 | PATCH Book with Publisher entity-reference (`@id`) (OData-Version: 4.01) |
| `DeepUpdate_NullUnlinks_V40` | 4.0 | PATCH Book with `Publisher@odata.bind: null` to remove relationship (4.0 uses bind annotation, not inline null) |
| `DeepUpdate_NullUnlinks_V401` | 4.01 | PATCH Book with `Publisher: null` to remove relationship (4.01 inline) |
| `DeepUpdate_FiresConventionMethods` | 4.01 | Verifies `OnUpdatingPublisher()` fires for nested entity update (inline deep update is 4.01 only) |
| `DeepUpdate_NestedDelta_Returns501` | 4.01 | Returns 501 when nested delta payload is detected (out of scope) |
| `DeepUpdate_ResponseIncludesExpandedEntities` | 4.01 | Updated response includes expanded navigation properties matching request depth |

All feature tests run on both EF6 and EFCore via the generic base class pattern. Tests that specify a particular OData-Version send it via the request header.

## Files Changed

### New Files

| File | Description |
|------|-------------|
| `src/Microsoft.Restier.Core/Submit/DeepOperationSettings.cs` | Configuration class |
| `src/Microsoft.Restier.Core/Submit/BindReference.cs` | Entity reference value object |
| `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs` | Nested entity extraction and entity reference parsing |
| `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationResponseBuilder.cs` | Builds SelectExpandClause from DataModificationItem tree for response expansion |
| `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Review.cs` | Test entity |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs` | Deep insert feature tests |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs` | Deep update feature tests |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/DeepInsertTests.cs` | EF6 subclass |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/DeepUpdateTests.cs` | EF6 subclass |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/DeepInsertTests.cs` | EFCore subclass |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/DeepUpdateTests.cs` | EFCore subclass |
| `test/Microsoft.Restier.Tests.AspNetCore/DeepOperationExtractorTests.cs` | Unit tests |
| `test/Microsoft.Restier.Tests.Core/Submit/DataModificationItemTests.cs` | Unit tests |
| `test/Microsoft.Restier.Tests.Core/Submit/BindReferenceTests.cs` | Unit tests |

### Modified Files

| File | Change |
|------|--------|
| `src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs` | Add ParentItem, ParentNavigationPropertyName, NestedItems, NavigationBindings to DataModificationItem |
| `src/Microsoft.Restier.Core/Submit/DefaultChangeSetInitializer.cs` | Add protected helpers for nav prop resolution, key extraction, containment detection |
| `src/Microsoft.Restier.AspNetCore/RestierController.cs` | Post() and Update() use DeepOperationExtractor; flatten nested entity tree into ChangeSet; deep update child matching with relationship removal/delete distinction; build SelectExpandClause for response expansion |
| `src/Microsoft.Restier.AspNetCore/Extensions/Extensions.cs` | No functional change — EdmEntityObject skip remains; extraction handled by DeepOperationExtractor |
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` | Register DeepOperationSettings via TryAddSingleton in route service container |
| `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs` | Phase 1: validate+resolve entity references before materialization; Phase 2: nav prop assignment after materialization for both nested entities and binds |
| `src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs` | Same two-phase extension as EFCore |
| `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Book.cs` | Add PublisherId FK, Reviews collection |
| `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Publisher.cs` | No change (already has Books collection) |
| `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryContext.cs` | Add DbSet\<Review\>, configure Review relationship |
| `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs` | Seed Review data |

### Removed from Original Design

| Item | Reason |
|------|--------|
| `IsBindOperation` on DataModificationItem | Entity references are not CUD operations; modeled as `NavigationBindings` on parent instead |
| `BindReferenceValidator` (separate validator class) | Bind validation moved to Phase 1 of initialization — runs before entity materialization for atomic failure |
| Registration in `ServiceCollectionExtensions` for validator | No longer needed; validation is part of initializer |

## Known Limitations

- **OData-Version: 4.01 header**: ASP.NET Core OData 9.x's untyped deserialization (`EdmEntityObject`) fails when the `OData-Version: 4.01` header is sent — the request body parameter arrives as null, producing HTTP 400. This is an upstream limitation, not a RESTier issue. As a result:
  - Version enforcement (rejecting inline deep update under 4.0, rejecting `@odata.bind` under 4.01) is not implemented — the framework rejects 4.01 requests before the controller.
  - All entity reference formats (`@odata.bind`, `@id`, `@odata.id`) work identically when no version header is sent (default 4.0 behavior). The OData deserializer resolves all formats into key-only `EdmEntityObject` instances.
  - Deep insert and entity references work correctly under default/4.0 semantics.

## Out of Scope

- **Nested delta payloads**: OData 4.01 delta representation for collections (add/remove/update semantics). Returns 501 if detected. May be added in a future iteration.
- **Cross-changeset deep operations**: Deep operations that span multiple changesets in a batch request.
- **Many-to-many skip navigations**: Relationships via join tables without an explicit join entity. These require EF-specific skip navigation support and are deferred.
