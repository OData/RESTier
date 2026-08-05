# EF6 Spatial Tests — Microsoft.SqlServer.Types 160.x Wiring (Implementation Plan)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **History:** v1 of this plan recommended `dotMorten.Microsoft.SqlServer.Types`. A spike on 2026-05-13 falsified that approach (dotMorten ships unsigned with `Version=2.5.0.0`, which EF6's `SqlTypesAssemblyLoader` rejects because it expects `PublicKeyToken=89845dcd8080cc91`). The same spike found that the **official `Microsoft.SqlServer.Types 160.1000.6`** package clears the loader and — surprisingly — the converter's WKT round-trip runs end-to-end on macOS with the managed types alone, without ever loading `SqlServerSpatial160.dll`. v2 (this version) pivots to the official package.

**Goal:** Unskip the EF6 spatial round-trip tests in `Microsoft.Restier.Tests.EntityFramework.Spatial` by adding the official `Microsoft.SqlServer.Types 160.x` package as a test-only dependency, and document the EF6-on-.NET-5+ situation for users.

**Architecture:** RESTier's source projects all target `net8.0;net9.0;net10.0` only. EF6's spatial bridge (`DbGeography.FromText`, `DbSpatialServices.Default.AsText…`) reflects into the `Microsoft.SqlServer.Types` assembly at runtime via a hardcoded strong-name list in `SqlTypesAssemblyLoader`. The official `Microsoft.SqlServer.Types 160.1000.6` package ships `lib/netstandard2.1/Microsoft.SqlServer.Types.dll` with `PublicKeyToken=89845dcd8080cc91, Version=16.0.0.0` — both inside EF6's accepted range, verified empirically. We add the package to the spatial test project (only there — downstream consumers stay free to pick their own backing assembly) and remove the `SqlServerTypesAvailable` probe + `Skip`/`SkipUnless` markers.

**Why not dotMorten:** dotMorten 2.5.0 ships unsigned (`PublicKeyToken=null`) with `Version=2.5.0.0`. EF6 loads via `Assembly.Load(strongName)` against a hardcoded list keyed on the Microsoft public-key token + versions 10–14 (and apparently 16 — see spike). dotMorten fails both checks. The spike implementer verified this end-to-end: all spatial tests failed with EF6's stock error *"Spatial types and functions are not available for this provider because the assembly 'Microsoft.SqlServer.Types' version 10 or higher could not be found"*.

**Native-binary scope:** The 160.x package also ships Windows-only native binaries (`SqlServerSpatial160.dll`) used by computational-geometry operations like `STEquals`, `STIntersects`, `STDistance`. Three of the round-trip tests currently assert via `roundTrip.SpatialEquals(original)`. Even on Windows, `SpatialEquals` requires `SqlServerTypes.Utilities.LoadNativeAssemblies(...)` to be called once at process startup — a fixture we'd rather avoid. We rewrite those three assertions to compare on `AsText()` + `CoordinateSystemId` instead — which is byte-exact (strictly stronger than tolerance-based `SpatialEquals`) and uses only the managed code path that the spike proved works on macOS without any native loader.

**Pre-existing test bug surfaced by the spike:** `ToEdm_returns_GeographyPoint_for_DbGeography_Point` asserts via `result.Should().BeOfType<GeographyPoint>()`. `BeOfType<T>` requires *exact* type equality, but `Microsoft.Spatial.ToEdm` always returns `GeographyPointImplementation` (a concrete subclass). The assertion was unreachable while the test was skipped; with the test running, it fails. Fix: rewrite the assertion to an explicit cast `var point = (GeographyPoint)_converter.ToEdm(...)`, matching the pattern used by the other tests in the same file.

The defensive try/catch in `LibraryTestInitializer.cs:208-230` stays in place — it correctly degrades for EF6 test runners that don't install the package.

**Tech Stack:** C# (.NET 8/9/10), Entity Framework 6.5.x, `Microsoft.SqlServer.Types` 160.1000.6, Microsoft.Spatial, xUnit v3, AwesomeAssertions (imported as `FluentAssertions`), Mintlify MDX docs.

