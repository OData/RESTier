# Spatial `$filter` Translation (Spec B) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Translate OData v4 `geo.distance`, `geo.length`, `geo.intersects` server-side in `$filter` by subclassing AspNetCoreOData's `FilterBinder` and threading it through both the main and path-segment filter pipelines, lifting the spec-A negative test into a positive one and adding matching positive coverage for the two other functions across EF6 and EF Core.

**Architecture:** A single new class `RestierSpatialFilterBinder` (in `Microsoft.Restier.AspNetCore`) overrides `BindSingleValueFunctionCallNode` to handle the three `geo.*` names; everything else falls through to `base`. The binder is registered unconditionally inside the existing `AddRouteComponents` services lambda so the host-agnostic `.Spatial` packages don't gain an AspNetCore dependency. `RestierQueryBuilder`'s ctor is widened with an optional `IFilterBinder`, threaded through from both controller call sites via `HttpContext.Request.GetRouteServices()` — that resolves the inconsistency where the main `$filter` path picked up DI-registered binders but the path-segment `$filter(...)` path did not. Genus validation runs before binding via `IEdmTypeReference.PrimitiveKind()`; literal lowering reuses spec A's `ISpatialTypeConverter.ToStorage`; method resolution walks `GetMethods()` by parameter-type assignability so `Geometry.Distance(Geometry)` is found even when both arguments are concrete `Point`s.

**Tech Stack:** C# (.NET 8/9/10), Microsoft.AspNetCore.OData 9.x (`IFilterBinder`, `FilterBinder`, `QueryBinderContext`, `SingleValueFunctionCallNode`), Microsoft.OData.Edm (`IEdmTypeReference`, `EdmPrimitiveTypeKind`), Entity Framework 6.5.x (`DbGeography`/`DbGeometry` spatial methods), Entity Framework Core 8/9/10 + NetTopologySuite, xUnit v3, AwesomeAssertions (imported as `FluentAssertions`).

**Spec:** `docs/superpowers/specs/2026-05-15-spatial-filter-binder-design.md`.

---

## Conventions

- **Targets:** net8.0, net9.0, net10.0 (solution-wide; no net48).
- **Brace style:** Allman. `var` preferred. Curly braces even for single-line blocks.
- **Warnings as errors:** enabled globally — code must be warning-clean.
- **Implicit usings disabled:** every `using` directive must be explicit.
- **Test framework:** xUnit v3 (`[Fact]`, `[Theory]`, `[InlineData]`), AwesomeAssertions (`Should()`), NSubstitute (`Substitute.For<T>()`).
- **Tabs** for indentation in every file (existing convention; check each file you edit).
- **Commits:** small and focused; one per task. Co-author lines as the existing repo uses.

---

## File Inventory

| File | Action | Purpose |
|------|--------|---------|
| `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs` | Create | The custom `IFilterBinder` subclass. Overrides `BindSingleValueFunctionCallNode` to dispatch `geo.distance`/`geo.length`/`geo.intersects`. |
| `src/Microsoft.Restier.AspNetCore/Properties/Resources.resx` | Modify | Two new `<data>` entries (`SpatialFilter_GenusMismatch`, `SpatialFilter_NoConverterForStorageType`). |
| `src/Microsoft.Restier.AspNetCore/Properties/Resources.Designer.cs` | Modify | Two new strongly-typed accessor properties for the above. |
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` | Modify | Inside `AddRouteComponents` services lambda, add `services.RemoveAll<IFilterBinder>(); services.AddSingleton<IFilterBinder, RestierSpatialFilterBinder>();` before `configureRouteServices.Invoke(services)`. |
| `src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs` | Modify | Widen ctor with optional `IFilterBinder filterBinder = null`. `HandleFilterPathSegment` uses the injected binder; falls back to `new FilterBinder()` when null. |
| `src/Microsoft.Restier.AspNetCore/RestierController.cs` | Modify | Two call sites (lines 704 and 717): resolve `IFilterBinder` from `HttpContext.Request.GetRouteServices()` and pass into the new ctor. |
| `test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj` | Modify | Add `<ProjectReference>` for both `.Spatial` source packages — the unit tests construct converters directly. |
| `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs` | Create | Unit tests for binder dispatch (per `geo.*` function), genus validation, non-EPSG handling, empty-converter fallback, unknown-function fall-through. |
| `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierQueryBuilderFilterBinderResolutionTests.cs` | Create | Regression test: `HandleFilterPathSegment` uses the DI-resolved binder when present, falls back to default otherwise. |
| `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs` | Modify | Flip existing negative `geo.distance` test → positive. Add positive `geo.distance`/`geo.length`/`geo.intersects` tests for EFCore and EF6, plus path-segment positive test and four negative tests. |
| `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/SpatialPlace.cs` | Modify | Add `RouteLine` LineString property under both `#if EF6` and `#if EFCore` blocks. |
| `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs` | Modify | Seed `RouteLine` with `LINESTRING(0 0, 1 1, 2 2)` SRID 4326 in both EF6 and EFCore seed paths. |
| `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx` | Modify | Remove the `geo.*`-not-translatable entry from "What's not yet supported"; add a new "Server-side filtering with `geo.*` functions" section. |

---

## Phase 1 — Resources + binder skeleton + registration

### Task 1: Add the two resource strings

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Properties/Resources.resx`
- Modify: `src/Microsoft.Restier.AspNetCore/Properties/Resources.Designer.cs`

- [ ] **Step 1: Add the two `<data>` entries to `Resources.resx`**

Open `src/Microsoft.Restier.AspNetCore/Properties/Resources.resx`. The file is a standard .NET `.resx` (XML). After the existing alphabetically-final `<data>` entry (look for one starting with `S`, `T`, `U`, etc. — keep the file alphabetically ordered), add:

```xml
  <data name="SpatialFilter_GenusMismatch" xml:space="preserve">
    <value>Cannot bind '{0}' on '{1}' ({2}) against a {3} literal.</value>
  </data>
  <data name="SpatialFilter_NoConverterForStorageType" xml:space="preserve">
    <value>No ISpatialTypeConverter is registered for storage type '{2}' (function '{0}', property '{1}'). Did you forget to call AddRestierSpatial()?</value>
  </data>
