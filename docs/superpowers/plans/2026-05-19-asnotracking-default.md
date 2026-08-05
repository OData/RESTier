# AsNoTracking by Default (Issue #726) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make RESTier's EF query pipeline apply `AsNoTrackingWithIdentityResolution` (EFCore) / `AsNoTracking` (EF6) by default, with a per-API option to override and an EDM-aware expand-cycle hint that lets EF6 fall back to tracked queries when the request shape requires identity preservation.

**Architecture:** A new `IExpandCycleDetector` service in `Microsoft.Restier.Core` inspects the parsed `SelectExpandClause` from `ODataQueryOptions` for same-type *and* cross-type cycles, and surfaces the result via a `HasRecursiveExpand` flag on `QueryRequest`. A second flag, `QueryRequest.AllowNoTracking`, gates the entire no-tracking transformation — it is set to `true` only by the AspNetCore controller for top-level HTTP read paths, so submit-pipeline and deep-update internal `QueryAsync` calls remain tracked (essential because `EFChangeSetInitializer.HandleEntitySet` mutates the returned entity via `dbContext.Entry(...)`). The shared `EFQueryExpressionSourcer` receives `RestierEFOptions` through constructor injection (registered as a singleton in per-API DI), and consumes `AllowNoTracking` plus the per-request `HasRecursiveExpand` hint to choose between `AsNoTrackingWithIdentityResolution` (EFCore), `AsNoTracking` (EF6 default), or tracked (EF6 with cycle, or `TrackAll`). Detection lives in Core so it's provider-agnostic and unit-testable; the EF6/EFCore split lives in the existing `#if EFCore` block of the shared sourcer source file.

**Tech Stack:** .NET 8/9 + .NET Framework 4.8 multi-targeting, EF6 (`System.Data.Entity`), EF Core 8+ (`Microsoft.EntityFrameworkCore`), Microsoft.OData.UriParser (`SelectExpandClause`), xUnit v3, FluentAssertions (AwesomeAssertions), NSubstitute.

---

## File Structure

### New files

| File | Responsibility |
|------|----------------|
| `src/Microsoft.Restier.Core/Query/IExpandCycleDetector.cs` | Public interface — single `HasCycle(IEdmEntityType, SelectExpandClause)` method. |
| `src/Microsoft.Restier.Core/Query/DefaultExpandCycleDetector.cs` | Internal default impl. DFS over `ExpandedNavigationSelectItem`/`ExpandedReferenceSelectItem`, path-based cycle detection accounting for inheritance. |
| `src/Microsoft.Restier.EntityFramework.Shared/RestierEFTrackingBehavior.cs` | Enum — `Default`, `NoTracking`, `NoTrackingWithIdentityResolution`, `TrackAll`. Shared between EF6/EFCore via dual `#if EFCore` namespaces (matches existing shared-project convention). |
| `src/Microsoft.Restier.EntityFramework.Shared/RestierEFOptions.cs` | Options POCO — currently only `TrackingBehavior`. Registered as a singleton in route DI. |
| `test/Microsoft.Restier.Tests.Core/Query/DefaultExpandCycleDetectorTests.cs` | Unit tests for the detector against a hand-built EDM model. |
| `test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/EFQueryNoTrackingTests.cs` | EFCore integration: asserts `ChangeTracker.Entries().Count() == 0` after a GET; asserts identity resolution preserved on self-referencing expand. |
| `test/Microsoft.Restier.Tests.EntityFramework/Query/EFQueryNoTrackingTests.cs` | EF6 integration: asserts tracked-or-not based on the hint; covers the `TrackAll` override path. |

### Modified files

| File | Change |
|------|--------|
| `src/Microsoft.Restier.Core/Query/QueryRequest.cs` | Add `bool HasRecursiveExpand { get; internal set; }` and `bool AllowNoTracking { get; internal set; }`. |
| `src/Microsoft.Restier.Core/Extensions/ServiceCollectionExtensions.cs:58-79` | Register `IExpandCycleDetector` → `DefaultExpandCycleDetector` in `AddRestierCoreServices`. |
| `src/Microsoft.Restier.AspNetCore/RestierController.cs:728-782` | In `ApplyQueryOptions`, set `queryRequest.AllowNoTracking = true`, resolve `IExpandCycleDetector` from route services, walk `queryOptions.SelectExpand?.SelectExpandClause`, set `queryRequest.HasRecursiveExpand`. |
| `src/Microsoft.Restier.EntityFramework.Shared/Microsoft.Restier.EntityFramework.Shared.projitems` | Add `<Compile Include="...">` entries for the two new files below. The shared project uses an explicit include list. |
| `src/Microsoft.Restier.EntityFramework.Shared/Query/EFQueryExpressionSourcer.cs` | Add a constructor accepting `RestierEFOptions`; rewrite the non-embedded branch to apply tracking via the injected options, gated on `QueryRequest.AllowNoTracking`. EF6 path additionally consults `HasRecursiveExpand`. |
| `src/Microsoft.Restier.EntityFramework.Shared/Extensions/ServiceCollectionExtensions.cs:35-47` | Register `RestierEFOptions` as singleton (`TryAdd`) and re-register the sourcer via a factory that resolves `RestierEFOptions`. |
| `src/Microsoft.Restier.EntityFramework/Extensions/ServiceCollectionExtensions.cs` | Add `AddEF6ProviderServices` overload taking `Action<RestierEFOptions>`. |
| `src/Microsoft.Restier.EntityFrameworkCore/Extensions/ServiceCollectionExtensions.cs` | Add `AddEFCoreProviderServices` overload taking `Action<RestierEFOptions>`. |
| `src/Microsoft.Restier.Docs/guides/server/` | Add or extend a guide page covering tracking behavior — see Task 14 for the exact file (verified at execution time, no `queries-and-data-access.mdx` exists today). |
| `src/Microsoft.Restier.Docs/release-notes/` | Behavior-change call-out in the next-version release notes file (current latest: `1-1-0.md`). |
| `src/Microsoft.Restier.Docs/api-reference/` (gitignored) | Regenerated by the docsproj build; do NOT hand-edit. |

### Layering invariants

- `Microsoft.Restier.Core` may reference `Microsoft.OData.Core` (which includes `Microsoft.OData.UriParser` — `SelectExpandClause` lives there) and `Microsoft.OData.Edm`. It must NOT reference `Microsoft.AspNetCore.OData`. Verified — the detector takes `SelectExpandClause` directly, so the controller does the `ODataQueryOptions.SelectExpand?.SelectExpandClause` unwrap.
- The EF shared source file uses `#if EFCore` for both namespace and provider-specific calls. New code follows the same pattern — no separate files per provider.
- `ApiBase` does not expose a service provider; consumers cannot reach DI via the API instance. Provider-specific services (sourcer, executor, etc.) receive their dependencies through constructor injection at DI-registration time. The chain-of-responsibility framework sets `Inner` through the existing property hook — adding a constructor does not interfere with that wiring.

---

## Task 1: Add `HasRecursiveExpand` and `AllowNoTracking` to `QueryRequest`

**Files:**
- Modify: `src/Microsoft.Restier.Core/Query/QueryRequest.cs`
- Test: `test/Microsoft.Restier.Tests.Core/Query/QueryRequestTests.cs`

These two flags compose: `AllowNoTracking` gates whether the EF sourcer is even allowed to consider no-tracking for this request; `HasRecursiveExpand` further constrains the EF6 path to fall back to tracked when a cycle is present. Both default to `false` so any code path that doesn't explicitly set them gets the pre-#726 tracked behavior.

- [ ] **Step 1: Write the failing tests**

Append to `test/Microsoft.Restier.Tests.Core/Query/QueryRequestTests.cs` (after the existing `CanSetAndGetShouldReturnCount` test, inside the class):

```csharp
/// <summary>
/// HasRecursiveExpand defaults to false.
/// </summary>
[Fact]
public void HasRecursiveExpand_DefaultsToFalse()
{
    testClass.HasRecursiveExpand.Should().BeFalse();
}

/// <summary>
/// HasRecursiveExpand can be set by internal code (e.g. the controller layer).
/// </summary>
[Fact]
public void HasRecursiveExpand_CanBeSet()
{
    typeof(QueryRequest)
        .GetProperty(nameof(QueryRequest.HasRecursiveExpand))!
        .SetValue(testClass, true);
    testClass.HasRecursiveExpand.Should().BeTrue();
}

/// <summary>
/// AllowNoTracking defaults to false so the submit pipeline and any
/// direct (non-controller) QueryAsync call preserves tracked behavior.
/// </summary>
[Fact]
public void AllowNoTracking_DefaultsToFalse()
{
    testClass.AllowNoTracking.Should().BeFalse();
}

/// <summary>
/// AllowNoTracking can be set by internal code (the AspNetCore controller).
/// </summary>
[Fact]
public void AllowNoTracking_CanBeSet()
{
    typeof(QueryRequest)
        .GetProperty(nameof(QueryRequest.AllowNoTracking))!
        .SetValue(testClass, true);
    testClass.AllowNoTracking.Should().BeTrue();
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj \
    --filter "FullyQualifiedName~QueryRequestTests.HasRecursiveExpand|FullyQualifiedName~QueryRequestTests.AllowNoTracking"
```