**Spec / context:** No formal spec — direct user request after observing that EF6 spatial tests skip on Windows .NET 8/9/10 because `Microsoft.SqlServer.Types` is .NET-Framework-only by default. See conversation memory: skip messages claim "Windows / SQL Server only" but the real requirement is a strong-named EF6-compatible build of `Microsoft.SqlServer.Types`, which the official 160.x package provides for `netstandard2.1`.

---

## Conventions

- **Targets:** net8.0, net9.0, net10.0 (no net48 — solution-wide convention).
- **Brace style:** Allman. `var` preferred. Curly braces even for single-line blocks.
- **Warnings as errors:** enabled globally — code must be warning-clean.
- **Implicit usings disabled:** every `using` directive must be explicit.
- **Test framework:** xUnit v3 (`[Fact]`, `[Theory]`), AwesomeAssertions (`Should()`), NSubstitute (`Substitute.For<T>()`).
- **Package scope:** `Microsoft.SqlServer.Types` is a **test-only** dependency. Do **not** add it to `src/Microsoft.Restier.EntityFramework.Spatial.csproj` — downstream consumers must pick their own backing assembly (e.g. some users prefer to ship without the native dll on Linux deployments and avoid `SpatialEquals` server-side).
- **Commits:** small and focused; one per task. Co-author lines as the existing repo uses.

---

## File Inventory

| File | Action | Purpose |
|------|--------|---------|
| `test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj` | Modify | Add `<PackageReference>` to `Microsoft.SqlServer.Types` 160.1000.6 (the latest stable in the 16.x line). |
| `test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs` | Modify | Remove `SqlServerTypesAvailable` probe property and every `Skip` / `SkipUnless` argument. Rewrite the three `SpatialEquals`-based assertions (`Round_trip_preserves_value`, `Round_trips_LineString`, `Round_trips_Polygon`) to compare `AsText()` + `CoordinateSystemId` instead — `SpatialEquals` requires Windows-only native binaries. Fix the pre-existing `BeOfType<GeographyPoint>` assertion bug in `ToEdm_returns_GeographyPoint_for_DbGeography_Point` (Microsoft.Spatial returns `GeographyPointImplementation`, a subclass — switch to explicit cast). |
| `test/Microsoft.Restier.Tests.EntityFramework.Spatial/EFChangeSetInitializerSpatialTests.cs` | Modify | Remove `SqlServerTypesAvailable` probe property and the `Skip` / `SkipUnless` argument. Test bodies untouched (no `SpatialEquals` use). |
| `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx` | Modify | Add a "Running EF6 spatial on .NET 5+" section explaining EF6's strong-name-based loader, recommending `Microsoft.SqlServer.Types 160.1000.6` as the working package, and noting that `dotMorten.Microsoft.SqlServer.Types` is **not** a viable substitute for EF6 because it's unsigned. |

