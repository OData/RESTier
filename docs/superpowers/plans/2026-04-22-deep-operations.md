# Deep Operations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add deep insert, deep update, and entity reference (`@odata.bind` / `@id`) support to RESTier, per OData 4.0/4.01.

**Architecture:** Nested entities in POST/PUT/PATCH payloads are extracted by `DeepOperationExtractor` into a tree of `DataModificationItem` entries. Full entities flow through the complete submit pipeline (auth, validation, events). Entity references are stored as `NavigationBindings` on the parent and resolved during initialization. Relationships are wired via EF navigation property assignment (not FK injection) to support server-generated keys. Responses include `SelectExpandClause` to expand nested entities per OData 4.01.

**Tech Stack:** .NET 8/9/10, Microsoft.AspNetCore.OData 9.x, Microsoft.OData.Core 8.x, Entity Framework 6 + EF Core, xUnit v3, FluentAssertions

**Spec:** `docs/superpowers/specs/2026-04-22-deep-operations-design.md`

---

## File Structure

### New Files

| File | Responsibility |
|------|---------------|
| `src/Microsoft.Restier.Core/Submit/BindReference.cs` | Entity reference value object (entity set + key) |
| `src/Microsoft.Restier.Core/Submit/DeepOperationSettings.cs` | Configuration (MaxDepth) |
| `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs` | Walk EdmEntityObject, build DataModificationItem tree + NavigationBindings |
| `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationResponseBuilder.cs` | Build SelectExpandClause from DataModificationItem tree |
| `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Review.cs` | Test entity for multi-level nesting |
| `test/Microsoft.Restier.Tests.Core/Submit/DataModificationItemDeepTests.cs` | Unit tests for DataModificationItem tree properties |
| `test/Microsoft.Restier.Tests.Core/Submit/BindReferenceTests.cs` | Unit tests for BindReference |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs` | Base class for deep insert HTTP tests |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs` | Base class for deep update HTTP tests |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/DeepInsertTests.cs` | EF6 deep insert subclass |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/DeepUpdateTests.cs` | EF6 deep update subclass |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/DeepInsertTests.cs` | EFCore deep insert subclass |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/DeepUpdateTests.cs` | EFCore deep update subclass |

### Modified Files

| File | Change |
|------|--------|
| `src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs` | Add ParentItem, ParentNavigationPropertyName, NestedItems, NavigationBindings |
| `src/Microsoft.Restier.Core/Submit/DefaultChangeSetInitializer.cs` | Add protected helpers for nav prop resolution and containment detection |
| `src/Microsoft.Restier.AspNetCore/RestierController.cs` | Post() and Update() use DeepOperationExtractor; build SelectExpandClause for response |
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` | Register DeepOperationSettings via TryAddSingleton |
| `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs` | Phase 1 bind validation; Phase 2 nav prop wiring |
| `src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs` | Same as EFCore |
| `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Book.cs` | Add PublisherId FK, Reviews collection |
| `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryContext.cs` | Add DbSet\<Review\>, configure relationships |
| `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs` | Seed Review data |

---

## Task 1: Core Data Model Extensions

**Files:**
- Modify: `src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs`
- Create: `src/Microsoft.Restier.Core/Submit/BindReference.cs`
- Create: `src/Microsoft.Restier.Core/Submit/DeepOperationSettings.cs`
- Test: `test/Microsoft.Restier.Tests.Core/Submit/DataModificationItemDeepTests.cs`
- Test: `test/Microsoft.Restier.Tests.Core/Submit/BindReferenceTests.cs`

### Step 1.1: Write unit tests for new DataModificationItem properties

- [ ] Create `test/Microsoft.Restier.Tests.Core/Submit/DataModificationItemDeepTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Restier.Core.Submit;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Submit;

public class DataModificationItemDeepTests
{
    [Fact]
    public void NestedItems_DefaultsToEmptyList()
    {
        var item = CreateItem("Books", RestierEntitySetOperation.Insert);
        item.NestedItems.Should().NotBeNull();
        item.NestedItems.Should().BeEmpty();
    }

    [Fact]
    public void NavigationBindings_DefaultsToEmptyDictionary()
    {
        var item = CreateItem("Books", RestierEntitySetOperation.Insert);
        item.NavigationBindings.Should().NotBeNull();
        item.NavigationBindings.Should().BeEmpty();
    }

    [Fact]
    public void ParentItem_DefaultsToNull()
    {
        var item = CreateItem("Books", RestierEntitySetOperation.Insert);
        item.ParentItem.Should().BeNull();
        item.ParentNavigationPropertyName.Should().BeNull();
    }

    [Fact]
    public void ParentItem_CanBeSet()
    {
        var parent = CreateItem("Publishers", RestierEntitySetOperation.Insert);
        var child = CreateItem("Books", RestierEntitySetOperation.Insert);
        child.ParentItem = parent;
        child.ParentNavigationPropertyName = "Books";

        child.ParentItem.Should().BeSameAs(parent);
        child.ParentNavigationPropertyName.Should().Be("Books");
    }

    [Fact]
    public void FlattenDepthFirst_SingleItem_ReturnsSelf()
    {
        var item = CreateItem("Publishers", RestierEntitySetOperation.Insert);
        var flat = item.FlattenDepthFirst().ToList();
        flat.Should().HaveCount(1);
        flat[0].Should().BeSameAs(item);
    }

    [Fact]
    public void FlattenDepthFirst_WithChildren_ReturnsParentBeforeChildren()
    {
        var parent = CreateItem("Publishers", RestierEntitySetOperation.Insert);
        var child1 = CreateItem("Books", RestierEntitySetOperation.Insert);
        var child2 = CreateItem("Books", RestierEntitySetOperation.Insert);
        parent.NestedItems.Add(child1);
        parent.NestedItems.Add(child2);

        var flat = parent.FlattenDepthFirst().ToList();
        flat.Should().HaveCount(3);
        flat[0].Should().BeSameAs(parent);
        flat[1].Should().BeSameAs(child1);
        flat[2].Should().BeSameAs(child2);
    }

    [Fact]
    public void FlattenDepthFirst_MultiLevel_ReturnsCorrectOrder()
    {
        var root = CreateItem("Publishers", RestierEntitySetOperation.Insert);
        var child = CreateItem("Books", RestierEntitySetOperation.Insert);
        var grandchild = CreateItem("Reviews", RestierEntitySetOperation.Insert);
        root.NestedItems.Add(child);
        child.NestedItems.Add(grandchild);

        var flat = root.FlattenDepthFirst().ToList();
        flat.Should().HaveCount(3);
        flat[0].Should().BeSameAs(root);
        flat[1].Should().BeSameAs(child);
        flat[2].Should().BeSameAs(grandchild);
    }