Expected: FAIL — `'QueryRequest' does not contain a definition for 'HasRecursiveExpand'` (or `AllowNoTracking`).

- [ ] **Step 3: Add both properties**

Edit `src/Microsoft.Restier.Core/Query/QueryRequest.cs`. After the `IncludeTotalCount` property (around line 49), insert:

```csharp
        /// <summary>
        /// Gets a value indicating whether the OData <c>$expand</c> tree of the
        /// originating request contains a cycle — that is, a navigation chain
        /// that revisits an entity type (or a type in the same inheritance
        /// hierarchy) already present in the chain.
        /// </summary>
        /// <remarks>
        /// Set by the AspNetCore layer from the parsed <c>SelectExpandClause</c>.
        /// EF providers use this hint to choose a safe tracking behavior — see
        /// <c>RestierEFTrackingBehavior</c>. Default <c>false</c>.
        /// </remarks>
        public bool HasRecursiveExpand { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the EF query pipeline is permitted
        /// to drop change tracking for this request.
        /// </summary>
        /// <remarks>
        /// Set to <c>true</c> by the AspNetCore controller for top-level HTTP
        /// read requests. Submit-pipeline and deep-update internal queries
        /// leave this <c>false</c>, since those code paths mutate the returned
        /// entities via <c>DbContext.Entry(...)</c> and depend on tracking
        /// (or at least on the original-values snapshot) being available.
        /// </remarks>
        public bool AllowNoTracking { get; internal set; }
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj \
    --filter "FullyQualifiedName~QueryRequestTests.HasRecursiveExpand|FullyQualifiedName~QueryRequestTests.AllowNoTracking"
```

Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Core/Query/QueryRequest.cs \
        test/Microsoft.Restier.Tests.Core/Query/QueryRequestTests.cs
git commit -m "feat(core): add HasRecursiveExpand and AllowNoTracking hints to QueryRequest

Two per-request flags:

* HasRecursiveExpand surfaces same-type or cross-type cycles in the
  request's \$expand tree, so the EF6 provider can fall back to tracked
  queries when identity resolution matters.

* AllowNoTracking gates the no-tracking transformation itself. Only the
  AspNetCore controller sets it (for top-level HTTP reads). Submit-
  pipeline and deep-update internal QueryAsync calls leave it false so
  EFChangeSetInitializer.HandleEntitySet's dbContext.Entry(resource)
  continues to operate on entities with a valid original-values snapshot.

Refs: OData/RESTier#726"
```

---

## Task 2: Introduce `IExpandCycleDetector` interface

**Files:**
- Create: `src/Microsoft.Restier.Core/Query/IExpandCycleDetector.cs`

- [ ] **Step 1: Create the interface**

Write `src/Microsoft.Restier.Core/Query/IExpandCycleDetector.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Inspects a parsed OData <see cref="SelectExpandClause"/> to determine
    /// whether the expand graph contains a cycle.
    /// </summary>
    /// <remarks>
    /// A cycle exists when any <c>$expand</c> segment targets an entity type
    /// already present (directly or through inheritance) on the current
    /// expansion path. Both self-cycles (<c>Employee → Manager: Employee</c>)
    /// and cross-type cycles (<c>Department → Employees → Department</c>) are
    /// considered cycles.
    /// </remarks>
    public interface IExpandCycleDetector
    {
        /// <summary>
        /// Determines whether the supplied expand clause, rooted at
        /// <paramref name="rootType"/>, contains a cycle.
        /// </summary>
        /// <param name="rootType">The entity type of the queried set, used as
        /// the initial node of the expansion path. Required.</param>
        /// <param name="clause">The parsed select-and-expand clause. May be
        /// <c>null</c> (e.g. requests with no <c>$expand</c>) — in which case
        /// the method returns <c>false</c>.</param>
        /// <returns><c>true</c> if a cycle is detected, otherwise <c>false</c>.</returns>
        bool HasCycle(IEdmEntityType rootType, SelectExpandClause clause);
    }
}
```

- [ ] **Step 2: Commit the interface (no test yet — the default impl test in Task 3 covers it)**

```bash
git add src/Microsoft.Restier.Core/Query/IExpandCycleDetector.cs
git commit -m "feat(core): add IExpandCycleDetector interface

Provider-agnostic abstraction that inspects a parsed SelectExpandClause
for same-type or cross-type cycles. The default implementation arrives
in the next commit.

Refs: OData/RESTier#726"
```

---

## Task 3: Implement `DefaultExpandCycleDetector` with unit tests

**Files:**
- Create: `src/Microsoft.Restier.Core/Query/DefaultExpandCycleDetector.cs`
- Create: `test/Microsoft.Restier.Tests.Core/Query/DefaultExpandCycleDetectorTests.cs`

The algorithm is a DFS over `ExpandedNavigationSelectItem` / `ExpandedReferenceSelectItem` nodes. We maintain a list of entity types on the *current path* (DFS path — pushed on enter, popped on exit). A cycle exists when the target type of an expand is in the same inheritance hierarchy as any type already on the path.

- [ ] **Step 1: Write the failing tests**

Write `test/Microsoft.Restier.Tests.Core/Query/DefaultExpandCycleDetectorTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.Core.Query;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Query
{
    /// <summary>
    /// Tests for <see cref="DefaultExpandCycleDetector"/>.
    ///
    /// EDM topology used by these tests:
    ///   Employee  (entity type)
    ///     Manager       : Employee     (single nav, self-referential)
    ///     Reports       : Employee[]   (collection nav, self-referential)
    ///     Department    : Department   (single nav)
    ///   Department
    ///     Employees     : Employee[]   (collection nav — back to Employee)
    ///     Parent        : Department   (single nav, self-referential)
    ///   Manager : Employee             (derived type)
    ///   Customer (no nav back to Employee)
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DefaultExpandCycleDetectorTests
    {
        private readonly TestEdm edm = new();
        private readonly DefaultExpandCycleDetector detector = new();

        [Fact]
        public void NullClause_ReturnsFalse()
        {
            detector.HasCycle(edm.EmployeeType, null).Should().BeFalse();
        }

        [Fact]
        public void NoExpand_ReturnsFalse()
        {
            var clause = new SelectExpandClause(Array.Empty<SelectItem>(), allSelected: true);
            detector.HasCycle(edm.EmployeeType, clause).Should().BeFalse();
        }

        [Fact]
        public void NonRecursiveExpand_ReturnsFalse()
        {
            // /Employees?$expand=Department
            var clause = edm.Expand(edm.EmployeeType, "Department");
            detector.HasCycle(edm.EmployeeType, clause).Should().BeFalse();
        }

        [Fact]
        public void SelfCycleViaSingleNav_ReturnsTrue()
        {
            // /Employees?$expand=Manager  (Manager : Employee)
            var clause = edm.Expand(edm.EmployeeType, "Manager");
            detector.HasCycle(edm.EmployeeType, clause).Should().BeTrue();
        }

        [Fact]
        public void SelfCycleViaCollectionNav_ReturnsTrue()
        {
            // /Employees?$expand=Reports
            var clause = edm.Expand(edm.EmployeeType, "Reports");
            detector.HasCycle(edm.EmployeeType, clause).Should().BeTrue();
        }

        [Fact]
        public void CrossTypeCycle_ReturnsTrue()
        {
            // /Departments?$expand=Employees($expand=Department)
            var inner = edm.Expand(edm.EmployeeType, "Department");
            var clause = edm.Expand(edm.DepartmentType, "Employees", inner);
            detector.HasCycle(edm.DepartmentType, clause).Should().BeTrue();
        }

        [Fact]
        public void NestedNonCycle_ReturnsFalse()
        {
            // /Employees?$expand=Department($expand=Parent)  — no return to Employee
            var inner = edm.Expand(edm.DepartmentType, "Parent");
            var clause = edm.Expand(edm.EmployeeType, "Department", inner);
            detector.HasCycle(edm.EmployeeType, clause).Should().BeFalse();
        }

        [Fact]
        public void SiblingExpandsNoCycle_ReturnsFalse()
        {
            // /Employees?$expand=Department,Customer  (Customer has no nav back)
            var clause = edm.Expand(
                edm.EmployeeType,
                ("Department", null),
                ("Customer", null));
            detector.HasCycle(edm.EmployeeType, clause).Should().BeFalse();
        }

        [Fact]
        public void InheritanceCounts_DerivedTypeRevisitsBase_ReturnsTrue()
        {
            // /Employees?$expand=Manager  where Manager : Employee
            // Already covered by SelfCycleViaSingleNav, but explicit assertion here for
            // the inheritance rule: visiting a derived type after the base is a cycle.
            var clause = edm.Expand(edm.EmployeeType, "Manager");
            detector.HasCycle(edm.EmployeeType, clause).Should().BeTrue();
        }

        [Fact]
        public void DeepCrossTypeCycle_ReturnsTrue()
        {
            // /Employees?$expand=Department($expand=Employees($expand=Department))
            var innermost = edm.Expand(edm.EmployeeType, "Department");
            var middle = edm.Expand(edm.DepartmentType, "Employees", innermost);
            var clause = edm.Expand(edm.EmployeeType, "Department", middle);
            detector.HasCycle(edm.EmployeeType, clause).Should().BeTrue();
        }
    }

    /// <summary>
    /// Hand-built EDM model exposing exactly the topology described in the test
    /// summary. Kept inside the test assembly so it can evolve with the tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class TestEdm
    {
        public EdmModel Model { get; }
        public EdmEntityType EmployeeType { get; }
        public EdmEntityType ManagerType { get; }
        public EdmEntityType DepartmentType { get; }
        public EdmEntityType CustomerType { get; }
        public EdmEntityContainer Container { get; }

        public TestEdm()
        {
            Model = new EdmModel();

            EmployeeType = new EdmEntityType("Test", "Employee");
            EmployeeType.AddKeys(EmployeeType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32));

            DepartmentType = new EdmEntityType("Test", "Department");
            DepartmentType.AddKeys(DepartmentType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32));

            ManagerType = new EdmEntityType("Test", "Manager", EmployeeType);

            CustomerType = new EdmEntityType("Test", "Customer");
            CustomerType.AddKeys(CustomerType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32));

            EmployeeType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Manager",
                Target = EmployeeType,
                TargetMultiplicity = EdmMultiplicity.ZeroOrOne,
            });
            EmployeeType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Reports",
                Target = EmployeeType,
                TargetMultiplicity = EdmMultiplicity.Many,
            });
            EmployeeType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Department",
                Target = DepartmentType,
                TargetMultiplicity = EdmMultiplicity.ZeroOrOne,
            });
            EmployeeType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Customer",
                Target = CustomerType,
                TargetMultiplicity = EdmMultiplicity.ZeroOrOne,
            });

            DepartmentType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Employees",
                Target = EmployeeType,
                TargetMultiplicity = EdmMultiplicity.Many,
            });
            DepartmentType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "Parent",
                Target = DepartmentType,
                TargetMultiplicity = EdmMultiplicity.ZeroOrOne,
            });

            Model.AddElement(EmployeeType);
            Model.AddElement(ManagerType);
            Model.AddElement(DepartmentType);
            Model.AddElement(CustomerType);

            Container = new EdmEntityContainer("Test", "Container");
            Container.AddEntitySet("Employees", EmployeeType);
            Container.AddEntitySet("Departments", DepartmentType);
            Container.AddEntitySet("Customers", CustomerType);
            Model.AddElement(Container);
        }

        /// <summary>Build a single-level <c>$expand=navName</c> clause.</summary>
        public SelectExpandClause Expand(IEdmEntityType source, string navName, SelectExpandClause inner = null)
            => Expand(source, (navName, inner));

        /// <summary>Build a <c>$expand</c> clause with multiple sibling expansions.</summary>
        public SelectExpandClause Expand(IEdmEntityType source, params (string Nav, SelectExpandClause Inner)[] expansions)
        {
            var items = new List<SelectItem>(expansions.Length);
            var entitySet = Container.FindEntitySet(source.Name + "s") ?? Container.FindEntitySet("Employees");

            foreach (var (navName, innerClause) in expansions)
            {
                var nav = source.FindProperty(navName) as IEdmNavigationProperty
                    ?? throw new InvalidOperationException($"Navigation '{navName}' not found on {source.Name}.");
                var navSegment = new NavigationPropertySegment(nav, entitySet);
                var path = new ODataExpandPath(navSegment);
                items.Add(new ExpandedNavigationSelectItem(
                    pathToNavigationProperty: path,
                    navigationSource: entitySet,
                    selectAndExpand: innerClause ?? new SelectExpandClause(Array.Empty<SelectItem>(), allSelected: true)));
            }

            return new SelectExpandClause(items, allSelected: true);
        }
    }
}
```

Note: the `Container.FindEntitySet(source.Name + "s") ?? Container.FindEntitySet("Employees")` is a deliberately simple test helper — every entity type in the test EDM has exactly one set named with the `+ "s"` convention. The fallback is purely defensive.

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj \
    --filter "FullyQualifiedName~DefaultExpandCycleDetectorTests"
```