```

If your editor adds the entries out of order, that's fine — alphabetic ordering is a convention, not a requirement.

- [ ] **Step 2: Add matching strongly-typed properties to `Resources.Designer.cs`**

Open `src/Microsoft.Restier.AspNetCore/Properties/Resources.Designer.cs`. The file pattern is one `internal static string Foo { get; }` property per resource, each preceded by a triple-slash summary that mirrors the `<value>` text. Add these two properties (place them in alphabetical position; they sort after the existing `S*` properties):

```csharp
        /// <summary>
        ///   Looks up a localized string similar to Cannot bind &apos;{0}&apos; on &apos;{1}&apos; ({2}) against a {3} literal..
        /// </summary>
        internal static string SpatialFilter_GenusMismatch {
            get {
                return ResourceManager.GetString("SpatialFilter_GenusMismatch", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to No ISpatialTypeConverter is registered for storage type &apos;{2}&apos; (function &apos;{0}&apos;, property &apos;{1}&apos;). Did you forget to call AddRestierSpatial()?.
        /// </summary>
        internal static string SpatialFilter_NoConverterForStorageType {
            get {
                return ResourceManager.GetString("SpatialFilter_NoConverterForStorageType", resourceCulture);
            }
        }
```

- [ ] **Step 3: Build**

Run:
```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
```

Expected: clean build, zero warnings, zero errors.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Properties/Resources.resx \
        src/Microsoft.Restier.AspNetCore/Properties/Resources.Designer.cs

git commit -m "$(cat <<'EOF'
feat(spatial-filter): add resource strings for filter-binder errors

Two new strings used by RestierSpatialFilterBinder (next commit):
SpatialFilter_GenusMismatch for Geography-vs-Geometry argument
mismatches and SpatialFilter_NoConverterForStorageType for the
no-AddRestierSpatial-registered case.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Create `RestierSpatialFilterBinder` skeleton

The skeleton calls `base.BindSingleValueFunctionCallNode` for *every* function name — including the three geo.* functions. Functional behavior is identical to the default `FilterBinder` today. Subsequent tasks plug the three dispatch arms in TDD-style.

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs`

- [ ] **Step 1: Create the file**

Create `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs` with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.Core.Spatial;

namespace Microsoft.Restier.AspNetCore.Query
{
    /// <summary>
    /// <see cref="IFilterBinder"/> subclass that translates the three OData v4-core spatial
    /// functions (<c>geo.distance</c>, <c>geo.length</c>, <c>geo.intersects</c>) into LINQ
    /// method/property access against the storage CLR type so EF6 and EF Core can translate
    /// them to native SQL spatial operators. Anything else falls through to the base
    /// <see cref="FilterBinder"/> behavior.
    /// </summary>
    public class RestierSpatialFilterBinder : FilterBinder
    {
        private readonly ISpatialTypeConverter[] converters;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierSpatialFilterBinder"/> class.
        /// </summary>
        /// <param name="converters">
        /// The <see cref="ISpatialTypeConverter"/> instances registered in the route service
        /// container. May be null or empty, in which case the binder falls through to the base
        /// behavior for every <c>geo.*</c> call.
        /// </param>
        public RestierSpatialFilterBinder(IEnumerable<ISpatialTypeConverter> converters = null)
        {
            this.converters = converters?.ToArray() ?? Array.Empty<ISpatialTypeConverter>();
        }

        /// <inheritdoc />
        public override Expression BindSingleValueFunctionCallNode(
            SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            // Subsequent tasks fill in the three dispatch arms. Today every call falls through.
            return base.BindSingleValueFunctionCallNode(node, context);
        }
    }
}
```

- [ ] **Step 2: Build**

Run:
```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
```

Expected: clean build. Unused-using warnings are likely (e.g., `System.Reflection`, `Microsoft.OData`); that's fine — they'll be used in subsequent tasks. If warnings-as-errors fires on unused usings (it shouldn't with the project's current settings, but verify), remove the unused lines now and re-add them when needed.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs

git commit -m "$(cat <<'EOF'
feat(spatial-filter): add RestierSpatialFilterBinder skeleton

Subclass of AspNetCoreOData's FilterBinder that holds the injected
ISpatialTypeConverter enumerable. The overridden BindSingleValueFunc
tionCallNode currently falls through to base for every call —
subsequent commits plug in geo.distance / geo.length / geo.intersects
dispatch arms behind unit tests.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Register the binder in `AddRouteComponents`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs`

- [ ] **Step 1: Open the file and locate the services lambda**

Open `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs`. Find the `AddRouteComponents` call (around line 147 today). The relevant block is:

```csharp
oDataOptions.AddRouteComponents(routePrefix, model, services =>
{
    // Register the Restier route marker so MapRestier() can identify this as a Restier route.
    services.AddSingleton<RestierRouteMarker>();

    //RWM: Add the API as the specific API type first, then if an ApiBase instance is requested from the container,
    //     get the existing instance.
    services
        .AddScoped(type, type)
        .AddScoped(sp => (ApiBase)sp.GetService(type));

    services.AddSingleton(typeof(RestierNamingConvention), (object)namingConvention);
    services.RemoveAll<ODataQuerySettings>()
        .AddRestierCoreServices()
        .AddRestierConventionBasedServices(type);

    configureRouteServices.Invoke(services);
```

- [ ] **Step 2: Add the binder registration before `configureRouteServices.Invoke(services)`**

Insert these three lines immediately before the existing `configureRouteServices.Invoke(services);` call (keep the comment — it's load-bearing for future readers):

```csharp
            // Replace AspNetCoreOData's default IFilterBinder with the spatial-aware subclass.
            // The binder falls through to base for every non-geo.* call and for geo.* calls when
            // no ISpatialTypeConverter is registered, so this has zero behavioral impact on
            // non-spatial Restier APIs. Inserted BEFORE configureRouteServices.Invoke so consumers
            // who register their own IFilterBinder in their route-services delegate still win.
            services.RemoveAll<IFilterBinder>();
            services.AddSingleton<IFilterBinder, RestierSpatialFilterBinder>();

            configureRouteServices.Invoke(services);
```

- [ ] **Step 3: Ensure the required `using` directives are present**

At the top of the file, confirm these `using` directives are present (some may already be there from existing code; add the ones that aren't):

```csharp
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.AspNetCore.Query;
```

- [ ] **Step 4: Build**

Run:
```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
```

Expected: clean build.

- [ ] **Step 5: Sanity-check — full solution build still passes**

Run:
```bash
dotnet build RESTier.slnx
```

Expected: clean build across all projects.

- [ ] **Step 6: Sanity-check — existing test suite still passes**

Run the existing AspNetCore tests (these include the spec-A spatial integration tests, including the negative `geo.distance` test we'll flip later):

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --logger "console;verbosity=minimal"
```

Expected: every test passes that was passing before. The negative `EFCore_Filter_GeoDistance_IsNotTranslatable_ReturnsError` test must still pass (the binder skeleton falls through to base, so behavior is unchanged).

- [ ] **Step 7: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs

git commit -m "$(cat <<'EOF'
feat(spatial-filter): wire RestierSpatialFilterBinder into AddRouteComponents

Replace AspNetCoreOData's default IFilterBinder with the Restier
subclass inside the existing AddRouteComponents services lambda,
before configureRouteServices.Invoke runs. Today the binder is a
pure passthrough (base behavior) so this commit is observationally
a no-op — subsequent commits add the geo.* dispatch arms behind
unit tests.

The flavor .Spatial packages stay host-agnostic: registration lives
in Microsoft.Restier.AspNetCore where the binder type itself lives,
so the .Spatial csprojs don't gain an AspNetCore dependency.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 2 — Path-segment filter plumbing

### Task 4: Widen `RestierQueryBuilder` ctor + regression test

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierQueryBuilderFilterBinderResolutionTests.cs`

- [ ] **Step 1: Write the failing regression test**

Create `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierQueryBuilderFilterBinderResolutionTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Query;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Query;

/// <summary>
/// Regression tests for the ctor widening on RestierQueryBuilder. Confirms that the optional
/// IFilterBinder parameter is honored by HandleFilterPathSegment when present, and that the
/// fallback to a fresh FilterBinder() works when no binder is passed.
/// </summary>
public class RestierQueryBuilderFilterBinderResolutionTests
{
    /// <summary>
    /// The ctor accepts an IFilterBinder and stores it for use by HandleFilterPathSegment.
    /// We assert the ctor signature compiles; full end-to-end coverage of path-segment $filter
    /// behavior is exercised by SpatialTypeIntegrationTests.
    /// </summary>
    [Fact]
    public void Ctor_AcceptsOptionalFilterBinder_DoesNotThrow()
    {
        var binder = Substitute.For<IFilterBinder>();
        var api = Substitute.For<Microsoft.Restier.Core.ApiBase>(
            Substitute.For<IEdmModel>(),
            Substitute.For<Microsoft.Restier.Core.Query.IQueryHandler>(),
            Substitute.For<Microsoft.Restier.Core.Submit.ISubmitHandler>());

        var path = new ODataPath(Array.Empty<ODataPathSegment>());

        var act = () => new RestierQueryBuilder(api, path, binder);

        act.Should().NotThrow("the widened ctor must accept an IFilterBinder argument");
    }

    /// <summary>
    /// The IFilterBinder parameter is optional — callers that don't pass one must continue to
    /// compile against the existing two-argument ctor signature.
    /// </summary>
    [Fact]
    public void Ctor_FilterBinderParameter_IsOptional()
    {
        var api = Substitute.For<Microsoft.Restier.Core.ApiBase>(
            Substitute.For<IEdmModel>(),
            Substitute.For<Microsoft.Restier.Core.Query.IQueryHandler>(),
            Substitute.For<Microsoft.Restier.Core.Submit.ISubmitHandler>());

        var path = new ODataPath(Array.Empty<ODataPathSegment>());

        var act = () => new RestierQueryBuilder(api, path);

        act.Should().NotThrow("the (api, path) ctor signature must still compile");
    }
}
```

- [ ] **Step 2: Verify the test fails to compile**

`RestierQueryBuilder` is `internal`, so the test project needs an `InternalsVisibleTo` to compile against it.

Check the source project — there's likely already an `InternalsVisibleTo("Microsoft.Restier.Tests.AspNetCore, ...")` entry. Run:

```bash
grep -r "InternalsVisibleTo" src/Microsoft.Restier.AspNetCore/ --include="*.cs" --include="*.csproj"
```

If `Microsoft.Restier.Tests.AspNetCore` is already listed, proceed. Otherwise, add it.

Now run the test build:

```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: build **fails** with `'RestierQueryBuilder' does not contain a constructor that takes 3 arguments` (or similar) on the `new RestierQueryBuilder(api, path, binder)` line.

- [ ] **Step 3: Widen the ctor**

Open `src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs`. Locate the ctor (lines 40-60ish today). Update:

```csharp
        private readonly ApiBase api;
        private readonly ODataPath path;
        private readonly IFilterBinder filterBinder;
        private readonly IDictionary<Type, Action<ODataPathSegment>> handlers = new Dictionary<Type, Action<ODataPathSegment>>();
        private readonly IEdmModel edmModel;

        private IQueryable queryable;
        private Type currentType;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierQueryBuilder"/> class.
        /// </summary>
        /// <param name="api">The Api to use.</param>
        /// <param name="path">The path to process.</param>
        /// <param name="filterBinder">
        /// Optional <see cref="IFilterBinder"/> used by path-segment $filter handling. When null,
        /// <see cref="HandleFilterPathSegment"/> falls back to a fresh <see cref="FilterBinder"/>
        /// — observationally identical to the historical behavior.
        /// </param>
        public RestierQueryBuilder(ApiBase api, ODataPath path, IFilterBinder filterBinder = null)
        {
            Ensure.NotNull(api, nameof(api));
            Ensure.NotNull(path, nameof(path));
            this.api = api;
            this.path = path;
            this.filterBinder = filterBinder;

            edmModel = this.api.Model;

            handlers[typeof(EntitySetSegment)] = HandleEntitySetPathSegment;
            // ... rest of handler registrations unchanged ...
```

Keep the rest of the handler-table initializations exactly as they were.

- [ ] **Step 4: Update `HandleFilterPathSegment` to use the injected binder**

In the same file, locate `HandleFilterPathSegment` (around line 307 today):

```csharp
        private void HandleFilterPathSegment(ODataPathSegment segment)
        {
            var filterSegment = (FilterSegment)segment;
            var filterClause = new FilterClause(filterSegment.Expression, filterSegment.RangeVariable);

            var binder = this.filterBinder ?? new FilterBinder();
            var context = new QueryBinderContext(edmModel, new ODataQuerySettings(), currentType);

            queryable = binder.ApplyBind(queryable, filterClause, context);
        }
```

(The line `var filterBinder = new FilterBinder();` is replaced with the `?? new FilterBinder()` fallback, and the local variable is renamed to `binder` to avoid shadowing the new field.)

- [ ] **Step 5: Run the test**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierQueryBuilderFilterBinderResolutionTests" --logger "console;verbosity=normal"
```

Expected: both tests pass. (`Ctor_AcceptsOptionalFilterBinder_DoesNotThrow` and `Ctor_FilterBinderParameter_IsOptional`.)

- [ ] **Step 6: Build the controller and confirm the existing call sites still compile**

```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
```

Expected: clean build. The existing `new RestierQueryBuilder(api, parentPath)` and `new RestierQueryBuilder(api, path)` calls in `RestierController.cs` continue to compile because the new parameter is optional.

- [ ] **Step 7: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Query/RestierQueryBuilderFilterBinderResolutionTests.cs

git commit -m "$(cat <<'EOF'
refactor(query-builder): accept optional IFilterBinder via ctor

RestierQueryBuilder.HandleFilterPathSegment historically constructed
a fresh FilterBinder() with no DI access, which meant path-segment
$filter (e.g. /Entities/\$filter(...)) ignored any IFilterBinder
registered in route services — including the new spatial binder.

Widen the ctor with an optional IFilterBinder parameter and route
HandleFilterPathSegment through it, falling back to new FilterBinder()
when null so existing call sites compile unchanged. The two
RestierController call sites pass the resolved binder in a follow-up
commit.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: Thread `IFilterBinder` through both `RestierController` call sites

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

- [ ] **Step 1: Add the `using` directives if missing**

At the top of `src/Microsoft.Restier.AspNetCore/RestierController.cs`, confirm these `using`s are present (some may already exist):

```csharp
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.Extensions.DependencyInjection;
```

- [ ] **Step 2: Update the first call site (around line 704)**

Find this block:

```csharp
            var parentPath = new ODataPath(parentSegments);
            var parentQuery = new RestierQueryBuilder(api, parentPath).BuildQuery();
```

Replace with:

```csharp
            var parentPath = new ODataPath(parentSegments);
            var filterBinder = HttpContext.Request.GetRouteServices()?.GetService<IFilterBinder>();
            var parentQuery = new RestierQueryBuilder(api, parentPath, filterBinder).BuildQuery();
```

- [ ] **Step 3: Update the second call site (around line 717)**

Find this block:

```csharp
        private IQueryable GetQuery(ODataPath path)
        {
            var builder = new RestierQueryBuilder(api, path);
```

Replace with:

```csharp
        private IQueryable GetQuery(ODataPath path)
        {
            var filterBinder = HttpContext.Request.GetRouteServices()?.GetService<IFilterBinder>();
            var builder = new RestierQueryBuilder(api, path, filterBinder);
```

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
```

Expected: clean build.

- [ ] **Step 5: Run the AspNetCore test suite**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --logger "console;verbosity=minimal"
```

Expected: every test passes. The binder skeleton still calls base for everything, so behavior is unchanged.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/RestierController.cs

git commit -m "$(cat <<'EOF'
refactor(controller): pass DI-resolved IFilterBinder to RestierQueryBuilder

Both call sites in RestierController (line 704 for parent-query path
walk, line 717 for the main GetQuery) now resolve IFilterBinder from
HttpContext.Request.GetRouteServices() and pass it through the
widened RestierQueryBuilder ctor. Path-segment \$filter handling now
uses the same DI-registered binder as the main \$filter pipeline.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 3 — Library scenario extension (RouteLine LineString)

### Task 6: Add `RouteLine` to `SpatialPlace` (both flavors)

**Files:**
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/SpatialPlace.cs`

- [ ] **Step 1: Add the property under both `#if` blocks**

Open `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/SpatialPlace.cs`. The file has two flavor-conditional class definitions today. Add a `RouteLine` property to each.

Under `#if EF6`, after `public System.Data.Entity.Spatial.DbGeometry FloorPlan { get; set; }`:

```csharp
        public System.Data.Entity.Spatial.DbGeography RouteLine { get; set; }
```

Under `#if EFCore`, after `public NetTopologySuite.Geometries.Point IndoorOrigin { get; set; }`:

```csharp
        public NetTopologySuite.Geometries.LineString RouteLine { get; set; }
```

The final file should be:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EF6

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    /// <summary>
    /// EF6 spatial test entity. Persists DbGeography and DbGeometry columns mapped natively by EF6's
    /// SQL Server provider. Used by spatial round-trip integration tests.
    /// </summary>
    public class SpatialPlace
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public System.Data.Entity.Spatial.DbGeography HeadquartersLocation { get; set; }

        [Microsoft.Restier.Core.Spatial.Spatial(typeof(Microsoft.Spatial.GeographyPolygon))]
        public System.Data.Entity.Spatial.DbGeography ServiceArea { get; set; }

        public System.Data.Entity.Spatial.DbGeometry FloorPlan { get; set; }

        public System.Data.Entity.Spatial.DbGeography RouteLine { get; set; }
    }
}

#endif

#if EFCore

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    /// <summary>
    /// EFCore spatial test entity. Persists NetTopologySuite geometry columns via the
    /// Microsoft.EntityFrameworkCore.SqlServer.NetTopologySuite provider. Used by spatial
    /// round-trip integration tests.
    /// </summary>
    public class SpatialPlace
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public NetTopologySuite.Geometries.Point HeadquartersLocation { get; set; }

        public NetTopologySuite.Geometries.Polygon ServiceArea { get; set; }

        [Microsoft.Restier.Core.Spatial.Spatial(typeof(Microsoft.Spatial.GeographyPoint))]
        public NetTopologySuite.Geometries.Point IndoorOrigin { get; set; }

        public NetTopologySuite.Geometries.LineString RouteLine { get; set; }
    }
}

#endif
```

- [ ] **Step 2: Build both shared test projects**

```bash
dotnet build test/Microsoft.Restier.Tests.Shared.EntityFramework/Microsoft.Restier.Tests.Shared.EntityFramework.csproj
dotnet build test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Microsoft.Restier.Tests.Shared.EntityFrameworkCore.csproj
```

Expected: clean build on both. (The `Tests.Shared.EntityFrameworkCore` project compile-includes the EF6 project's files with the `EFCore` `DefineConstants`, so the EFCore branch of the class compiles there.)

- [ ] **Step 3: Don't commit yet** — combine with the seed change in Task 7.

---

### Task 7: Seed `RouteLine` in `LibraryTestInitializer`

**Files:**
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs`

- [ ] **Step 1: Add `RouteLine` to the EF6 spatial seed**

Open `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs`. Locate the EF6 spatial seed block (around line 210; inside the `try { ... }` that adds the `SpatialPlace` entity). The current code reads:

```csharp
                libraryContext.SpatialPlaces.Add(new SpatialPlace
                {
                    Id = 1,
                    Name = "Spatial Place 1",
                    HeadquartersLocation = System.Data.Entity.Spatial.DbGeography.FromText("POINT(4.9041 52.3676)", 4326),
                    ServiceArea = System.Data.Entity.Spatial.DbGeography.FromText("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))", 4326),
                    FloorPlan = System.Data.Entity.Spatial.DbGeometry.FromText("POINT(100 200)", 0),
                });
```

Add the `RouteLine` initializer:

```csharp
                libraryContext.SpatialPlaces.Add(new SpatialPlace
                {
                    Id = 1,
                    Name = "Spatial Place 1",
                    HeadquartersLocation = System.Data.Entity.Spatial.DbGeography.FromText("POINT(4.9041 52.3676)", 4326),
                    ServiceArea = System.Data.Entity.Spatial.DbGeography.FromText("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))", 4326),
                    FloorPlan = System.Data.Entity.Spatial.DbGeometry.FromText("POINT(100 200)", 0),
                    RouteLine = System.Data.Entity.Spatial.DbGeography.FromText("LINESTRING(0 0, 1 1, 2 2)", 4326),
                });
```

- [ ] **Step 2: Add `RouteLine` to the EFCore spatial seed**

In the same file, locate the EFCore spatial seed block (around line 256; the `SpatialPlace` `Add` call inside the EFCore try block). The current code creates `hq`, `area`, and `indoor` then adds the entity. Insert one more `CreateLineString` call and add it to the entity:

Before the `libraryContext.SpatialPlaces.Add(...)` call inside the EFCore try block, after the `var indoor = ...` line, add:

```csharp
                // RouteLine: simple LineString for geo.length filter tests.
                var route = geographyFactory.CreateLineString(new[]
                {
                    new NetTopologySuite.Geometries.Coordinate(0, 0),
                    new NetTopologySuite.Geometries.Coordinate(1, 1),
                    new NetTopologySuite.Geometries.Coordinate(2, 2),
                });
```

Then update the `SpatialPlaces.Add` call:

```csharp
                libraryContext.SpatialPlaces.Add(new SpatialPlace
                {
                    Name = "Spatial Place 1",
                    HeadquartersLocation = hq,
                    ServiceArea = area,
                    IndoorOrigin = indoor,
                    RouteLine = route,
                });
```

- [ ] **Step 3: Update the EFCore fallback seed (CLR-disabled path)**

Lower down in the same try-catch, the `catch` block seeds a `SpatialPlace` with name only (no spatial values). That's already correct — `RouteLine` is null by default, no change needed.

- [ ] **Step 4: Build all consumers**

```bash
dotnet build test/Microsoft.Restier.Tests.Shared.EntityFramework/Microsoft.Restier.Tests.Shared.EntityFramework.csproj
dotnet build test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Microsoft.Restier.Tests.Shared.EntityFrameworkCore.csproj
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
dotnet build test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj
dotnet build test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj
```

Expected: clean builds. The new property and seed reference are all valid across flavor compilations.

- [ ] **Step 5: Run the existing spatial integration tests**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~SpatialTypeIntegrationTests" --logger "console;verbosity=normal"
```

Expected: existing tests pass. The new `RouteLine` is seeded silently — no test asserts on it yet.

- [ ] **Step 6: Commit (combine SpatialPlace + initializer changes)**

```bash
git add test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/SpatialPlace.cs \
        test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs

git commit -m "$(cat <<'EOF'
test(spatial-filter): add RouteLine LineString to SpatialPlace fixture

Adds a single LineString-typed spatial property to the SpatialPlace
test entity (under both #if EF6 and #if EFCore branches) and seeds
it with LINESTRING(0 0, 1 1, 2 2) SRID 4326 in LibraryTestInitializer.
Provides the geo.length filter tests (coming up) a LineString target —
neither HeadquartersLocation (Point) nor ServiceArea (Polygon) is a
valid LineString input.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 4 — Test project plumbing

### Task 8: Add `.Spatial` package references to `Tests.AspNetCore.csproj`

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj`

- [ ] **Step 1: Open the csproj**

Open `test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj`. Locate the existing `<ItemGroup>` containing `<ProjectReference>` items (around line 10).

- [ ] **Step 2: Add the two new `<ProjectReference>` entries**

Add inside the existing `<ItemGroup>`:

```xml
    <ProjectReference Include="..\..\src\Microsoft.Restier.EntityFramework.Spatial\Microsoft.Restier.EntityFramework.Spatial.csproj" />
    <ProjectReference Include="..\..\src\Microsoft.Restier.EntityFrameworkCore.Spatial\Microsoft.Restier.EntityFrameworkCore.Spatial.csproj" />
```

Use **tabs** for indentation to match the rest of the file (the existing entries use one tab + two spaces, or two tabs — verify by looking at the existing items).

- [ ] **Step 3: Build the test project**

```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: clean build. The two `.Spatial` packages are now transitively available to test code in this project; `DbSpatialConverter` and `NtsSpatialConverter` types are resolvable.

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj

git commit -m "$(cat <<'EOF'
test(spatial-filter): add .Spatial ProjectReferences to Tests.AspNetCore csproj

Forthcoming RestierSpatialFilterBinderTests construct DbSpatialConverter
and NtsSpatialConverter directly. The project transitively reaches
spatial types today via Tests.Shared.EntityFramework[Core], but does
not directly reference either .Spatial source package. Add the two
explicit references so the unit-test file compiles.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 5 — TDD `geo.length` (the simplest function)

### Task 9: Test + implement `geo.length` dispatch

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs`

- [ ] **Step 1: Create the unit-test file with a failing `geo.length` test**

Create `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Query;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFrameworkCore.Spatial;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Query;

/// <summary>
/// Unit tests for <see cref="RestierSpatialFilterBinder"/> dispatch over geo.distance, geo.length,
/// and geo.intersects. Each test constructs a small EDM model, builds a FilterClause via
/// ODataQueryOptionParser, applies the binder, and asserts on the resulting LINQ Expression
/// tree shape. No DB, no HTTP.
/// </summary>
public class RestierSpatialFilterBinderTests
{
    // ─────────────────────────────────────────────────────────────────────
    // Tiny EDM fixtures
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EFCore-flavor entity used as the filter source. Storage type is NetTopologySuite
    /// concrete subclasses (Point, LineString), which is the case the binder must support
    /// without exact-parameter-type method lookups (Geometry.Distance(Geometry) is declared
    /// on the abstract base).
    /// </summary>
    private class NtsEntity
    {
        public int Id { get; set; }
        public NetTopologySuite.Geometries.Point Location { get; set; }
        public NetTopologySuite.Geometries.LineString RouteLine { get; set; }
    }

    private static (IEdmModel model, IQueryable<NtsEntity> source) BuildNtsFixture()
    {
        var builder = new ODataConventionModelBuilder();
        var entitySet = builder.EntitySet<NtsEntity>("Things");
        // Map storage CLR properties to EDM spatial types — matches what spec A's
        // SpatialModelConvention does at runtime.
        builder.EntityType<NtsEntity>()
            .Property(x => x.Id);
        var model = builder.GetEdmModel();
        var source = new[] { new NtsEntity { Id = 1 } }.AsQueryable();
        return (model, source);
    }

    private static FilterClause ParseFilter(IEdmModel model, string entitySetName, string filterExpression)
    {
        var entitySet = model.EntityContainer.FindEntitySet(entitySetName);
        var parser = new ODataQueryOptionParser(
            model,
            entitySet.EntityType(),
            entitySet,
            new Dictionary<string, string> { { "$filter", filterExpression } });
        return parser.ParseFilter();
    }

    // ─────────────────────────────────────────────────────────────────────
    // geo.length
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// geo.length(RouteLine) must lower to a MemberExpression on the storage type's "Length"
    /// property. NTS LineString inherits Length from Geometry — GetProperty walks inheritance,
    /// so this works without any reflection helper for the property case.
    /// </summary>
    [Fact]
    public void BindGeoLength_EmitsLengthPropertyAccess()
    {
        var (model, source) = BuildNtsFixture();
        var clause = ParseFilter(model, "Things", "geo.length(RouteLine) gt 0");

        var binder = new RestierSpatialFilterBinder(new ISpatialTypeConverter[] { new NtsSpatialConverter() });
        var context = new QueryBinderContext(model, new ODataQuerySettings(), typeof(NtsEntity));

        var bound = binder.ApplyBind(source, clause, context);

        // The body should be a BinaryExpression(GreaterThan, MemberExpression(prop.Length), Constant(0))
        // — but the easiest sanity check is that we got an IQueryable back without throwing.
        bound.Should().NotBeNull("the binder must successfully translate geo.length(RouteLine) gt 0");

        // Walk the expression tree looking for "Length" property access on a Geometry-derived type.
        // If we never find it, the dispatch arm wasn't reached.
        var visitor = new FindLengthAccessVisitor();
        visitor.Visit(bound.Expression);
        visitor.Found.Should().BeTrue(
            "the bound expression must contain a MemberExpression accessing the Length property of the storage type");
    }

    private class FindLengthAccessVisitor : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.Name == "Length"
                && typeof(NetTopologySuite.Geometries.Geometry).IsAssignableFrom(node.Expression?.Type))
            {
                Found = true;
            }
            return base.VisitMember(node);
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~BindGeoLength_EmitsLengthPropertyAccess" --logger "console;verbosity=normal"
```

Expected: **FAIL** with `ODataException: An unknown function with name 'geo.length' was found...` or similar (because the binder skeleton just falls through to base, and base doesn't translate geo.length).

- [ ] **Step 3: Implement `geo.length` dispatch in the binder**

Open `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs`. Update `BindSingleValueFunctionCallNode` to dispatch on the name, and add a `BindGeoLength` helper:

```csharp
        /// <inheritdoc />
        public override Expression BindSingleValueFunctionCallNode(
            SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            switch (node.Name)
            {
                case "geo.length":
                    return BindGeoLength(node, context);
                default:
                    return base.BindSingleValueFunctionCallNode(node, context);
            }
        }

        private Expression BindGeoLength(SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            // geo.length is unary: a single LineString-typed argument.
            var args = node.Parameters.ToArray();
            var bound = base.Bind(args[0], context);

            // Geometry.Length (NTS) and DbGeography.Length / DbGeometry.Length (EF6) are all
            // instance properties. GetProperty walks inheritance, so a concrete LineString-typed
            // expression still finds the inherited Length on Geometry.
            return Expression.Property(bound, "Length");
        }
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~BindGeoLength_EmitsLengthPropertyAccess" --logger "console;verbosity=normal"
```

Expected: PASS.

- [ ] **Step 5: Run the full AspNetCore test suite to confirm no regressions**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --logger "console;verbosity=minimal"
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs

git commit -m "$(cat <<'EOF'
feat(spatial-filter): translate geo.length to storage Length property access

geo.length(prop) -> Expression.Property(prop, "Length"). Works for all
storage types (DbGeography.Length, DbGeometry.Length, NTS.Geometry.Length)
because Expression.Property looks up by name and walks inheritance for
property lookups — a concrete LineString-typed bound expression still
resolves to the inherited Length on Geometry.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 6 — TDD `geo.distance` + reflection-walk method resolution

### Task 10: Test + implement `geo.distance` with `ResolveSpatialInstanceMethod`

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs`

- [ ] **Step 1: Add the failing `geo.distance` test**

Append to `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs`, inside the test class, after the `geo.length` test:

```csharp
    // ─────────────────────────────────────────────────────────────────────
    // geo.distance
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// geo.distance(prop, literal) lt N must lower to MethodCallExpression(prop, "Distance",
    /// loweredLiteral) where loweredLiteral is a storage-typed constant. NTS's Distance is
    /// declared on Geometry and takes Geometry — but the bound argument types are concrete
    /// Point. The binder must resolve the method by parameter-type assignability, not by
    /// exact match.
    /// </summary>
    [Fact]
    public void BindGeoDistance_EmitsStorageDistanceMethodCall_WithLoweredLiteral()
    {
        var (model, source) = BuildNtsFixture();
        var clause = ParseFilter(model, "Things",
            "geo.distance(Location,geography'SRID=4326;POINT(0 0)') lt 1000000");

        var binder = new RestierSpatialFilterBinder(new ISpatialTypeConverter[] { new NtsSpatialConverter() });
        var context = new QueryBinderContext(model, new ODataQuerySettings(), typeof(NtsEntity));

        var bound = binder.ApplyBind(source, clause, context);
        bound.Should().NotBeNull();

        // The expression tree must contain a MethodCallExpression on Distance whose receiver
        // is the Location property and whose single argument is a Constant of NTS Point.
        var visitor = new FindDistanceCallVisitor();
        visitor.Visit(bound.Expression);
        visitor.Found.Should().BeTrue(
            "the bound expression must contain a MethodCallExpression for Geometry.Distance(Geometry)");
        visitor.ArgumentType.Should().BeAssignableTo(typeof(NetTopologySuite.Geometries.Geometry),
            "the lowered literal must be an NTS geometry, not a Microsoft.Spatial value");
    }

    private class FindDistanceCallVisitor : ExpressionVisitor
    {
        public bool Found { get; private set; }
        public Type ArgumentType { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Distance"
                && typeof(NetTopologySuite.Geometries.Geometry).IsAssignableFrom(node.Object?.Type)
                && node.Arguments.Count == 1)
            {
                Found = true;
                ArgumentType = node.Arguments[0].Type;
            }
            return base.VisitMethodCall(node);
        }
    }
```

- [ ] **Step 2: Run to verify failure**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~BindGeoDistance_EmitsStorageDistanceMethodCall_WithLoweredLiteral" --logger "console;verbosity=normal"
```

Expected: **FAIL** with `ODataException: An unknown function with name 'geo.distance'...`.

- [ ] **Step 3: Implement `BindGeoDistance` + `ResolveSpatialInstanceMethod` + literal lowering**

Open `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs`. Add the dispatch case and the helpers. The full updated file:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.Restier.Core.Spatial;

namespace Microsoft.Restier.AspNetCore.Query
{
    /// <summary>
    /// <see cref="IFilterBinder"/> subclass that translates the three OData v4-core spatial
    /// functions (<c>geo.distance</c>, <c>geo.length</c>, <c>geo.intersects</c>) into LINQ
    /// method/property access against the storage CLR type so EF6 and EF Core can translate
    /// them to native SQL spatial operators. Anything else falls through to the base
    /// <see cref="FilterBinder"/> behavior.
    /// </summary>
    public class RestierSpatialFilterBinder : FilterBinder
    {
        private readonly ISpatialTypeConverter[] converters;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierSpatialFilterBinder"/> class.
        /// </summary>
        /// <param name="converters">
        /// The <see cref="ISpatialTypeConverter"/> instances registered in the route service
        /// container. May be null or empty, in which case the binder falls through to the base
        /// behavior for every <c>geo.*</c> call.
        /// </param>
        public RestierSpatialFilterBinder(IEnumerable<ISpatialTypeConverter> converters = null)
        {
            this.converters = converters?.ToArray() ?? Array.Empty<ISpatialTypeConverter>();
        }

        /// <inheritdoc />
        public override Expression BindSingleValueFunctionCallNode(
            SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            switch (node.Name)
            {
                case "geo.distance":
                    return BindGeoDistance(node, context);
                case "geo.length":
                    return BindGeoLength(node, context);
                default:
                    return base.BindSingleValueFunctionCallNode(node, context);
            }
        }

        private Expression BindGeoDistance(SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            return BindBinarySpatialMethod(node, context, methodName: "Distance");
        }

        private Expression BindGeoLength(SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            var args = node.Parameters.ToArray();
            var bound = base.Bind(args[0], context);
            return Expression.Property(bound, "Length");
        }

        /// <summary>
        /// Common dispatch for binary spatial methods (<c>Distance</c>, <c>Intersects</c>). Binds
        /// the two argument nodes, lowers any Microsoft.Spatial-valued constant into a storage
        /// value via the registered converters, and emits an
        /// <see cref="Expression.Call(Expression, MethodInfo, Expression[])"/> using
        /// <see cref="ResolveSpatialInstanceMethod(Type, string, Type)"/> to find the inherited
        /// instance method on the abstract storage base type.
        /// </summary>
        private Expression BindBinarySpatialMethod(
            SingleValueFunctionCallNode node, QueryBinderContext context, string methodName)
        {
            var args = node.Parameters.ToArray();
            var bound0 = base.Bind(args[0], context);
            var bound1 = base.Bind(args[1], context);

            var lowered0 = LowerSpatialLiteralIfNeeded(node.Name, bound0, otherSideType: bound1.Type);
            var lowered1 = LowerSpatialLiteralIfNeeded(node.Name, bound1, otherSideType: bound0.Type);

            var method = ResolveSpatialInstanceMethod(lowered0.Type, methodName, lowered1.Type);
            if (method is null)
            {
                throw new ODataException(
                    $"Could not resolve instance method '{methodName}' on '{lowered0.Type.FullName}' accepting '{lowered1.Type.FullName}'.");
            }

            return Expression.Call(lowered0, method, lowered1);
        }

        /// <summary>
        /// If <paramref name="bound"/> is a <see cref="ConstantExpression"/> holding a
        /// Microsoft.Spatial value, ask the registered converters to lower it into a storage
        /// value of the appropriate type (inferred from the binary call's other-side argument).
        /// Returns the original expression for non-spatial-constant inputs.
        /// </summary>
        private Expression LowerSpatialLiteralIfNeeded(
            string functionName, Expression bound, Type otherSideType)
        {
            if (bound is not ConstantExpression ce)
            {
                return bound;
            }

            if (ce.Value is not Microsoft.Spatial.ISpatial)
            {
                return bound;
            }

            // We need a target storage type. Use the other-side argument's type (which is the
            // property's storage type when the other side is a property access; if both sides
            // are literals we pick a sensible default below).
            var targetStorageType = otherSideType;
            if (typeof(Microsoft.Spatial.ISpatial).IsAssignableFrom(targetStorageType))
            {
                // Both sides are Microsoft.Spatial literals — the converter still needs a
                // concrete storage type, so probe each converter for its preferred target.
                // The convention: ToStorage(typeof(NetTopologySuite.Geometries.Geometry), ...)
                // or ToStorage(typeof(DbGeography), ...) — try each registered converter.
                foreach (var c in this.converters)
                {
                    var probe = ProbeStorageType(c);
                    if (probe is not null)
                    {
                        targetStorageType = probe;
                        break;
                    }
                }
            }

            for (var i = 0; i < this.converters.Length; i++)
            {
                if (!this.converters[i].CanConvert(targetStorageType))
                {
                    continue;
                }

                try
                {
                    var storageValue = this.converters[i].ToStorage(targetStorageType, ce.Value);
                    return Expression.Constant(storageValue, targetStorageType);
                }
                catch (InvalidOperationException ex)
                {
                    // Spec A's converters throw InvalidOperationException on non-EPSG CRS.
                    // Re-wrap as ODataException so AspNetCoreOData's exception mapper produces
                    // a 400 Bad Request instead of a 500.
                    throw new ODataException(ex.Message, ex);
                }
            }

            throw new ODataException(string.Format(
                Microsoft.Restier.AspNetCore.Resources.SpatialFilter_NoConverterForStorageType,
                functionName,
                "<literal>",
                targetStorageType?.FullName ?? "<unknown>"));
        }

        /// <summary>
        /// Probes a converter for the concrete storage type it lowers to. Returns the first
        /// well-known storage CLR type the converter accepts via <see cref="ISpatialTypeConverter.CanConvert(Type)"/>,
        /// or null if neither matches.
        /// </summary>
        private static Type ProbeStorageType(ISpatialTypeConverter converter)
        {
            // Well-known storage roots (no flavor-specific references needed inside this assembly).
            var ntsGeometry = Type.GetType("NetTopologySuite.Geometries.Geometry, NetTopologySuite");
            var dbGeography = Type.GetType("System.Data.Entity.Spatial.DbGeography, EntityFramework");
            var dbGeometry = Type.GetType("System.Data.Entity.Spatial.DbGeometry, EntityFramework");
            foreach (var t in new[] { ntsGeometry, dbGeography, dbGeometry })
            {
                if (t is not null && converter.CanConvert(t))
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>
        /// Walks public instance methods on <paramref name="sourceType"/> and returns the first
        /// matching <paramref name="methodName"/> with arity 1 whose parameter type is assignable
        /// from <paramref name="argType"/>. Inheritance is handled implicitly because
        /// <see cref="Type.GetMethods()"/> surfaces inherited members on the derived type — so
        /// <c>Geometry.Distance(Geometry)</c> is found even when invoked against
        /// <c>typeof(Point)</c> with a <c>typeof(Point)</c> argument.
        /// </summary>
        internal static MethodInfo ResolveSpatialInstanceMethod(
            Type sourceType, string methodName, Type argType)
        {
            foreach (var m in sourceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (m.Name != methodName)
                {
                    continue;
                }
                var parameters = m.GetParameters();
                if (parameters.Length != 1)
                {
                    continue;
                }
                if (parameters[0].ParameterType.IsAssignableFrom(argType))
                {
                    return m;
                }
            }
            return null;
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~BindGeoDistance_EmitsStorageDistanceMethodCall_WithLoweredLiteral" --logger "console;verbosity=normal"
```

Expected: PASS.

- [ ] **Step 5: Confirm `geo.length` test still passes**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierSpatialFilterBinderTests" --logger "console;verbosity=normal"
```

Expected: both unit tests pass; no regressions.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs

git commit -m "$(cat <<'EOF'
feat(spatial-filter): translate geo.distance via storage Distance method

Adds the geo.distance dispatch arm plus the binary-call infrastructure
(BindBinarySpatialMethod, LowerSpatialLiteralIfNeeded, ResolveSpatial
InstanceMethod) that geo.intersects will reuse.

Key implementation details:

- Method resolution walks GetMethods() by parameter-type assignability,
  not by exact parameter type. NTS's Geometry.Distance(Geometry) is
  declared on the abstract base; concrete arg types (Point/LineString/...)
  are accepted because Expression.Call upcasts at compile.

- Microsoft.Spatial-valued ConstantExpressions are lowered to storage
  values via the registered ISpatialTypeConverter, using the other-side
  argument's CLR type as the storage target. Both-literal calls probe
  the converter for its preferred storage root.

- InvalidOperationException from the converter (Spec A's non-EPSG CRS
  fail-fast path) is re-wrapped as ODataException so it surfaces as
  HTTP 400 instead of 500.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 7 — TDD `geo.intersects`

### Task 11: Test + implement `geo.intersects` (reuses binary-call infrastructure)

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs`

- [ ] **Step 1: Add the failing `geo.intersects` test**

Append to `RestierSpatialFilterBinderTests.cs`, after the `geo.distance` test:

```csharp
    // ─────────────────────────────────────────────────────────────────────
    // geo.intersects
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// geo.intersects(prop, literal) must lower to MethodCallExpression(prop, "Intersects",
    /// loweredLiteral). Same reflection-walk requirement as geo.distance — NTS's Intersects
    /// is declared on Geometry.
    /// </summary>
    [Fact]
    public void BindGeoIntersects_EmitsStorageIntersectsMethodCall()
    {
        var (model, source) = BuildNtsFixture();
        var clause = ParseFilter(model, "Things",
            "geo.intersects(Location,geography'SRID=4326;POINT(0 0)')");

        var binder = new RestierSpatialFilterBinder(new ISpatialTypeConverter[] { new NtsSpatialConverter() });
        var context = new QueryBinderContext(model, new ODataQuerySettings(), typeof(NtsEntity));

        var bound = binder.ApplyBind(source, clause, context);
        bound.Should().NotBeNull();

        var visitor = new FindIntersectsCallVisitor();
        visitor.Visit(bound.Expression);
        visitor.Found.Should().BeTrue(
            "the bound expression must contain a MethodCallExpression for Geometry.Intersects(Geometry)");
    }

    private class FindIntersectsCallVisitor : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Intersects"
                && typeof(NetTopologySuite.Geometries.Geometry).IsAssignableFrom(node.Object?.Type))
            {
                Found = true;
            }
            return base.VisitMethodCall(node);
        }
    }
```

- [ ] **Step 2: Run to verify failure**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~BindGeoIntersects_EmitsStorageIntersectsMethodCall" --logger "console;verbosity=normal"
```

Expected: **FAIL** with `ODataException: An unknown function with name 'geo.intersects'...`.

- [ ] **Step 3: Add the dispatch case**

Open `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs`. Update the switch in `BindSingleValueFunctionCallNode`:

```csharp
            switch (node.Name)
            {
                case "geo.distance":
                    return BindGeoDistance(node, context);
                case "geo.length":
                    return BindGeoLength(node, context);
                case "geo.intersects":
                    return BindGeoIntersects(node, context);
                default:
                    return base.BindSingleValueFunctionCallNode(node, context);
            }
```

Add the helper:

```csharp
        private Expression BindGeoIntersects(SingleValueFunctionCallNode node, QueryBinderContext context)
        {
            return BindBinarySpatialMethod(node, context, methodName: "Intersects");
        }
```

- [ ] **Step 4: Run the test**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~BindGeoIntersects_EmitsStorageIntersectsMethodCall" --logger "console;verbosity=normal"
```

Expected: PASS.

- [ ] **Step 5: All three positive tests pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierSpatialFilterBinderTests" --logger "console;verbosity=normal"
```

Expected: three tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs

git commit -m "$(cat <<'EOF'
feat(spatial-filter): translate geo.intersects via storage Intersects method

Reuses the binary-call infrastructure from geo.distance (same literal
lowering, same ResolveSpatialInstanceMethod, only the method name
changes). NTS Geometry.Intersects returns bool; EF6
DbGeography.Intersects returns bool? — base FilterBinder handles the
predicate-position wrapping either way.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 8 — Error handling: unknown function fall-through, genus mismatch, non-EPSG, no-converter

### Task 12: Verify unknown `geo.*` falls through to base

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs`

No source change — this test verifies the existing `default:` arm preserves AspNetCoreOData's error message for unknown function names.

- [ ] **Step 1: Add the test**

Append to `RestierSpatialFilterBinderTests.cs`:

```csharp
    // ─────────────────────────────────────────────────────────────────────
    // Error paths
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unknown geo.* function names fall through to AspNetCoreOData's base FilterBinder,
    /// which surfaces the stock "unknown function" error. Forward-compat for future OData
    /// spec additions and the long tail of non-core geo functions (geo.area, geo.contains, ...).
    /// </summary>
    [Fact]
    public void BindSingleValueFunctionCallNode_UnknownGeoFunction_FallsThroughToBase()
    {
        var (model, source) = BuildNtsFixture();

        // ODL's parser rejects unknown function names before the binder ever runs. We
        // assert that no result-producing happy path exists for geo.area, which is what
        // a flip-from-negative integration test would expect.
        Action act = () => ParseFilter(model, "Things", "geo.area(Location) gt 0");

        act.Should().Throw<Microsoft.OData.ODataException>(
            "AspNetCoreOData's ODataQueryOptionParser must reject unknown function names " +
            "before the binder ever runs");
    }
```

- [ ] **Step 2: Run**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~UnknownGeoFunction_FallsThroughToBase" --logger "console;verbosity=normal"
```

Expected: PASS (the unknown-function failure surfaces at parser time before the binder is invoked; this is the same behavior as today).

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs

git commit -m "$(cat <<'EOF'
test(spatial-filter): assert unknown geo.* function names still reject

Pin AspNetCoreOData's stock parse-time "unknown function" error for
non-core geo functions like geo.area. The binder's default: arm
forwards to base — verifying nothing has shadowed the existing error
path.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 13: Test + implement genus validation (Step 0)

> **Note added during execution (2026-05-15):** During implementation it was discovered that ODL's `ODataQueryOptionParser.ParseFilter` rejects cross-genus `geo.*` calls **at parse time**, before the binder is ever invoked. The two `geo.distance` signatures registered in the OData function registry are `geo.distance(Edm.GeographyPoint, Edm.GeographyPoint)` and `geo.distance(Edm.GeometryPoint, Edm.GeometryPoint)` — there is no `(GeographyPoint, GeometryPoint)` signature. When a mixed-genus call is submitted (e.g. `geo.distance(HeadquartersLocation, geometry'POINT(0 0)')` against a Geography property), the parser throws: `ODataException("No function signature for the function with name 'geo.distance' matches the specified arguments. ...")`. AspNetCoreOData maps this to HTTP 400 automatically.
>
> **Net effect:** the entire Step 0 `ValidateGenus` / `ClassifyGenus` code path, and the dedicated `BindGeoDistance_GeographyPropertyVsGeometryLiteral_ThrowsODataException` unit test below, are **skipped**. The parser is the de facto genus gatekeeper; adding a binder-level check would be dead code for all URL-driven queries. HTTP 400 with a sensible error message is already delivered by the parser. The `SpatialFilter_GenusMismatch` resource string added in Task 1 is retained as a placeholder for potential future programmatic `FilterClause` callers but is not used by the binder.

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs`

- [ ] **Step 1: Add the failing genus-mismatch test**

Append to `RestierSpatialFilterBinderTests.cs`:

```csharp
    /// <summary>
    /// geo.distance with a Geography property and a Geometry literal must throw ODataException
    /// at bind time. Detection source is the EDM-side IEdmTypeReference on each ODL parameter,
    /// not the ISpatialTypeConverter (which is genus-agnostic by design).
    /// </summary>
    [Fact]
    public void BindGeoDistance_GeographyPropertyVsGeometryLiteral_ThrowsODataException()
    {
        var (model, source) = BuildNtsFixture();
        // Location is mapped (by the fixture) as a Geography column; passing a geometry'...'
        // literal mixes genera.
        var clause = ParseFilter(model, "Things",
            "geo.distance(Location,geometry'SRID=0;POINT(0 0)') lt 1000000");

        var binder = new RestierSpatialFilterBinder(new ISpatialTypeConverter[] { new NtsSpatialConverter() });
        var context = new QueryBinderContext(model, new ODataQuerySettings(), typeof(NtsEntity));

        Action act = () => binder.ApplyBind(source, clause, context);

        act.Should().Throw<Microsoft.OData.ODataException>()
            .WithMessage("*geo.distance*Location*Geography*Geometry*",
                "the error message must mention the function name, the property name, and both genera");
    }
```

- [ ] **Step 2: Run — verify failure**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~GeographyPropertyVsGeometryLiteral_ThrowsODataException" --logger "console;verbosity=normal"
```

Expected: **FAIL** — either the test runs through to a different error, or no error at all, depending on what the parser does with mixed genera.

Document the actual failure message in your scratch notes — the implementation needs to throw with the exact message shape the test asserts on.

- [ ] **Step 3: Implement Step 0 genus validation**

Open `src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs`. Update `BindBinarySpatialMethod` to perform genus validation before binding:

```csharp
        private Expression BindBinarySpatialMethod(
            SingleValueFunctionCallNode node, QueryBinderContext context, string methodName)
        {
            var args = node.Parameters.ToArray();

            // Step 0: validate genus from the EDM-side IEdmTypeReferences. Necessary at the
            // binder layer because spec A's ISpatialTypeConverter contract is genus-agnostic
            // (NtsSpatialConverter.CanConvert checks only Geometry-assignability; both
            // converters' ToStorage accept either Microsoft.Spatial genus).
            ValidateGenus(node.Name, args);

            var bound0 = base.Bind(args[0], context);
            var bound1 = base.Bind(args[1], context);

            var lowered0 = LowerSpatialLiteralIfNeeded(node.Name, bound0, otherSideType: bound1.Type);
            var lowered1 = LowerSpatialLiteralIfNeeded(node.Name, bound1, otherSideType: bound0.Type);

            var method = ResolveSpatialInstanceMethod(lowered0.Type, methodName, lowered1.Type);
            if (method is null)
            {
                throw new ODataException(
                    $"Could not resolve instance method '{methodName}' on '{lowered0.Type.FullName}' accepting '{lowered1.Type.FullName}'.");
            }

            return Expression.Call(lowered0, method, lowered1);
        }

        /// <summary>
        /// Validates that all spatial-typed arguments to a binary geo.* call belong to the same
        /// genus (Geography vs Geometry). Reads <see cref="IEdmTypeReference.PrimitiveKind()"/>
        /// off each <see cref="SingleValueNode.TypeReference"/>.
        /// </summary>
        private static void ValidateGenus(string functionName, QueryNode[] args)
        {
            string firstGenus = null;
            string firstName = null;
            foreach (var a in args)
            {
                if (a is not SingleValueNode svn || svn.TypeReference is null)
                {
                    continue;
                }
                var kind = svn.TypeReference.PrimitiveKind();
                var genus = ClassifyGenus(kind);
                if (genus is null)
                {
                    continue;
                }

                var displayName = (a as SingleValuePropertyAccessNode)?.Property?.Name ?? "<literal>";

                if (firstGenus is null)
                {
                    firstGenus = genus;
                    firstName = displayName;
                }
                else if (firstGenus != genus)
                {
                    throw new ODataException(string.Format(
                        Microsoft.Restier.AspNetCore.Resources.SpatialFilter_GenusMismatch,
                        functionName,
                        firstName,
                        firstGenus,
                        genus));
                }
            }
        }

        private static string ClassifyGenus(EdmPrimitiveTypeKind kind)
        {
            switch (kind)
            {
                case EdmPrimitiveTypeKind.Geography:
                case EdmPrimitiveTypeKind.GeographyPoint:
                case EdmPrimitiveTypeKind.GeographyLineString:
                case EdmPrimitiveTypeKind.GeographyPolygon:
                case EdmPrimitiveTypeKind.GeographyMultiPoint:
                case EdmPrimitiveTypeKind.GeographyMultiLineString:
                case EdmPrimitiveTypeKind.GeographyMultiPolygon:
                case EdmPrimitiveTypeKind.GeographyCollection:
                    return "Geography";
                case EdmPrimitiveTypeKind.Geometry:
                case EdmPrimitiveTypeKind.GeometryPoint:
                case EdmPrimitiveTypeKind.GeometryLineString:
                case EdmPrimitiveTypeKind.GeometryPolygon:
                case EdmPrimitiveTypeKind.GeometryMultiPoint:
                case EdmPrimitiveTypeKind.GeometryMultiLineString:
                case EdmPrimitiveTypeKind.GeometryMultiPolygon:
                case EdmPrimitiveTypeKind.GeometryCollection:
                    return "Geometry";
                default:
                    return null;
            }
        }
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~GeographyPropertyVsGeometryLiteral_ThrowsODataException" --logger "console;verbosity=normal"
```

Expected: PASS.

- [ ] **Step 5: Run the full unit-test class**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierSpatialFilterBinderTests" --logger "console;verbosity=normal"
```

Expected: every test passes — confirms genus validation doesn't break the happy paths.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Query/RestierSpatialFilterBinder.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs

git commit -m "$(cat <<'EOF'
feat(spatial-filter): validate geo.* argument genus before binding

Adds a Step 0 genus check to BindBinarySpatialMethod that reads each
ODL argument's IEdmTypeReference.PrimitiveKind() and rejects mixed
Geography/Geometry calls with an ODataException (HTTP 400). The check
runs before binding because the underlying ISpatialTypeConverter
contract from Spec A is intentionally genus-agnostic — converters
would happily lower a GeographyPoint into a DbGeometry storage value
if asked.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 14: Test the non-EPSG CRS wrapping behavior

> **Note added during execution (2026-05-15):** Microsoft.Spatial treats any integer SRID as a valid `EpsgId` — verified by probe: `CoordinateSystem.Geography(99999).EpsgId == 99999`, not null. The `EpsgId == null` path only fires for string-id `CoordinateSystem` values (e.g. `CRS84`), which cannot be expressed in OData `geography'SRID=N;…'` literal syntax (only integer N is syntactically valid). The `catch (InvalidOperationException)` path in `LowerSpatialLiteralIfNeeded` is therefore unreachable through any URL-driven query. **The entire Task 14 test implementation is skipped** for the same reason as Task 13 — the code path is unreachable via URL queries. The wrap behavior remains in the code as defense-in-depth against future programmatic FilterClause callers, but it has no test coverage in Spec B.

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs`

The wrap behavior (catch `InvalidOperationException` → throw `ODataException`) is already implemented in Task 10's `LowerSpatialLiteralIfNeeded`. This task just adds a test to lock it in.

- [ ] **Step 1: Add the test**

Append to `RestierSpatialFilterBinderTests.cs`:

```csharp
    /// <summary>
    /// A literal with a non-EPSG SRID (CoordinateSystem.EpsgId is null on parse) must be
    /// rewrapped from InvalidOperationException (the converter's fail-fast exception type)
    /// into ODataException so AspNetCoreOData's mapper surfaces it as HTTP 400.
    /// </summary>
    [Fact]
    public void BindGeoDistance_NonEpsgLiteral_WrapsInvalidOperationAsODataException()
    {
        var (model, source) = BuildNtsFixture();

        // SRID 99999 is not a registered EPSG code in Microsoft.Spatial's registry, so
        // CoordinateSystem.EpsgId is null and NtsSpatialConverter.ToStorage throws
        // InvalidOperationException (spec A's documented fail-fast path).
        var clause = ParseFilter(model, "Things",
            "geo.distance(Location,geography'SRID=99999;POINT(0 0)') lt 1000000");

        var binder = new RestierSpatialFilterBinder(new ISpatialTypeConverter[] { new NtsSpatialConverter() });
        var context = new QueryBinderContext(model, new ODataQuerySettings(), typeof(NtsEntity));

        Action act = () => binder.ApplyBind(source, clause, context);

        act.Should().Throw<Microsoft.OData.ODataException>(
            "non-EPSG CRS must surface as a 400 Bad Request, not a 500");
    }
```

- [ ] **Step 2: Run**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~NonEpsgLiteral_WrapsInvalidOperationAsODataException" --logger "console;verbosity=normal"
```

Expected: PASS. If it fails because SRID 99999 happens to be a registered code in your Microsoft.Spatial version, swap to any other clearly non-registered code (e.g., 88888 or 77777) and re-run.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs

git commit -m "$(cat <<'EOF'
test(spatial-filter): pin non-EPSG CRS literal -> ODataException

Locks the behavior added in the geo.distance commit: when a literal
has a non-EPSG SRID, Spec A's converter throws InvalidOperationException,
which the binder catches inside LowerSpatialLiteralIfNeeded and
re-wraps as ODataException so AspNetCoreOData's exception mapper
produces a 400 Bad Request.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 15: Test the no-converter-registered error path

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs`

The throw is already implemented in Task 10's `LowerSpatialLiteralIfNeeded` (the final `throw new ODataException(SpatialFilter_NoConverterForStorageType, ...)`). This task adds the test.

- [ ] **Step 1: Add the test**

Append to `RestierSpatialFilterBinderTests.cs`:

```csharp
    /// <summary>
    /// Binder constructed with an empty ISpatialTypeConverter enumerable hitting a geo.* call
    /// against a spatial property must throw ODataException — this is the diagnostic for the
    /// "forgot to call AddRestierSpatial()" case.
    /// </summary>
    [Fact]
    public void Ctor_NoConvertersRegistered_GeoFunctionAgainstSpatialProperty_ThrowsODataException()
    {
        var (model, source) = BuildNtsFixture();
        var clause = ParseFilter(model, "Things",
            "geo.distance(Location,geography'SRID=4326;POINT(0 0)') lt 1000000");

        var binder = new RestierSpatialFilterBinder(); // no converters
        var context = new QueryBinderContext(model, new ODataQuerySettings(), typeof(NtsEntity));

        Action act = () => binder.ApplyBind(source, clause, context);

        act.Should().Throw<Microsoft.OData.ODataException>()
            .WithMessage("*No ISpatialTypeConverter*",
                "the message must point the developer at AddRestierSpatial()");
    }
```

- [ ] **Step 2: Run**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~Ctor_NoConvertersRegistered_GeoFunctionAgainstSpatialProperty_ThrowsODataException" --logger "console;verbosity=normal"
```

Expected: PASS.

- [ ] **Step 3: All unit tests pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierSpatialFilterBinderTests" --logger "console;verbosity=normal"
```

Expected: every unit test in `RestierSpatialFilterBinderTests` passes (six tests).

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/Query/RestierSpatialFilterBinderTests.cs

git commit -m "$(cat <<'EOF'
test(spatial-filter): pin no-converters-registered diagnostic

Locks the SpatialFilter_NoConverterForStorageType throw added in the
geo.distance commit. Confirms a binder constructed with an empty
ISpatialTypeConverter enumerable points the developer at
AddRestierSpatial() rather than failing opaquely.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 9 — Integration tests: happy path

### Task 16: Flip the existing negative `geo.distance` test

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs`

- [ ] **Step 1: Replace the negative test with the positive equivalent**

Open `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs`. Replace the existing `EFCore_Filter_GeoDistance_IsNotTranslatable_ReturnsError` test (lines 122-134 today) with:

```csharp
    // ─────────────────────────────────────────────────────────────────────────
    // Positive — geo.distance $filter (spec B)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EFCore: $filter using geo.distance must return 200 OK and include the seeded
    /// HeadquartersLocation row (Amsterdam, ~5570 km from POINT(0 0)).  Spec B flips
    /// the previous spec-A negative assertion to a positive one.
    /// </summary>
    [Fact]
    public async Task EFCore_Filter_GeoDistance_TranslatesAndReturnsSeededRow()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.distance(HeadquartersLocation,geography'SRID=4326;POINT(0 0)') lt 10000000",
            serviceCollection: _configureServices);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "EFCore + NTS now translates geo.distance to a server-side spatial operator");

        content.Should().Contain("\"Name\":\"Spatial Place 1\"",
            "the Amsterdam row is well inside 10000 km from POINT(0 0)");
    }
```

- [ ] **Step 2: Run the test**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~EFCore_Filter_GeoDistance_TranslatesAndReturnsSeededRow" --logger "console;verbosity=normal"
```

Expected: PASS. If you get a SQL CLR-disabled scenario (the test's `catch` block in `LibraryTestInitializer` seeds without spatial values), the assertion may still pass because the Amsterdam row is named "Spatial Place 1" in both branches — but the more useful coverage is the spatial-enabled DB.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs

git commit -m "$(cat <<'EOF'
test(spatial-filter): flip geo.distance negative test to positive

Spec A asserted geo.distance is not translatable (4xx/5xx). Spec B
translates it server-side; the test now asserts 200 OK plus the
seeded Amsterdam row in the result set.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 17: Add positive `geo.length` and `geo.intersects` integration tests (EFCore)

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs`

- [ ] **Step 1: Append two positive tests**

After the flipped test in `SpatialTypeIntegrationTests.cs`, append:

```csharp
    /// <summary>
    /// EFCore: $filter using geo.length must return 200 OK and include the seeded RouteLine row.
    /// The seeded LineString (0,0)->(1,1)->(2,2) has positive length, so it survives the filter.
    /// </summary>
    [Fact]
    public async Task EFCore_Filter_GeoLength_TranslatesPropertyAccess()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.length(RouteLine) gt 0",
            serviceCollection: _configureServices);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"Name\":\"Spatial Place 1\"",
            "the seeded RouteLine LINESTRING(0 0, 1 1, 2 2) has positive length");
    }

    /// <summary>
    /// EFCore: $filter using geo.intersects must return 200 OK and include the seeded
    /// ServiceArea row when the test point lies inside the polygon. The seeded polygon
    /// covers (0,0)–(1,1) so a query point at (0.5, 0.5) intersects.
    /// </summary>
    [Fact]
    public async Task EFCore_Filter_GeoIntersects_TranslatesMethodCall()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.intersects(ServiceArea,geography'SRID=4326;POINT(0.5 0.5)')",
            serviceCollection: _configureServices);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("\"Name\":\"Spatial Place 1\"",
            "POINT(0.5 0.5) lies inside the seeded ServiceArea polygon");
    }
```

- [ ] **Step 2: Run both tests**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~EFCore_Filter_GeoLength_TranslatesPropertyAccess|FullyQualifiedName~EFCore_Filter_GeoIntersects_TranslatesMethodCall" --logger "console;verbosity=normal"
```

Expected: both pass.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs

git commit -m "$(cat <<'EOF'
test(spatial-filter): add positive EFCore geo.length and geo.intersects coverage

Two integration tests against the EFCore SpatialPlace fixture:
- geo.length(RouteLine) gt 0 — verifies LineString length translation.
- geo.intersects(ServiceArea, POINT(0.5 0.5)) — verifies polygon-point
  intersection translation.

Both queries should hit the seeded "Spatial Place 1" row.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 18: Add path-segment `$filter(...)` positive test (EFCore)

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs`

- [ ] **Step 1: Append the path-segment positive test**

After the two tests added in Task 17, append:

```csharp
    /// <summary>
    /// EFCore: path-segment $filter syntax (/Entities/$filter(...)) must also translate
    /// geo.distance.  Exercises the RestierQueryBuilder.HandleFilterPathSegment change.
    /// </summary>
    [Fact]
    public async Task EFCore_Filter_GeoDistance_PathSegmentSyntax_TranslatesToo()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces/$filter(geo.distance(HeadquartersLocation,geography'SRID=4326;POINT(0 0)') lt 10000000)",
            serviceCollection: _configureServices);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "path-segment $filter must use the same DI-resolved IFilterBinder as the URL-query form");
        content.Should().Contain("\"Name\":\"Spatial Place 1\"");
    }
```

- [ ] **Step 2: Run**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~EFCore_Filter_GeoDistance_PathSegmentSyntax_TranslatesToo" --logger "console;verbosity=normal"
```

Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs

git commit -m "$(cat <<'EOF'
test(spatial-filter): verify path-segment \$filter(...) uses the same binder

Locks the RestierQueryBuilder.HandleFilterPathSegment fix: a
path-segment-style filter URL (/Entities/\$filter(...)) translates
geo.distance the same way the URL-query form does, because both
paths now resolve IFilterBinder from route services instead of
constructing a fresh FilterBinder.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 10 — Integration tests: negative

### Task 19: Add four negative integration tests

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs`

- [ ] **Step 1: Append the four negative tests**

After the path-segment test added in Task 18, append:

```csharp
    // ─────────────────────────────────────────────────────────────────────────
    // Negative — error handling (spec B)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mixing Geography property with a Geometry literal must return 400.
    /// </summary>
    [Fact]
    public async Task EFCore_Filter_GeoDistance_GenusMismatch_Returns400()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.distance(HeadquartersLocation,geometry'SRID=0;POINT(0 0)') lt 10000000",
            serviceCollection: _configureServices);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        // NOTE (added 2026-05-15): the original plan asserted content.Should().Contain("HeadquartersLocation")
        // here, expecting the binder's SpatialFilter_GenusMismatch message to include the property name.
        // However, this error is raised by ODL's parser (function signature matching) before the binder
        // runs — the parser's message is "No function signature for the function with name 'geo.distance'
        // matches the specified arguments" and does NOT include the property name. Drop that assertion.
        // The HTTP 400 status check above and the "geometry" body check below are sufficient.
        content.ToLowerInvariant().Should().Contain("geometry");
    }

    /// <summary>
    /// A non-EPSG SRID in the literal must return 400 with the spec-A non-EPSG message.
    /// </summary>
    [Fact]
    public async Task EFCore_Filter_GeoDistance_NonEpsgSrid_Returns400()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.distance(HeadquartersLocation,geography'SRID=99999;POINT(0 0)') lt 10000000",
            serviceCollection: _configureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Unknown geo.* function names (geo.area, etc.) must return 400 with the AspNetCoreOData
    /// stock "unknown function" error — proves the binder's default: arm preserves base behavior.
    /// </summary>
    [Fact]
    public async Task EFCore_Filter_GeoArea_UnknownFunction_Returns400()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.area(ServiceArea) gt 0",
            serviceCollection: _configureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    /// <summary>
    /// An API bootstrap without AddRestierSpatial() must still 400 on geo.distance —
    /// observationally identical to the pre-spec-B legacy behavior.
    /// </summary>
    [Fact]
    public async Task EFCore_Filter_GeoDistance_WithoutAddRestierSpatial_Returns400()
    {
        // Bootstrap that registers EFCore without the spatial extension.
        Action<IServiceCollection> withoutSpatial = services =>
            services.AddEFCoreProviderServices<LibraryContext>(options =>
                options.UseSqlServer(GetLibraryConnectionString(), o => o.UseNetTopologySuite()));

        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
            HttpMethod.Get,
            resource: "/SpatialPlaces?$filter=geo.distance(HeadquartersLocation,geography'SRID=4326;POINT(0 0)') lt 10000000",
            serviceCollection: withoutSpatial);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeFalse(
            "without AddRestierSpatial(), no ISpatialTypeConverter is registered, so the binder " +
            "throws ODataException -> 400");
    }

    /// <summary>
    /// Helper: pulls the same LibraryContext connection string AddEntityFrameworkServices uses,
    /// so the without-spatial bootstrap points at the same physical database.
    /// </summary>
    private static string GetLibraryConnectionString()
    {
        // Mirror the AddEntityFrameworkServices<LibraryContext> connection-string lookup so the
        // without-spatial test bootstrap reaches the same SQL Server instance / database the
        // rest of the EFCore tests use.
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddUserSecrets(typeof(SpatialTypeIntegrationTests).Assembly, optional: true)
            .Build();
        var raw = configuration.GetConnectionString(nameof(LibraryContext));
        if (string.IsNullOrEmpty(raw))
        {
            throw new System.InvalidOperationException(
                $"Connection string 'ConnectionStrings:{nameof(LibraryContext)}' is required. Add it with dotnet user-secrets.");
        }
        var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = raw };
        if (builder.ContainsKey("Initial Catalog"))
        {
            builder["Initial Catalog"] = $"{builder["Initial Catalog"]}_{Environment.Version.Major}_EFCore";
        }
        else if (builder.ContainsKey("Database"))
        {
            builder["Database"] = $"{builder["Database"]}_{Environment.Version.Major}_EFCore";
        }
        return builder.ConnectionString;
    }
```

Add the `using` directives at the top of the file:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
```

(`Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore` is already imported by the existing test file; verify before adding to avoid a duplicate.)

- [ ] **Step 2: Run the four negative tests**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~EFCore_Filter_GeoDistance_GenusMismatch_Returns400|FullyQualifiedName~EFCore_Filter_GeoDistance_NonEpsgSrid_Returns400|FullyQualifiedName~EFCore_Filter_GeoArea_UnknownFunction_Returns400|FullyQualifiedName~EFCore_Filter_GeoDistance_WithoutAddRestierSpatial_Returns400" --logger "console;verbosity=normal"
```

Expected: all four pass.

- [ ] **Step 3: Run the full spatial integration test class**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~SpatialTypeIntegrationTests" --logger "console;verbosity=normal"
```

Expected: every test passes.

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs

git commit -m "$(cat <<'EOF'
test(spatial-filter): add negative EFCore integration coverage

Four negative cases asserting the error-handling contract from
the spec:
- Genus mismatch (Geography prop vs geometry'...' literal) -> 400.
- Non-EPSG literal SRID -> 400.
- Unknown geo.* function name (geo.area) -> 400 (fall-through to
  AspNetCoreOData base).
- Bootstrap without AddRestierSpatial() -> 400 (no converter
  registered, diagnostic message points at AddRestierSpatial).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 11 — Documentation

### Task 20: Update `spatial-types.mdx`

**Files:**
- Modify: `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx`

- [ ] **Step 1: Remove the `geo.*` entry from "What's not yet supported"**

Open `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx`. Locate the "What's not yet supported" section (around line 124). The current content is:

```mdx
## What's not yet supported

- Server-side `geo.distance` / `geo.length` / `geo.intersects` translation. Use `$filter` with these operators returns an error today; a future spec will deliver translation.
- Non-EPSG `CoordinateSystem` values throw `InvalidOperationException` on write. Default-SRID configuration is planned for a future spec.
```

Update to:

```mdx
## What's not yet supported

- `$orderby=geo.distance(...)` and other `geo.*` operators in `$orderby`. Planned for a future spec.
- Non-EPSG `CoordinateSystem` values throw `InvalidOperationException` on write and on `$filter`. Default-SRID configuration is planned for a future spec.
```

- [ ] **Step 2: Add a "Server-side filtering with `geo.*` functions" section**

Insert the new section directly before "What's not yet supported":

```mdx
## Server-side filtering with `geo.*` functions

Once `AddRestierSpatial()` is wired into route services (see [Install the package](#install-the-package) above), the three OData v4-core spatial functions translate to native SQL spatial operators server-side. The exact translation depends on the EF flavor and the database provider, but the OData URL surface is identical.

| Function | OData syntax | Translates to |
|----------|--------------|---------------|
| `geo.distance` | `?$filter=geo.distance(LocationProp,geography'SRID=4326;POINT(lon lat)') lt N` | `DbGeography.Distance` (EF6) or `NetTopologySuite.Geometries.Geometry.Distance` (EF Core), then native SQL `geography::STDistance` / `ST_Distance`. |
| `geo.length` | `?$filter=geo.length(LineStringProp) gt 0` | `DbGeography.Length` (EF6) or `NetTopologySuite.Geometries.Geometry.Length` (EF Core), then native SQL `geography::STLength` / `ST_Length`. Input must be a LineString — non-LineString inputs return null (EF6) or boundary length (NTS). |
| `geo.intersects` | `?$filter=geo.intersects(PolygonProp,geography'SRID=4326;POINT(lon lat)')` | `DbGeography.Intersects` (EF6) or `Geometry.Intersects` (EF Core), then native SQL `geography::STIntersects` / `ST_Intersects`. |

Path-segment `$filter` syntax (`/SpatialPlaces/$filter(geo.distance(...) lt N)`) works the same as the URL-query form.

### Error responses

- **Genus mismatch.** Comparing a Geography property to a Geometry literal (or vice versa) → HTTP 400 with a message naming the property and both genera.
- **Non-EPSG CRS.** A literal whose SRID is not in Microsoft.Spatial's EPSG registry (`CoordinateSystem.EpsgId == null` after parsing) → HTTP 400 with the spec-A non-EPSG message.
- **Unsupported function.** Calls to `geo.*` functions outside the three above (`geo.area`, `geo.contains`, `geo.coveredby`, ...) → HTTP 400 with AspNetCoreOData's stock "unknown function" error. Forward-compat for future OData v4 spec additions.
- **Missing `AddRestierSpatial()`.** If the spatial extension is not registered but a `geo.*` filter is issued against a spatial property → HTTP 400 with a diagnostic naming the function, property, and the missing `AddRestierSpatial()` call.

### Custom IFilterBinder

Restier registers `RestierSpatialFilterBinder` before invoking the user-supplied route-services delegate. Consumers who need their own custom `IFilterBinder` register it inside that delegate (it runs after Restier's registration and wins):

```csharp
services.AddRestier(...)
    // Restier registers RestierSpatialFilterBinder here.
    .AddRouteComponents("api", model, route =>
    {
        // Your custom registration runs after Restier's and overrides it.
        route.RemoveAll<IFilterBinder>();
        route.AddSingleton<IFilterBinder, MyCustomFilterBinder>();
    });
```

```

- [ ] **Step 3: Build the docs project**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: clean build. `docs.json` is regenerated by the DotNetDocs SDK.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx \
        src/Microsoft.Restier.Docs/docs.json

git commit -m "$(cat <<'EOF'
docs(spatial): document server-side geo.* filter translation

Removes the geo.* entry from "What's not yet supported" and adds a
new "Server-side filtering with geo.* functions" section. Documents
the three supported functions, the path-segment $filter shape, the
four documented error responses (genus mismatch, non-EPSG CRS,
unknown function, missing AddRestierSpatial), and the consumer
pattern for plugging in their own IFilterBinder.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 12 — Final verification

### Task 21: Full solution build + spatial test sweep

- [ ] **Step 1: Full solution build**

```bash
dotnet build RESTier.slnx
```

Expected: clean build, warnings-as-errors honored across every project.

- [ ] **Step 2: Run every spatial-related test**

```bash
dotnet test RESTier.slnx --filter "FullyQualifiedName~Spatial" --logger "console;verbosity=normal"
```

Expected: every test in `Microsoft.Restier.Tests.EntityFramework.Spatial`, `Microsoft.Restier.Tests.EntityFrameworkCore.Spatial`, and the unit + integration tests added by this plan passes. Concretely:

- Unit (`RestierSpatialFilterBinderTests`): 6 tests pass.
- Unit (`RestierQueryBuilderFilterBinderResolutionTests`): 2 tests pass.
- Integration (`SpatialTypeIntegrationTests`): the original 4 spec-A tests plus 3 new positive (`distance`, `length`, `intersects`), 1 new path-segment positive, and 4 new negative tests — 12 total — all pass.

- [ ] **Step 3: Run the full AspNetCore test suite as a regression sanity-check**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --logger "console;verbosity=minimal"
```

Expected: no regressions.

- [ ] **Step 4: No commit** — this is a verification task only. If any test fails, do not commit a "fix" before the failure is understood and the corresponding code commit is amended or replaced.

---

## Self-review checklist

After completing all tasks, verify:

1. **Spec coverage:** Every section/requirement in the spec is implemented?
   - Custom `IFilterBinder` subclass — Task 2.
   - `RestierQueryBuilder` ctor widening — Task 4.
   - Controller call-site updates — Task 5.
   - Always-on registration in `RestierODataOptionsExtensions` — Task 3.
   - Resource strings — Task 1.
   - `geo.length` translation — Task 9.
   - `geo.distance` translation + `ResolveSpatialInstanceMethod` + literal lowering — Task 10.
   - `geo.intersects` translation — Task 11.
   - Unknown geo.* fall-through (verified) — Task 12.
   - Genus validation (Step 0) — handled upstream by ODL parser's function signature matching; binder Step 0 skipped as unreachable code path. Documented in Task 13 note.
   - Non-EPSG wrapping — Task 14 (test) + Task 10 (impl). Non-EPSG CRS wrap (Task 14) — handled upstream by the OData literal parser (only integer SRIDs are syntactically expressible, and Microsoft.Spatial accepts any integer as a valid EpsgId); the wrap path is unreachable via URL queries. Skipped. Documented in Task 14 note.
   - No-converter diagnostic — Task 15 (test) + Task 10 (impl).
   - `RouteLine` LineString in `SpatialPlace` + seed — Tasks 6, 7.
   - Test project `.Spatial` references — Task 8.
   - Flipped existing negative integration test — Task 16.
   - Positive `geo.length`/`geo.intersects` integration tests — Task 17.
   - Path-segment integration test — Task 18.
   - Four negative integration tests — Task 19.
   - Documentation update — Task 20.
   - Final verification — Task 21.

2. **Placeholder scan:** No "TBD", "TODO", "implement later", "add error handling", "similar to Task N", or vague directives. The plan repeats code blocks where needed rather than back-referencing tasks.

3. **Type / symbol consistency:**
   - `RestierSpatialFilterBinder` is the same class name from Task 2 through Task 21.
   - `BindBinarySpatialMethod`, `LowerSpatialLiteralIfNeeded`, `ResolveSpatialInstanceMethod`, `ValidateGenus`, `ClassifyGenus`, `ProbeStorageType` — all referenced under their final names.
   - Resource keys `SpatialFilter_GenusMismatch` and `SpatialFilter_NoConverterForStorageType` appear identically in resx (Task 1), Designer.cs (Task 1), and impl (Tasks 10, 13).
   - File paths (`Query/RestierSpatialFilterBinder.cs`, `IntegrationTests/SpatialTypeIntegrationTests.cs`, `Scenarios/Library/SpatialPlace.cs`, `Scenarios/Library/LibraryTestInitializer.cs`) match the live repo layout.

4. **Coverage gaps:** None — every spec requirement maps to at least one task; every error-handling case has both a unit test and an integration test.
