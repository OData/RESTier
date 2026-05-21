# xUnit→MSTest migration follow-ups Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the three known migration debts left after the xUnit→MSTest conversion: (1) restore conditional skips on the six spatial tests that were turned into unconditional `[Ignore]`s, (2) replace the leftover xUnit-style `IDisposable` cleanup in `EFQuerySourcerTrackingTests` with MSTest's `[TestCleanup]` (both the EFCore class that already implements `IDisposable` and the EF6 sibling class that needs disposal but has none), and (3) eliminate the solution-level `dotnet test RESTier.slnx` flakiness on net8.0 caused by multiple test-host processes hitting the same LocalDB databases concurrently — without sacrificing the cross-assembly parallelism that the in-memory-only and DB-less projects can safely keep.

**Architecture:** Use MSTest's native idioms throughout: `Assert.Inconclusive("...")` for runtime-conditional skips (preserves "Skipped" outcome while letting the probe actually decide); `[TestCleanup]` for per-test cleanup. For the cross-process LocalDB race, add a *targeted* cross-process named semaphore that **only** the assemblies which actually touch shared LocalDB databases acquire in `[AssemblyInitialize]` (and release in `[AssemblyCleanup]`). Within those assemblies, MSTest's existing `[DoNotParallelize]` markers on `LibraryApi{EFCore,EF6}` consumers continue to govern in-process serialization exactly as the old xUnit `[Collection]` attributes did. Assemblies that don't touch shared LocalDB (`Tests.Core`, `*.Spatial`, `Tests.EntityFrameworkCore` which is in-memory only) never reference the semaphore and continue to run in full parallel across processes. The lock is a named OS primitive (Windows-only — guarded by an `OperatingSystem.IsWindows()` check because LocalDB itself is Windows-only, so non-Windows test hosts simply skip acquisition).

**Tech Stack:**
- **Test framework:** MSTest 3.x (`Assert.Inconclusive`, `[TestCleanup]`, `[AssemblyInitialize]` / `[AssemblyCleanup]`)
- **Cross-process serialization:** `System.Threading.Semaphore` with a global name (`Global\RESTier_SharedLocalDb_AssemblyLock`)
- **Spatial probes:** existing `GeodeticNativeAvailable` (in `DbSpatialConverterTests`) and `SqlServerClrEnabled` (in `SpatialTypeIntegrationTests`) — no probe changes needed, only their consumers

---

## Conditional-skip recipe (reference — used by Tasks 1–2)

xUnit v3's `[Fact(SkipUnless = nameof(Probe))]` had no direct MSTest equivalent, so the conversion turned the six call sites into unconditional `[TestMethod, Ignore("...")]`. MSTest's idiomatic equivalent is to keep the probe call **inside the test body** and short-circuit with `Assert.Inconclusive(...)`. That produces a "Skipped" test outcome (same as `[Ignore]`) when the probe is false, but lets the test actually execute and pass/fail when the probe is true — which is the entire point of `SkipUnless`.

```csharp
// Before — conversion left it unconditionally skipped
[TestMethod, Ignore("Requires Windows-only SqlServerSpatial160.dll (...)")]
public void Round_trips_LineString()
{
    // body
}

// After — runs when the probe succeeds, otherwise reports "Skipped" via Inconclusive
[TestMethod]
public void Round_trips_LineString()
{
    if (!GeodeticNativeAvailable)
    {
        Assert.Inconclusive(
            "Requires Windows-only SqlServerSpatial160.dll (geodesic LineString/Polygon "
            + "validity check). Install Microsoft.SqlServer.Types and call "
            + "SqlServerTypes.Utilities.LoadNativeAssemblies(...) at startup to enable.");
    }

    // body unchanged
}
```

Important rules:
- **Do not** remove the existing skip messages — they are documentation for whoever reads test output. Move the exact prose into the `Assert.Inconclusive(...)` argument.
- **Do not** wrap the body in an `if (Probe) { ... }` block — the inconclusive call must be the first statement so MSTest reports the test as skipped before touching anything else.
- **Do not** memoize the probe at class load time — the existing probes already handle that internally (`SqlServerClrEnabled` uses a `_clrProbeResult` cache; `GeodeticNativeAvailable` is cheap enough on the success path that it can stay un-cached).

---

## File map