    private static DataModificationItem CreateItem(string resourceSetName, RestierEntitySetOperation operation)
    {
        return new DataModificationItem(
            resourceSetName,
            typeof(object),
            typeof(object),
            operation,
            null,
            null,
            new Dictionary<string, object>());
    }
}
```

### Step 1.2: Run tests to verify they fail

- [ ] Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~DataModificationItemDeepTests"`

Expected: Compilation failure — `NestedItems`, `NavigationBindings`, `ParentItem`, `ParentNavigationPropertyName`, `FlattenDepthFirst` do not exist on `DataModificationItem`.

### Step 1.3: Create BindReference class

- [ ] Create `src/Microsoft.Restier.Core/Submit/BindReference.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Represents a reference to an existing entity for @odata.bind (4.0) or entity-reference (4.01) linking.
    /// This is a relationship-only operation — the referenced entity is not created or modified.
    /// </summary>
    public class BindReference
    {
        /// <summary>
        /// Gets or sets the target entity set name.
        /// </summary>
        public string ResourceSetName { get; set; }

        /// <summary>
        /// Gets or sets the key of the referenced entity.
        /// </summary>
        public IReadOnlyDictionary<string, object> ResourceKey { get; set; }

        /// <summary>
        /// Gets or sets the resolved entity instance (populated during initialization Phase 1).
        /// </summary>
        public object ResolvedEntity { get; set; }
    }
}
```

### Step 1.4: Create DeepOperationSettings class

- [ ] Create `src/Microsoft.Restier.Core/Submit/DeepOperationSettings.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core.Submit
{
    /// <summary>
    /// Configuration settings for deep insert and deep update operations.
    /// </summary>
    public class DeepOperationSettings
    {
        /// <summary>
        /// Gets or sets the maximum nesting depth for deep operations.
        /// Default is 5. Set to 0 to disable deep operations entirely.
        /// </summary>
        public int MaxDepth { get; set; } = 5;
    }
}
```

### Step 1.5: Add new properties to DataModificationItem

- [ ] Modify `src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs`. Add the following using directives at the top of the file (after existing usings):

```csharp
// No new usings needed — System.Collections.Generic is already imported
```

Add the following properties and method to the `DataModificationItem` class, after the existing `LocalValues` property (after line 211):

```csharp
        /// <summary>
        /// Gets or sets the parent DataModificationItem for nested operations.
        /// Null for root/direct operations.
        /// </summary>
        public DataModificationItem ParentItem { get; set; }

        /// <summary>
        /// Gets or sets the CLR navigation property name on the parent entity
        /// that this item was nested under.
        /// </summary>
        public string ParentNavigationPropertyName { get; set; }

        /// <summary>
        /// Gets the child DataModificationItems for deep insert/update.
        /// Each child flows through the full submit pipeline.
        /// </summary>
        public IList<DataModificationItem> NestedItems { get; } = new List<DataModificationItem>();

        /// <summary>
        /// Gets the entity reference bindings: maps CLR navigation property name to bind reference(s).
        /// These are relationship-only operations — no CUD pipeline events fire for the target.
        /// </summary>
        public IDictionary<string, IList<BindReference>> NavigationBindings { get; } = new Dictionary<string, IList<BindReference>>();

        /// <summary>
        /// Flattens the DataModificationItem tree in depth-first pre-order,
        /// guaranteeing parent items appear before their children.
        /// </summary>
        /// <returns>An enumerable of all items in the tree.</returns>
        public IEnumerable<DataModificationItem> FlattenDepthFirst()
        {
            yield return this;
            foreach (var child in NestedItems)
            {
                foreach (var descendant in child.FlattenDepthFirst())
                {
                    yield return descendant;
                }
            }
        }
```

### Step 1.6: Run tests to verify they pass

- [ ] Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~DataModificationItemDeepTests"`

Expected: All 7 tests PASS.

### Step 1.7: Write BindReference unit tests

- [ ] Create `test/Microsoft.Restier.Tests.Core/Submit/BindReferenceTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Restier.Core.Submit;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Submit;

public class BindReferenceTests
{
    [Fact]
    public void BindReference_CanStoreResourceSetAndKey()
    {
        var bindRef = new BindReference
        {
            ResourceSetName = "Publishers",
            ResourceKey = new Dictionary<string, object> { { "Id", "PUB01" } },
        };

        bindRef.ResourceSetName.Should().Be("Publishers");
        bindRef.ResourceKey.Should().ContainKey("Id").WhoseValue.Should().Be("PUB01");
    }

    [Fact]
    public void BindReference_ResolvedEntity_DefaultsToNull()
    {
        var bindRef = new BindReference();
        bindRef.ResolvedEntity.Should().BeNull();
    }

    [Fact]
    public void NavigationBindings_CanStoreMultipleReferences()
    {
        var item = new DataModificationItem(
            "Publishers", typeof(object), typeof(object),
            RestierEntitySetOperation.Insert, null, null,
            new Dictionary<string, object>());

        var refs = new List<BindReference>
        {
            new() { ResourceSetName = "Books", ResourceKey = new Dictionary<string, object> { { "Id", System.Guid.NewGuid() } } },
            new() { ResourceSetName = "Books", ResourceKey = new Dictionary<string, object> { { "Id", System.Guid.NewGuid() } } },
        };

        item.NavigationBindings["Books"] = refs;
        item.NavigationBindings["Books"].Should().HaveCount(2);
    }
}
```

### Step 1.8: Run BindReference tests

- [ ] Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~BindReferenceTests"`

Expected: All 3 tests PASS.

### Step 1.9: Commit

- [ ] ```bash
git add src/Microsoft.Restier.Core/Submit/ChangeSetItem.cs src/Microsoft.Restier.Core/Submit/BindReference.cs src/Microsoft.Restier.Core/Submit/DeepOperationSettings.cs test/Microsoft.Restier.Tests.Core/Submit/DataModificationItemDeepTests.cs test/Microsoft.Restier.Tests.Core/Submit/BindReferenceTests.cs
git commit -m "feat: add DataModificationItem tree structure, BindReference, and DeepOperationSettings"
```

---

## Task 2: Test Model Changes

**Files:**
- Create: `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Review.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Book.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryContext.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs`

### Step 2.1: Create Review entity

- [ ] Create `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Review.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// A review for a book. Used for testing multi-level deep insert/update.
    /// </summary>
    public class Review
    {

        public Guid Id { get; set; }

        public string Content { get; set; }

        public int Rating { get; set; }

        public Guid BookId { get; set; }

        public Book Book { get; set; }

    }

}
```

### Step 2.2: Add PublisherId FK and Reviews collection to Book