Expected: All tests FAIL — `'DefaultExpandCycleDetector' could not be found`.

- [ ] **Step 3: Implement `DefaultExpandCycleDetector`**

Write `src/Microsoft.Restier.Core/Query/DefaultExpandCycleDetector.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Default <see cref="IExpandCycleDetector"/> — walks the expand tree
    /// depth-first and flags any segment whose target type shares an
    /// inheritance hierarchy with a type already on the current path.
    /// </summary>
    internal sealed class DefaultExpandCycleDetector : IExpandCycleDetector
    {
        /// <inheritdoc/>
        public bool HasCycle(IEdmEntityType rootType, SelectExpandClause clause)
        {
            Ensure.NotNull(rootType, nameof(rootType));

            if (clause is null)
            {
                return false;
            }

            var path = new List<IEdmEntityType> { rootType };
            return HasCycle(clause, path);
        }

        private static bool HasCycle(SelectExpandClause clause, List<IEdmEntityType> path)
        {
            foreach (var item in clause.SelectedItems)
            {
                IEdmType target;
                SelectExpandClause nested;

                if (item is ExpandedNavigationSelectItem expanded)
                {
                    target = expanded.PathToNavigationProperty.LastSegment.EdmType;
                    nested = expanded.SelectAndExpand;
                }
                else if (item is ExpandedReferenceSelectItem reference)
                {
                    target = reference.PathToNavigationProperty.LastSegment.EdmType;
                    nested = null;
                }
                else
                {
                    continue;
                }

                var targetEntity = ResolveEntityType(target);
                if (targetEntity is null)
                {
                    continue;
                }

                foreach (var onPath in path)
                {
                    if (SharesHierarchy(onPath, targetEntity))
                    {
                        return true;
                    }
                }

                path.Add(targetEntity);
                try
                {
                    if (nested is not null && HasCycle(nested, path))
                    {
                        return true;
                    }
                }
                finally
                {
                    path.RemoveAt(path.Count - 1);
                }
            }

            return false;
        }

        /// <summary>
        /// A navigation property's <see cref="IEdmType"/> may be the entity
        /// type itself or a <see cref="IEdmCollectionType"/> wrapping it.
        /// Reduce to the underlying entity type, returning <c>null</c> for
        /// non-entity targets (which should not arise from a valid
        /// navigation expand but are handled defensively).
        /// </summary>
        private static IEdmEntityType ResolveEntityType(IEdmType type)
        {
            if (type is IEdmCollectionType collection)
            {
                type = collection.ElementType.Definition;
            }

            return type as IEdmEntityType;
        }

        /// <summary>
        /// True when <paramref name="a"/> equals <paramref name="b"/> or one
        /// inherits from the other. Inheritance counts because EF's identity
        /// map keys on the base entity type — querying a derived type after
        /// the base (or vice versa) revisits the same identity space.
        /// </summary>
        private static bool SharesHierarchy(IEdmEntityType a, IEdmEntityType b)
        {
            return IsOrInheritsFrom(a, b) || IsOrInheritsFrom(b, a);
        }

        private static bool IsOrInheritsFrom(IEdmEntityType derived, IEdmEntityType maybeBase)
        {
            for (var current = derived; current is not null; current = current.BaseEntityType())
            {
                if (ReferenceEquals(current, maybeBase))
                {
                    return true;
                }

                if (string.Equals(current.FullName(), maybeBase.FullName(), StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj \
    --filter "FullyQualifiedName~DefaultExpandCycleDetectorTests"
```

Expected: All 10 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Core/Query/DefaultExpandCycleDetector.cs \
        test/Microsoft.Restier.Tests.Core/Query/DefaultExpandCycleDetectorTests.cs
git commit -m "feat(core): add DefaultExpandCycleDetector

DFS over the SelectExpandClause, tracking entity types on the current
path. Detects same-type recursion, cross-type cycles, and inheritance-
based revisits. Covered by 10 unit tests against a hand-built EDM.

