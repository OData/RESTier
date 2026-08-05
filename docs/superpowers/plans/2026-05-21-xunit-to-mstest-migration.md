# Migrate from xUnit v3 to MSTest + add CI code coverage

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace xUnit v3 with MSTest 3.x across the 9 real test projects under `test/` plus 3 shared-helper projects (`Tests.Shared`, `Tests.Shared.EntityFramework`, `Tests.Shared.EntityFrameworkCore` — flagged `<IsTestProject>false</IsTestProject>` in their csproj, but they still contain xUnit `using` directives and helper types that have to migrate too) — ~928 test methods total. Keep AwesomeAssertions and NSubstitute. Add Cobertura code coverage collection + publishing to the Azure Pipelines CI build.

**Architecture:** Add MSTest packages alongside xUnit in `Directory.Build.props` first so the repo never enters a broken state. Convert test projects one at a time (per-project verification = `dotnet test <project>` green). Once every project compiles and passes on MSTest, remove the xUnit packages and per-project `OutputType=exe` workarounds. Finally, extend the Azure pipeline to collect, merge, and publish coverage; update CLAUDE.md and the contributor docs.

**Tech Stack:**
- **Test framework (new):** MSTest 3.x (`MSTest.TestFramework` + `MSTest.TestAdapter`)
- **Test runner:** `Microsoft.NET.Test.Sdk` (already present, unchanged)
- **Assertions:** AwesomeAssertions 8.x (unchanged, FluentAssertions fork)
- **Mocking:** NSubstitute 5.x (unchanged)
- **Coverage collection:** `coverlet.collector` 6.x (already present, just unused in CI)
- **Coverage reporting:** `dotnet-reportgenerator-globaltool` (new in CI only)
- **CI:** Azure DevOps Pipelines (single file `.pipelines/RESTier-CI.yml`)

---

## Conversion recipe (reference — used by Tasks 4–15)

Each test project conversion follows the same mechanical recipe. Apply it in this order:

### 1. Project file (`*.csproj`)

- If the file contains `<OutputType>exe</OutputType>`, **delete that line**. MSTest projects use the default Library output. (Affects: `Tests.Core`, `Tests.AspNetCore`, `Tests.EntityFramework.Spatial`, `Tests.EntityFrameworkCore.Spatial`.)
- If the file references `xunit.v3.extensibility.core` directly (Tests.Shared only), delete that `<PackageReference>`. MSTest doesn't need a separate extensibility assembly.
- No other csproj edits — xUnit/MSTest packages are centralised in `Directory.Build.props`.

### 2. Using directives

In every `*.cs` file under the project:

- Replace `using Xunit;` with `using Microsoft.VisualStudio.TestTools.UnitTesting;`.
- Remove any `using Xunit.Abstractions;`, `using Xunit.v3;`, or `using Xunit.Sdk;` lines.

### 3. Attribute swaps (case-sensitive, exact)

| xUnit v3                                  | MSTest 3                                              |
|-------------------------------------------|-------------------------------------------------------|
| `[Fact]`                                  | `[TestMethod]`                                        |
| `[Fact(Skip = "...")]`                    | `[TestMethod, Ignore("...")]`                         |
| `[Theory]`                                | `[TestMethod]` (no separate Theory attribute)         |
| `[InlineData(a, b)]`                      | `[DataRow(a, b)]`                                     |
| `[MemberData(nameof(Foo))]`               | `[DynamicData(nameof(Foo))]`                          |
| `[ClassData(typeof(Foo))]`                | `[DynamicData(nameof(Foo.GetData), typeof(Foo))]` (after refactoring the data class into a static method) |
| `[CollectionDefinition("X")]` (on a class) | **Delete the entire class file.** Collections without fixtures have no MSTest equivalent. |
| `[Collection("X")]` (on a test class)     | `[DoNotParallelize]` on the test class                |
| `[Trait("category", "value")]`            | `[TestCategory("value")]`                             |

In addition, every test **class** that contains test methods must have `[TestClass]`. (xUnit auto-discovered test classes; MSTest requires the attribute.) Add it just above the class declaration.

### 3a. xUnit `Assert.*` API → AwesomeAssertions (FluentAssertions)

xUnit's `Assert` class and MSTest's `Assert` class have different method names *and* different argument orders (xUnit is `expected, actual`; MSTest is `actual, expected`). Mechanical swapping is error-prone. **The repo's dominant assertion style is AwesomeAssertions (`.Should().Be(...)`)** — convert xUnit `Assert.*` calls to that style for consistency.

Verified call sites (25 total across 4 files): `Tests.AspNetCore/Filters/RestierExceptionFilterAttributeTests.cs`, `Tests.AspNetCore/Extensions/RestierHttpContextExtensionsTests.cs`, `Tests.AspNetCore/Extensions/RestierHttpRequestExtensionsTests.cs`, `Tests.AspNetCore/Batch/RestierChangeSetPropertyTests.cs`. (All four files live in `Microsoft.Restier.Tests.AspNetCore`, so this conversion is concentrated in Task 13.)

| xUnit `Assert.*`                              | AwesomeAssertions equivalent                           |
|-----------------------------------------------|--------------------------------------------------------|
| `Assert.Equal(expected, actual)`              | `actual.Should().Be(expected)`                         |
| `Assert.True(condition)`                      | `condition.Should().BeTrue()`                          |
| `Assert.False(condition)`                     | `condition.Should().BeFalse()`                         |
| `Assert.Null(obj)`                            | `obj.Should().BeNull()`                                |
| `Assert.NotNull(obj)`                         | `obj.Should().NotBeNull()`                             |
| `Assert.Empty(collection)`                    | `collection.Should().BeEmpty()`                        |
| `Assert.IsType<T>(obj)` (returns `T`)         | `obj.Should().BeOfType<T>().Subject` (returns `T`)     |
| `Assert.Throws<T>(() => action())`            | `FluentActions.Invoking(() => action()).Should().Throw<T>()` |
| `var ex = await Assert.ThrowsAsync<T>(...)`   | `var ex = (await FluentActions.Awaiting(...).Should().ThrowAsync<T>()).Which;` |