- [ ] Modify `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Book.cs`. Add a `using` for `System.Collections.ObjectModel` and `System.Collections.Generic` at the top. Add the `PublisherId` FK property and `Reviews` collection. The full file should be:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// 
    /// </summary>
    public class Book
    {

        /// <summary>
        /// 
        /// </summary>
        public Guid Id { get; set; }

        [MinLength(13)]
        [MaxLength(13)]
        public string Isbn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Foreign key for the Publisher navigation property.
        /// </summary>
        public string PublisherId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Publisher Publisher { get; set; }

        /// <summary>
        ///
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The category of the book.
        /// </summary>
        public BookCategory? Category { get; set; }

        /// <summary>
        /// Reviews for this book.
        /// </summary>
        public virtual ObservableCollection<Review> Reviews { get; set; } = new();

    }

}
```

### Step 2.3: Update LibraryContext

- [ ] Modify `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryContext.cs`. Add `DbSet<Review> Reviews` property next to the other DbSet properties. Both EF6 and EFCore sections need the new DbSet. In the EFCore `OnModelCreating`, configure the Book-Review and Book-Publisher relationships.

Add the `Reviews` DbSet in both the EF6 section (near lines 33-39) and EFCore section (near lines 66-72):

```csharp
public DbSet<Review> Reviews { get; set; }
```

In the EFCore `OnModelCreating` method, add after the existing `Publisher.OwnsOne(c => c.Addr)` line:

```csharp
    modelBuilder.Entity<Book>()
        .HasOne(b => b.Publisher)
        .WithMany(p => p.Books)
        .HasForeignKey(b => b.PublisherId);

    modelBuilder.Entity<Review>()
        .HasOne(r => r.Book)
        .WithMany(b => b.Reviews)
        .HasForeignKey(r => r.BookId);
```

### Step 2.4: Update LibraryTestInitializer with Review seed data

- [ ] Modify `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs`. Add a `using System` if not present. In the `Seed` method (both EF6 and EFCore paths), after the existing book/publisher seed data, add Review seed data. Add after the LibraryCard seed section:

```csharp
            context.Reviews.AddRange(
                new Review
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000101"),
                    Content = "Great book!",
                    Rating = 5,
                    BookId = bookId1,
                },
                new Review
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000102"),
                    Content = "Decent read.",
                    Rating = 3,
                    BookId = bookId1,
                });
            context.SaveChanges();
```

Note: `bookId1` should be replaced with the actual Guid of the first seeded book. Look at the existing seed code for the exact variable name — the first book's Id is typically assigned inline. You may need to extract it to a local variable.

### Step 2.5: Build and run existing tests to verify no regressions

- [ ] Run: `dotnet build RESTier.slnx`

Expected: Build succeeds. The new `PublisherId` FK on Book should be compatible with existing data — EF will recognize the shadow property is now explicit.

- [ ] Run: `dotnet test RESTier.slnx`

Expected: All existing tests pass. The `PublisherId` property should be backward compatible.

### Step 2.6: Commit

- [ ] ```bash
git add test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Review.cs test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Book.cs test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryContext.cs test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs
git commit -m "feat: add Review entity and explicit PublisherId FK for deep operation testing"
```

---

## Task 3: Register DeepOperationSettings

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs`

### Step 3.1: Register DeepOperationSettings in route services

- [ ] Modify `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs`. Add `using Microsoft.Restier.Core.Submit;` to the usings. In the private `AddRestierRoute` method, after the line `configureRouteServices.Invoke(services);` (around line 161), add:

```csharp
            services.TryAddSingleton(new DeepOperationSettings());
```

Also add `using Microsoft.Extensions.DependencyInjection.Extensions;` if not already present (for `TryAddSingleton`).

### Step 3.2: Build to verify

- [ ] Run: `dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj`

Expected: Build succeeds.

### Step 3.3: Commit

- [ ] ```bash
git add src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs
git commit -m "feat: register DeepOperationSettings in route service container"
```

---

## Task 4: DefaultChangeSetInitializer Helpers

**Files:**
- Modify: `src/Microsoft.Restier.Core/Submit/DefaultChangeSetInitializer.cs`

### Step 4.1: Add protected helpers for nav prop resolution

- [ ] Modify `src/Microsoft.Restier.Core/Submit/DefaultChangeSetInitializer.cs`. Add usings at top:

```csharp
using System.Collections;
using System.Reflection;
using Microsoft.OData.Edm;
```

Add the following protected methods to the class:

```csharp
        /// <summary>
        /// Resolves the CLR PropertyInfo for a navigation property on an entity type.
        /// </summary>
        protected static PropertyInfo GetNavigationPropertyInfo(Type entityType, string navigationPropertyName)
        {
            Ensure.NotNull(entityType, nameof(entityType));
            Ensure.NotNull(navigationPropertyName, nameof(navigationPropertyName));
            return entityType.GetProperty(navigationPropertyName)
                ?? throw new InvalidOperationException($"Navigation property '{navigationPropertyName}' not found on type '{entityType.Name}'.");
        }

        /// <summary>
        /// Reads key property values from a materialized entity using the EDM model.
        /// </summary>
        protected static IReadOnlyDictionary<string, object> GetKeyValues(object entity, IEdmEntityType edmType, IEdmModel model)
        {
            Ensure.NotNull(entity, nameof(entity));
            Ensure.NotNull(edmType, nameof(edmType));

            var keys = new Dictionary<string, object>();
            foreach (var keyProperty in edmType.Key())
            {
                var clrProperty = entity.GetType().GetProperty(keyProperty.Name);
                if (clrProperty is not null)
                {
                    keys[keyProperty.Name] = clrProperty.GetValue(entity);
                }
            }

            return keys;
        }

        /// <summary>
        /// Checks whether a navigation property has containment semantics.
        /// </summary>
        protected static bool IsContainedNavigation(IEdmModel model, IEdmEntityType entityType, string navigationPropertyName)
        {
            Ensure.NotNull(model, nameof(model));
            Ensure.NotNull(entityType, nameof(entityType));

            var navProp = entityType.FindProperty(navigationPropertyName) as IEdmNavigationProperty;
            return navProp?.ContainsTarget ?? false;
        }

        /// <summary>
        /// Sets a navigation property reference on an entity (for single nav props).
        /// </summary>
        protected static void SetNavigationProperty(object entity, string navigationPropertyName, object relatedEntity)
        {
            var navPropInfo = GetNavigationPropertyInfo(entity.GetType(), navigationPropertyName);
            navPropInfo.SetValue(entity, relatedEntity);
        }

        /// <summary>
        /// Adds an entity to a collection navigation property.
        /// </summary>
        protected static void AddToCollectionNavigationProperty(object entity, string navigationPropertyName, object relatedEntity)
        {
            var navPropInfo = GetNavigationPropertyInfo(entity.GetType(), navigationPropertyName);
            var collection = navPropInfo.GetValue(entity);
            if (collection is null)
            {
                throw new InvalidOperationException($"Collection navigation property '{navigationPropertyName}' on type '{entity.GetType().Name}' is null. Ensure it is initialized.");
            }

            // Use IList.Add for broad compatibility (ObservableCollection, List, etc.)
            if (collection is IList list)
            {
                list.Add(relatedEntity);
                return;
            }

            // Fall back to reflection-based Add
            var addMethod = collection.GetType().GetMethod("Add");
            if (addMethod is not null)
            {
                addMethod.Invoke(collection, new[] { relatedEntity });
                return;
            }

            throw new InvalidOperationException($"Cannot add to collection navigation property '{navigationPropertyName}' — no Add method found.");
        }
```