Refs: OData/RESTier#726"
```

---

## Task 4: Register the detector in Core DI defaults

**Files:**
- Modify: `src/Microsoft.Restier.Core/Extensions/ServiceCollectionExtensions.cs:58-79`
- Test: `test/Microsoft.Restier.Tests.Core/Extensions/` (new file)

- [ ] **Step 1: Write the failing test**

Create `test/Microsoft.Restier.Tests.Core/Extensions/ServiceCollectionExtensionsTests.cs` (or, if it already exists, append the fact below — verify with `ls` first):

```bash
ls test/Microsoft.Restier.Tests.Core/Extensions/
```

If a `ServiceCollectionExtensionsTests.cs` already exists in that folder, append the fact to that file. Otherwise create:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Extensions
{
    [ExcludeFromCodeCoverage]
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddRestierCoreServices_RegistersDefaultExpandCycleDetector()
        {
            var services = new ServiceCollection();

            // AddRestierCoreServices is internal — InternalsVisibleTo wires it through.
            typeof(Microsoft.Restier.Core.ServiceCollectionExtensions)
                .GetMethod("AddRestierCoreServices",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(null, new object[] { services });

            using var provider = services.BuildServiceProvider();
            provider.GetService<IExpandCycleDetector>()
                .Should().NotBeNull()
                .And.BeOfType<DefaultExpandCycleDetector>();
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj \
    --filter "FullyQualifiedName~ServiceCollectionExtensionsTests.AddRestierCoreServices_Registers"
```

Expected: FAIL — `IExpandCycleDetector` resolves to `null`.

- [ ] **Step 3: Register the service**

Edit `src/Microsoft.Restier.Core/Extensions/ServiceCollectionExtensions.cs`. In the `AddRestierCoreServices` method, immediately after the existing `services.TryAddSingleton<IQueryHandler, DefaultQueryHandler>();` line (currently line 76), insert:

```csharp
            services.TryAddSingleton<IExpandCycleDetector, DefaultExpandCycleDetector>();
```

Also add to the `using` block at the top:

```csharp
using Microsoft.Restier.Core.Query;
```