**Modified, tests:**
- `test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs` — 2 test bodies (Task 1)
- `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs` — 4 test bodies (Task 2)
- `test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/EFQueryNoTrackingTests.cs` — `EFQuerySourcerTrackingTests` class only (Task 3)
- `test/Microsoft.Restier.Tests.EntityFramework/Query/EFQueryNoTrackingTests.cs` — `EFQuerySourcerTrackingTests` class only (Task 4)

**Created, shared infrastructure (Task 5):**
- `test/Microsoft.Restier.Tests.Shared/SharedLocalDbLock.cs` — process-wide named-semaphore helper

**Created, per LocalDB-consuming assembly (Task 5):**
- `test/Microsoft.Restier.Tests.AspNetCore/AssemblyHooks.cs`
- `test/Microsoft.Restier.Tests.AspNetCore.NSwag/AssemblyHooks.cs`
- `test/Microsoft.Restier.Tests.AspNetCore.Swagger/AssemblyHooks.cs`
- `test/Microsoft.Restier.Tests.AspNetCore.Versioning/AssemblyHooks.cs`
- `test/Microsoft.Restier.Tests.EntityFramework/AssemblyHooks.cs`

**Why each of those five and only those five?** Verified by grep (`MSSQLLocalDB | LibraryContext | MarvelContext | ExecuteTestRequest | GetApiMetadata | RestierTestHelpers`):

| Assembly | LocalDB-touching? | Adds AssemblyHooks? |
|---|---|---|
| `Tests.AspNetCore` | Yes (82 hits, Breakdance + `LibraryContext` on LocalDB) | **Yes** |
| `Tests.AspNetCore.NSwag` | Yes (`CombinedAppTests`, `KeylessViewOpenApiTests` use Breakdance with `LibraryContext`) | **Yes** |
| `Tests.AspNetCore.Swagger` | Yes (`Issue766_PrimitiveParamOperationTests`, `KeylessViewOpenApiTests` use Breakdance) | **Yes** |
| `Tests.AspNetCore.Versioning` | Yes (`SwaggerIntegrationTests`, `NSwagIntegrationTests`, fixtures use Breakdance) | **Yes** |
| `Tests.EntityFramework` | Yes (`ChangeSetPreparerTests` uses `LibraryContext` against real EF6 SQL) | **Yes** |
| `Tests.EntityFrameworkCore` | No (every consumer uses `UseInMemoryDatabase($"…-{Guid.NewGuid()}")` — unique per test instance, no shared state) | No |
| `Tests.EntityFramework.Spatial`, `Tests.EntityFrameworkCore.Spatial` | No (offline `DbSpatialConverter` round-trips; no DB) | No |
| `Tests.Core` | No (no DB references) | No |

---

## Task 1: Restore conditional skip for the two DbSpatialConverter geodesic round-trip tests

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs:89-99` (`Round_trips_LineString`)
- Modify: `test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs:101-111` (`Round_trips_Polygon`)

- [ ] **Step 1: Baseline test run**

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --configuration Debug
```

Record `Passed: N, Skipped: S` for each TFM. Expected before this task: `Skipped` includes the two Round_trips_LineString / Round_trips_Polygon tests for each TFM (so S ≥ 2 per TFM).

- [ ] **Step 2: Edit `Round_trips_LineString`**

Replace the existing attribute + body with:

```csharp
[TestMethod]
public void Round_trips_LineString()
{
    if (!GeodeticNativeAvailable)
    {
        Assert.Inconclusive(
            "Requires Windows-only SqlServerSpatial160.dll (geodesic LineString/Polygon "
            + "validity check). Install Microsoft.SqlServer.Types and call "
            + "SqlServerTypes.Utilities.LoadNativeAssemblies(...) at startup to enable.");
    }

    var original = DbGeography.FromText("LINESTRING(0 0, 1 1, 2 2)", 4326);

    var edm = (GeographyLineString)_converter.ToEdm(original, typeof(GeographyLineString));
    var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

    roundTrip.AsText().Should().Be(original.AsText());
    roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
}
```

(The body is byte-identical to the existing code — only the `[TestMethod, Ignore("...")]` attribute is replaced with `[TestMethod]` and the `Assert.Inconclusive` guard.)

- [ ] **Step 3: Edit `Round_trips_Polygon`**

Replace the existing attribute + body with:

```csharp
[TestMethod]
public void Round_trips_Polygon()
{
    if (!GeodeticNativeAvailable)
    {
        Assert.Inconclusive(
            "Requires Windows-only SqlServerSpatial160.dll (geodesic LineString/Polygon "
            + "validity check). Install Microsoft.SqlServer.Types and call "
            + "SqlServerTypes.Utilities.LoadNativeAssemblies(...) at startup to enable.");
    }

    var original = DbGeography.FromText("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))", 4326);

    var edm = (GeographyPolygon)_converter.ToEdm(original, typeof(GeographyPolygon));
    var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

    roundTrip.AsText().Should().Be(original.AsText());
    roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
}
```

- [ ] **Step 4: Build the project**

```bash
dotnet build test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --configuration Debug
```

Expected: build succeeds with no errors.

- [ ] **Step 5: Run the project's tests**

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --configuration Debug --no-build
```

Expected per TFM, on a machine where `SqlServerSpatial160.dll` is **not** installed (i.e. macOS dev box, or Windows without the native binary):
- `Passed: N` matches the Step 1 baseline `Passed: N`.
- `Skipped` is exactly the same count as Step 1 (the two tests now report Inconclusive instead of Ignored; both count as "Skipped" in MSTest's output).
- `Failed: 0`.

On a Windows machine with the native binary loadable, the same command should now report **two more `Passed`** and two fewer `Skipped` than Step 1. The user is unlikely to have this configuration locally; CI's `windows-latest` agent does **not** ship the native binary by default, so the count will match the baseline there too. Either outcome is acceptable; we are restoring the *conditional* behavior, not asserting which branch is taken.

- [ ] **Step 6: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs
git commit -m "test(ef.spatial): restore conditional skip on geodesic round-trip tests"
```

---

## Task 2: Restore conditional skip for the four SpatialTypeIntegrationTests CLR-spatial tests

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs:179-193` (`EFCore_Filter_GeoDistance_TranslatesAndReturnsSeededRow`)
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs:199-211` (`EFCore_Filter_GeoLength_TranslatesPropertyAccess`)
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs:218-230` (`EFCore_Filter_GeoIntersects_TranslatesMethodCall`)
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs:236-248` (`EFCore_Filter_GeoDistance_PathSegmentSyntax_TranslatesToo`)