Example — converting `RestierChangeSetPropertyTests.cs` lines 97–98:

```csharp
// Before (xUnit)
var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => changeSetProperty.OnChangeSetCompleted());
Assert.Equal("Test exception", exception.Message);

// After (AwesomeAssertions)
var exception = (await FluentActions.Awaiting(() => changeSetProperty.OnChangeSetCompleted())
    .Should().ThrowAsync<InvalidOperationException>()).Which;
exception.Message.Should().Be("Test exception");
```

Add `using FluentAssertions;` (already present in most files via the AwesomeAssertions package) where needed. Do **not** mix MSTest's `Assert.AreEqual` into these files — the existing style is `.Should()` chains, keep it.

### 4. `TheoryData` / `TheoryDataRow`

Replace `IEnumerable<TheoryDataRow<T1, T2, ...>>` data sources with `IEnumerable<object[]>`:

```csharp
// Before (xUnit v3)
public static IEnumerable<TheoryDataRow<RestierPipelineState, string>> GetData()
{
    yield return new TheoryDataRow<RestierPipelineState, string>(RestierPipelineState.Authorization, "Can");
}

// After (MSTest 3)
public static IEnumerable<object[]> GetData()
{
    yield return new object[] { RestierPipelineState.Authorization, "Can" };
}
```

### 5. `TestContext.Current.CancellationToken`

xUnit v3's ambient `TestContext.Current` doesn't exist in MSTest. Two replacement patterns:

- **Test classes** that already declare `public TestContext TestContext { get; set; }` (MSTest property injection):
  `TestContext.Current.CancellationToken` → `TestContext.CancellationTokenSource.Token`
- **Test classes that don't yet have it:** add the property first, then swap as above.

```csharp
[TestClass]
public class CombinedAppTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Foo()
    {
        var ct = TestContext.CancellationTokenSource.Token;
        // ...
    }
}
```

### 6. `ITestOutputHelper`

In `Tests.AspNetCore/FeatureTests/{ActionTests,FunctionTests}.cs` (and their EF6/EFCore subclasses), the abstract base classes use primary-constructor injection of `ITestOutputHelper`. Convert to MSTest's `TestContext` pattern:

```csharp
// Before
public abstract class ActionTests<TApi, TContext>(ITestOutputHelper outputHelper) : RestierTestBase<TApi> ...
{
    [Fact]
    public async Task Foo()
    {
        outputHelper.Write(content);
    }
}

// After
public abstract class ActionTests<TApi, TContext> : RestierTestBase<TApi> ...
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task Foo()
    {
        TestContext.WriteLine(content);
    }
}
```

Concrete subclasses (e.g., `FeatureTests/EFCore/ActionTests.cs`) currently use the primary constructor:

```csharp
// Before
public class ActionTests(ITestOutputHelper outputHelper) : ActionTests<LibraryApi, LibraryContext>(outputHelper)

// After
[TestClass]
public class ActionTests : ActionTests<LibraryApi, LibraryContext>
```

The base class's `TestContext` property is shared with subclasses because MSTest injects it on the runtime type.

### 7. Per-project verification

```bash
dotnet test test/<project>/<project>.csproj --configuration Debug
```

Expected output: `Passed: N, Failed: 0, Skipped: S` for every TFM (`net8.0`, `net9.0`, `net10.0`). **Both N (passed) and S (skipped) must match the baseline captured before conversion.** The suite intentionally skips some tests (e.g., `DbSpatialConverterTests` skips two tests that require Windows-only `SqlServerSpatial160.dll`; `SpatialTypeIntegrationTests` skips four tests that need CLR-enabled SQL Server). Forcing `Skipped: 0` would mean either disabling the `[Ignore]` attributes or "fixing" tests that depend on environment-specific prerequisites — neither is the goal here.

---

## File map

**Modified, central:**
- `Directory.Build.props` — swap centralised test packages
- `.pipelines/RESTier-CI.yml` — add coverage collection + publishing
- `CLAUDE.md` — update Test Conventions section
- `.github/CONTRIBUTING.md` — replace xUnit references
- `src/Microsoft.Restier.Docs/contribution-guidelines.mdx` — replace xUnit references
- `src/Microsoft.Restier.Docs/guides/server/testing.mdx` — replace xUnit references and `[Fact]` examples

**Modified, per test project (12 projects, see Tasks 4–15):**
- Each project's `*.csproj`: remove `<OutputType>exe</OutputType>` where present, remove `xunit.v3.extensibility.core` from `Tests.Shared.csproj`
- Every `*.cs` file containing tests: usings + attributes + (where applicable) `TestContext` patterns

**Deleted:**
- `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/LibraryApiEFCoreTestCollection.cs` (collection-definition class with no fixture; replaced by `[DoNotParallelize]` on the test classes)
- `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/LibraryApiEF6TestCollection.cs` (same pattern, EF6 variant)

---

## Task 1: Add MSTest packages alongside xUnit (no removal yet)

**Files:**
- Modify: `Directory.Build.props:136-144`

- [ ] **Step 1: Edit `Directory.Build.props` test-project ItemGroup**

Open `Directory.Build.props`, locate the `ItemGroup` guarded by `Condition=" $(IsTestProject) == 'true' and $(IsSampleProject) != 'true' and $(IsTestSharedProject) != 'true'"` (lines 136–144). Replace with:

```xml
<ItemGroup Condition=" $(IsTestProject) == 'true' and $(IsSampleProject) != 'true' and $(IsTestSharedProject) != 'true'">
    <PackageReference Include="coverlet.collector" Version="6.*" />
    <PackageReference Include="AwesomeAssertions" Version="8.*" PrivateAssets="All" />
    <PackageReference Include="AwesomeAssertions.Analyzers" Version="0.*" PrivateAssets="All" />
    <PackageReference Include="MSTest.TestFramework" Version="3.*" />
    <PackageReference Include="MSTest.TestAdapter" Version="3.*" />
    <PackageReference Include="xunit.v3" Version="3.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="NSubstitute" Version="5.3.0"></PackageReference>
</ItemGroup>
```