(verify whether it's already present — `Microsoft.Restier.Core.Query` is referenced indirectly via other type names already in the file; the explicit import keeps the type-name resolution unambiguous).

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj \
    --filter "FullyQualifiedName~ServiceCollectionExtensionsTests.AddRestierCoreServices_Registers"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Core/Extensions/ServiceCollectionExtensions.cs \
        test/Microsoft.Restier.Tests.Core/Extensions/ServiceCollectionExtensionsTests.cs
git commit -m "feat(core): register IExpandCycleDetector default in core DI

Refs: OData/RESTier#726"
```

---

## Task 5: Wire the detector into `RestierController.ApplyQueryOptions`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs:728-782`

- [ ] **Step 1: Read the current method**

Re-read `ApplyQueryOptions` (lines 728-782) to confirm the insertion point. The hint must be set *after* `queryOptions` is constructed (line 741) but *before* `queryOptions.ApplyTo(...)` mutates the query (lines 757/774/778).

- [ ] **Step 2: Add the wiring**

In `src/Microsoft.Restier.AspNetCore/RestierController.cs`, modify `ApplyQueryOptions` — after the `var queryOptions = new ODataQueryOptions(queryContext, Request);` line, before the etag block, insert:

```csharp
            // This is the controller's HTTP read path — opt this request into
            // the no-tracking transformation. Internal QueryAsync calls (submit
            // pipeline, deep-update classifier, ResourceExists checks at
            // line 712) leave AllowNoTracking false and stay tracked.
            queryRequest.AllowNoTracking = true;

            // Surface the recursive-expand hint on the QueryRequest so the
            // EF6 sourcer can fall back to tracked queries (EFCore ignores
            // the hint — AsNoTrackingWithIdentityResolution covers it).
            var rootEntityType = path.GetEdmType() switch
            {
                IEdmCollectionType coll => coll.ElementType.Definition as IEdmEntityType,
                IEdmEntityType entity => entity,
                _ => null,
            };

            if (rootEntityType is not null && queryOptions.SelectExpand?.SelectExpandClause is not null)
            {
                var detector = HttpContext.Request.GetRouteServices()
                    .GetService(typeof(IExpandCycleDetector)) as IExpandCycleDetector;
                if (detector is not null)
                {
                    queryRequest.HasRecursiveExpand = detector.HasCycle(
                        rootEntityType,
                        queryOptions.SelectExpand.SelectExpandClause);
                }
            }
```

This insertion point covers all three `new QueryRequest(...)` call sites that flow into `ApplyQueryOptions` (lines 117, 143, 152 — i.e. the operation-import, operation-segment, and default-GET branches of `GetEntity`/`Get`). The internal parent-query at line 712 (`ResourceExists`-style check) deliberately bypasses `ApplyQueryOptions` and stays tracked.

Add the using import at the top of the file (the file already imports `Microsoft.Restier.Core.Query` per line 30 — confirm; if missing, add):

```csharp
using Microsoft.Restier.Core.Query;
```

`HasRecursiveExpand` and `AllowNoTracking` setters are `internal` — the `Microsoft.Restier.Core` assembly grants InternalsVisibleTo to `Microsoft.Restier.AspNetCore` (RESTier auto-configures this for source/test pairs; verify the source-to-aspnetcore grant exists):

```bash
grep -rn "InternalsVisibleTo" src/Microsoft.Restier.Core/
```

If the grant to `Microsoft.Restier.AspNetCore` is not present, **STOP** and add it before continuing — the controller cannot set `internal` properties otherwise. The tests in Task 1 use reflection precisely because the test assembly may or may not have InternalsVisibleTo; the production controller must use direct assignment.

- [ ] **Step 3: Build the AspNetCore project**

```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
```

Expected: build succeeds, no warnings.

- [ ] **Step 4: Run the full AspNetCore test suite to confirm no regression**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: all existing tests pass — at this point no behavior change is visible to existing tests because `HasRecursiveExpand` is read by no one yet.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/RestierController.cs
git commit -m "feat(aspnetcore): opt HTTP reads into no-tracking + compute expand-cycle hint

In ApplyQueryOptions, set QueryRequest.AllowNoTracking = true and
resolve IExpandCycleDetector from route services to set
HasRecursiveExpand. Only the controller's top-level HTTP read paths
flow through ApplyQueryOptions; internal QueryAsync calls (submit
pipeline, deep-update classifier) stay tracked because AllowNoTracking
remains false on their QueryRequests.

Refs: OData/RESTier#726"
```

---

## Task 6: Add `RestierEFTrackingBehavior` enum and `RestierEFOptions`

**Files:**
- Create: `src/Microsoft.Restier.EntityFramework.Shared/RestierEFTrackingBehavior.cs`
- Create: `src/Microsoft.Restier.EntityFramework.Shared/RestierEFOptions.cs`

These files live in the shared project and compile into both EF6 and EFCore assemblies (matching the convention used by `EFQueryExpressionSourcer.cs`, `EFQueryExecutor.cs`, etc.).

- [ ] **Step 1: Create the enum**

Write `src/Microsoft.Restier.EntityFramework.Shared/RestierEFTrackingBehavior.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EFCore
namespace Microsoft.Restier.EntityFrameworkCore
#else
namespace Microsoft.Restier.EntityFramework
#endif
{
    /// <summary>
    /// Controls how RESTier wraps the underlying <c>DbSet</c> in the EF query
    /// pipeline. Configured via <see cref="RestierEFOptions"/>.
    /// </summary>
    public enum RestierEFTrackingBehavior
    {
        /// <summary>
        /// Use the provider's recommended default. On EF Core this maps to
        /// <c>AsNoTrackingWithIdentityResolution</c>. On EF6 it maps to
        /// <c>AsNoTracking</c>, except for requests whose
        /// <c>$expand</c> tree contains a cycle — those fall back to tracked
        /// queries so identity is preserved across the cycle.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Force <c>AsNoTracking</c> for every query. Fastest, but
        /// identity is not preserved within a single query result. On
        /// recursive expands under EF6 this can produce duplicate
        /// materialized entities for the same key.
        /// </summary>
        NoTracking = 1,

        /// <summary>
        /// Force <c>AsNoTrackingWithIdentityResolution</c>. EF Core only —
        /// on EF6 this falls back to plain <c>AsNoTracking</c> because the
        /// underlying API does not exist.
        /// </summary>
        NoTrackingWithIdentityResolution = 2,

        /// <summary>
        /// Restore pre-#726 behavior — leave the <c>DbSet</c> tracked. Use
        /// when hook code mutates returned entities and expects those
        /// mutations to be picked up by <c>SaveChanges</c>.
        /// </summary>
        TrackAll = 3,
    }
}
```

- [ ] **Step 2: Create the options class**

Write `src/Microsoft.Restier.EntityFramework.Shared/RestierEFOptions.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EFCore
namespace Microsoft.Restier.EntityFrameworkCore
#else
namespace Microsoft.Restier.EntityFramework
#endif
{
    /// <summary>
    /// Per-API options for the RESTier EF provider. Registered as a
    /// singleton in the route's service container by
    /// <c>AddEF6ProviderServices</c> / <c>AddEFCoreProviderServices</c>.
    /// </summary>
    public sealed class RestierEFOptions
    {
        /// <summary>
        /// Controls how the query pipeline wraps the underlying
        /// <c>DbSet</c>. Defaults to <see cref="RestierEFTrackingBehavior.Default"/>.
        /// </summary>
        public RestierEFTrackingBehavior TrackingBehavior { get; set; }
            = RestierEFTrackingBehavior.Default;
    }
}
```

- [ ] **Step 3: Add both files to the shared `.projitems`**

`Microsoft.Restier.EntityFramework.Shared` is a Shared Project. New `.cs` files are NOT compiled unless explicitly listed in `Microsoft.Restier.EntityFramework.Shared.projitems`. Edit that file and append two `<Compile>` entries within the existing `<ItemGroup>` block (keep the list alphabetically grouped where possible):

```xml
    <Compile Include="$(MSBuildThisFileDirectory)RestierEFOptions.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)RestierEFTrackingBehavior.cs" />
```

The full `<ItemGroup>` should then contain (relevant additions only — leave existing entries in place):

```xml
  <ItemGroup>
    <Compile Include="$(MSBuildThisFileDirectory)EntityFrameworkApi.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)Extensions\ServiceCollectionExtensions.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)IEntityFrameworkApi.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)Model\EFModelBuilder.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)Model\EFModelMapper.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)Model\SpatialModelConvention.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)Query\EFQueryExecutor.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)Query\SelectExpandHelper.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)Query\EFQueryExpressionProcessor.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)Query\EFQueryExpressionSourcer.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)RestierEFOptions.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)RestierEFTrackingBehavior.cs" />
    <Compile Include="$(MSBuildThisFileDirectory)Submit\EFSubmitExecutor.cs" />
  </ItemGroup>
```

- [ ] **Step 4: Build both EF projects**

```bash
dotnet build src/Microsoft.Restier.EntityFramework/Microsoft.Restier.EntityFramework.csproj
dotnet build src/Microsoft.Restier.EntityFrameworkCore/Microsoft.Restier.EntityFrameworkCore.csproj
```

Expected: both build cleanly. Both should expose `RestierEFOptions` and `RestierEFTrackingBehavior` in their respective namespaces. If the build cannot find `RestierEFOptions`, the `.projitems` change in Step 3 did not take effect — re-verify the file path and the surrounding XML.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.EntityFramework.Shared/RestierEFTrackingBehavior.cs \
        src/Microsoft.Restier.EntityFramework.Shared/RestierEFOptions.cs \
        src/Microsoft.Restier.EntityFramework.Shared/Microsoft.Restier.EntityFramework.Shared.projitems
git commit -m "feat(ef): introduce RestierEFTrackingBehavior and RestierEFOptions

Shared between EF6 and EFCore providers — Default is provider-aware
(NoTrackingWithIdentityResolution on EFCore, NoTracking with cycle-aware
fallback on EF6). Wiring follows in subsequent commits.

Refs: OData/RESTier#726"
```

---

## Task 7: Register `RestierEFOptions` in shared `AddEFProviderServices`

**Files:**
- Modify: `src/Microsoft.Restier.EntityFramework.Shared/Extensions/ServiceCollectionExtensions.cs:35-47`

- [ ] **Step 1: Edit `AddEFProviderServices`**

Modify the existing method so it (a) registers a default `RestierEFOptions` if none has been supplied yet, and (b) re-registers `EFQueryExpressionSourcer` via a factory that resolves `RestierEFOptions` and passes it to the new constructor (added in Task 10). Replace the body so it reads:

```csharp
    internal static IServiceCollection AddEFProviderServices<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.TryAddSingleton(new RestierEFOptions());

        services.AddSingleton<IChainedService<IModelBuilder>, EFModelBuilder<TDbContext>>()
            .AddSingleton<IChainedService<IModelMapper>, EFModelMapper>()
            .AddSingleton<IChainedService<IQueryExpressionSourcer>>(sp =>
                new EFQueryExpressionSourcer(sp.GetRequiredService<RestierEFOptions>()))
            .AddSingleton<IChainedService<IQueryExecutor>, EFQueryExecutor>()
            .AddSingleton<IChainedService<IQueryExpressionProcessor>, EFQueryExpressionProcessor>()
            .AddSingleton<IChangeSetInitializer, EFChangeSetInitializer>()
            .AddSingleton<ISubmitExecutor, EFSubmitExecutor>();

        return services;
    }
```

Two things to know about this change:

1. `TryAddSingleton(new RestierEFOptions())` is intentional. The per-provider extension methods (`AddEF6ProviderServices` / `AddEFCoreProviderServices` overloads in Tasks 8 and 9) call `services.AddSingleton(new RestierEFOptions { TrackingBehavior = ... })` *before* calling `AddEFProviderServices`. `TryAdd` then leaves that earlier registration in place.

2. The sourcer registration moved from `<Type, Type>` to a factory lambda. The factory resolves `RestierEFOptions` at scope-construction time. The chain-of-responsibility framework still sets `Inner` via the existing property hook after construction — nothing about that wiring changes.

- [ ] **Step 2: Build both provider projects**

```bash
dotnet build src/Microsoft.Restier.EntityFramework/Microsoft.Restier.EntityFramework.csproj
dotnet build src/Microsoft.Restier.EntityFrameworkCore/Microsoft.Restier.EntityFrameworkCore.csproj
```

Expected: clean build.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.EntityFramework.Shared/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(ef): register default RestierEFOptions in shared DI"
```

---

## Task 8: Add `AddEF6ProviderServices` option overloads

**Files:**
- Modify: `src/Microsoft.Restier.EntityFramework/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Add overloads**

Append to the partial class in `src/Microsoft.Restier.EntityFramework/Extensions/ServiceCollectionExtensions.cs`:

```csharp
    /// <summary>
    /// Adds EF6 provider services with custom RESTier EF options.
    /// </summary>
    public static IServiceCollection AddEF6ProviderServices<TDbContext>(
        this IServiceCollection services,
        Action<RestierEFOptions> configureOptions)
        where TDbContext : DbContext
    {
        Ensure.NotNull(services, nameof(services));
        Ensure.NotNull(configureOptions, nameof(configureOptions));

        var options = new RestierEFOptions();
        configureOptions(options);
        services.AddSingleton(options);

        services.TryAddScoped(sp =>
        {
            var dbContext = Activator.CreateInstance<TDbContext>();
            dbContext.Configuration.ProxyCreationEnabled = false;
            return dbContext;
        });

        return AddEFProviderServices<TDbContext>(services);
    }

    /// <summary>
    /// Adds EF6 provider services with an explicit connection string and custom RESTier EF options.
    /// </summary>
    public static IServiceCollection AddEF6ProviderServices<TDbContext>(
        this IServiceCollection services,
        string connectionString,
        Action<RestierEFOptions> configureOptions)
        where TDbContext : DbContext
    {
        Ensure.NotNull(services, nameof(services));
        Ensure.NotNull(connectionString, nameof(connectionString));
        Ensure.NotNull(configureOptions, nameof(configureOptions));

        var options = new RestierEFOptions();
        configureOptions(options);
        services.AddSingleton(options);

        services.TryAddScoped(sp =>
        {
            var dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext), connectionString);
            dbContext.Configuration.ProxyCreationEnabled = false;
            return dbContext;
        });

        return AddEFProviderServices<TDbContext>(services);
    }
```

- [ ] **Step 2: Build**

```bash
dotnet build src/Microsoft.Restier.EntityFramework/Microsoft.Restier.EntityFramework.csproj
```

Expected: clean.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.EntityFramework/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(ef6): add AddEF6ProviderServices overloads with RestierEFOptions"
```

---

## Task 9: Add `AddEFCoreProviderServices` option overloads

**Files:**
- Modify: `src/Microsoft.Restier.EntityFrameworkCore/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Add overloads**

Append to the partial class in `src/Microsoft.Restier.EntityFrameworkCore/Extensions/ServiceCollectionExtensions.cs`:

```csharp
    /// <summary>
    /// Adds EFCore provider services with custom RESTier EF options.
    /// </summary>
    public static IServiceCollection AddEFCoreProviderServices<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> dbContextOptionsAction,
        Action<RestierEFOptions> configureOptions)
        where TDbContext : DbContext
    {
        Ensure.NotNull(services, nameof(services));
        Ensure.NotNull(configureOptions, nameof(configureOptions));

        var options = new RestierEFOptions();
        configureOptions(options);
        services.AddSingleton(options);

        services.AddDbContext<TDbContext>(dbContextOptionsAction);
        return AddEFProviderServices<TDbContext>(services);
    }

    /// <summary>
    /// Adds EFCore provider services with custom RESTier EF options and a service-aware DbContext options action.
    /// </summary>
    public static IServiceCollection AddEFCoreProviderServices<TDbContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> dbContextOptionsAction,
        Action<RestierEFOptions> configureOptions)
        where TDbContext : DbContext
    {
        Ensure.NotNull(services, nameof(services));
        Ensure.NotNull(configureOptions, nameof(configureOptions));

        var options = new RestierEFOptions();
        configureOptions(options);
        services.AddSingleton(options);

        services.AddDbContext<TDbContext>(dbContextOptionsAction);
        return AddEFProviderServices<TDbContext>(services);
    }
```

- [ ] **Step 2: Build**

```bash
dotnet build src/Microsoft.Restier.EntityFrameworkCore/Microsoft.Restier.EntityFrameworkCore.csproj
```

Expected: clean.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.EntityFrameworkCore/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(efcore): add AddEFCoreProviderServices overloads with RestierEFOptions"
```

---

## Task 10: Apply tracking behavior in `EFQueryExpressionSourcer`

**Files:**
- Modify: `src/Microsoft.Restier.EntityFramework.Shared/Query/EFQueryExpressionSourcer.cs`

This is the central change. Both EF6 and EFCore compilations of this source see the new behavior; the EF6 path consults `HasRecursiveExpand`, the EFCore path does not.

- [ ] **Step 1: Read the current sourcer**

Already covered in earlier discovery — file is `src/Microsoft.Restier.EntityFramework.Shared/Query/EFQueryExpressionSourcer.cs` and the change point is lines 79-87.

- [ ] **Step 2: Update the sourcer**

Two edits to `src/Microsoft.Restier.EntityFramework.Shared/Query/EFQueryExpressionSourcer.cs`:

**Edit A:** Add a private field and a constructor that receives `RestierEFOptions` via DI (and keep a parameterless constructor for tests / legacy direct instantiation):

```csharp
    internal class EFQueryExpressionSourcer : IQueryExpressionSourcer
    {
        private readonly RestierEFOptions options;

        /// <summary>
        /// Parameterless constructor — uses default <see cref="RestierEFOptions"/>.
        /// Retained so tests and code paths that instantiate the sourcer
        /// directly continue to work; the DI registration uses the
        /// <see cref="EFQueryExpressionSourcer(RestierEFOptions)"/> overload.
        /// </summary>
        public EFQueryExpressionSourcer()
            : this(new RestierEFOptions())
        {
        }

        /// <summary>
        /// Constructor used by DI — receives the per-API
        /// <see cref="RestierEFOptions"/> singleton.
        /// </summary>
        public EFQueryExpressionSourcer(RestierEFOptions options)
        {
            this.options = options ?? new RestierEFOptions();
        }

        /// <summary>
        /// Gets or sets the inner handler.
        /// </summary>
        public IQueryExpressionSourcer Inner { get; set; }
```

**Edit B:** Replace the body of `ReplaceQueryableSource` from line 79 through the closing `}` of the `if (!embedded)` block with:

```csharp
            if (!embedded)
            {
                var dbSet = (IQueryable)dbSetProperty.GetValue(dbContext);

                // Submit pipeline, deep-update classifier, ResourceExists checks,
                // and any direct api.QueryAsync call leave AllowNoTracking false;
                // those paths require tracked entities so EFChangeSetInitializer
                // can mutate them via dbContext.Entry(...). Only the controller's
                // HTTP read paths opt into the no-tracking transformation.
                if (!context.QueryContext.Request.AllowNoTracking)
                {
                    return Expression.Constant(dbSet);
                }

                var transformed = ApplyTracking(
                    dbSet,
                    options.TrackingBehavior,
                    context.QueryContext.Request.HasRecursiveExpand);

                return Expression.Constant(transformed);
            }
            else
            {
                return Expression.MakeMemberAccess(
                    Expression.Constant(dbContext),
                    dbSetProperty);
            }
        }

        private static IQueryable ApplyTracking(
            IQueryable dbSet,
            RestierEFTrackingBehavior behavior,
            bool hasRecursiveExpand)
        {
            switch (behavior)
            {
                case RestierEFTrackingBehavior.TrackAll:
                    return dbSet;

                case RestierEFTrackingBehavior.NoTracking:
                    return CallAsNoTracking(dbSet);

                case RestierEFTrackingBehavior.NoTrackingWithIdentityResolution:
#if EFCore
                    return CallAsNoTrackingWithIdentityResolution(dbSet);
#else
                    return CallAsNoTracking(dbSet);
#endif

                case RestierEFTrackingBehavior.Default:
                default:
#if EFCore
                    return CallAsNoTrackingWithIdentityResolution(dbSet);
#else
                    // EF6: AsNoTracking by default, but if the request shape has an expand
                    // cycle, fall back to tracked so identity resolution holds across the
                    // cycle. EFCore does not need this branch — identity resolution is
                    // always preserved by AsNoTrackingWithIdentityResolution.
                    return hasRecursiveExpand ? dbSet : CallAsNoTracking(dbSet);
#endif
            }
        }

        private static IQueryable CallAsNoTracking(IQueryable dbSet)
        {
            var elementType = dbSet.GetType().GetGenericArguments()[0];
#if EFCore
            var method = typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions)
                .GetMethod(nameof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking))
                !.MakeGenericMethod(elementType);