Files **not** touched (deliberate):
- `src/Microsoft.Restier.EntityFramework.Spatial/Microsoft.Restier.EntityFramework.Spatial.csproj` — library stays dependency-free; consumers choose.
- `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs:208-230` — the try/catch around the EF6 SpatialPlace seed is correct defensive code for consumers/CI runs without the package installed (other EF6 test runners don't install it).

---

## Phase 1 — Wire up dotMorten and unskip the EF6 spatial unit tests

### Task 1: Add dotMorten package reference

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj`

- [ ] **Step 1: Add the package reference**

Open `test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj` and add a new `<ItemGroup>` containing the package, after the existing `<ItemGroup>` with `<ProjectReference>` items. Use the pinned version `160.1000.6` (the spike verified this against EF6 6.5.x):

```xml
<ItemGroup>
    <!--
        Test-only PackageReference. Microsoft.SqlServer.Types 160.x ships
        lib/netstandard2.1/Microsoft.SqlServer.Types.dll with the official Microsoft
        strong name (PublicKeyToken=89845dcd8080cc91) and Version=16.0.0.0, which
        EF6's SqlTypesAssemblyLoader accepts on .NET 5+. WKT round-trip operations
        run entirely in the managed types — the Windows-only SqlServerSpatial160.dll
        is only needed for computational-geometry methods (STEquals, STIntersects,
        etc.) which the converter under test does not invoke.

        We avoid dotMorten.Microsoft.SqlServer.Types here: it ships unsigned
        (PublicKeyToken=null) with Version=2.5.0.0, and EF6 rejects both.
    -->
    <PackageReference Include="Microsoft.SqlServer.Types" Version="160.1000.6" />
</ItemGroup>
```

Use **tabs** for indentation to match the rest of the file.

- [ ] **Step 2: Restore and build**

Run:

```bash
dotnet build test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj
```

Expected: build succeeds with zero warnings and zero errors. If a NU1701 warning appears about net462 / netstandard2.1 fallback, capture the text but don't change the version — the spike confirmed this exact version compiles cleanly across net8.0/net9.0/net10.0.

- [ ] **Step 3: Smoke-check the probe property now passes**

The existing probe property (`SqlServerTypesAvailable`) probes EF6's loader by calling `DbGeography.FromText`. Verify it succeeds now by running one currently-skipped test with the probe + `SkipUnless` still in place — xUnit v3 will report it as **failed** (rather than skipped) once the probe returns `true`. (The expected failure is the pre-existing `BeOfType<GeographyPoint>` bug, fixed in Task 2 Step 3.)

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --filter "FullyQualifiedName~ToEdm_returns_GeographyPoint_for_DbGeography_Point" --logger "console;verbosity=normal"
```

Expected: 1 test **failed** with `Expected type to be Microsoft.Spatial.GeographyPoint, but found Microsoft.Spatial.GeographyPointImplementation`. This confirms (a) the probe passes, (b) the WKT pipeline works, (c) the only failure is the pre-existing assertion bug that Task 2 fixes. If you get a different error — especially `version 10 or higher could not be found` or `Unable to load DLL 'SqlServerSpatial160.dll'` — stop and report; the spike's findings would no longer apply.

**Do not commit yet** — combine with Task 2 and Task 3 into one commit per the conventions.

---

### Task 2: Remove the probe and Skip markers in `DbSpatialConverterTests.cs`

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs`

- [ ] **Step 1: Delete the `SqlServerTypesAvailable` property**

Remove lines 14-37 entirely (the whole `/// <summary>…</summary>` block plus the property body). The file currently reads:

```csharp
    public class DbSpatialConverterTests
    {
        /// <summary>
        /// Returns true when the native <c>Microsoft.SqlServer.Types</c> assembly can be loaded
        /// by EF6's spatial loader.  On non-Windows hosts (or machines without SQL Server native
        /// types installed) the three geometry-exercising tests are skipped rather than failing.
        /// </summary>
        public static bool SqlServerTypesAvailable
        {
            get
            {
                try
                {
                    // Force EF6 to probe for the native types assembly now.
                    _ = DbSpatialServices.Default;
                    DbGeography.FromText("POINT(0 0)", 4326);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private readonly DbSpatialConverter _converter = new();
```

After the edit it should be:

```csharp
    public class DbSpatialConverterTests
    {
        private readonly DbSpatialConverter _converter = new();
```

- [ ] **Step 2: Remove every `Skip = "…"` / `SkipUnless = nameof(SqlServerTypesAvailable)` argument**

There are eight call sites in this file (seven `[Fact(…)]` and one `[Theory(…)]`). For each, reduce the attribute to its bare form. Concretely:

Find each occurrence of:

```csharp
        [Fact(Skip = "Requires Microsoft.SqlServer.Types native assembly (Windows / SQL Server only).",
              SkipUnless = nameof(SqlServerTypesAvailable))]
```

and replace with:

```csharp
        [Fact]
```

And for the single `[Theory(…)]` occurrence:

```csharp
        [Theory(Skip = "Requires Microsoft.SqlServer.Types native assembly (Windows / SQL Server only).",
                SkipUnless = nameof(SqlServerTypesAvailable))]
```

replace with:

```csharp
        [Theory]
```

- [ ] **Step 3: Rewrite the three `SpatialEquals`-based round-trip assertions**

`DbGeography.SpatialEquals` / `DbGeometry.SpatialEquals` route through `STEquals`, which lives in the Windows-only native `SqlServerSpatial160.dll`. We deliberately avoid loading the native binary so the tests run cross-platform without a startup fixture, so rewrite each affected test to compare on the round-trip evidence that actually matters: WKT body and SRID. WKT equality is stronger than `SpatialEquals` for a round-trip test anyway — `SpatialEquals` is tolerance-based, WKT byte-equality is exact. (The spike verified `AsText()` works managed-only on macOS for the test inputs we use.)

**Test 1 — `Round_trip_preserves_value` (currently at lines 77-86):**

Replace the body:

```csharp
        public void Round_trip_preserves_value()
        {
            var original = DbGeography.FromText("POINT(4.9041 52.3676)", 4326);

            var edm = _converter.ToEdm(original, typeof(GeographyPoint));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.SpatialEquals(original).Should().BeTrue();
            roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
        }
```

with:

```csharp
        public void Round_trip_preserves_value()
        {
            var original = DbGeography.FromText("POINT(4.9041 52.3676)", 4326);

            var edm = _converter.ToEdm(original, typeof(GeographyPoint));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.AsText().Should().Be(original.AsText());
            roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
        }
```

**Test 2 — `Round_trips_LineString` (currently at lines 90-98):**

Replace the body:

```csharp
        public void Round_trips_LineString()
        {
            var original = DbGeography.FromText("LINESTRING(0 0, 1 1, 2 2)", 4326);

            var edm = (GeographyLineString)_converter.ToEdm(original, typeof(GeographyLineString));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.SpatialEquals(original).Should().BeTrue();
        }
```

with:

```csharp
        public void Round_trips_LineString()
        {
            var original = DbGeography.FromText("LINESTRING(0 0, 1 1, 2 2)", 4326);

            var edm = (GeographyLineString)_converter.ToEdm(original, typeof(GeographyLineString));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.AsText().Should().Be(original.AsText());
            roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
        }
```

**Test 3 — `Round_trips_Polygon` (currently at lines 102-110):**

Replace the body:

```csharp
        public void Round_trips_Polygon()
        {
            var original = DbGeography.FromText("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))", 4326);

            var edm = (GeographyPolygon)_converter.ToEdm(original, typeof(GeographyPolygon));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.SpatialEquals(original).Should().BeTrue();
        }
```

with:

```csharp
        public void Round_trips_Polygon()
        {
            var original = DbGeography.FromText("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))", 4326);

            var edm = (GeographyPolygon)_converter.ToEdm(original, typeof(GeographyPolygon));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.AsText().Should().Be(original.AsText());
            roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
        }
```

Note: `DbGeography.AsText()` is the public WKT-without-SRID-prefix accessor. The spike confirmed it works on macOS with `Microsoft.SqlServer.Types 160.1000.6` — the managed types preserve the original WKT text from `FromText` without ever calling the native dll.

- [ ] **Step 4: Fix the pre-existing `BeOfType<GeographyPoint>` assertion bug**

`Microsoft.Spatial.ToEdm` always returns `GeographyPointImplementation` (a concrete subclass), but the assertion in `ToEdm_returns_GeographyPoint_for_DbGeography_Point` uses `BeOfType<GeographyPoint>()`, which requires *exact* type equality. The test was previously skipped, so the bug never surfaced — with the skip removed, the test fails. Rewrite the assertion to an explicit cast, matching the pattern the other tests in this file already use (e.g. `(GeographyLineString)_converter.ToEdm(...)`).

Replace the body:

```csharp
        public void ToEdm_returns_GeographyPoint_for_DbGeography_Point()
        {
            var dbg = DbGeography.FromText("POINT(4.9041 52.3676)", 4326);

            var result = _converter.ToEdm(dbg, typeof(GeographyPoint));

            var point = result.Should().BeOfType<GeographyPoint>().Subject;
            point.Latitude.Should().BeApproximately(52.3676, 0.0001);
            point.Longitude.Should().BeApproximately(4.9041, 0.0001);
            point.CoordinateSystem.EpsgId.Should().Be(4326);
        }
```

with:

```csharp
        public void ToEdm_returns_GeographyPoint_for_DbGeography_Point()
        {
            var dbg = DbGeography.FromText("POINT(4.9041 52.3676)", 4326);

            var point = (GeographyPoint)_converter.ToEdm(dbg, typeof(GeographyPoint));

            point.Latitude.Should().BeApproximately(52.3676, 0.0001);
            point.Longitude.Should().BeApproximately(4.9041, 0.0001);
            point.CoordinateSystem.EpsgId.Should().Be(4326);
        }
```

The explicit cast itself enforces assignability — if the concrete type is incompatible with `GeographyPoint`, the cast throws `InvalidCastException` and the test fails at the right place.

- [ ] **Step 5: Remove the now-unused `System` using if it has no other consumer**

The probe property's `catch (Exception)` was the only user of `using System;` in this file. After removing the property, check whether anything else needs `System`. The file body uses no other `System.*` types directly (constructions like `new()` and string literals don't require it), so:

Open `test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs` and remove the line:

```csharp
using System;
```

If your editor/IDE complains that `System` is still needed after the removal (unlikely), put it back — build is the source of truth.

- [ ] **Step 6: Build**

Run:

```bash
dotnet build test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj
```

Expected: build succeeds, warnings-as-errors clean.

- [ ] **Step 7: Run the file's tests**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --filter "FullyQualifiedName~DbSpatialConverterTests" --logger "console;verbosity=normal"
```

Expected: all tests pass, zero skipped. Concretely (one row per `[Fact]` / `[Theory]` data row):

- `CanConvert_returns_true_for_DbGeography` ✓
- `ToEdm_returns_GeographyPoint_for_DbGeography_Point` ✓
- `ToStorage_returns_DbGeography_for_GeographyPoint` ✓
- `Round_trip_preserves_value` ✓
- `Round_trips_LineString` ✓
- `Round_trips_Polygon` ✓
- `Preserves_Geography_SRID(srid: 4326)` ✓
- `Preserves_Geography_SRID(srid: 4269)` ✓
- `Preserves_Z_coordinate` ✓
- `Round_trips_DbGeometry_Point_with_planar_SRID` ✓
- `Null_storage_value_returns_null` ✓
- `ToStorage_with_unsupported_storage_type_throws` ✓
- `ToEdm_with_unsupported_storage_value_throws` ✓

If any test fails: most likely failure modes are (a) `Unable to load DLL 'SqlServerSpatial160.dll'` — means a code path crossed into native; report which test, since the spike showed all of these tests work managed-only and that would be new information; or (b) WKT formatting normalization differences between `original` and `roundTrip` (e.g. trailing whitespace, ordering of polygon ring vertices). If (b), investigate by printing both `AsText()` results and compare — both go through the same managed code path so they should be byte-identical, but if not, normalize via `.Replace(" ", "").ToUpperInvariant()` and document the quirk in a code comment.

---

### Task 3: Remove the probe and Skip marker in `EFChangeSetInitializerSpatialTests.cs`

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFramework.Spatial/EFChangeSetInitializerSpatialTests.cs`