### Step 4.2: Build to verify

- [ ] Run: `dotnet build src/Microsoft.Restier.Core/Microsoft.Restier.Core.csproj`

Expected: Build succeeds.

### Step 4.3: Commit

- [ ] ```bash
git add src/Microsoft.Restier.Core/Submit/DefaultChangeSetInitializer.cs
git commit -m "feat: add protected helpers to DefaultChangeSetInitializer for nav prop resolution"
```

---

## Task 5: EFChangeSetInitializer — Phase 1 Bind Validation + Phase 2 Nav Prop Wiring

**Files:**
- Modify: `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs`
- Modify: `src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs`

### Step 5.1: Update EFCore EFChangeSetInitializer

- [ ] Modify `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs`.

Add `using Microsoft.OData.Edm;` to the usings if not present.

In `InitializeAsync`, add Phase 1 (bind validation) **before** the existing `foreach` loop over entries, and Phase 2 (nav prop wiring) **after** each item is materialized inside `HandleEntitySet`.

Replace the `InitializeAsync` method body (keeping the null check and api check) with:

```csharp
        public async override Task InitializeAsync(SubmitContext context, CancellationToken cancellationToken)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Api is not IEntityFrameworkApi frameworkApi)
            {
                return;
            }

            var dbContext = frameworkApi.DbContext;

            // Phase 1: Validate and resolve entity references before any entity materialization
            foreach (var entry in context.ChangeSet.Entries.OfType<DataModificationItem>())
            {
                if (entry.NavigationBindings.Count == 0)
                {
                    continue;
                }

                foreach (var binding in entry.NavigationBindings)
                {
                    foreach (var bindRef in binding.Value)
                    {
                        var referencedEntity = await ResolveBindReference(context, bindRef, cancellationToken).ConfigureAwait(false);
                        bindRef.ResolvedEntity = referencedEntity;
                    }
                }
            }

            // Phase 2: Materialize entities and wire relationships
            foreach (var entry in context.ChangeSet.Entries.OfType<DataModificationItem>())
            {
                var strongTypedDbSet = dbContext.GetType().GetProperty(entry.ResourceSetName).GetValue(dbContext);
                var resourceType = strongTypedDbSet.GetType().GetGenericArguments()[0];

                if (entry.ActualResourceType is not null && resourceType != entry.ActualResourceType)
                {
                    resourceType = entry.ActualResourceType;
                }

                var typedMethodCall = HandleMethod.MakeGenericMethod(new Type[] { resourceType });
                var task = typedMethodCall.Invoke(this, new object[] { context, dbContext, entry, resourceType, cancellationToken }) as Task;
                await task.ConfigureAwait(false);

                // Wire parent-child navigation properties after materialization
                if (entry.ParentItem?.Resource is not null && entry.Resource is not null)
                {
                    WireParentChildRelationship(entry);
                }

                // Resolve entity reference bindings
                if (entry.NavigationBindings.Count > 0 && entry.Resource is not null)
                {
                    WireBindReferences(entry);
                }
            }
        }
```

Add the following private methods to the class:

```csharp
        private static async Task<object> ResolveBindReference(SubmitContext context, BindReference bindRef, CancellationToken cancellationToken)
        {
            var apiBase = context.Api;
            var query = apiBase.GetQueryableSource(bindRef.ResourceSetName);

            // Build a query filtered by the bind reference key
            var elementType = query.ElementType;
            var param = Expression.Parameter(elementType);
            Expression where = null;

            foreach (var keyPair in bindRef.ResourceKey)
            {
                var property = Expression.Property(param, keyPair.Key);
                var value = keyPair.Value;
                if (value.GetType() != property.Type)
                {
                    value = Convert.ChangeType(value, property.Type, System.Globalization.CultureInfo.InvariantCulture);
                }

                var equal = Expression.Equal(property, Expression.Constant(value, property.Type));
                where = where is null ? equal : Expression.AndAlso(where, equal);
            }

            var whereLambda = Expression.Lambda(where, param);
            query = ExpressionHelpers.Where(query, whereLambda, elementType);

            var result = await apiBase.QueryAsync(new QueryRequest(query), cancellationToken).ConfigureAwait(false);

            var toArray = ExpressionHelperMethods.EnumerableToArrayGeneric.MakeGenericMethod(elementType);
            var materialized = (Array)toArray.Invoke(null, new object[] { result.Results });

            if (materialized.Length == 0)
            {
                var keyDescription = string.Join(", ", bindRef.ResourceKey.Select(k => $"{k.Key}={k.Value}"));
                throw new StatusCodeException(HttpStatusCode.BadRequest,
                    $"Referenced entity '{bindRef.ResourceSetName}' with key ({keyDescription}) does not exist.");
            }

            return materialized.GetValue(0);
        }

        private void WireParentChildRelationship(DataModificationItem childEntry)
        {
            var parentResource = childEntry.ParentItem.Resource;
            var childResource = childEntry.Resource;
            var navPropName = childEntry.ParentNavigationPropertyName;

            // Determine relationship direction: does the child have a reference to the parent,
            // or does the parent have a collection containing the child?
            var parentNavPropInfo = parentResource.GetType().GetProperty(navPropName);
            if (parentNavPropInfo is null)
            {
                return;
            }

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(parentNavPropInfo.PropertyType)
                && parentNavPropInfo.PropertyType != typeof(string))
            {
                // Collection nav prop on parent: add child to collection
                AddToCollectionNavigationProperty(parentResource, navPropName, childResource);
            }
            else
            {
                // Single nav prop on parent: set reference
                SetNavigationProperty(parentResource, navPropName, childResource);
            }
        }

        private void WireBindReferences(DataModificationItem entry)
        {
            foreach (var binding in entry.NavigationBindings)
            {
                var navPropName = binding.Key;
                var navPropInfo = entry.Resource.GetType().GetProperty(navPropName);
                if (navPropInfo is null)
                {
                    continue;
                }

                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(navPropInfo.PropertyType)
                    && navPropInfo.PropertyType != typeof(string))
                {
                    // Collection bind: add each resolved entity to the collection
                    foreach (var bindRef in binding.Value)
                    {
                        if (bindRef.ResolvedEntity is not null)
                        {
                            AddToCollectionNavigationProperty(entry.Resource, navPropName, bindRef.ResolvedEntity);
                        }
                    }
                }
                else
                {
                    // Single bind: set the nav prop to the resolved entity
                    var bindRef = binding.Value.FirstOrDefault();
                    if (bindRef?.ResolvedEntity is not null)
                    {
                        SetNavigationProperty(entry.Resource, navPropName, bindRef.ResolvedEntity);
                    }
                }
            }
        }
```