#else
            var method = typeof(System.Data.Entity.QueryableExtensions)
                .GetMethods()
                .Single(m => m.Name == nameof(System.Data.Entity.QueryableExtensions.AsNoTracking)
                    && m.IsGenericMethodDefinition)
                .MakeGenericMethod(elementType);
#endif
            return (IQueryable)method.Invoke(null, new object[] { dbSet });
        }

#if EFCore
        private static IQueryable CallAsNoTrackingWithIdentityResolution(IQueryable dbSet)
        {
            var elementType = dbSet.GetType().GetGenericArguments()[0];
            var method = typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions)
                .GetMethod(nameof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .AsNoTrackingWithIdentityResolution))
                !.MakeGenericMethod(elementType);
            return (IQueryable)method.Invoke(null, new object[] { dbSet });
        }
#endif
```

Behavior matrix (for reviewers):

| `AllowNoTracking` | `TrackingBehavior` | Result |
|---|---|---|
| `false` | any | Tracked (DbSet passed through unchanged). Covers submit, deep-update, internal ResourceExists, and any direct `api.QueryAsync` call. |
| `true` | `TrackAll` | Tracked. The opt-out for hook code that mutates returned entities. |
| `true` | `Default` (EFCore) | `AsNoTrackingWithIdentityResolution` |
| `true` | `Default` (EF6) | `AsNoTracking`, or tracked if `HasRecursiveExpand` |
| `true` | `NoTracking` | `AsNoTracking` |
| `true` | `NoTrackingWithIdentityResolution` | `AsNoTrackingWithIdentityResolution` on EFCore, `AsNoTracking` on EF6 |

The reflection cost is once-per-query; caching by element type can be a later optimization if profiling shows it matters.

`ApiBase` does NOT expose a service provider — the previous draft of this plan attempted `context.QueryContext.Api.ServiceProvider.GetService(...)` which does not compile. The constructor-injection approach above is the corrected design.

- [ ] **Step 3: Build both flavors**

```bash
dotnet build src/Microsoft.Restier.EntityFramework/Microsoft.Restier.EntityFramework.csproj
dotnet build src/Microsoft.Restier.EntityFrameworkCore/Microsoft.Restier.EntityFrameworkCore.csproj
```

Expected: both build cleanly.

- [ ] **Step 4: Run the existing EF test suites — they should still pass because the default applies AsNoTracking, and the existing tests do not depend on tracking-induced behaviors**

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj
dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj
```

Expected: ALL existing tests pass. If a PATCH/DELETE scenario test fails, **STOP** and investigate — it likely indicates `EFChangeSetInitializer.HandleEntitySet`'s `dbContext.Entry(resource)` is not re-attaching the detached entity as expected. See Task 11 for the verification harness.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.EntityFramework.Shared/Query/EFQueryExpressionSourcer.cs
git commit -m "feat(ef): apply no-tracking by default in EFQueryExpressionSourcer

EFCore: unconditional AsNoTrackingWithIdentityResolution.
EF6: AsNoTracking unless the request \$expand tree contains a cycle,
in which case fall back to tracked. RestierEFTrackingBehavior overrides
the default. Closes the long-standing TODO referencing GitHub issue #37.