- [ ] **Step 1: Delete the `SqlServerTypesAvailable` property**

Remove lines 16-31 (the property). Before:

```csharp
    public class EFChangeSetInitializerSpatialTests
    {
        public static bool SqlServerTypesAvailable
        {
            get
            {
                try
                {
                    _ = DbSpatialServices.Default;
                    DbGeography.FromText("POINT(0 0)", 4326);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        [Fact(Skip = "Requires Microsoft.SqlServer.Types native assembly (Windows / SQL Server only).",
              SkipUnless = nameof(SqlServerTypesAvailable))]
        public void ConvertToEfValue_dispatches_to_registered_spatial_converter_for_DbGeography()
```

After:

```csharp
    public class EFChangeSetInitializerSpatialTests
    {
        [Fact]
        public void ConvertToEfValue_dispatches_to_registered_spatial_converter_for_DbGeography()
```

- [ ] **Step 2: Build**

Run:

```bash
dotnet build test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj
```

Expected: build succeeds, warnings-as-errors clean.

- [ ] **Step 3: Run the file's tests**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --filter "FullyQualifiedName~EFChangeSetInitializerSpatialTests" --logger "console;verbosity=normal"
```

Expected: both tests pass, zero skipped:

- `ConvertToEfValue_dispatches_to_registered_spatial_converter_for_DbGeography` ✓
- `ConvertToEfValue_passes_through_when_no_converter_registered` ✓

---

### Task 4: Run the full spatial test project & commit

- [ ] **Step 1: Full test run for the project**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --logger "console;verbosity=normal"
```