- [ ] **Step 1: Baseline test run**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --configuration Debug --filter "FullyQualifiedName~SpatialTypeIntegrationTests"
```

Record `Passed: N, Skipped: S` for each TFM. Expected: `Skipped` includes these four CLR-gated tests on machines where SQL Server CLR is disabled (which is the dev box and the LocalDB-only CI agent).

- [ ] **Step 2: Edit `EFCore_Filter_GeoDistance_TranslatesAndReturnsSeededRow`**

Replace the existing attribute + body with:

```csharp
[TestMethod]
public async Task EFCore_Filter_GeoDistance_TranslatesAndReturnsSeededRow()
{
    if (!SqlServerClrEnabled)
    {
        Assert.Inconclusive(
            "Requires SQL Server CLR for geography spatial method execution "
            + "(sp_configure 'clr enabled', 1).");
    }

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

- [ ] **Step 3: Edit `EFCore_Filter_GeoLength_TranslatesPropertyAccess`**

Replace the existing attribute + body with:

```csharp
[TestMethod]
public async Task EFCore_Filter_GeoLength_TranslatesPropertyAccess()
{
    if (!SqlServerClrEnabled)
    {
        Assert.Inconclusive(
            "Requires SQL Server CLR for geography spatial method execution "
            + "(sp_configure 'clr enabled', 1).");
    }

    var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(
        HttpMethod.Get,
        resource: "/SpatialPlaces?$filter=geo.length(RouteLine) gt 0",
        serviceCollection: _configureServices);
    var content = await TraceListener.LogAndReturnMessageContentAsync(response);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    content.Should().Contain("\"Name\":\"Spatial Place 1\"",
        "the seeded RouteLine LINESTRING(0 0, 1 1, 2 2) has positive length");
}
```

- [ ] **Step 4: Edit `EFCore_Filter_GeoIntersects_TranslatesMethodCall`**

Replace the existing attribute + body with:

```csharp
[TestMethod]
public async Task EFCore_Filter_GeoIntersects_TranslatesMethodCall()
{
    if (!SqlServerClrEnabled)
    {
        Assert.Inconclusive(
            "Requires SQL Server CLR for geography spatial method execution "
            + "(sp_configure 'clr enabled', 1).");
    }

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

- [ ] **Step 5: Edit `EFCore_Filter_GeoDistance_PathSegmentSyntax_TranslatesToo`**

Replace the existing attribute + body with:

```csharp
[TestMethod]
public async Task EFCore_Filter_GeoDistance_PathSegmentSyntax_TranslatesToo()
{
    if (!SqlServerClrEnabled)
    {
        Assert.Inconclusive(
            "Requires SQL Server CLR for geography spatial method execution "
            + "(sp_configure 'clr enabled', 1).");
    }

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

- [ ] **Step 6: Build the project**

```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --configuration Debug
```

Expected: build succeeds with no errors.

- [ ] **Step 7: Run the affected tests**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --configuration Debug --no-build --filter "FullyQualifiedName~SpatialTypeIntegrationTests"
```

Expected: same `Passed` and `Skipped` totals as the Step 1 baseline. On a CLR-enabled SQL Server the four formerly-skipped tests will now run for real (and likely pass); on a default LocalDB box they'll report Inconclusive ("Skipped"). Either outcome matches the baseline counts within the "we restored conditional behavior" goal.

- [ ] **Step 8: Run the full AspNetCore test project to confirm no regressions**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --configuration Debug --no-build
```

Expected: `Failed: 0`. `Passed: N` and `Skipped: S` match the project's pre-task baseline (offset only by the four tests whose state may have moved from Skipped to Passed if CLR is enabled).

- [ ] **Step 9: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs
git commit -m "test(aspnetcore): restore conditional skip on CLR-gated spatial filter tests"
```

---

## Task 3: Replace `IDisposable` with `[TestCleanup]` in EFCore `EFQuerySourcerTrackingTests`

The class currently declares `IDisposable` + `Dispose()` and relies on xUnit's per-test dispose contract. MSTest does **not** call `Dispose()` automatically — each test creates a fresh instance via the constructor (so `context` is reset per test, which is why tests appear to pass), but the previous instance's `LibraryContext` is left to GC. Today this is benign for in-memory contexts, but it's a correctness bug waiting for a test that subscribes the context to long-lived events. Convert to MSTest's idiomatic per-test cleanup.

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/EFQueryNoTrackingTests.cs:92-104`

- [ ] **Step 1: Baseline test run**

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj --configuration Debug --no-build --filter "FullyQualifiedName~EFQuerySourcerTrackingTests"
```

Record `Passed: N, Failed: 0` for each TFM. Expected: all 5 tests pass.

- [ ] **Step 2: Edit the class declaration and disposal**

Open `test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/EFQueryNoTrackingTests.cs`. Find the existing class declaration:

```csharp
[ExcludeFromCodeCoverage]
[TestClass]
public class EFQuerySourcerTrackingTests : IDisposable
{
    private readonly LibraryContext context;

    public EFQuerySourcerTrackingTests()
    {
        var options = new DbContextOptionsBuilder<LibraryContext>()
            .UseInMemoryDatabase($"sourcer-{Guid.NewGuid()}")
            .Options;
        context = new LibraryContext(options);
    }

    public void Dispose() => context?.Dispose();
```

Replace with:

```csharp
[ExcludeFromCodeCoverage]
[TestClass]
public class EFQuerySourcerTrackingTests
{
    private readonly LibraryContext context;

    public EFQuerySourcerTrackingTests()
    {
        var options = new DbContextOptionsBuilder<LibraryContext>()
            .UseInMemoryDatabase($"sourcer-{Guid.NewGuid()}")
            .Options;
        context = new LibraryContext(options);
    }

    [TestCleanup]
    public void Cleanup() => context?.Dispose();
```

Three changes: remove `: IDisposable`, remove `Dispose()` method, add `[TestCleanup] Cleanup()` calling the same disposal logic.

- [ ] **Step 3: Verify the `using System;` for IDisposable is no longer the only reason `using System;` exists**

Read the using block at the top of the file. `using System;` is already required by `Guid.NewGuid()` (line ~99). No using changes needed — leave the block as-is.

- [ ] **Step 4: Build the project**

```bash
dotnet build test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj --configuration Debug
```

Expected: build succeeds with no errors.

- [ ] **Step 5: Run the affected tests**

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj --configuration Debug --no-build --filter "FullyQualifiedName~EFQuerySourcerTrackingTests"
```

Expected: `Passed: N` matches Step 1 baseline. No new failures.

- [ ] **Step 6: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/EFQueryNoTrackingTests.cs
git commit -m "test(efcore): replace IDisposable with [TestCleanup] in EFQuerySourcerTrackingTests"
```

---

## Task 4: Add `[TestCleanup]` to EF6 `EFQuerySourcerTrackingTests`

The EF6 sibling class **never had** an `IDisposable` implementation (xUnit migration didn't add one because the original test pre-dates the EFCore version's pattern), but it does construct a `TrackingTestContext` (an EF6 `DbContext`) per test via field initializer. That context is never disposed, and unlike the in-memory EFCore variant it's a real `DbContext` with internal `IDisposable` connections — even if `Database.SetInitializer<TrackingTestContext>(null)` means no SQL connection is opened, leaving instances un-disposed accumulates finalisation pressure during a full test run.

Add the same `[TestCleanup]` pattern as Task 3 for parity and for cleanliness.

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFramework/Query/EFQueryNoTrackingTests.cs:122-124`

- [ ] **Step 1: Baseline test run**

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj --configuration Debug --no-build --filter "FullyQualifiedName~EFQuerySourcerTrackingTests"
```

Record `Passed: N` for each TFM. Expected: all 5 tests pass.

- [ ] **Step 2: Edit the class declaration to add `[TestCleanup]`**

Open `test/Microsoft.Restier.Tests.EntityFramework/Query/EFQueryNoTrackingTests.cs`. Find the class declaration:

```csharp
[ExcludeFromCodeCoverage]
[TestClass]
public class EFQuerySourcerTrackingTests
{
    private readonly TrackingTestContext context = new TrackingTestContext();
```

Replace with:

```csharp
[ExcludeFromCodeCoverage]
[TestClass]
public class EFQuerySourcerTrackingTests
{
    private readonly TrackingTestContext context = new TrackingTestContext();

    [TestCleanup]
    public void Cleanup() => context?.Dispose();
```

(Insert the two-line `[TestCleanup]` method immediately after the `context` field. Do not change any other line in the class — the test methods, the xmldoc above the class, and the closing brace all stay put.)

- [ ] **Step 3: Build the project**

```bash
dotnet build test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj --configuration Debug
```

Expected: build succeeds with no errors.

- [ ] **Step 4: Run the affected tests**

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj --configuration Debug --no-build --filter "FullyQualifiedName~EFQuerySourcerTrackingTests"
```

Expected: `Passed: N` matches Step 1 baseline.

- [ ] **Step 5: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFramework/Query/EFQueryNoTrackingTests.cs
git commit -m "test(ef): dispose TrackingTestContext via [TestCleanup] in EFQuerySourcerTrackingTests"
```

---

## Task 5: Serialise LocalDB-touching assemblies cross-process via a named semaphore

`dotnet test RESTier.slnx` runs each test assembly × TFM in a separate vstest host process. On net8.0 the migration intermittently shows two of those processes contending on the same `LocalDB.LibraryContext` database — typically one EF6 host trying to drop/recreate the DB while an AspNetCore host has it open. Inside an assembly, MSTest's existing `[DoNotParallelize]` markers (the migrated successors of the old xUnit `[Collection("LibraryApiEFCore")]` / `[Collection("LibraryApiEF6")]` definitions) preserve the previous serialization semantics. What was missing — and what this task adds — is the **cross-process** half of the same idea: assemblies that hit the shared LocalDB databases acquire a named OS semaphore in `[AssemblyInitialize]` and release it in `[AssemblyCleanup]`, so two such hosts never run simultaneously. Assemblies that don't touch shared LocalDB (`Tests.Core`, `Tests.EntityFrameworkCore` which is in-memory only, `*.Spatial` which is offline-converter-only) never reference the semaphore and continue to parallelize fully.

**Why a named OS semaphore and not `MaxCpuCount=1`?** `MaxCpuCount=1` would force every assembly — including the DB-less ones — to run serially, throwing away the cross-assembly parallelism for `Tests.Core` and the others. A named semaphore is surgical: only the five LocalDB-touching assemblies queue against each other; the other ~7 keep running in parallel.

**Why a *named* semaphore (vs. `static Semaphore` field)?** Because vstest spawns each assembly as a **separate process**. A `static` field can't be shared across processes — every host gets its own. Only an OS-named primitive crosses process boundaries on the same machine.

**Why a semaphore and not a named mutex?** Named mutexes are thread-affine — acquisition and release must happen on the same thread. MSTest's `[AssemblyInitialize]` and `[AssemblyCleanup]` are static methods that the framework can dispatch on different threads. Named semaphores are not thread-affine — any thread can `Release()` what another thread `Wait()`'d on. Safer here.

**Files:**
- Create: `test/Microsoft.Restier.Tests.Shared/SharedLocalDbLock.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/AssemblyHooks.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/AssemblyHooks.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Swagger/AssemblyHooks.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/AssemblyHooks.cs`
- Create: `test/Microsoft.Restier.Tests.EntityFramework/AssemblyHooks.cs`

- [ ] **Step 1: Capture the flake to confirm reproduction (best-effort)**

The race is intermittent (~10–30% on net8.0 per the migration notes). Run the full solution test once or twice:

```bash
dotnet test RESTier.slnx --configuration Debug --no-build
```

If flakiness reproduces, jot down one failing test name to re-check after Step 8. If it doesn't reproduce, proceed anyway — the fix is correct regardless and CI is the ultimate verifier.

- [ ] **Step 2: Create the shared lock helper**

Create `test/Microsoft.Restier.Tests.Shared/SharedLocalDbLock.cs` with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Restier.Tests.Shared
{
    /// <summary>
    /// Cross-process named semaphore used by test assemblies that share access to
    /// the LocalDB-backed LibraryContext / MarvelContext databases. Assemblies that
    /// hit those databases acquire the lock in <c>[AssemblyInitialize]</c> and
    /// release it in <c>[AssemblyCleanup]</c>, serialising their test-host
    /// processes against each other regardless of TFM or project. Assemblies that
    /// do not touch shared LocalDB resources do not reference this lock and run in
    /// full parallel.
    /// </summary>
    /// <remarks>
    /// Named OS semaphores in .NET are Windows-only (Unix throws
    /// <c>PlatformNotSupportedException</c> for the named-constructor overload).
    /// LocalDB itself is also Windows-only, so on non-Windows hosts the lock is a
    /// no-op — the tests that would need the lock are either skipped or hit
    /// in-memory stores instead.
    /// </remarks>
    public static class SharedLocalDbLock
    {
        // The "Global\" prefix scopes the semaphore to all sessions on the
        // machine, so two `dotnet test` processes — even started from different
        // user sessions — synchronise correctly. For per-user scoping use
        // "Local\". "Global\" is the right default for CI agents and
        // dev-box solution runs.
        private const string Name = @"Global\RESTier_SharedLocalDb_AssemblyLock";

        private static Semaphore _semaphore;

        /// <summary>
        /// Acquires the cross-process lock. Call from <c>[AssemblyInitialize]</c>.
        /// On non-Windows hosts this method is a no-op.
        /// </summary>
        public static void Acquire()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            // initialCount=1, maximumCount=1 → mutual exclusion. The out parameter
            // (createdNew) is intentionally discarded: we don't care which process
            // created the OS handle first.
            _semaphore = new Semaphore(initialCount: 1, maximumCount: 1, name: Name, out _);
            _semaphore.WaitOne();
        }

        /// <summary>
        /// Releases the cross-process lock. Call from <c>[AssemblyCleanup]</c>.
        /// Safe to call when <see cref="Acquire"/> was a no-op.
        /// </summary>
        public static void Release()
        {
            if (_semaphore is null)
            {
                return;
            }

            _semaphore.Release();
            _semaphore.Dispose();
            _semaphore = null;
        }
    }
}
```

- [ ] **Step 3: Add `AssemblyHooks.cs` to `Tests.AspNetCore`**

Create `test/Microsoft.Restier.Tests.AspNetCore/AssemblyHooks.cs` with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore
{
    /// <summary>
    /// Per-assembly setup that acquires <see cref="SharedLocalDbLock"/> for the
    /// duration of this assembly's test-host process. See SharedLocalDbLock for
    /// the rationale (cross-process serialisation against other LocalDB-touching
    /// test assemblies).
    /// </summary>
    [TestClass]
    public static class AssemblyHooks
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext _) => SharedLocalDbLock.Acquire();

        [AssemblyCleanup]
        public static void AssemblyCleanup() => SharedLocalDbLock.Release();
    }
}
```

- [ ] **Step 4: Add `AssemblyHooks.cs` to `Tests.AspNetCore.NSwag`**

Create `test/Microsoft.Restier.Tests.AspNetCore.NSwag/AssemblyHooks.cs` with the same body as Step 3, but with the namespace changed to `Microsoft.Restier.Tests.AspNetCore.NSwag`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag
{
    [TestClass]
    public static class AssemblyHooks
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext _) => SharedLocalDbLock.Acquire();

        [AssemblyCleanup]
        public static void AssemblyCleanup() => SharedLocalDbLock.Release();
    }
}
```

- [ ] **Step 5: Add `AssemblyHooks.cs` to `Tests.AspNetCore.Swagger`**

Create `test/Microsoft.Restier.Tests.AspNetCore.Swagger/AssemblyHooks.cs` with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Swagger
{
    [TestClass]
    public static class AssemblyHooks
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext _) => SharedLocalDbLock.Acquire();

        [AssemblyCleanup]
        public static void AssemblyCleanup() => SharedLocalDbLock.Release();
    }
}
```

- [ ] **Step 6: Add `AssemblyHooks.cs` to `Tests.AspNetCore.Versioning`**

Create `test/Microsoft.Restier.Tests.AspNetCore.Versioning/AssemblyHooks.cs` with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning
{
    [TestClass]
    public static class AssemblyHooks
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext _) => SharedLocalDbLock.Acquire();

        [AssemblyCleanup]
        public static void AssemblyCleanup() => SharedLocalDbLock.Release();
    }
}
```

- [ ] **Step 7: Add `AssemblyHooks.cs` to `Tests.EntityFramework`**

Create `test/Microsoft.Restier.Tests.EntityFramework/AssemblyHooks.cs` with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFramework
{
    [TestClass]
    public static class AssemblyHooks
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext _) => SharedLocalDbLock.Acquire();

        [AssemblyCleanup]
        public static void AssemblyCleanup() => SharedLocalDbLock.Release();
    }
}
```

Note: deliberately **not** adding this file to `Tests.EntityFrameworkCore` — that project uses `UseInMemoryDatabase` exclusively (verified by grep), so its hosts have no LocalDB to contend over. Same reasoning excludes `Tests.Core`, `Tests.EntityFramework.Spatial`, and `Tests.EntityFrameworkCore.Spatial`.

- [ ] **Step 8: Build the solution to confirm everything compiles**

```bash
dotnet build RESTier.slnx --configuration Debug
```

Expected: build succeeds with no errors. The `SharedLocalDbLock` symbol resolves via the `ProjectReference` each of these projects already has to `Tests.Shared` (verified during the original migration — Tests.Shared exposes types consumed by every real test project).

- [ ] **Step 9: Run the full solution tests and confirm flakiness is gone**

```bash
dotnet test RESTier.slnx --configuration Debug --no-build
```

Expected: `Failed: 0` across all TFMs. If you captured a flaky test name in Step 1, re-run this command 3 times — all 3 runs must report 0 failures. Wall-clock time should be close to parallel (only the five LocalDB-touching assemblies wait for each other; the seven others still run in full parallel), with one strict-sequential chain of five processes per TFM.

- [ ] **Step 10: Confirm parallelism for non-LocalDB assemblies is intact**

Sanity-check: when you start the test run, watch the test runner output (or `ps`/Task Manager) to confirm that `Tests.Core` and at least one `Tests.AspNetCore.*` host start simultaneously — they should, because `Tests.Core` doesn't acquire the lock. If they appear strictly sequential, the lock is being acquired by something it shouldn't be (re-check Steps 3–7 for an `AssemblyHooks.cs` accidentally added to the wrong project).

- [ ] **Step 11: Confirm CI test step is unaffected**

`.pipelines/RESTier-CI.yml:83-96` invokes `dotnet test` against `test/**/*.csproj` with no special settings. The lock is honored automatically because the `AssemblyHooks` classes are part of the respective assemblies. CI was reportedly already clean; with this change it becomes deterministically clean rather than incidentally clean. No CI YAML edit is required.

- [ ] **Step 12: Commit**

```bash
git add test/Microsoft.Restier.Tests.Shared/SharedLocalDbLock.cs \
        test/Microsoft.Restier.Tests.AspNetCore/AssemblyHooks.cs \
        test/Microsoft.Restier.Tests.AspNetCore.NSwag/AssemblyHooks.cs \
        test/Microsoft.Restier.Tests.AspNetCore.Swagger/AssemblyHooks.cs \
        test/Microsoft.Restier.Tests.AspNetCore.Versioning/AssemblyHooks.cs \
        test/Microsoft.Restier.Tests.EntityFramework/AssemblyHooks.cs