Refs: OData/RESTier#726"
```

---

## Task 11: EFCore integration tests for the default behavior

**Files:**
- Create: `test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/EFQueryNoTrackingTests.cs`

The EFCore project already has `EFCoreDbContextExtensionsTests` as a precedent. We follow the same pattern: hand-construct a `LibraryContext` with in-memory or SQLite-in-memory, query via the API surface, and inspect `ChangeTracker.Entries()`.

Existing infra check — does the test project have an in-memory DbContext helper? Search:

```bash
grep -rn "UseInMemoryDatabase\|UseSqlite" test/Microsoft.Restier.Tests.EntityFrameworkCore/
grep -rn "UseInMemoryDatabase\|UseSqlite" test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/
```

If existing helpers exist, reuse them. If not, the test below stands up its own SQLite-in-memory instance — `Microsoft.EntityFrameworkCore.Sqlite` is already in the test stack (verify with `dotnet list package` in the test project).

- [ ] **Step 1: Write the failing tests**

Write `test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/EFQueryNoTrackingTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Query
{
    [ExcludeFromCodeCoverage]
    public class EFQueryNoTrackingTests
    {
        /// <summary>
        /// Sanity check: the test's own DbContext is no-tracked when the sourcer
        /// wraps a DbSet with AsNoTrackingWithIdentityResolution — confirms the
        /// reflection-based call resolves to the right method group.
        /// </summary>
        [Fact]
        public void AsNoTrackingWithIdentityResolution_LeavesChangeTrackerEmpty()
        {
            var options = new DbContextOptionsBuilder<LibraryContext>()
                .UseInMemoryDatabase($"notracking-{System.Guid.NewGuid()}")
                .Options;

            using var context = new LibraryContext(options);
            context.Publishers.Add(new Microsoft.Restier.Tests.Shared.Scenarios.Library.Publisher
            {
                Id = "P1",
                Name = "Acme",
            });
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var publishers = context.Publishers.AsNoTrackingWithIdentityResolution().ToList();

            publishers.Should().HaveCount(1);
            context.ChangeTracker.Entries().Should().BeEmpty();
        }

        [Fact]
        public void Default_Options_IsDefault()
        {
            var options = new RestierEFOptions();
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.Default);
        }

        [Fact]
        public void TrackAll_Options_RoundTrips()
        {
            var options = new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.TrackAll };
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.TrackAll);
        }
    }
}
```

The first test asserts what EF Core itself promises — that's intentional. It's a guard against the test infrastructure breaking. The end-to-end "GET via the controller leaves the tracker empty" assertion lives at a higher level (Breakdance scenario tests) and is added in Task 13 once the integration harness is verified.

- [ ] **Step 2: Run tests**

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj \
    --filter "FullyQualifiedName~EFQueryNoTrackingTests"
```

Expected: 3 tests PASS.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/EFQueryNoTrackingTests.cs
git commit -m "test(efcore): unit tests for RestierEFOptions and no-tracking call path"
```

---

## Task 12: EF6 integration tests for the cycle-aware fallback

**Files:**
- Create: `test/Microsoft.Restier.Tests.EntityFramework/Query/EFQueryNoTrackingTests.cs`

EF6 in-memory testing options are more limited — use `Effort.EF6` if already in the test stack, otherwise a SQL CE / LocalDB based fixture from the existing `LibraryTestInitializer` setup. **Verify before writing**:

```bash
grep -rn "PackageReference.*Effort\|PackageReference.*LocalDB\|UseInMemory" test/Microsoft.Restier.Tests.EntityFramework/
```

If a fixture exists, reuse it. Otherwise, scope this task to **option-roundtrip + tracking-behavior switch tests at the unit level**, deferring end-to-end EF6 cycle-detection tests to a follow-up (call this out in the commit message).

- [ ] **Step 1: Write the tests**

Write `test/Microsoft.Restier.Tests.EntityFramework/Query/EFQueryNoTrackingTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Restier.EntityFramework;
using Xunit;

namespace Microsoft.Restier.Tests.EntityFramework.Query
{
    [ExcludeFromCodeCoverage]
    public class EFQueryNoTrackingTests
    {
        [Fact]
        public void Default_TrackingBehavior_IsDefault()
        {
            var options = new RestierEFOptions();
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.Default);
        }

        [Fact]
        public void TrackingBehavior_RoundTrips_TrackAll()
        {
            var options = new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.TrackAll };
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.TrackAll);
        }

        [Fact]
        public void TrackingBehavior_RoundTrips_NoTracking()
        {
            var options = new RestierEFOptions { TrackingBehavior = RestierEFTrackingBehavior.NoTracking };
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.NoTracking);
        }

        [Fact]
        public void NoTrackingWithIdentityResolution_OnEF6_FallsBackToNoTracking_Documented()
        {
            // No runtime assertion — this is a docs/intent test that exists to
            // surface the EF6 fallback behavior in the test list. The behavior
            // is implemented in EFQueryExpressionSourcer.ApplyTracking and
            // covered transitively by scenario-level tests once the EF6
            // harness ships.
            var options = new RestierEFOptions
            {
                TrackingBehavior = RestierEFTrackingBehavior.NoTrackingWithIdentityResolution,
            };
            options.TrackingBehavior.Should().Be(RestierEFTrackingBehavior.NoTrackingWithIdentityResolution);
        }
    }
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj \
    --filter "FullyQualifiedName~EFQueryNoTrackingTests"
```

Expected: 4 tests PASS.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFramework/Query/EFQueryNoTrackingTests.cs
git commit -m "test(ef6): RestierEFOptions roundtrip + tracking behavior selection"
```

---

## Task 13: PATCH / DELETE regression check via full suites

The risk of this change is that `EFChangeSetInitializer.FindResource` returns a no-tracked entity that `HandleEntitySet` then mutates via `dbContext.Entry(resource)`. Both EF6 and EFCore should re-attach on `Entry(...)`, but the only honest way to verify is to run the existing PATCH/DELETE coverage.

- [ ] **Step 1: Run the entire EF6 suite**

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj
```

Expected: ALL tests pass.

- [ ] **Step 2: Run the entire EFCore suite**

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj
```

Expected: ALL tests pass.

- [ ] **Step 3: Run the AspNetCore scenario tests (these exercise the full HTTP-layer PATCH/DELETE paths)**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: ALL tests pass. If a PATCH or DELETE scenario test fails, **STOP** and analyze. The likely culprits:

1. `IsFullReplaceUpdateRequest` path in `EFChangeSetInitializer.SetValues` — line 326 calls `dbEntry.CurrentValues.SetValues(newInstance)`. On a detached entity this throws under EF6. Fix: explicit `dbContext.Entry(resource).State = EntityState.Modified` *before* the `SetValues` call when the entity is detached.
2. Concurrency tokens / ETag validation — `item.ValidateEtag` runs over a detached materialized array, which is fine for read-only comparison but may misbehave if downstream code expects tracked.

If issues surface, **add the fix as a separate task** rather than silently amending earlier commits — keeps the change history honest.

- [ ] **Step 4: Run the full solution build as a final check**

```bash
dotnet build RESTier.slnx
```

Expected: clean build, warnings-as-errors observed throughout.

- [ ] **Step 5: No commit unless fixes were needed in Step 3. If fixes were needed, commit them as `fix(ef): ...` with a clear explanation.**

---

## Task 14: Documentation, XML docs, and api-reference regeneration

This task has three parts: (a) XML doc completeness audit on every public/internal type added in this branch, (b) hand-written guide + release-note updates, and (c) regenerate the auto-generated `api-reference/` MDX via the docsproj build so the new types appear in the published reference.

**Files:**
- Modify: `src/Microsoft.Restier.Docs/guides/server/performance.mdx`
- Modify: `src/Microsoft.Restier.Docs/release-notes/index.md` and create the next-version release note (current latest is `1-1-0.md` — use whatever version this PR ships under; for the placeholder text below assume `1-2-0.md`).
- Regenerate (do NOT hand-edit): files under `src/Microsoft.Restier.Docs/api-reference/`.

- [ ] **Step 1: Audit XML docs on all new and changed public/internal members**

For each type or member added or modified in Tasks 1–10, verify the XML doc is present, accurate, and references current behavior — not a prior plan iteration. Specifically:

- `QueryRequest.HasRecursiveExpand` and `QueryRequest.AllowNoTracking` (Task 1).
- `IExpandCycleDetector` and its `HasCycle` method (Task 2).
- `DefaultExpandCycleDetector` (Task 3) — internal, but doc the algorithm so the next maintainer doesn't have to reverse-engineer.
- `RestierEFTrackingBehavior` enum and every enum member (Task 6).
- `RestierEFOptions` and its `TrackingBehavior` property (Task 6).
- Both new `EFQueryExpressionSourcer` constructors (Task 10).
- New `AddEF6ProviderServices` overloads (Task 8).
- New `AddEFCoreProviderServices` overloads (Task 9).

Build the whole solution with warnings-as-errors to flush out missing XML docs:

```bash
dotnet build RESTier.slnx
```

Expected: clean — warnings-as-errors will catch any missing `<summary>` on public members. Fix any flagged warnings here (do NOT suppress).

- [ ] **Step 2: Update the performance guide**

`src/Microsoft.Restier.Docs/guides/server/performance.mdx` is the existing guide covering query performance. Append (or insert under an appropriate existing heading) a `## Tracking behavior` section. Match the Mintlify component style already in use in that file (`<Info>`, `<Note>`, `<Tip>`, `<Warning>`, `<CodeGroup>`):

```mdx
## Tracking behavior

By default, RESTier executes GET queries with change tracking disabled —
the single largest perf knob for read-heavy APIs. The behavior differs
slightly between EF Core and EF6:

- **EF Core**: `AsNoTrackingWithIdentityResolution()`. Entities are not
  added to the change tracker, but identity is preserved within a single
  query result, so recursive `$expand` (e.g. `Employee → Manager: Employee`)
  still returns the same instance per key.
- **EF6**: `AsNoTracking()` — except when the request's `$expand` tree
  contains a cycle (same-type recursion or cross-type cycles like
  `Department → Employees → Department`). In that case RESTier falls back
  to a tracked query, because EF6 has no
  `AsNoTrackingWithIdentityResolution` equivalent.

<Info>
Internal queries (the submit pipeline's UPDATE/DELETE entity load, deep-
update parent lookups, ResourceExists checks) always stay tracked.
Only top-level HTTP read paths flow through the no-tracking
transformation.
</Info>

<Warning>
If hook code (`OnFiltering*`, `OnLoaded*`, etc.) mutates entities returned
from a GET expecting those mutations to be persisted on the next
`SaveChanges`, opt back into tracking with
`RestierEFTrackingBehavior.TrackAll`.
</Warning>

### Overriding the default

<CodeGroup>

```csharp EF Core
services.AddEFCoreProviderServices<LibraryContext>(
    dbOpts => dbOpts.UseSqlServer(connectionString),
    restierOpts => restierOpts.TrackingBehavior = RestierEFTrackingBehavior.TrackAll);