Both MSTest and xUnit references coexist while we migrate. Test discovery picks up both adapters; xUnit tests keep running until each project is converted.

- [ ] **Step 2: Restore + build the solution**

Run: `dotnet restore RESTier.slnx && dotnet build RESTier.slnx --configuration Debug`
Expected: build succeeds with no errors. (Warnings about duplicate test-framework references are acceptable here — they go away in Task 16.)

- [ ] **Step 3: Run a smoke test on one project to confirm xUnit still works**

Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --configuration Debug --no-build`
Expected: all xUnit tests still pass under all three TFMs.

- [ ] **Step 4: Commit**

```bash
git add Directory.Build.props
git commit -m "test: add MSTest 3.x packages alongside xUnit for migration"
```

---

## Task 2: Strip xUnit references from `Tests.Shared` so it can compile against MSTest types only

`Tests.Shared` is **not** a test project (`IsTestProject=false` in its csproj). It exposes `RestierTestBase<TApi>` as a *shared* base used by every real test project. It currently directly references `xunit.v3.extensibility.core` and uses `Xunit.TestContext.Current`. Both must go before downstream test projects can drop xUnit.

**Files:**
- Modify: `test/Microsoft.Restier.Tests.Shared/Microsoft.Restier.Tests.Shared.csproj`
- Modify: `test/Microsoft.Restier.Tests.Shared/RestierTestBase.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared/Extensions/TraceWriterExtensions.cs`

- [ ] **Step 1: Add MSTest framework reference to `Tests.Shared`**

Open `test/Microsoft.Restier.Tests.Shared/Microsoft.Restier.Tests.Shared.csproj`. In the `<ItemGroup>` that currently contains `xunit.v3.extensibility.core`, replace that line with `MSTest.TestFramework` so the project sees `TestContext`:

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.OData.ModelBuilder" Version="2.*" />
    <PackageReference Include="Breakdance.AspNetCore" Version="$(RestierBreakdanceVersion)" />
    <PackageReference Include="MSTest.TestFramework" Version="3.*" />
</ItemGroup>
```

- [ ] **Step 2: Rewrite `RestierTestBase.cs` to use MSTest types**

Open `test/Microsoft.Restier.Tests.Shared/RestierTestBase.cs`. Replace the file contents with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Microsoft.Restier.Tests.Shared
{
    /// <summary>
    /// Common base for Restier test classes. Adds a trace listener that captures output
    /// for inspection by individual tests.
    /// </summary>
    public class RestierTestBase<TApi> : RestierBreakdanceTestBase<TApi>
        where TApi : ApiBase
    {
        public RestierTestBase()
        {
            Trace.Listeners.Add(TraceListener);
        }

        /// <summary>
        /// Gets or sets the MSTest test context. Populated by the runner.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Gets the trace listener that can be used for test output.
        /// </summary>
        public TraceListener TraceListener { get; } = new TestTraceListener();
    }
}
```

- [ ] **Step 3: Update `Extensions/TraceWriterExtensions.cs`**

`using Xunit;` is unused (the file never references any xUnit symbol). Open `test/Microsoft.Restier.Tests.Shared/Extensions/TraceWriterExtensions.cs` and delete the `using Xunit;` line.

- [ ] **Step 4: Build `Tests.Shared` to verify it compiles without xUnit**

Run: `dotnet build test/Microsoft.Restier.Tests.Shared/Microsoft.Restier.Tests.Shared.csproj --configuration Debug`
Expected: build succeeds.

- [ ] **Step 5: Build the whole solution**

Run: `dotnet build RESTier.slnx --configuration Debug`
Expected: build succeeds. Downstream test projects still compile because they still pull in xUnit themselves.

- [ ] **Step 6: Commit**

```bash
git add test/Microsoft.Restier.Tests.Shared
git commit -m "test(shared): swap Xunit.TestContext for MSTest TestContext in RestierTestBase"
```

---

## Task 3: Pilot conversion — `Microsoft.Restier.Tests.EntityFrameworkCore.Spatial`

This is the smallest test project (18 files, mostly `[Fact]`/`[Theory]` with `[InlineData]`). It validates the conversion recipe end-to-end before we batch the rest.

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial.csproj`
- Modify: every `*.cs` file under `test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial/`

- [ ] **Step 1: Record baseline test count**

Run: `dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial.csproj --configuration Debug --no-build`
Note the `Passed: N` and `Skipped: S` numbers for each TFM. Save them (mentally or in a scratchpad) — every TFM should still report the same N and S after conversion. (For this project both N and S are deterministic across TFMs; S is expected to be 0.)

- [ ] **Step 2: Remove `<OutputType>exe</OutputType>` from the csproj**

Open `test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial.csproj`. Delete the `<OutputType>exe</OutputType>` line inside the first `<PropertyGroup>`.

- [ ] **Step 3: Apply the conversion recipe to every `*.cs` file**

For each `*.cs` file in the project, in this order:
1. Replace `using Xunit;` with `using Microsoft.VisualStudio.TestTools.UnitTesting;`.
2. Add `[TestClass]` above every class declaration that contains `[Fact]`, `[Theory]`, or `[TestMethod]` attributes.
3. Replace `[Fact]` with `[TestMethod]`.
4. Replace `[Theory]` with `[TestMethod]`.
5. Replace `[InlineData(` with `[DataRow(`.
6. Replace `[MemberData(nameof(X))]` with `[DynamicData(nameof(X))]` (none expected in this project).

No `ITestOutputHelper`, `TheoryDataRow`, `[Collection]`, or `TestContext.Current` usage in this project — pure attribute swap.

- [ ] **Step 4: Build the project**

Run: `dotnet build test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial.csproj --configuration Debug`
Expected: build succeeds with no errors.

- [ ] **Step 5: Run the project's tests**

Run: `dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial.csproj --configuration Debug --no-build`
Expected: `Passed: N, Failed: 0, Skipped: S` — same N and S as Step 1 — for each TFM.