git commit -m "test: serialise LocalDB-touching test assemblies via cross-process semaphore"
```

---

## Task 6: Final verification

- [ ] **Step 1: Full clean build**

```bash
dotnet clean RESTier.slnx
dotnet restore RESTier.slnx
dotnet build RESTier.slnx --configuration Debug
```

Expected: clean build, warnings-as-errors honored.

- [ ] **Step 2: Full solution test**

```bash
dotnet test RESTier.slnx --configuration Debug --no-build
```

Expected: `Failed: 0` across all TFMs. `Skipped` counts include the six spatial tests on non-CLR / non-native-binary boxes — that's the conditional skip working as intended.

- [ ] **Step 3: Sanity-check no `[Ignore]` lingers on the six conditional-skip tests**

```bash
grep -nH "Ignore(" test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs
```

Expected: empty output. Any hit means a Task 1 or Task 2 edit was missed.

- [ ] **Step 4: Sanity-check no `IDisposable` lingers on `EFQuerySourcerTrackingTests`**

```bash
grep -nH "EFQuerySourcerTrackingTests" test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/EFQueryNoTrackingTests.cs test/Microsoft.Restier.Tests.EntityFramework/Query/EFQueryNoTrackingTests.cs
grep -nH "IDisposable" test/Microsoft.Restier.Tests.EntityFrameworkCore/Query/EFQueryNoTrackingTests.cs test/Microsoft.Restier.Tests.EntityFramework/Query/EFQueryNoTrackingTests.cs
```

Expected: the first grep shows the class declarations (without `: IDisposable`); the second grep shows empty output.

- [ ] **Step 5: Confirm `AssemblyHooks.cs` exists in exactly the three LocalDB-touching projects**

```bash
find test -maxdepth 2 -name AssemblyHooks.cs | sort
```

Expected output, exactly three lines, in alphabetical order:

```
test/Microsoft.Restier.Tests.AspNetCore/AssemblyHooks.cs
test/Microsoft.Restier.Tests.EntityFramework/AssemblyHooks.cs
test/Microsoft.Restier.Tests.EntityFrameworkCore/AssemblyHooks.cs
```

The original Task 5 plan listed five projects, but `Tests.AspNetCore.{NSwag,Swagger,Versioning}` were found during execution to use in-process `TestHost` with infrastructure that never touches LocalDB (no `LibraryContext`/`LibraryApi`/`RestierTestHelpers`/`AddEntityFrameworkServices` references) — those three were correctly excluded by the fix-up commit. Conversely, `Tests.EntityFrameworkCore` was originally excluded on the false assumption that it was in-memory only, but `EFModelBuilderTests`/`EFModelMapperTests`/`EFCoreDbContextExtensionsTests` all call `AddEntityFrameworkServices<LibraryContext>()` which wires `UseSqlServer(...)` against the shared `LibraryContext_*_EFCore` LocalDB database — that one was correctly added by the same fix-up. Any deviation from these three means a Task 5 step was missed or a regression slipped in.

- [ ] **Step 6: Confirm the lock is acquired exactly once per AssemblyHooks file**

```bash
grep -nHE "SharedLocalDbLock\.(Acquire|Release)" test/*/AssemblyHooks.cs | sort
```

Expected: six lines (three files × one Acquire + one Release each), all referencing `SharedLocalDbLock.Acquire()` or `SharedLocalDbLock.Release()`.

- [ ] **Step 7: No commit** (verification only). If everything's green, the branch is ready to push.