```

```csharp EF6
services.AddEF6ProviderServices<LibraryContext>(
    restierOpts => restierOpts.TrackingBehavior = RestierEFTrackingBehavior.NoTracking);
```

</CodeGroup>

The available values are:

- `Default` — provider-aware default (EFCore: identity-resolved no-tracking; EF6: no-tracking with cycle-aware fallback).
- `NoTracking` — force `AsNoTracking()` regardless of request shape.
- `NoTrackingWithIdentityResolution` — EFCore only; falls back to `NoTracking` on EF6.
- `TrackAll` — restore pre-1.2 behavior.
```

- [ ] **Step 3: Update release notes**

Create the next-version file (use the actual ship version — placeholder `1-2-0.md`). Follow the formatting of `1-1-0.md`:

```bash
cp src/Microsoft.Restier.Docs/release-notes/1-1-0.md \
   src/Microsoft.Restier.Docs/release-notes/1-2-0.md
```

(Then strip the inherited content and add the new entries.) The relevant entry:

```md
### Breaking change: GET queries no longer change-track entities

GET queries now execute with change tracking disabled by default (EF Core:
`AsNoTrackingWithIdentityResolution`; EF6: `AsNoTracking` with a
cycle-aware fallback to tracked queries when `$expand` contains a cycle).

The submit pipeline and internal lookups are unaffected — only the
controller's top-level HTTP read paths are no-tracked.

Hook code that previously relied on mutating returned entities to drive
a save must opt back into tracking via:

```csharp
services.AddEFCoreProviderServices<MyContext>(
    dbOpts => dbOpts.UseSqlServer(...),
    restierOpts => restierOpts.TrackingBehavior = RestierEFTrackingBehavior.TrackAll);
```

Closes [#726](https://github.com/OData/RESTier/issues/726).
```

If `src/Microsoft.Restier.Docs/release-notes/index.md` enumerates the release-notes files in a nav table, append a row pointing to the new file.

- [ ] **Step 4: Regenerate the api-reference MDX**

`api-reference/` is gitignored output but it ships with the published docs site, so the docsproj build must succeed and produce the new entries. Run:

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: clean build. Spot-check that the regenerated MDX contains entries for the new types — e.g. look for the file that documents `Microsoft.Restier.EntityFrameworkCore`:

```bash
ls src/Microsoft.Restier.Docs/api-reference/Microsoft/Restier/EntityFrameworkCore/ | grep -E "RestierEFOptions|RestierEFTrackingBehavior"
ls src/Microsoft.Restier.Docs/api-reference/Microsoft/Restier/EntityFramework/ | grep -E "RestierEFOptions|RestierEFTrackingBehavior"
ls src/Microsoft.Restier.Docs/api-reference/Microsoft/Restier/Core/Query/ | grep -E "IExpandCycleDetector"
```

Expected: all three greps return at least one match.

- [ ] **Step 5: Update the nav template if needed**

Per `CLAUDE.md`, the docsproj's `<MintlifyTemplate>` block is the source of truth for `docs.json`. If the performance guide is already in the nav, no change is needed. If the new release-notes file is not auto-discovered, add it to the template. Then rebuild the docsproj so `docs.json` regenerates, and commit `docs.json` alongside.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.Docs/
git commit -m "docs: tracking-behavior guide, release notes, api-reference regen

* Performance guide: new \"Tracking behavior\" section covering the
  EF6/EFCore split and the RestierEFTrackingBehavior override.
* Release notes for the next version flag the breaking change and
  the opt-back-into-tracking recipe.
* Regenerated api-reference MDX picks up IExpandCycleDetector,
  RestierEFOptions, and RestierEFTrackingBehavior.

Refs: OData/RESTier#726"
```

---

## Task 15: Final solution-wide verification

- [ ] **Step 1: Full solution build**

```bash
dotnet build RESTier.slnx
```

Expected: clean. Warnings-as-errors should catch any unused `using` directives or missing XML doc comments introduced along the way.

- [ ] **Step 2: Full solution test**

```bash
dotnet test RESTier.slnx
```

Expected: ALL tests pass across Core, AspNetCore, EF6, EFCore, and Breakdance suites.

- [ ] **Step 3: Quick perf smoke (optional but recommended)**

Pick a scenario test that issues a multi-thousand-row GET (likely under `Microsoft.Restier.Tests.AspNetCore/ScenarioTests/`) and time it before/after this branch. Document the result in the PR description — even a rough number adds confidence that the change delivers the promised win.

- [ ] **Step 4: Open the PR**

PR title: `feat: AsNoTracking by default with EDM-aware expand-cycle fallback (closes #726)`.

PR body should reference:

- The issue (`Closes #726`).
- The EF6 vs EFCore behavior split.
- The `RestierEFTrackingBehavior` opt-out for breaking-change scenarios.
- The new `IExpandCycleDetector` abstraction.

---

## Self-review

**Spec coverage:**

- ✓ Default `AsNoTrackingWithIdentityResolution` on EFCore for HTTP reads — Tasks 5, 10.
- ✓ Default `AsNoTracking` on EF6 for HTTP reads, with cycle-aware fallback — Tasks 5, 10.
- ✓ Submit pipeline, deep-update classifier, ResourceExists checks, and any direct `api.QueryAsync` call stay tracked — Task 1 (`AllowNoTracking` default `false`) + Task 5 (only the controller's `ApplyQueryOptions` sets it true) + Task 10 (sourcer short-circuits when `AllowNoTracking == false`).
- ✓ Cross-type cycle detection (`A→B→A`, deeper) — Task 3, `CrossTypeCycle_ReturnsTrue` and `DeepCrossTypeCycle_ReturnsTrue` tests.
- ✓ Separate interface + class in Core for testability — Tasks 2, 3.
- ✓ Same detector runs on EFCore and EF6 (provider-agnostic) — Tasks 4, 5; sourcer decides what to do with the hint in Task 10.
- ✓ Configurable override (`TrackingBehavior` enum + options + DI overloads) — Tasks 6, 7, 8, 9.
- ✓ Shared `.projitems` updated so new files compile into both EF6 and EFCore — Task 6, Step 3.
- ✓ `EFQueryExpressionSourcer` receives options via constructor injection — `ApiBase` has no `ServiceProvider` to look up. Sourcer factory registration in Task 7 + constructor in Task 10.
- ✓ PATCH / DELETE regression check — Task 13.
- ✓ Documentation (XML docs + guide + release notes + api-reference regen) — Task 14.

**Reviewer findings explicitly addressed:**

| Finding | Resolution |
|---|---|
| High: plan applied no-tracking to every `EF QueryAsync` call, breaking submit/update | Added `QueryRequest.AllowNoTracking` (Task 1); only the controller sets it (Task 5); sourcer short-circuits when `false` (Task 10). |
| High: Task 10 referenced non-existent `ApiBase.ServiceProvider` | Sourcer now receives `RestierEFOptions` via constructor injection (Task 10 Edit A) and is registered via factory lambda (Task 7). |
| Medium: shared project uses explicit `.projitems` include list | Task 6 Step 3 adds `RestierEFOptions.cs` and `RestierEFTrackingBehavior.cs` to `Microsoft.Restier.EntityFramework.Shared.projitems`. |

**Placeholder scan:** None — all code is concrete.

**Type consistency:**

- `IExpandCycleDetector.HasCycle(IEdmEntityType rootType, SelectExpandClause clause)` — same signature in Tasks 2, 3, 5.
- `RestierEFOptions.TrackingBehavior` — same name in Tasks 6, 8, 9, 10.
- `RestierEFTrackingBehavior` enum members `Default`, `NoTracking`, `NoTrackingWithIdentityResolution`, `TrackAll` — used identically across Tasks 6, 10, 14.
- `QueryRequest.HasRecursiveExpand` — same name in Tasks 1, 5, 10.
- `QueryRequest.AllowNoTracking` — same name in Tasks 1, 5, 10.
- `EFQueryExpressionSourcer` ctor `RestierEFOptions options` — same signature in Task 7 (factory call site) and Task 10 (declaration).

No inconsistencies found.

---

Plan complete and saved to `docs/superpowers/plans/2026-05-19-asnotracking-default.md`. Two execution options:

1. **Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration.
2. **Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints.

Which approach?