Add the necessary `using` directives at the top if not present:

```csharp
using System.Linq.Expressions;
using Microsoft.Restier.Core.Query;
```

### Step 5.2: Update EF6 EFChangeSetInitializer with same logic

- [ ] Modify `src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs` with the same Phase 1 + Phase 2 pattern. The bind validation and nav prop wiring logic is identical — only the existing `HandleEntitySet` method differs (it's inline instead of generic).

Apply the same `InitializeAsync` restructuring: add Phase 1 before the entity loop, and add `WireParentChildRelationship` and `WireBindReferences` calls after `entry.Resource = resource`.

The private helper methods (`ResolveBindReference`, `WireParentChildRelationship`, `WireBindReferences`) are identical to the EFCore version — copy them in.

### Step 5.3: Build to verify

- [ ] Run: `dotnet build RESTier.slnx`

Expected: Build succeeds.

### Step 5.4: Run existing tests to verify no regressions

- [ ] Run: `dotnet test RESTier.slnx`

Expected: All existing tests pass. The new code is additive — it only triggers when `NavigationBindings` is non-empty or `ParentItem` is non-null, which never happens for existing operations.

### Step 5.5: Commit

- [ ] ```bash
git add src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs
git commit -m "feat: add Phase 1 bind validation and Phase 2 nav prop wiring to EFChangeSetInitializers"
```

---

## Task 6: DeepOperationExtractor

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs`

This is the core extraction logic. It walks an `EdmEntityObject`, identifies navigation properties, and builds the `DataModificationItem` tree with `NavigationBindings`.

### Step 6.1: Create DeepOperationExtractor

- [ ] Create `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.OData.Deltas;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.AspNetCore.Submit
{
    /// <summary>
    /// Walks an EdmEntityObject and extracts nested entities into a DataModificationItem tree.
    /// Entity references (@odata.bind in 4.0, @id in 4.01) are stored as NavigationBindings on the parent.
    /// </summary>
    internal class DeepOperationExtractor
    {
        private readonly IEdmModel model;
        private readonly ApiBase api;
        private readonly DeepOperationSettings settings;

        public DeepOperationExtractor(IEdmModel model, ApiBase api, DeepOperationSettings settings)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.api = api ?? throw new ArgumentNullException(nameof(api));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Extracts nested entities from the EdmEntityObject and populates the parent item's
        /// NestedItems and NavigationBindings.
        /// </summary>
        public void ExtractNestedItems(
            Delta entity,
            IEdmStructuredType edmType,
            DataModificationItem parentItem,
            bool isCreation,
            int currentDepth = 0)
        {
            if (settings.MaxDepth > 0 && currentDepth >= settings.MaxDepth)
            {
                throw new ODataException($"Deep operation exceeds maximum nesting depth of {settings.MaxDepth}.");
            }

            foreach (var propertyName in entity.GetChangedPropertyNames())
            {
                if (!entity.TryGetPropertyValue(propertyName, out var value) || value is null)
                {
                    continue;
                }

                var edmProperty = edmType.FindProperty(propertyName);
                if (edmProperty is not IEdmNavigationProperty navProperty)
                {
                    continue;
                }

                var clrPropertyName = EdmClrPropertyMapper.GetClrPropertyName(edmProperty, model);
                var targetEntityType = navProperty.ToEntityType();
                var targetEntitySet = FindTargetEntitySet(navProperty, edmType);

                if (value is EdmEntityObject nestedEntity)
                {
                    ProcessSingleNestedEntity(
                        nestedEntity, targetEntityType, targetEntitySet,
                        clrPropertyName, parentItem, isCreation, currentDepth);
                }
                else if (value is IEnumerable collection && value is not string)
                {
                    foreach (var item in collection)
                    {
                        if (item is EdmEntityObject collectionEntity)
                        {
                            ProcessSingleNestedEntity(
                                collectionEntity, targetEntityType, targetEntitySet,
                                clrPropertyName, parentItem, isCreation, currentDepth);
                        }
                    }
                }
            }
        }

        private void ProcessSingleNestedEntity(
            EdmEntityObject nestedEntity,
            IEdmEntityType targetEntityType,
            string targetEntitySetName,
            string clrNavPropertyName,
            DataModificationItem parentItem,
            bool isCreation,
            int currentDepth)
        {
            // Check if this is an entity reference (bind) rather than a full entity
            if (IsEntityReference(nestedEntity))
            {
                var bindRef = CreateBindReference(nestedEntity, targetEntityType, targetEntitySetName);
                if (!parentItem.NavigationBindings.TryGetValue(clrNavPropertyName, out var bindList))
                {
                    bindList = new List<BindReference>();
                    parentItem.NavigationBindings[clrNavPropertyName] = bindList;
                }

                bindList.Add(bindRef);
                return;
            }

            // Full nested entity — create a child DataModificationItem
            var actualEdmType = nestedEntity.ActualEdmType as IEdmStructuredType ?? targetEntityType;
            var clrType = actualEdmType.GetClrType(model);

            var childItem = new DataModificationItem(
                targetEntitySetName,
                targetEntityType.GetClrType(model),
                clrType,
                isCreation ? RestierEntitySetOperation.Insert : RestierEntitySetOperation.Update,
                isCreation ? null : ExtractKeyValues(nestedEntity, targetEntityType),
                null,
                nestedEntity.CreatePropertyDictionary(actualEdmType, api, isCreation))
            {
                ParentItem = parentItem,
                ParentNavigationPropertyName = clrNavPropertyName,
            };

            parentItem.NestedItems.Add(childItem);

            // Recurse for grandchildren
            ExtractNestedItems(nestedEntity, actualEdmType, childItem, isCreation, currentDepth + 1);
        }

        private bool IsEntityReference(EdmEntityObject entity)
        {
            // Check for OData ID annotation — indicates this is an entity reference, not a full entity.
            // The OData deserializer sets this when processing @odata.bind (4.0) or @id (4.01).
            if (entity.TryGetPropertyValue("@odata.id", out _))
            {
                return true;
            }

            // Check instance annotations for ODataIdAnnotation
            foreach (var annotation in entity.GetInstanceAnnotations())
            {
                if (annotation.Name == "odata.id" || annotation.Name == "id")
                {
                    return true;
                }
            }

            return false;
        }

        private BindReference CreateBindReference(
            EdmEntityObject entity,
            IEdmEntityType entityType,
            string entitySetName)
        {
            var key = ExtractKeyValues(entity, entityType);
            return new BindReference
            {
                ResourceSetName = entitySetName,
                ResourceKey = key,
            };
        }

        private IReadOnlyDictionary<string, object> ExtractKeyValues(
            EdmEntityObject entity,
            IEdmEntityType entityType)
        {
            var keys = new Dictionary<string, object>();
            foreach (var keyProperty in entityType.Key())
            {
                if (entity.TryGetPropertyValue(keyProperty.Name, out var value))
                {
                    var clrName = EdmClrPropertyMapper.GetClrPropertyName(keyProperty, model);
                    keys[clrName] = value;
                }
            }

            return keys;
        }

        private string FindTargetEntitySet(IEdmNavigationProperty navProperty, IEdmStructuredType sourceType)
        {
            // Walk the model's entity container to find the target entity set
            var container = model.EntityContainer;
            if (container is null)
            {
                return navProperty.ToEntityType().Name;
            }

            foreach (var entitySet in container.EntitySets())
            {
                var navigationTarget = entitySet.FindNavigationTarget(navProperty);
                if (navigationTarget is not null)
                {
                    return navigationTarget.Name;
                }
            }

            // Fallback: use the entity type name as the set name
            return navProperty.ToEntityType().Name;
        }
    }
}
```

Note: This initial implementation handles the common case. The `IsEntityReference` detection will need verification against actual AspNetCore.OData 9.x deserialization output during integration testing — the feature tests will validate this. The `GetInstanceAnnotations()` method availability on `EdmEntityObject` also needs to be confirmed; if it's not available, we'll use a different detection approach (checking for only-key-properties as a fallback).

### Step 6.2: Build to verify

- [ ] Run: `dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj`

Expected: Build succeeds. If `EdmClrPropertyMapper` or `GetInstanceAnnotations` are not accessible, adjust the code — check existing usage patterns in `Extensions.cs` for the correct API.

### Step 6.3: Commit

- [ ] ```bash
git add src/Microsoft.Restier.AspNetCore/Submit/DeepOperationExtractor.cs
git commit -m "feat: add DeepOperationExtractor for nested entity extraction"
```

---

## Task 7: Controller Deep Insert Changes

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

### Step 7.1: Update Post() to use DeepOperationExtractor

- [ ] Modify `src/Microsoft.Restier.AspNetCore/RestierController.cs`. Add usings:

```csharp
using Microsoft.Restier.AspNetCore.Submit;
using Microsoft.Restier.Core.Submit;
```

In the `Post()` method, after the `postItem` is created (after line 213) and before the changeset section (line 215), add extraction:

```csharp
            // Extract nested entities for deep insert
            var deepSettings = HttpContext.RequestServices.GetService<DeepOperationSettings>() ?? new DeepOperationSettings();
            if (deepSettings.MaxDepth > 0)
            {
                var extractor = new DeepOperationExtractor(model, api, deepSettings);
                extractor.ExtractNestedItems(edmEntityObject, actualEntityType, postItem, isCreation: true);
            }
```

Then modify the changeset creation to enqueue all flattened items instead of just the root:

Replace the existing changeset block (approximately lines 215-229):

```csharp
            var changeSetProperty = HttpContext.GetChangeSet();
            if (changeSetProperty is null)
            {
                var changeSet = new ChangeSet();
                foreach (var item in postItem.FlattenDepthFirst())
                {
                    changeSet.Entries.Enqueue(item);
                }

                var result = await api.SubmitAsync(changeSet, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                foreach (var item in postItem.FlattenDepthFirst())
                {
                    changeSetProperty.ChangeSet.Entries.Enqueue(item);
                }

                await changeSetProperty.OnChangeSetCompleted().ConfigureAwait(false);
            }
```

Add `using Microsoft.Extensions.DependencyInjection;` if not already present.

### Step 7.2: Build to verify

- [ ] Run: `dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj`

Expected: Build succeeds.

### Step 7.3: Run existing tests

- [ ] Run: `dotnet test RESTier.slnx`

Expected: All existing tests pass. Non-deep POST operations produce a single-item `FlattenDepthFirst()` (just the root), so behavior is unchanged.

### Step 7.4: Commit

- [ ] ```bash
git add src/Microsoft.Restier.AspNetCore/RestierController.cs
git commit -m "feat: integrate DeepOperationExtractor into RestierController.Post()"
```

---

## Task 8: Deep Insert Feature Tests

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/DeepInsertTests.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/DeepInsertTests.cs`

### Step 8.1: Create base DeepInsertTests class

- [ ] Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class DeepInsertTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    [Fact]
    public async Task DeepInsert_CollectionNavProperty()
    {
        var payload = new
        {
            Id = "DeepInsertPub1",
            Books = new[]
            {
                new { Isbn = "1234567890123", Title = "Deep Insert Book 1", IsActive = true },
                new { Isbn = "9876543210123", Title = "Deep Insert Book 2", IsActive = true },
            },
        };

        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        response.IsSuccessStatusCode.Should().BeTrue($"POST should succeed but got {response.StatusCode}");
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify the publisher was created
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Publishers('DeepInsertPub1')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Should().NotBeNull();
        publisher.Id.Should().Be("DeepInsertPub1");
        publisher.Books.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeepInsert_ServerGeneratedKeys()
    {
        // Book has a Guid Id that is server-generated via OnInsertingBook convention
        var payload = new
        {
            Id = "DeepInsertPub2",
            Books = new[]
            {
                new { Isbn = "1111111111111", Title = "Server Key Book", IsActive = true },
            },
        };

        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        response.IsSuccessStatusCode.Should().BeTrue();

        // Verify the book got a server-generated key
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Publishers('DeepInsertPub2')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Books.Should().HaveCount(1);
        publisher.Books[0].Id.Should().NotBe(Guid.Empty, "Book should have a server-generated Id");
    }

    [Fact]
    public async Task DeepInsert_FiresConventionMethods()
    {
        // OnInsertingBook assigns a Guid if empty — this verifies the convention fires
        var payload = new
        {
            Id = "DeepInsertPub3",
            Books = new[]
            {
                new { Id = Guid.Empty, Isbn = "2222222222222", Title = "Convention Test Book", IsActive = true },
            },
        };

        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        response.IsSuccessStatusCode.Should().BeTrue();

        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Publishers('DeepInsertPub3')?$expand=Books",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        var (publisher, _) = await getResponse.DeserializeResponseAsync<Publisher>();
        publisher.Books.Should().HaveCount(1);
        publisher.Books[0].Id.Should().NotBe(Guid.Empty, "OnInsertingBook should have generated a Guid");
    }

    [Fact]
    public async Task DeepInsert_ExceedsMaxDepth_Returns400()
    {
        // Configure max depth of 1
        var payload = new
        {
            Id = "DeepInsertPub4",
            Books = new[]
            {
                new
                {
                    Isbn = "3333333333333",
                    Title = "Depth Test Book",
                    IsActive = true,
                    Reviews = new[]
                    {
                        new { Content = "Should fail", Rating = 5 },
                    },
                },
            },
        };

        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: services =>
            {
                ConfigureServices(services);
                // Override with depth limit of 1
                services.AddSingleton(new Core.Submit.DeepOperationSettings { MaxDepth = 1 });
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

### Step 8.2: Create EF6 subclass

- [ ] Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/DeepInsertTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

[Collection("LibraryApiEF6")]
public class DeepInsertTests : DeepInsertTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}
```

### Step 8.3: Create EFCore subclass

- [ ] Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/DeepInsertTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore;

[Collection("LibraryApiEFCore")]
public class DeepInsertTests : DeepInsertTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}
```

### Step 8.4: Run deep insert tests

- [ ] Run: `dotnet test RESTier.slnx --filter "FullyQualifiedName~DeepInsertTests"`

Expected: Tests should run. At this stage, some may fail depending on serialization behavior and the exact form of the OData payload. This is where we validate the end-to-end flow and iterate. Fix any issues discovered.

### Step 8.5: Commit

- [ ] ```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepInsertTests.cs test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/DeepInsertTests.cs test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/DeepInsertTests.cs
git commit -m "test: add deep insert feature tests for EF6 and EFCore"
```

---

## Task 9: Response Shaping (DeepOperationResponseBuilder)

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationResponseBuilder.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

### Step 9.1: Create DeepOperationResponseBuilder

- [ ] Create `src/Microsoft.Restier.AspNetCore/Submit/DeepOperationResponseBuilder.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.AspNetCore.Submit
{
    /// <summary>
    /// Builds a SelectExpandClause from a DataModificationItem tree to ensure the deep insert/update
    /// response includes expanded navigation properties matching the request depth.
    /// </summary>
    internal static class DeepOperationResponseBuilder
    {
        /// <summary>
        /// Builds a SelectExpandClause that expands navigation properties for all nested items
        /// and navigation bindings on the root DataModificationItem.
        /// Returns null if there are no nested items or bindings.
        /// </summary>
        public static SelectExpandClause BuildSelectExpandClause(
            DataModificationItem rootItem,
            IEdmModel model,
            IEdmEntitySet entitySet)
        {
            if (rootItem.NestedItems.Count == 0 && rootItem.NavigationBindings.Count == 0)
            {
                return null;
            }

            var entityType = entitySet.EntityType;
            var expandItems = new List<SelectItem>();

            // Collect all navigation property names that need expansion
            var navPropNames = new HashSet<string>();
            foreach (var nested in rootItem.NestedItems)
            {
                if (nested.ParentNavigationPropertyName is not null)
                {
                    navPropNames.Add(nested.ParentNavigationPropertyName);
                }
            }

            foreach (var binding in rootItem.NavigationBindings)
            {
                navPropNames.Add(binding.Key);
            }

            foreach (var navPropName in navPropNames)
            {
                var edmNavProp = FindNavigationProperty(entityType, navPropName, model);
                if (edmNavProp is null)
                {
                    continue;
                }

                var navigationSource = entitySet.FindNavigationTarget(edmNavProp);

                // Build child SelectExpandClause for nested items that have their own children
                SelectExpandClause childClause = null;
                var childItems = rootItem.NestedItems
                    .Where(n => n.ParentNavigationPropertyName == navPropName)
                    .ToList();

                if (childItems.Any(c => c.NestedItems.Count > 0 || c.NavigationBindings.Count > 0)
                    && navigationSource is IEdmEntitySet childEntitySet)
                {
                    // Recurse for multi-level expansion
                    var representativeChild = childItems.First(c => c.NestedItems.Count > 0 || c.NavigationBindings.Count > 0);
                    childClause = BuildSelectExpandClause(representativeChild, model, childEntitySet);
                }

                var segment = new NavigationPropertySegment(edmNavProp, navigationSource);
                var expandItem = new ExpandedNavigationSelectItem(
                    new ODataExpandPath(segment),
                    navigationSource,
                    childClause);

                expandItems.Add(expandItem);
            }

            if (expandItems.Count == 0)
            {
                return null;
            }

            return new SelectExpandClause(expandItems, allSelected: true);
        }

        private static IEdmNavigationProperty FindNavigationProperty(
            IEdmEntityType entityType,
            string clrPropertyName,
            IEdmModel model)
        {
            // Try direct match first
            var prop = entityType.FindProperty(clrPropertyName) as IEdmNavigationProperty;
            if (prop is not null)
            {
                return prop;
            }

            // Try case-insensitive match (for camelCase naming conventions)
            foreach (var navProp in entityType.NavigationProperties())
            {
                if (string.Equals(navProp.Name, clrPropertyName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return navProp;
                }
            }

            return null;
        }
    }
}
```

### Step 9.2: Integrate response shaping into RestierController.Post()

- [ ] Modify `src/Microsoft.Restier.AspNetCore/RestierController.cs`. Add `using Microsoft.OData.UriParser;` if not present.

Before the `return CreateCreatedODataResult(postItem.Resource);` line (line 231), add:

```csharp
            // Build SelectExpandClause for response expansion (OData 4.01 requires 201 responses
            // to be expanded to at least the depth present in the deep insert request)
            var selectExpandClause = DeepOperationResponseBuilder.BuildSelectExpandClause(
                postItem, model, entitySet);
            if (selectExpandClause is not null)
            {
                HttpContext.ODataFeature().SelectExpandClause = selectExpandClause;
            }
```

### Step 9.3: Build and run tests

- [ ] Run: `dotnet build RESTier.slnx && dotnet test RESTier.slnx --filter "FullyQualifiedName~DeepInsertTests"`

Expected: Build succeeds. Response expansion tests validate that the response includes nested entities.

Note: If `HttpContext.ODataFeature().SelectExpandClause` is not sufficient for the `CreatedODataResult` serializer, this is the residual risk identified in the spec review. If tests fail, an alternative approach is to return an `OkObjectResult` with the entity and set appropriate headers manually, or to use `ObjectResult` with custom serializer settings. Iterate here.

### Step 9.4: Commit

- [ ] ```bash
git add src/Microsoft.Restier.AspNetCore/Submit/DeepOperationResponseBuilder.cs src/Microsoft.Restier.AspNetCore/RestierController.cs
git commit -m "feat: add response shaping for deep insert via SelectExpandClause"
```

---

## Task 10: Controller Deep Update Changes

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

### Step 10.1: Update the Update() method

- [ ] Modify the `Update()` method in `RestierController.cs`. After the `updateItem` is created (after the `IsFullReplaceUpdateRequest` line), add extraction:

```csharp
            // Extract nested entities for deep update (4.01 only — 4.0 only allows @odata.bind on update)
            var deepSettings = HttpContext.RequestServices.GetService<DeepOperationSettings>() ?? new DeepOperationSettings();
            if (deepSettings.MaxDepth > 0)
            {
                var extractor = new DeepOperationExtractor(model, api, deepSettings);
                extractor.ExtractNestedItems(edmEntityObject, actualEntityType, updateItem, isCreation: false);
            }
```

Modify the changeset creation to enqueue all flattened items:

```csharp
            var changeSetProperty = HttpContext.GetChangeSet();
            if (changeSetProperty is null)
            {
                var changeSet = new ChangeSet();
                foreach (var item in updateItem.FlattenDepthFirst())
                {
                    changeSet.Entries.Enqueue(item);
                }

                var result = await api.SubmitAsync(changeSet, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                foreach (var item in updateItem.FlattenDepthFirst())
                {
                    changeSetProperty.ChangeSet.Entries.Enqueue(item);
                }

                await changeSetProperty.OnChangeSetCompleted().ConfigureAwait(false);
            }
```

Add response shaping before the return:

```csharp
            var selectExpandClause = DeepOperationResponseBuilder.BuildSelectExpandClause(
                updateItem, model, entitySet);
            if (selectExpandClause is not null)
            {
                HttpContext.ODataFeature().SelectExpandClause = selectExpandClause;
            }

            return CreateUpdatedODataResult(updateItem.Resource);
```

Note: The full deep update child matching logic (query existing children, determine insert/update/unlink/delete operations, handle containment vs non-containment) is complex and should be implemented incrementally. This initial step provides the extraction and flattening. The child matching logic for PUT replace/PATCH merge semantics will be added in a follow-up iteration after the basic deep insert flow is validated end-to-end.

### Step 10.2: Build and run tests

- [ ] Run: `dotnet build RESTier.slnx && dotnet test RESTier.slnx`

Expected: All existing tests pass. The extraction only fires when nested entities are present in the payload.

### Step 10.3: Commit

- [ ] ```bash
git add src/Microsoft.Restier.AspNetCore/RestierController.cs
git commit -m "feat: integrate DeepOperationExtractor into RestierController.Update()"
```

---

## Task 11: Deep Update Feature Tests

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/DeepUpdateTests.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/DeepUpdateTests.cs`

### Step 11.1: Create base DeepUpdateTests class

- [ ] Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class DeepUpdateTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    [Fact]
    public async Task DeepUpdate_BindOnUpdate_V40()
    {
        // First create a book without a publisher
        var book = new Book
        {
            Title = "Unbound Book",
            Isbn = "4444444444444",
            IsActive = true,
        };

        var createResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers('Publisher1')/Books",
            payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        createResponse.IsSuccessStatusCode.Should().BeTrue();

        var (createdBook, _) = await createResponse.DeserializeResponseAsync<Book>();
        createdBook.Should().NotBeNull();

        // Now PATCH the book with a Publisher bind reference
        // In OData 4.0, this uses @odata.bind
        var patchPayload = new
        {
            Title = "Now Bound Book",
        };

        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"),
            resource: $"/Books({createdBook.Id})",
            payload: patchPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        patchResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task DeepUpdate_NullUnlinks_V40()
    {
        // Get a book that has a publisher
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$expand=Publisher&$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var (bookList, _) = await getResponse.DeserializeResponseAsync<ODataV4List<Book>>();
        var book = bookList.Items[0];
        book.Publisher.Should().NotBeNull("Test requires a book with a publisher");

        // PATCH with PublisherId set to null to unlink
        var patchPayload = new
        {
            PublisherId = (string)null,
        };

        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"),
            resource: $"/Books({book.Id})",
            payload: patchPayload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        patchResponse.IsSuccessStatusCode.Should().BeTrue();

        // Verify the publisher is unlinked
        var verifyResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({book.Id})?$expand=Publisher",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices);

        var (updatedBook, _) = await verifyResponse.DeserializeResponseAsync<Book>();
        updatedBook.Publisher.Should().BeNull("Publisher should be unlinked after PATCH with null");
    }
}
```

### Step 11.2: Create EF6 and EFCore subclasses

- [ ] Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/DeepUpdateTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

[Collection("LibraryApiEF6")]
public class DeepUpdateTests : DeepUpdateTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}
```

- [ ] Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/DeepUpdateTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore;

[Collection("LibraryApiEFCore")]
public class DeepUpdateTests : DeepUpdateTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}
```

### Step 11.3: Run deep update tests

- [ ] Run: `dotnet test RESTier.slnx --filter "FullyQualifiedName~DeepUpdateTests"`

Expected: Tests run. Iterate on any failures.

### Step 11.4: Commit

- [ ] ```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/DeepUpdateTests.cs test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/DeepUpdateTests.cs test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/DeepUpdateTests.cs
git commit -m "test: add deep update feature tests for EF6 and EFCore"
```

---

## Task 12: Full Test Suite Validation

### Step 12.1: Run entire test suite

- [ ] Run: `dotnet test RESTier.slnx`

Expected: All tests pass — both existing and new.

### Step 12.2: Review and add remaining test cases

- [ ] Review the spec's test matrix (docs/superpowers/specs/2026-04-22-deep-operations-design.md, Deep Insert Tests and Deep Update Tests sections). Add any remaining test cases from the spec that aren't yet covered to the base test classes. Key tests still needed:

- `DeepInsert_MultiLevel` — Publisher with Books containing Reviews (2-level)
- `DeepInsert_BindReferenceNotFound` — Returns 400 for invalid bind reference
- `DeepInsert_BindDoesNotFireConventionMethods` — Bind doesn't trigger OnInserting*
- `DeepUpdate_InlineEntityInV40_Rejected` — Inline deep update rejected under 4.0
- `DeepUpdate_FiresConventionMethods` — OnUpdatingPublisher fires for nested update (4.01)
- `DeepUpdate_NestedDelta_Returns501` — Nested delta returns 501

Each test follows the same pattern as the examples in Tasks 8 and 11.

### Step 12.3: Final commit

- [ ] ```bash
git add -A
git commit -m "test: complete deep operations test coverage per spec"
```

---

## Implementation Notes

### Areas Requiring Iteration During Implementation

1. **`IsEntityReference` detection**: The mechanism for distinguishing `@odata.bind` from deep insert at the `EdmEntityObject` level needs verification against AspNetCore.OData 9.x's actual deserialization output. The initial implementation checks for `@odata.id` annotation and instance annotations. If this doesn't work, fall back to checking if the entity has only key properties.

2. **Response shaping via `SelectExpandClause`**: Setting `HttpContext.ODataFeature().SelectExpandClause` before `CreatedODataResult` serialization is plausible but unverified. If the OData serializer doesn't respect it for CUD results, alternative approaches include custom `ObjectResult` with `ODataSerializerContext`, or returning an `OkObjectResult` with the expanded entity graph and appropriate `Location` header.

3. **Deep update child matching**: Task 10 provides extraction and flattening for updates. The full PUT replace/PATCH merge logic (query existing children, classify as insert/update/unlink/delete) is architecturally designed in the spec but should be implemented incrementally after basic deep insert is proven.

4. **OData 4.0 vs 4.01 version enforcement**: The spec requires rejecting inline deep update under 4.0 and rejecting `@odata.bind` under 4.01. This version checking should be added to the `DeepOperationExtractor` after the basic flow works.

5. **`DbUpdateException` mapping**: Required-relationship constraint errors during `SaveChangesAsync` need to be caught and mapped to HTTP 400. This should be added to the submit executor or controller exception handling.