- [ ] **Step 6: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial
git commit -m "test(efcore.spatial): convert from xUnit to MSTest"
```

---

## Task 4: Convert `Microsoft.Restier.Tests.EntityFramework.Spatial` (16 files)

Use the conversion recipe. Same shape as Task 3 — no `ITestOutputHelper`, no collections. **Note:** `DbSpatialConverterTests.cs` lines 88 and 101 each carry `[Fact(Skip = "...")]` for tests that need Windows-only `SqlServerSpatial160.dll`. Apply the recipe row: `[Fact(Skip = "...")]` → `[TestMethod, Ignore("...")]`. Expect `Skipped: S >= 2` per TFM after conversion (matches baseline).

- [ ] **Step 1: Baseline**: `dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --no-build` — record `Passed: N, Skipped: S`.
- [ ] **Step 2: csproj**: delete `<OutputType>exe</OutputType>` from `test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj`.
- [ ] **Step 3: Apply recipe** (steps 2–6) to every `*.cs` file under the project. Pay attention to the two `[Fact(Skip = ...)]` rows in `DbSpatialConverterTests.cs`.
- [ ] **Step 4: Build**: `dotnet build test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj` — expect success.
- [ ] **Step 5: Test**: same as baseline, expect `Passed: N, Skipped: S`.
- [ ] **Step 6: Commit** `git commit -m "test(ef.spatial): convert from xUnit to MSTest"`.

---

## Task 5: Convert `Microsoft.Restier.Tests.EntityFramework` (15 files)

- [ ] **Step 1: Baseline**: `dotnet test test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj --no-build` — record `Passed: N, Skipped: S`.
- [ ] **Step 2: csproj**: nothing to edit (no `OutputType=exe`).
- [ ] **Step 3: Apply recipe** to every `*.cs` file. Note: `EFQueryNoTrackingTests.cs` defines a `TrackingTestContext` (DbContext, not test framework) — leave that class alone, only convert the test class.
- [ ] **Step 4: Build** — expect success.
- [ ] **Step 5: Test** — expect `Passed: N, Skipped: S`.
- [ ] **Step 6: Commit** `git commit -m "test(ef): convert from xUnit to MSTest"`.

---

## Task 6: Convert `Microsoft.Restier.Tests.EntityFrameworkCore` (19 files)

- [ ] **Step 1: Baseline**: `dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj --no-build` — record `Passed: N, Skipped: S`.
- [ ] **Step 2: csproj**: nothing to edit.
- [ ] **Step 3: Apply recipe** to every `*.cs` file.
- [ ] **Step 4: Build** — expect success.
- [ ] **Step 5: Test** — expect `Passed: N, Skipped: S`.
- [ ] **Step 6: Commit** `git commit -m "test(efcore): convert from xUnit to MSTest"`.

---

## Task 7: Convert `Microsoft.Restier.Tests.Shared.EntityFramework` (15 files)

Note: this is a *shared-source* test helpers project. Its `csproj` likely doesn't set `IsTestProject`, but the recipe still applies if any `*.cs` file uses `[Fact]`/`[Theory]`.

- [ ] **Step 1: Baseline**: there is no standalone test runner for this project; it's consumed by `Tests.EntityFramework`. Skip the baseline step and rely on Task 5's tests for validation.
- [ ] **Step 2: csproj**: open `test/Microsoft.Restier.Tests.Shared.EntityFramework/Microsoft.Restier.Tests.Shared.EntityFramework.csproj`. If it references `xunit.v3` or `xunit.v3.extensibility.core` directly, replace with `MSTest.TestFramework` Version `3.*`. Otherwise no changes.
- [ ] **Step 3: Apply recipe** to every `*.cs` file that uses xUnit. Files that don't use any test attributes need only the `using` swap if a `using Xunit;` line is present.
- [ ] **Step 4: Build** the consumer: `dotnet build test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj` — expect success.
- [ ] **Step 5: Test** the consumer: `dotnet test test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj --no-build` — same `Passed: N, Skipped: S` as Task 5.
- [ ] **Step 6: Commit** `git commit -m "test(shared.ef): convert from xUnit to MSTest"`.

---

## Task 8: Convert `Microsoft.Restier.Tests.Shared.EntityFrameworkCore` (13 files)

Same shape as Task 7, paired with `Tests.EntityFrameworkCore` from Task 6 as the consumer for verification.

- [ ] **Step 1: Baseline**: skip; covered by Task 6 tests.
- [ ] **Step 2: csproj**: same conditional treatment as Task 7.
- [ ] **Step 3: Apply recipe** to every `*.cs` file that uses xUnit.
- [ ] **Step 4: Build** consumer: `dotnet build test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj` — expect success.
- [ ] **Step 5: Test** consumer: `dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj --no-build` — `Passed: N, Skipped: S` from Task 6.
- [ ] **Step 6: Commit** `git commit -m "test(shared.efcore): convert from xUnit to MSTest"`.

---

## Task 9: Convert `Microsoft.Restier.Tests.Core` (52 files)

This project has `[Theory]` + `[InlineData]` heavily, plus `[MemberData]` in `ConventionBasedMethodNameFactoryTests.cs` with a `TheoryDataRow<>` helper. Both patterns need the recipe's TheoryData replacement.

- [ ] **Step 1: Baseline**: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --no-build` — record `Passed: N, Skipped: S`.
- [ ] **Step 2: csproj**: delete `<OutputType>exe</OutputType>` from `test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj`.
- [ ] **Step 3: Apply recipe** to every `*.cs` file.
- [ ] **Step 4: Convert `TheoryDataRow<>` in `ConventionBasedMethodNameFactoryTests.cs`**

Locate `public static IEnumerable<TheoryDataRow<RestierPipelineState, RestierEntitySetOperation, string>> GetMethodNameData()` (around line 225). Replace the return type and yield statements:

```csharp
public static IEnumerable<object[]> GetMethodNameData()
{
    yield return new object[] { RestierPipelineState.Authorization, RestierEntitySetOperation.Insert, "CanInsert" };
    // ... preserve all existing rows, rewriting each `new TheoryDataRow<...>(a, b, c)` as `new object[] { a, b, c }`
}
```

The `[Theory]` + `[MemberData(nameof(GetMethodNameData))]` consumer becomes `[TestMethod]` + `[DynamicData(nameof(GetMethodNameData))]`.