Expected: every test in the project passes, zero skipped. Tally previously-skipped tests now running: 10 test rows (7 `[Fact]` + 1 `[Theory]` with 2 data rows in `DbSpatialConverterTests`, plus 1 `[Fact]` in `EFChangeSetInitializerSpatialTests`).

- [ ] **Step 2: Sanity-check the rest of the solution still builds**

The package is contained to this one test csproj, but the `Microsoft.SqlServer.Types` assembly may now resolve in any test runner that references this project transitively. We don't expect it to, but check:

```bash
dotnet build RESTier.slnx
```

Expected: full solution builds.

- [ ] **Step 3: Stage and commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj \
        test/Microsoft.Restier.Tests.EntityFramework.Spatial/DbSpatialConverterTests.cs \
        test/Microsoft.Restier.Tests.EntityFramework.Spatial/EFChangeSetInitializerSpatialTests.cs

git commit -m "$(cat <<'EOF'
test(ef6-spatial): unskip round-trip tests via Microsoft.SqlServer.Types 160.x

EF6's SqlTypesAssemblyLoader reflects into Microsoft.SqlServer.Types at runtime
via strong-named Assembly.Load against PublicKeyToken=89845dcd8080cc91. On
.NET 5+ that assembly is not present by default — not even on Windows — so
every spatial round-trip test was [Skip]-marked with a misleading "Windows /
SQL Server only" reason and skipped on every TFM the project targets.

Add Microsoft.SqlServer.Types 160.1000.6 as a test-only PackageReference. Its
lib/netstandard2.1/Microsoft.SqlServer.Types.dll is strong-named with the
official Microsoft key and reports Version=16.0.0.0, both inside EF6's
accepted range. The converter's WKT round-trip runs entirely in the managed
types, so the tests pass cross-platform without loading the Windows-only
SqlServerSpatial160.dll native binary. The library under test
(Microsoft.Restier.EntityFramework.Spatial) stays free of the dependency —
downstream consumers choose their own backing assembly.

Removed the SqlServerTypesAvailable probe and every Skip / SkipUnless marker.
All 10 previously-skipped round-trip test rows now run and pass. The three
tests that asserted via DbGeography.SpatialEquals are rewritten to compare
WKT and SRID directly — SpatialEquals routes through STEquals, which lives
in the Windows-only native binary that we deliberately avoid loading.

Fixed an unrelated pre-existing assertion bug in
ToEdm_returns_GeographyPoint_for_DbGeography_Point: it used
BeOfType<GeographyPoint>() but Microsoft.Spatial.ToEdm returns
GeographyPointImplementation (a subclass) — rewritten to use an explicit
cast, matching the pattern used by the other tests in the file.

Note: dotMorten.Microsoft.SqlServer.Types is NOT a viable substitute here.
It ships unsigned (PublicKeyToken=null) with Version=2.5.0.0, which EF6
rejects on both checks. (See spike findings in the plan history.)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 2 — Document the EF6-on-.NET-5+ situation

### Task 5: Add a "Running EF6 spatial on .NET 5+" section to `spatial-types.mdx`

**Files:**
- Modify: `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx`

- [ ] **Step 1: Insert the new section**

Open `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx`. The current end of the file (line 80-88) reads:

```mdx
## What's not yet supported

- Server-side `geo.distance` / `geo.length` / `geo.intersects` translation. Use `$filter` with these operators returns an error today; a future spec will deliver translation.
- Non-EPSG `CoordinateSystem` values throw `InvalidOperationException` on write. Default-SRID configuration is planned for a future spec.

## How it works

Round-trip flows through Microsoft.Spatial's `WellKnownTextSqlFormatter` (SQL Server extended WKT with `SRID=N;` prefix) and the storage-library WKT APIs (`DbGeography.FromText` / NTS `WKTReader`). SRID and Z/M ordinates survive both directions.
```

Insert a new `## Running EF6 spatial on .NET 5+` section **before** `## What's not yet supported`. Use the existing Mintlify components (`<Warning>`, `<Info>`, `<Tabs>`, `<CodeGroup>`) consistently with the rest of the file:

```mdx
## Running EF6 spatial on .NET 5+

EF6's spatial bridge (`DbGeography.FromText`, `DbSpatialServices.Default.AsText…`) reflects into the `Microsoft.SqlServer.Types` assembly at runtime via `Assembly.Load` against a hardcoded strong-name list (`PublicKeyToken=89845dcd8080cc91`, official Microsoft key). On **.NET Framework + Windows + SQL Server installed** that assembly lives in the GAC alongside its native `SqlServerSpatial*.dll` — no extra setup. On **.NET 5+ (including .NET 8/9/10 on Windows)** the assembly is **not present by default** and EF6 throws *"Spatial types and functions are not available for this provider because the assembly 'Microsoft.SqlServer.Types' version 10 or higher could not be found"* on any `DbGeography` / `DbGeometry` operation.

<Warning>
This affects RESTier too — if you've installed `Microsoft.Restier.EntityFramework.Spatial` on a .NET 5+ host without the package below, `DbSpatialConverter` will throw on every read/write. `Microsoft.Restier.EntityFramework.Spatial` deliberately does **not** take a hard dependency on the backing assembly so that you stay in control of how/whether the Windows-only native binaries are deployed.
</Warning>

### Recommended: install `Microsoft.SqlServer.Types` 160.x

The official Microsoft package ships `lib/netstandard2.1/Microsoft.SqlServer.Types.dll` strong-named with the Microsoft key (`PublicKeyToken=89845dcd8080cc91`, `Version=16.0.0.0`), which EF6 6.5.x accepts on .NET 8/9/10:

```bash
dotnet add package Microsoft.SqlServer.Types --version 160.1000.6
```

This is what the RESTier test suite uses. With just the package installed, the WKT round-trip path (`DbGeography.FromText` → read column → `AsText`) works on Windows, Linux, and macOS using only the managed types.

#### When you also need native operations

`SpatialEquals`, `STDistance`, `STIntersects`, and the other computational-geometry methods require the Windows-only native `SqlServerSpatial160.dll`. To enable them you must call the loader once at process startup:

```csharp
// Program.cs (or any one-time init)
Microsoft.SqlServer.Types.SqlServerTypes.Utilities
    .LoadNativeAssemblies(AppContext.BaseDirectory);
```

Linux / macOS hosts cannot run native operations — those code paths will throw `Unable to load DLL 'SqlServerSpatial160.dll'`. Push computations server-side (do the comparison in SQL with `$filter` future work) or use the managed-only WKT/SRID path RESTier already exposes.

<Warning>
**`dotMorten.Microsoft.SqlServer.Types` is not a viable substitute for EF6.** Although it's frequently recommended as a cross-platform shim for SqlClient consumers, dotMorten ships **unsigned** (`PublicKeyToken=null`) with `Version=2.5.0.0`. EF6's `SqlTypesAssemblyLoader` rejects it on both checks and falls through to the *"version 10 or higher could not be found"* error. If you want EF6 spatial on .NET 5+, you need the strong-named official package.
</Warning>

<Info>
EF Core users do not need any of this — the EF Core spatial path uses NetTopologySuite, which is a self-contained managed library with no native dependencies.
</Info>
```

- [ ] **Step 2: Rebuild the docs project**

Run:

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: build succeeds. The DotNetDocs SDK regenerates `docs.json`; nothing else under `guides/` should change.

- [ ] **Step 3: Verify the page renders sensibly**

This is an MDX file — there's no local Mintlify renderer wired into the repo. Eyeball-check:

```bash
sed -n '78,150p' src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx
```

Expected: the new section appears between the EF Core declaration tabs and `## What's not yet supported`, with all Mintlify component tags (`<Warning>`, `<Info>`) opened and closed correctly. No stray markdown.

- [ ] **Step 4: Stage and commit**

```bash
git add src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx \
        src/Microsoft.Restier.Docs/docs.json

git commit -m "$(cat <<'EOF'
docs(spatial): explain EF6 + .NET 5+ Microsoft.SqlServer.Types requirement

Add a "Running EF6 spatial on .NET 5+" section to the spatial-types guide
documenting that EF6's SqlTypesAssemblyLoader reflects into Microsoft.SqlServer.Types
via strong-named Assembly.Load and that the assembly is not present on .NET 5+
hosts by default.

Recommends Microsoft.SqlServer.Types 160.x as the only working package
(strong-named, version 16.0.0.0 accepted by EF6 6.5.x). Documents the
managed-only happy path (WKT round-trip works on Windows/Linux/macOS) versus
the native loader requirement for computational-geometry operations (Windows-
only, requires SqlServerTypes.Utilities.LoadNativeAssemblies at startup).

Explicitly calls out that dotMorten.Microsoft.SqlServer.Types is NOT a
viable substitute for EF6 — it's unsigned with Version=2.5.0.0, both of
which EF6's strong-named loader rejects.

Microsoft.Restier.EntityFramework.Spatial deliberately takes no dependency on
the backing assembly, so consumers stay in control of native deployment.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 3 — Final verification

### Task 6: Whole-solution build + spatial test re-run

- [ ] **Step 1: Full solution build**

Run:

```bash
dotnet build RESTier.slnx
```

Expected: clean build, warnings-as-errors honored.

- [ ] **Step 2: Re-run the spatial test project end-to-end**

Run:

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework.Spatial/Microsoft.Restier.Tests.EntityFramework.Spatial.csproj --logger "console;verbosity=normal"
```

Expected: every test passes, zero skipped, on whichever TFM (`net8.0` / `net9.0` / `net10.0`) the runner picks.

- [ ] **Step 3: Confirm the integration test seed still degrades gracefully without the package**

The EF6 SpatialPlace seed in `LibraryTestInitializer.cs:208-230` is still inside a try/catch. The test runners that consume it (`Tests.EntityFramework`, `Tests.AspNetCore`) do **not** install `Microsoft.SqlServer.Types`, so the SpatialPlaces table will remain empty on their EF6 path — exactly as before this change. Spot-check by running:

```bash
dotnet test test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj --filter "FullyQualifiedName~LibraryContext" --logger "console;verbosity=minimal"
```

Expected: passes; no exception escapes the LibraryTestInitializer seed.

If you want EF6 spatial seed values to actually persist in those runners, that's a future change — out of scope here. The new docs section already tells consumers how to do it for their own apps.

---

## Self-review checklist

After completing all tasks, verify:

1. **Spec coverage:** Every user request is addressed?
   - "Investigate" — root cause is documented in the plan header.
   - "Propose a solution" — `Microsoft.SqlServer.Types 160.x` chosen after a spike falsified the original dotMorten approach; rationale in Architecture.
   - "Add a task to update the documentation about EF6 on .NET 8/9/10 and dotMorten" — Task 5. The docs section explicitly addresses dotMorten (calling out that it's NOT viable) plus the working package.

2. **Placeholder scan:** No `<…_VERSION>`, "TBD", "TODO", "fill in details", or vague "handle edge cases". The package version is pinned to `160.1000.6` (verified by spike).

3. **Type / symbol consistency:** All file paths in the plan exist on disk (`test/Microsoft.Restier.Tests.EntityFramework.Spatial/*`, `src/Microsoft.Restier.Docs/guides/extending-restier/spatial-types.mdx`, `LibraryTestInitializer.cs`). The package name `Microsoft.SqlServer.Types` is the actual NuGet ID. The property `SqlServerTypesAvailable`, the test names, and the `BeOfType<GeographyPoint>` line all match the current source.