- [ ] **Step 5: Build**: `dotnet build test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj` — expect success.
- [ ] **Step 6: Test**: `dotnet test ... --no-build` — expect `Passed: N, Skipped: S`.
- [ ] **Step 7: Commit** `git commit -m "test(core): convert from xUnit to MSTest"`.

---

## Task 10: Convert `Microsoft.Restier.Tests.AspNetCore.Swagger` (17 files)

- [ ] **Step 1: Baseline**: `dotnet test test/Microsoft.Restier.Tests.AspNetCore.Swagger/Microsoft.Restier.Tests.AspNetCore.Swagger.csproj --no-build` — record `Passed: N, Skipped: S`.
- [ ] **Step 2: csproj**: no edits.
- [ ] **Step 3: Apply recipe** to every `*.cs` file. Add the `public TestContext TestContext { get; set; }` property to any class that uses `TestContext.Current.CancellationToken` (e.g., `IntegrationTests/Issue766_PrimitiveParamOperationTests.cs`), then replace `TestContext.Current.CancellationToken` with `TestContext.CancellationTokenSource.Token`.
- [ ] **Step 4: Build** — expect success.
- [ ] **Step 5: Test** — expect `Passed: N, Skipped: S`.
- [ ] **Step 6: Commit** `git commit -m "test(aspnetcore.swagger): convert from xUnit to MSTest"`.

---

## Task 11: Convert `Microsoft.Restier.Tests.AspNetCore.NSwag` (19 files)

Several files use `TestContext.Current.CancellationToken`. Apply the `TestContext` pattern.

- [ ] **Step 1: Baseline**: `dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --no-build` — record `Passed: N, Skipped: S`.
- [ ] **Step 2: csproj**: no edits.
- [ ] **Step 3: Apply recipe** plus `TestContext` injection where needed:
  - `IntegrationTests/CombinedAppTests.cs` — has `TestContext.Current.CancellationToken` calls
  - `IntegrationTests/KeylessViewOpenApiTests.cs` — has `TestContext.Current.CancellationToken` calls
  - `Extensions/IApplicationBuilderExtensionsTests.cs` — has many `TestContext.Current.CancellationToken` calls
  For each: add `public TestContext TestContext { get; set; }` to the test class, then rewrite `TestContext.Current.CancellationToken` → `TestContext.CancellationTokenSource.Token`.
- [ ] **Step 4: Build** — expect success.
- [ ] **Step 5: Test** — expect `Passed: N, Skipped: S`.
- [ ] **Step 6: Commit** `git commit -m "test(aspnetcore.nswag): convert from xUnit to MSTest"`.

---

## Task 12: Convert `Microsoft.Restier.Tests.AspNetCore.Versioning` (27 files)

- [ ] **Step 1: Baseline**: `dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --no-build` — record `Passed: N, Skipped: S`.
- [ ] **Step 2: csproj**: no edits.
- [ ] **Step 3: Apply recipe** to every `*.cs` file. Check for `TestContext.Current.CancellationToken` and apply the property-injection fix if present.
- [ ] **Step 4: Build** — expect success.
- [ ] **Step 5: Test** — expect `Passed: N, Skipped: S`.
- [ ] **Step 6: Commit** `git commit -m "test(aspnetcore.versioning): convert from xUnit to MSTest"`.

---

## Task 13: Convert `Microsoft.Restier.Tests.AspNetCore` (149 files) — *biggest project*

This is the largest project and has four complications: `ITestOutputHelper` in abstract base classes, **two parallel `[CollectionDefinition]` families** (`LibraryApiEFCore` and `LibraryApiEF6`) for sequential test runs, regression tests under `RegressionTests/EF6/` and `RegressionTests/EFCore/` that also use those collections, and 25 raw xUnit `Assert.*` calls in four files (see recipe section 3a).

- [ ] **Step 1: Baseline**: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --no-build` — record `Passed: N, Skipped: S`. The current suite has intentionally skipped tests (4 in `IntegrationTests/SpatialTypeIntegrationTests.cs` requiring CLR-enabled SQL Server), so S will be > 0.
- [ ] **Step 2: csproj**: delete `<OutputType>exe</OutputType>` from `test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj`.
- [ ] **Step 3: Convert abstract base classes — `FeatureTests/ActionTests.cs` and `FeatureTests/FunctionTests.cs`**

For each file:

```csharp
// Before
public abstract class ActionTests<TApi, TContext>(ITestOutputHelper outputHelper) : RestierTestBase<TApi>
    where TApi : ApiBase where TContext : class
{
    [Fact]
    public async Task ActionParameters_MissingParameter()
    {
        // ...
        outputHelper.Write(content);
    }
}

// After
public abstract class ActionTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase where TContext : class
{
    [TestMethod]
    public async Task ActionParameters_MissingParameter()
    {
        // ...
        TestContext.WriteLine(content);
    }
}
```

The `TestContext` property is already inherited from `RestierTestBase<TApi>` (set up in Task 2), so no property declaration is needed in the derived class. Repeat for every `outputHelper.Write(...)` call (8 in ActionTests.cs, 9 in FunctionTests.cs based on the survey).

- [ ] **Step 4: Convert concrete EF6/EFCore subclasses — `FeatureTests/EF6/{Action,Function}Tests.cs`, `FeatureTests/EFCore/{Action,Function}Tests.cs`**

```csharp
// Before (e.g., FeatureTests/EFCore/ActionTests.cs)
[Collection("LibraryApiEFCore")]
public class ActionTests(ITestOutputHelper outputHelper) : ActionTests<LibraryApi, LibraryContext>(outputHelper)
{
    protected override Action<IServiceCollection> ConfigureServices => ...
}

// After
[TestClass]
[DoNotParallelize]
public class ActionTests : ActionTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices => ...
}
```

Apply the same shape to all 4 derived files in `FeatureTests/EF6/` and `FeatureTests/EFCore/`.

- [ ] **Step 5: Convert all `[Collection("LibraryApiEFCore")]` consumers**

These are every test class that needs `[DoNotParallelize]` to preserve sequential execution against the shared EF Core LibraryContext:

- `FeatureTests/EFCore/`: `ActionTests.cs`, `AuthorizationTests.cs`, `BatchTests.cs`, `DeepInsertTests.cs`, `DeepUpdateTests.cs`, `ExpandTests.cs`, `FunctionTests.cs`, `InsertTests.cs`, `InTests.cs`, `MetadataTests.cs`, `NamingConventionTests.cs`, `NavigationPropertyTests.cs`, `NoTrackingTests.cs`, `PagingTests.cs`, `QueryTests.cs`, `UpdateTests.cs`, `ValidationOptionsTests.cs`, `ValidationTests.cs`
- `RegressionTests/EFCore/`: `Issue519_SingleNavPropertyFilter.cs`, `Issue541_CountPlusParametersFails.cs`, `Issue671_MultipleContexts.cs` (three `[Collection]` attributes — one per nested test class), `Issue704_DateTimeFilterKind.cs` (two), `Issue714_ComplexTypes.cs`, `Issue759_BatchInsertWithRelatedEntities.cs`
- `IntegrationTests/SpatialTypeIntegrationTests.cs`

For each: add `[TestClass]` and replace every `[Collection("LibraryApiEFCore")]` with `[DoNotParallelize]`. Apply the rest of the conversion recipe to the file (usings + attributes + Theory data).

- [ ] **Step 6: Convert all `[Collection("LibraryApiEF6")]` consumers**

The EF6 collection mirrors EFCore. These test classes also need `[DoNotParallelize]`:

- `FeatureTests/EF6/`: `ActionTests.cs`, `AuthorizationTests.cs`, `BatchTests.cs`, `DeepInsertTests.cs`, `DeepUpdateTests.cs`, `ExpandTests.cs`, `FunctionTests.cs`, `InsertTests.cs`, `InTests.cs`, `MetadataTests.cs`, `NavigationPropertyTests.cs`, `NoTrackingTests.cs`, `OrphanExpandRepro.cs`, `PagingTests.cs`, `QueryTests.cs`, `UpdateTests.cs`, `ValidationOptionsTests.cs`, `ValidationTests.cs`
- `RegressionTests/EF6/`: `Issue541_CountPlusParametersFails.cs`, `Issue671_MultipleContexts.cs` (three nested classes), `Issue714_ComplexTypes.cs`, `Issue759_BatchInsertWithRelatedEntities.cs`

Same transformation: `[TestClass]` + `[DoNotParallelize]` on each class, replacing `[Collection("LibraryApiEF6")]`. Apply the rest of the recipe.

Cross-check: after Steps 5–6, no file under `test/Microsoft.Restier.Tests.AspNetCore/` should contain `[Collection(`. Verify:

```bash
grep -rn "\\[Collection(" test/Microsoft.Restier.Tests.AspNetCore/ --include="*.cs"
```

Expected: empty output.

- [ ] **Step 7: Delete both now-unused collection definitions**

```bash
git rm test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/LibraryApiEFCoreTestCollection.cs
git rm test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/LibraryApiEF6TestCollection.cs
```

- [ ] **Step 8: Convert the four files that use raw xUnit `Assert.*`**

Per recipe section 3a, convert to AwesomeAssertions:
- `Extensions/RestierHttpContextExtensionsTests.cs` (5 `Assert.*` calls, including `Assert.Throws<ArgumentNullException>`)
- `Extensions/RestierHttpRequestExtensionsTests.cs` (4 `Assert.True`/`Assert.False`)
- `Filters/RestierExceptionFilterAttributeTests.cs` (10 calls including `Assert.IsType<T>`)
- `Batch/RestierChangeSetPropertyTests.cs` (6 calls including `Assert.ThrowsAsync<T>`)

For `Assert.IsType<T>` calls that are used both for assertion *and* to get the typed result, use the `.Subject` pattern:

```csharp
// Before
var result = Assert.IsType<ObjectResult>(context.Result);
Assert.Equal((int)HttpStatusCode.BadRequest, result.StatusCode);

// After
var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
result.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
```

- [ ] **Step 9: Apply the conversion recipe to all remaining `*.cs` files in the project**

Every test class needs `[TestClass]`, every `[Fact]`/`[Theory]` swap, every `using Xunit;` swap. There are roughly 105 files left at this point — the change is mechanical and repetitive.

- [ ] **Step 10: Build**: `dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj` — expect success.
- [ ] **Step 11: Test**: `dotnet test ... --no-build` — expect `Passed: N, Skipped: S` matching the baseline. If `[DoNotParallelize]` was missed on a class that relies on it, expect spurious data-contention failures — re-check that every previously-`[Collection]`-decorated class now has `[DoNotParallelize]`.
- [ ] **Step 12: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore
git commit -m "test(aspnetcore): convert from xUnit to MSTest"
```

---

## Task 14: Final full-solution test run

- [ ] **Step 1: Restore + build solution**: `dotnet restore RESTier.slnx && dotnet build RESTier.slnx --configuration Debug` — expect success.
- [ ] **Step 2: Run all tests**: `dotnet test RESTier.slnx --configuration Debug --no-build`
Expected: `Passed: total-N, Failed: 0` across all TFMs. The total should match the sum of the baselines from Tasks 3–13.
- [ ] **Step 3: No commit** (verification only).

---

## Task 15: Remove xUnit packages from `Directory.Build.props`

Now that every project compiles and tests pass under MSTest, remove the dual-framework references.

**Files:**
- Modify: `Directory.Build.props:136-144`

- [ ] **Step 1: Remove xUnit entries from the test-project ItemGroup**

```xml
<ItemGroup Condition=" $(IsTestProject) == 'true' and $(IsSampleProject) != 'true' and $(IsTestSharedProject) != 'true'">
    <PackageReference Include="coverlet.collector" Version="6.*" />
    <PackageReference Include="AwesomeAssertions" Version="8.*" PrivateAssets="All" />
    <PackageReference Include="AwesomeAssertions.Analyzers" Version="0.*" PrivateAssets="All" />
    <PackageReference Include="MSTest.TestFramework" Version="3.*" />
    <PackageReference Include="MSTest.TestAdapter" Version="3.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="NSubstitute" Version="5.3.0"></PackageReference>
</ItemGroup>
```

(Two lines deleted: `xunit.v3` and `xunit.runner.visualstudio`.)

- [ ] **Step 2: Verify no residual xUnit references**

Run: `grep -rn "xunit\|Xunit" test/ src/ --include="*.cs" --include="*.csproj"`
Expected: empty output. Any hits indicate missed references — convert them before continuing.

- [ ] **Step 3: Full solution test**: `dotnet test RESTier.slnx --configuration Debug` — expect `Passed: total-N`.

- [ ] **Step 4: Commit**

```bash
git add Directory.Build.props
git commit -m "test: remove xUnit packages now that all projects use MSTest"
```

---

## Task 16: Add code coverage collection to the Azure pipeline

**Files:**
- Modify: `.pipelines/RESTier-CI.yml:76-87` (the existing `dotnet test` task)
- Modify: `.pipelines/RESTier-CI.yml` — insert three new tasks after `dotnet test`

- [ ] **Step 1: Update the `dotnet test` task to collect coverage**

Replace lines 76–87 of `.pipelines/RESTier-CI.yml` with:

```yaml
    # --- Test ---
    - task: DotNetCoreCLI@2
      displayName: dotnet test
      env:
        ConnectionStrings__LibraryContext: 'Server=(localdb)\MSSQLLocalDB;Initial Catalog=LibraryContext;Trusted_Connection=true;TrustServerCertificate=true'
        ConnectionStrings__MarvelContext: 'Server=(localdb)\MSSQLLocalDB;Initial Catalog=MarvelContext;Trusted_Connection=true;TrustServerCertificate=true'
      inputs:
        command: test
        projects: 'test/**/*.csproj'
        arguments: >
          --configuration $(BuildConfiguration)
          --no-build
          --collect:"XPlat Code Coverage"
          --results-directory $(Agent.TempDirectory)/TestResults
        publishTestResults: true
```

The `--collect:"XPlat Code Coverage"` flag activates `coverlet.collector` (already referenced by every test project) and emits one `coverage.cobertura.xml` per project/TFM into `$(Agent.TempDirectory)/TestResults/<guid>/`.

- [ ] **Step 2: Insert a ReportGenerator install task**

Immediately after the `dotnet test` task, add:

```yaml
    # --- Coverage report ---
    - task: DotNetCoreCLI@2
      displayName: Install ReportGenerator
      inputs:
        command: custom
        custom: tool
        arguments: install dotnet-reportgenerator-globaltool --tool-path $(Agent.TempDirectory)/tools
```

Using `--tool-path` (local install to a known directory) instead of `--global` avoids the standard Windows-agent gotcha where `%USERPROFILE%\.dotnet\tools` is not on `PATH` for subsequent pipeline steps. We invoke the binary by its full path in Step 3 below, so PATH never matters.

- [ ] **Step 3: Insert a ReportGenerator merge/publish task**

After the install task:

```yaml
    - powershell: |
        & "$(Agent.TempDirectory)/tools/reportgenerator.exe" `
          "-reports:$(Agent.TempDirectory)/TestResults/**/coverage.cobertura.xml" `
          "-targetdir:$(Build.ArtifactStagingDirectory)/CoverageReport" `
          "-reporttypes:Cobertura;HtmlInline_AzurePipelines;TextSummary"
        Get-Content "$(Build.ArtifactStagingDirectory)/CoverageReport/Summary.txt"
      displayName: Generate Coverage Report
```

The agent pool is `windows-latest` (see `.pipelines/RESTier-CI.yml:20`), so the binary is `reportgenerator.exe`. The `HtmlInline_AzurePipelines` format renders inline in the Azure DevOps build summary; `TextSummary` is dumped to the log for quick eyeballing.

- [ ] **Step 4: Insert a `PublishCodeCoverageResults@2` task**

After the ReportGenerator task:

```yaml
    - task: PublishCodeCoverageResults@2
      displayName: Publish Code Coverage
      inputs:
        summaryFileLocation: '$(Build.ArtifactStagingDirectory)/CoverageReport/Cobertura.xml'
        pathToSources: '$(Build.SourcesDirectory)'
```

This must be `@2` — the older `@1` task is deprecated and uses a different parameter shape.

- [ ] **Step 5: (Optional) Publish the HTML report as an artifact**

For local download/inspection, append:

```yaml
    - task: PublishPipelineArtifact@1
      displayName: 'Publish Artifact: CoverageReport'
      inputs:
        targetPath: '$(Build.ArtifactStagingDirectory)/CoverageReport'
        artifact: CoverageReport
      condition: succeededOrFailed()
```

- [ ] **Step 6: Lint the YAML**

Open `.pipelines/RESTier-CI.yml` in an editor with YAML schema support (VS Code with the Azure Pipelines extension is best) and confirm no schema errors. Indentation is two-space; reuse the indent of the surrounding steps.

- [ ] **Step 7: Commit**

```bash
git add .pipelines/RESTier-CI.yml
git commit -m "ci(coverage): collect XPlat Code Coverage, merge with ReportGenerator, publish to Azure DevOps"
```

- [ ] **Step 8: Verify in CI**

Push the branch and observe a CI run (or trigger one manually). Expected:
- Build still green
- New "Code Coverage" tab in the Azure DevOps build summary
- Inline HTML coverage report visible
- `CoverageReport` artifact published

---

## Task 17: Update `CLAUDE.md`

**Files:**
- Modify: `CLAUDE.md` (Test Conventions section, around line 82)

- [ ] **Step 1: Edit the Framework line**

Find:

```markdown
- **Framework:** xUnit v3, FluentAssertions (AwesomeAssertions), NSubstitute
```

Replace with:

```markdown
- **Framework:** MSTest 3.x, FluentAssertions (AwesomeAssertions), NSubstitute
```

- [ ] **Step 2: Commit**

```bash
git add CLAUDE.md
git commit -m "docs(claude.md): note MSTest as the test framework"
```

---

## Task 18: Update `.github/CONTRIBUTING.md`

**Files:**
- Modify: `.github/CONTRIBUTING.md` (line 41, Test specification section)

- [ ] **Step 1: Edit the Test specification opening sentence**

Find on line 41:

```markdown
All tests need to be written with xUnit. Here are some rules to follow when you are organizing the test code:
```

Replace with:

```markdown
All tests need to be written with MSTest. Here are some rules to follow when you are organizing the test code:
```

- [ ] **Step 2: Commit**

```bash
git add .github/CONTRIBUTING.md
git commit -m "docs(contributing): MSTest replaces xUnit"
```

---

## Task 19: Update `src/Microsoft.Restier.Docs/contribution-guidelines.mdx`

**Files:**
- Modify: `src/Microsoft.Restier.Docs/contribution-guidelines.mdx` (line 70, Test specification section)

- [ ] **Step 1: Update the framework reference**

Find on line 70:

```markdown
All tests need to be written with **xUnit v3**. Use **FluentAssertions** for assertions and **NSubstitute** for mocking. Here are some rules to follow when you are organizing the
test code:
```

Replace with:

```markdown
All tests need to be written with **MSTest 3.x**. Use **FluentAssertions** for assertions and **NSubstitute** for mocking. Here are some rules to follow when you are organizing the
test code:
```

- [ ] **Step 2: Commit**

```bash
git add src/Microsoft.Restier.Docs/contribution-guidelines.mdx
git commit -m "docs(contribution-guidelines): MSTest replaces xUnit"
```

---

## Task 20: Update `src/Microsoft.Restier.Docs/guides/server/testing.mdx`

This page has both a prose mention of xUnit and full code examples using `[Fact]` and `using Xunit;`. Both need updating.

**Files:**
- Modify: `src/Microsoft.Restier.Docs/guides/server/testing.mdx`

- [ ] **Step 1: Update the prose mention (around line 25)**

Find:

```markdown
You will also need a test framework. RESTier's own tests use xUnit v3, FluentAssertions, and NSubstitute,
but any .NET test framework will work.
```

Replace with:

```markdown
You will also need a test framework. RESTier's own tests use MSTest 3.x, FluentAssertions, and NSubstitute,
but any .NET test framework will work.
```

- [ ] **Step 2: Update the first code example (lines ~36–76)**

Find the `using Xunit;` line and the `[Fact]` attributes. Replace:

```csharp
using Xunit;

public class BookQueryTests
{
    [Fact]
    public async Task GetBooksReturns200()
    {
        // ...
    }

    [Fact]
    public async Task MetadataDocumentIsValid()
    {
        // ...
    }
}
```

with:

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class BookQueryTests
{
    [TestMethod]
    public async Task GetBooksReturns200()
    {
        // ...
    }

    [TestMethod]
    public async Task MetadataDocumentIsValid()
    {
        // ...
    }
}
```

(Keep the bodies — only attributes, usings, and the class-level `[TestClass]` change.)

- [ ] **Step 3: Update the second code example (lines ~103–160)**

Same transformation for the `LibraryApiTests : RestierBreakdanceTestBase<LibraryApi>` example: `using Xunit;` → MSTest using; `[Fact]` → `[TestMethod]`; add `[TestClass]` above the class.

- [ ] **Step 4: Build the docs project to confirm MDX still renders**

Run: `dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/testing.mdx
git commit -m "docs(testing): MSTest examples replace xUnit examples"
```

---

## Task 21: Final verification + branch wrap-up

- [ ] **Step 1: Full clean build**

```bash
dotnet clean RESTier.slnx
dotnet restore RESTier.slnx
dotnet build RESTier.slnx --configuration Release
```

Expected: clean restore + build with warnings-as-errors still set.

- [ ] **Step 2: Full test run with local coverage**

```bash
dotnet test RESTier.slnx --configuration Release --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory TestResults/Coverage
```

Expected: every project's tests pass on every TFM. Cobertura XML files appear under `TestResults/Coverage/<guid>/`.

- [ ] **Step 3: Ensure `reportgenerator` is available locally**

The CI install (Task 16) only touches the CI agent, not the developer machine. The repo has no `dotnet-tools.json` manifest, so a clean machine following this plan has no `reportgenerator` on PATH. Install it as a global tool once:

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool || dotnet tool update --global dotnet-reportgenerator-globaltool
```

The fallback to `update` handles the case where it's already installed (the bare `install` would fail with "tool already installed"). Confirm:

```bash
which reportgenerator || ls ~/.dotnet/tools/reportgenerator
```

Expected: a non-empty path (either on `$PATH` or under `~/.dotnet/tools/`).

- [ ] **Step 4: Generate a local coverage summary**

```bash
~/.dotnet/tools/reportgenerator \
  "-reports:TestResults/Coverage/*/coverage.cobertura.xml" \
  "-targetdir:TestResults/CoverageReport" \
  "-reporttypes:TextSummary"
cat TestResults/CoverageReport/Summary.txt
```

(Use `reportgenerator` without the path prefix if `~/.dotnet/tools/` is on your `PATH`.) Inspect the summary. Note the line/branch coverage percentages as a baseline.

- [ ] **Step 5: Confirm no leftover xUnit references**

```bash
grep -rn "xunit\|Xunit\|\[Fact\]\|\[Theory\]\|ITestOutputHelper\|TheoryDataRow" \
  test/ src/ CLAUDE.md .github/ \
  --include="*.cs" --include="*.csproj" --include="*.md" --include="*.mdx"
```

Sources scanned cover every place Tasks 1–20 modified: `test/` and `src/` for code (the published docs live at `src/Microsoft.Restier.Docs/` and are already covered by `src/`), `CLAUDE.md` (project root — updated by Task 17), and `.github/` (updated by Task 18). **Do not** include `docs/` — that path is agent scratch space (this plan, prior plans, specs) and contains historical mentions of xUnit and `[Fact]` that are intentional and not subject to this migration. Expected: empty output.

- [ ] **Step 6: Push and open PR**

Push the branch (`feature/vnext` or a new feature branch) and open a PR titled e.g. `test: migrate from xUnit v3 to MSTest 3 + add CI code coverage`. Reference this plan path in the PR description.
