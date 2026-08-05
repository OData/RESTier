# API Versioning Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a new opt-in `Microsoft.Restier.AspNetCore.Versioning` package that brings URL-segment API versioning to RESTier on top of `Asp.Versioning.Abstractions` / `Asp.Versioning.Mvc.ApiExplorer`. Deliver per-version EDMs and `$metadata`, version-discovery response headers, an `IApiVersionDescriptionProvider` adapter, registry-aware NSwag and Swagger UI integrations, a runnable two-version sample, and a documentation page. No behavior changes to `Microsoft.Restier.AspNetCore` request handling.

**Architecture:** Versioned routes are registered through a new `services.AddRestierApiVersioning(builder => builder.AddVersion<TApi>(...))` entry point that registers an `IConfigureOptions<ODataOptions>` and a registry singleton. When `ODataOptions` materializes, the configurator iterates pending registrations and calls the existing `oDataOptions.AddRestierRoute<TApi>(composedPrefix, ...)`. NSwag and Swagger integrations gain optional `IRestierApiVersionRegistry` consumption (null/empty → existing prefix-based behavior; non-empty → version-named documents merged with any unversioned routes).

**Tech Stack:** .NET 8/9/10, `Asp.Versioning.Mvc` 8.x and `Asp.Versioning.Mvc.ApiExplorer` 8.x (no `Asp.Versioning.OData` dependency — RESTier builds EDMs from conventions, not `ODataModelBuilder`), `Microsoft.AspNetCore.OData` 9.x (transitive via `Microsoft.Restier.AspNetCore`), xUnit v3, AwesomeAssertions, NSubstitute, `Microsoft.AspNetCore.Mvc.Testing` for `TestServer`-based integration tests.

**Spec:** [`docs/superpowers/specs/2026-05-03-api-versioning-design.md`](../specs/2026-05-03-api-versioning-design.md). Refer to the spec for any context the steps below assume — particularly the **Materialization invariant** (every component reading `IRestierApiVersionRegistry` must first resolve `IOptions<ODataOptions>.Value`) and the **"registry effectively absent" rule** (fallback when registry is null OR empty).

**Branch:** Work directly on `feature/vnext`. Additive (new package, new test project, new sample, plus small registry-aware updates to two existing packages).

**Public API note (refinement of the spec):** The spec listed three `AddVersion` overloads; this plan ships **two** because `[ApiVersion]` supports `AllowMultiple = true`, making the IEnumerable overload redundant with the attribute path:

1. Attribute-driven: `AddVersion<TApi>(string basePrefix, Action<IServiceCollection> configureRouteServices, ...)` — reads every `[ApiVersion]` attribute on `TApi`.
2. Imperative: `AddVersion<TApi>(ApiVersion apiVersion, bool deprecated, string basePrefix, ...)` — explicit version + deprecation flag, no attribute read.

**Asp.Versioning package version note:** This plan pins `Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer` to `[8.*, 9.0.0)`. These packages are AspNetCore-version-agnostic (they don't depend on `Microsoft.AspNetCore.OData`), so they work cleanly with RESTier's OData 9.x. If a 9.x release of Asp.Versioning is published before implementation, switch the range and run the integration tests.

**xUnit v3 + `TreatWarningsAsErrors` note:** xUnit v3's `xUnit1051` analyzer is enabled in this repo and warnings-as-errors is on. Every `client.GetAsync(...)`, `Content.ReadAsStringAsync()`, and `host.StartAsync()` call MUST receive a `CancellationToken` argument. Pattern:

```csharp
var cancellationToken = TestContext.Current.CancellationToken;
using var host = await BuildHostAsync(cancellationToken);
var response = await client.GetAsync("/api/v1/$metadata", cancellationToken);
var body = await response.Content.ReadAsStringAsync(cancellationToken);
```

**`ApiBase` constructor signature note:** `Microsoft.Restier.Core.ApiBase` has the constructor `protected ApiBase(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)`. Test fixtures and sample API classes must use this signature.

**Project conventions you must follow** (from `Directory.Build.props` and `CLAUDE.md`):

- Allman braces; prefer `var`; curly braces even for single-line blocks.
- `ImplicitUsings` is **disabled** — every `using` directive must be explicit.
- `Nullable` is **disabled**.
- `TreatWarningsAsErrors` is **enabled** globally.
- `InternalsVisibleTo` is auto-configured by `Directory.Build.props` for `Microsoft.Restier.X` → `Microsoft.Restier.Tests.X`. The test project gets access to `internal` types automatically.
- Test project package references (`xunit.v3`, `AwesomeAssertions`, `NSubstitute`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`) come from `Directory.Build.props` automatically. Do not repeat them in the test csproj.
- Commit message style: lowercase prefix (`feat:`, `test:`, `docs:`, `chore:`); always include `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` trailer.

---

## Phase 1 — Foundation contracts and project skeletons

### Task 1: Add the read-only registry contracts to `Microsoft.Restier.AspNetCore`

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore/Versioning/IRestierApiVersionRegistry.cs`
- Create: `src/Microsoft.Restier.AspNetCore/Versioning/RestierApiVersionDescriptor.cs`

These are type-only additions. They live in the base package (no `Asp.Versioning` dependency) so NSwag and Swagger can consume the registry contract without taking the Versioning package as a dependency.

- [ ] **Step 1: Verify the directory does not exist**

```bash
test ! -e src/Microsoft.Restier.AspNetCore/Versioning && echo "OK"
```

Expected: `OK`

- [ ] **Step 2: Create the directory**

```bash
mkdir -p src/Microsoft.Restier.AspNetCore/Versioning
```

- [ ] **Step 3: Write `RestierApiVersionDescriptor.cs`**

Path: `src/Microsoft.Restier.AspNetCore/Versioning/RestierApiVersionDescriptor.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Read-only description of a single versioned Restier route.
    /// Populated by the Microsoft.Restier.AspNetCore.Versioning package and consumed by
    /// version-aware OpenAPI integrations (NSwag, Swagger) and the version-discovery
    /// response-header middleware.
    /// </summary>
    public sealed class RestierApiVersionDescriptor
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierApiVersionDescriptor"/> class.
        /// </summary>
        /// <param name="version">The version string (e.g., "1.0").</param>
        /// <param name="basePrefix">The logical API group key — the <c>basePrefix</c> passed to <c>AddVersion</c>.</param>
        /// <param name="routePrefix">The composed route prefix (e.g., "api/v1").</param>
        /// <param name="apiType">The <see cref="Microsoft.Restier.Core.ApiBase"/>-derived type for this version.</param>
        /// <param name="isDeprecated">Whether this version is deprecated.</param>
        /// <param name="groupName">The group name used as the OpenAPI document name (e.g., "v1").</param>
        /// <param name="sunsetDate">Optional sunset date emitted via the <c>Sunset</c> response header.</param>
        public RestierApiVersionDescriptor(
            string version,
            string basePrefix,
            string routePrefix,
            Type apiType,
            bool isDeprecated,
            string groupName,
            DateTimeOffset? sunsetDate)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
            BasePrefix = basePrefix ?? throw new ArgumentNullException(nameof(basePrefix));
            RoutePrefix = routePrefix ?? throw new ArgumentNullException(nameof(routePrefix));
            ApiType = apiType ?? throw new ArgumentNullException(nameof(apiType));
            IsDeprecated = isDeprecated;
            GroupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
            SunsetDate = sunsetDate;
        }

        /// <summary>The version string (e.g., "1.0").</summary>
        public string Version { get; }

        /// <summary>The logical API group key — the <c>basePrefix</c> passed to <c>AddVersion</c>.</summary>
        public string BasePrefix { get; }

        /// <summary>The composed route prefix (e.g., "api/v1").</summary>
        public string RoutePrefix { get; }

        /// <summary>The <see cref="Microsoft.Restier.Core.ApiBase"/>-derived type for this version.</summary>
        public Type ApiType { get; }

        /// <summary>Whether this version is deprecated.</summary>
        public bool IsDeprecated { get; }

        /// <summary>The group name used as the OpenAPI document name (e.g., "v1").</summary>
        public string GroupName { get; }

        /// <summary>Optional sunset date emitted via the <c>Sunset</c> response header.</summary>
        public DateTimeOffset? SunsetDate { get; }

    }

}
```

- [ ] **Step 4: Write `IRestierApiVersionRegistry.cs`**

Path: `src/Microsoft.Restier.AspNetCore/Versioning/IRestierApiVersionRegistry.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Read-only access to the set of versioned Restier routes registered via the
    /// Microsoft.Restier.AspNetCore.Versioning package.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Materialization invariant</b>: descriptors are populated when
    /// <see cref="Microsoft.Extensions.Options.IOptions{ODataOptions}"/>'s <c>Value</c> first
    /// materializes. Any component that reads this registry directly MUST first resolve
    /// <c>IOptions&lt;ODataOptions&gt;.Value</c> from the same scope to guarantee the
    /// configurator pipeline has run. <c>IOptions&lt;T&gt;.Value</c> caches.
    /// </para>
    /// </remarks>
    public interface IRestierApiVersionRegistry
    {

        /// <summary>
        /// All registered version descriptors, in registration order.
        /// </summary>
        IReadOnlyList<RestierApiVersionDescriptor> Descriptors { get; }

        /// <summary>
        /// Finds the descriptor whose composed <see cref="RestierApiVersionDescriptor.RoutePrefix"/>
        /// equals <paramref name="routePrefix"/> (ordinal). Returns null if not found.
        /// </summary>
        RestierApiVersionDescriptor FindByPrefix(string routePrefix);

        /// <summary>
        /// Finds the descriptor whose <see cref="RestierApiVersionDescriptor.GroupName"/>
        /// equals <paramref name="groupName"/> (ordinal, case-insensitive).
        /// Returns null if not found.
        /// </summary>
        RestierApiVersionDescriptor FindByGroupName(string groupName);

        /// <summary>
        /// Returns descriptors that share the supplied logical API group key —
        /// the <c>basePrefix</c> passed to <c>AddVersion</c>. Used by header reporting
        /// so <c>api-supported-versions</c> / <c>api-deprecated-versions</c> reflect only
        /// the API the request belongs to, not unrelated APIs at other prefixes.
        /// </summary>
        IReadOnlyList<RestierApiVersionDescriptor> FindByBasePrefix(string basePrefix);

    }

}
```

- [ ] **Step 5: Build the project**

```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
```

Expected: `Build succeeded` with zero warnings/errors. Two new public types added; no other behavior changes.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Versioning/IRestierApiVersionRegistry.cs \
        src/Microsoft.Restier.AspNetCore/Versioning/RestierApiVersionDescriptor.cs
git commit -m "$(cat <<'COMMIT'
feat: add IRestierApiVersionRegistry / RestierApiVersionDescriptor contracts

Read-only types for version-aware integrations to consume without taking
a dependency on Asp.Versioning. Concrete implementation lands in the
Microsoft.Restier.AspNetCore.Versioning package.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```


### Task 2: Create the `Microsoft.Restier.AspNetCore.Versioning` source project skeleton

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.Versioning/Microsoft.Restier.AspNetCore.Versioning.csproj`

- [ ] **Step 1: Verify the directory does not exist**

```bash
test ! -e src/Microsoft.Restier.AspNetCore.Versioning && echo "OK"
```

Expected: `OK`

- [ ] **Step 2: Create the directory and csproj**

```bash
mkdir -p src/Microsoft.Restier.AspNetCore.Versioning
```

Write `src/Microsoft.Restier.AspNetCore.Versioning/Microsoft.Restier.AspNetCore.Versioning.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFrameworks>net8.0;net9.0;net10.0;</TargetFrameworks>
		<StrongNamePublicKey>$(StrongNamePublicKey)</StrongNamePublicKey>
		<DocumentationFile>$(DocumentationFile)\$(AssemblyName).xml</DocumentationFile>
	</PropertyGroup>

	<ItemGroup>
		<ProjectReference Include="..\Microsoft.Restier.AspNetCore\Microsoft.Restier.AspNetCore.csproj" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="Asp.Versioning.Mvc" Version="[8.*, 9.0.0)" />
		<PackageReference Include="Asp.Versioning.Mvc.ApiExplorer" Version="[8.*, 9.0.0)" />
	</ItemGroup>

</Project>
```

- [ ] **Step 3: Verify the project restores**

```bash
dotnet restore src/Microsoft.Restier.AspNetCore.Versioning/Microsoft.Restier.AspNetCore.Versioning.csproj
```

Expected: `Restore complete` with no errors.

- [ ] **Step 4: Verify the empty project builds**

```bash
dotnet build src/Microsoft.Restier.AspNetCore.Versioning/Microsoft.Restier.AspNetCore.Versioning.csproj
```

Expected: `Build succeeded` with no errors.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Versioning/Microsoft.Restier.AspNetCore.Versioning.csproj
git commit -m "$(cat <<'COMMIT'
chore: add Microsoft.Restier.AspNetCore.Versioning project skeleton

Empty package referencing Microsoft.Restier.AspNetCore plus
Asp.Versioning.Mvc / Mvc.ApiExplorer.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 3: Create the `Microsoft.Restier.Tests.AspNetCore.Versioning` test project skeleton

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj`

The test project automatically picks up xunit.v3, AwesomeAssertions, NSubstitute, and Microsoft.NET.Test.Sdk via `Directory.Build.props` because its name matches `*.Tests.*`. `InternalsVisibleTo` is also auto-configured.

- [ ] **Step 1: Verify the directory does not exist**

```bash
test ! -e test/Microsoft.Restier.Tests.AspNetCore.Versioning && echo "OK"
```

Expected: `OK`

- [ ] **Step 2: Create the directory and csproj**

```bash
mkdir -p test/Microsoft.Restier.Tests.AspNetCore.Versioning
```

Write `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0;</TargetFrameworks>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Microsoft.Restier.AspNetCore.Versioning\Microsoft.Restier.AspNetCore.Versioning.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="$(RestierNet9AspNetCoreTestHostVersion)" Condition="'$(TargetFramework)' == 'net9.0'" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="$(RestierNet10AspNetCoreTestHostVersion)" Condition="'$(TargetFramework)' == 'net10.0'" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="[8.*, 9.0.0)" Condition="'$(TargetFramework)' == 'net8.0'" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Verify the test project restores and builds (no test files yet)**

```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj
git commit -m "$(cat <<'COMMIT'
chore: add Microsoft.Restier.Tests.AspNetCore.Versioning project skeleton

Test packages and InternalsVisibleTo come from Directory.Build.props.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 4: Wire both new projects into `RESTier.slnx`

**Files:**
- Modify: `RESTier.slnx`

- [ ] **Step 1: Read the current slnx**

```bash
cat RESTier.slnx
```

Note the existing `/src/Web/` and `/test/Web/` folders.

- [ ] **Step 2: Add the source project to `/src/Web/`**

Edit `RESTier.slnx`. Inside the `<Folder Name="/src/Web/" Id="bf61c3f1-7c7e-4515-8b51-14b374a034f9">` element, add:

```xml
    <Project Path="src/Microsoft.Restier.AspNetCore.Versioning/Microsoft.Restier.AspNetCore.Versioning.csproj" />
```

The folder element after the change should contain three project entries (NSwag, Swagger, AspNetCore — keep their order; insert Versioning alphabetically between NSwag and Swagger).

- [ ] **Step 3: Add the test project to `/test/Web/`**

Inside the `<Folder Name="/test/Web/" Id="ae160b58-fb2d-4b9f-9357-8c7648381b95">` element, add:

```xml
    <Project Path="test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj" />
```

Insert alphabetically between the existing NSwag test project and the Swagger test project.

- [ ] **Step 4: Build the solution to confirm both projects integrate**

```bash
dotnet build RESTier.slnx
```

Expected: `Build succeeded` for the solution. All projects compile.

- [ ] **Step 5: Commit**

```bash
git add RESTier.slnx
git commit -m "$(cat <<'COMMIT'
chore: wire Versioning + tests into RESTier.slnx

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```


---

## Phase 2 — Helpers and value types

### Task 5: TDD `ApiVersionSegmentFormatters`

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.Versioning/ApiVersionSegmentFormatters.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/ApiVersionSegmentFormattersTests.cs`

- [ ] **Step 1: Write the failing test**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/ApiVersionSegmentFormattersTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Asp.Versioning;
using FluentAssertions;
using Microsoft.Restier.AspNetCore.Versioning;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning
{

    public class ApiVersionSegmentFormattersTests
    {

        [Fact]
        public void Major_FormatsAsVPrefixedMajorOnly()
        {
            ApiVersionSegmentFormatters.Major(new ApiVersion(1, 0)).Should().Be("v1");
            ApiVersionSegmentFormatters.Major(new ApiVersion(2, 7)).Should().Be("v2");
        }

        [Fact]
        public void MajorMinor_FormatsAsVPrefixedMajorAndMinor()
        {
            ApiVersionSegmentFormatters.MajorMinor(new ApiVersion(1, 0)).Should().Be("v1.0");
            ApiVersionSegmentFormatters.MajorMinor(new ApiVersion(2, 7)).Should().Be("v2.7");
        }

    }

}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~ApiVersionSegmentFormatters"
```

Expected: COMPILATION FAILS — `ApiVersionSegmentFormatters` does not exist.

- [ ] **Step 3: Write the implementation**

Path: `src/Microsoft.Restier.AspNetCore.Versioning/ApiVersionSegmentFormatters.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Built-in <see cref="ApiVersion"/>-to-URL-segment formatters.
    /// </summary>
    public static class ApiVersionSegmentFormatters
    {

        /// <summary>
        /// Formats an <see cref="ApiVersion"/> as <c>v{Major}</c> (e.g., "v1").
        /// </summary>
        public static Func<ApiVersion, string> Major { get; } = static v => $"v{v.MajorVersion}";

        /// <summary>
        /// Formats an <see cref="ApiVersion"/> as <c>v{Major}.{Minor}</c> (e.g., "v1.0").
        /// </summary>
        public static Func<ApiVersion, string> MajorMinor { get; } = static v => $"v{v.MajorVersion}.{v.MinorVersion}";

    }

}
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~ApiVersionSegmentFormatters"
```

Expected: 2 passed.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Versioning/ApiVersionSegmentFormatters.cs \
        test/Microsoft.Restier.Tests.AspNetCore.Versioning/ApiVersionSegmentFormattersTests.cs
git commit -m "$(cat <<'COMMIT'
feat: add ApiVersionSegmentFormatters with Major and MajorMinor

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 6: Add `RestierVersioningOptions`

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.Versioning/RestierVersioningOptions.cs`

POCO. No tests required at this stage; behavior is exercised by `RestierApiVersioningOptionsConfigurator` tests in Task 10.

- [ ] **Step 1: Write the type**

Path: `src/Microsoft.Restier.AspNetCore.Versioning/RestierVersioningOptions.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Per-version options passed to <c>IRestierApiVersioningBuilder.AddVersion</c>.
    /// </summary>
    public sealed class RestierVersioningOptions
    {

        /// <summary>
        /// How to render an <see cref="ApiVersion"/> as the URL segment appended to the base prefix.
        /// Defaults to <see cref="ApiVersionSegmentFormatters.Major"/>.
        /// </summary>
        public Func<ApiVersion, string> SegmentFormatter { get; set; } = ApiVersionSegmentFormatters.Major;

        /// <summary>
        /// Override the composed route prefix entirely. When set, <see cref="SegmentFormatter"/>
        /// and the base prefix are ignored — the supplied value is used verbatim as the
        /// <c>routePrefix</c> argument to <c>AddRestierRoute</c>.
        /// </summary>
        public string ExplicitRoutePrefix { get; set; }

        /// <summary>
        /// Optional sunset date for this version. When set, the headers middleware emits
        /// <c>Sunset: &lt;RFC 1123 date&gt;</c> on responses for routes belonging to this version.
        /// </summary>
        /// <remarks>
        /// <c>[ApiVersion]</c> does not carry sunset metadata, so it must be configured here per call.
        /// Future enhancement: integrate with <c>Asp.Versioning.IPolicyManager</c>.
        /// </remarks>
        public DateTimeOffset? SunsetDate { get; set; }

        /// <summary>
        /// Optional formatter that produces the OpenAPI document <c>GroupName</c> for this version.
        /// When null (default), <see cref="SegmentFormatter"/> is used (so a v1 segment also
        /// produces the "v1" group name). When you register multiple logical APIs at different
        /// <c>basePrefix</c>es that share a version, set this on each call to disambiguate
        /// (e.g., <c>opts.GroupNameFormatter = v =&gt; $"orders-v{v.MajorVersion}"</c>); the
        /// configurator throws <see cref="InvalidOperationException"/> if two descriptors would
        /// have the same GroupName.
        /// </summary>
        public Func<ApiVersion, string> GroupNameFormatter { get; set; }

    }

}
```

- [ ] **Step 2: Build**

```bash
dotnet build src/Microsoft.Restier.AspNetCore.Versioning/Microsoft.Restier.AspNetCore.Versioning.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Versioning/RestierVersioningOptions.cs
git commit -m "$(cat <<'COMMIT'
feat: add RestierVersioningOptions (segment formatter, explicit prefix, sunset)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 7: TDD `ApiVersionAttributeReader`

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/ApiVersionAttributeReader.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Internal/ApiVersionAttributeReaderTests.cs`

`ApiVersionAttributeReader` reads `[ApiVersion]` (`AllowMultiple = true`) and returns one `(ApiVersion, bool deprecated)` per attribute. Throws when zero attributes are present. Does NOT read sunset (sunset comes from `RestierVersioningOptions.SunsetDate`).

- [ ] **Step 1: Write the failing tests**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Internal/ApiVersionAttributeReaderTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.Restier.AspNetCore.Versioning.Internal;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Internal
{

    public class ApiVersionAttributeReaderTests
    {

        [Fact]
        public void Read_SingleAttribute_ReturnsOneEntry()
        {
            var entries = ApiVersionAttributeReader.Read(typeof(SingleVersion)).ToArray();

            entries.Should().HaveCount(1);
            entries[0].ApiVersion.Should().Be(new ApiVersion(1, 0));
            entries[0].IsDeprecated.Should().BeFalse();
        }

        [Fact]
        public void Read_MultipleAttributes_ReturnsAllEntriesInDeclarationOrder()
        {
            var entries = ApiVersionAttributeReader.Read(typeof(TwoVersions)).ToArray();

            entries.Should().HaveCount(2);
            entries.Should().ContainSingle(e => e.ApiVersion == new ApiVersion(1, 0) && e.IsDeprecated);
            entries.Should().ContainSingle(e => e.ApiVersion == new ApiVersion(2, 0) && !e.IsDeprecated);
        }

        [Fact]
        public void Read_NoAttribute_ThrowsInvalidOperation()
        {
            Action act = () => ApiVersionAttributeReader.Read(typeof(NoAttribute)).ToArray();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{typeof(NoAttribute).FullName}*[ApiVersion]*imperative overload*");
        }

        [ApiVersion("1.0")]
        private class SingleVersion { }

        [ApiVersion("1.0", Deprecated = true)]
        [ApiVersion("2.0")]
        private class TwoVersions { }

        private class NoAttribute { }

    }

}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~ApiVersionAttributeReader"
```

Expected: COMPILATION FAILS — `ApiVersionAttributeReader` does not exist.

- [ ] **Step 3: Write the implementation**

Path: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/ApiVersionAttributeReader.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Asp.Versioning;

namespace Microsoft.Restier.AspNetCore.Versioning.Internal
{

    /// <summary>
    /// Reads <see cref="ApiVersionAttribute"/> instances from a type and projects each declared
    /// version into an <see cref="ApiVersionAttributeReadResult"/>.
    /// </summary>
    /// <remarks>
    /// Sunset is intentionally NOT read here — <see cref="ApiVersionAttribute"/> does not carry
    /// sunset metadata. Sunset comes from <see cref="RestierVersioningOptions.SunsetDate"/>.
    /// </remarks>
    internal static class ApiVersionAttributeReader
    {

        public static IEnumerable<ApiVersionAttributeReadResult> Read(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var attributes = type.GetCustomAttributes<ApiVersionAttribute>(inherit: true).ToArray();
            if (attributes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Type {type.FullName} has no [ApiVersion] attribute. " +
                    "Add [ApiVersion(\"1.0\")] (or another version) to the class, " +
                    "or use the imperative overload of AddVersion that takes an ApiVersion argument explicitly.");
            }

            foreach (var attribute in attributes)
            {
                foreach (var version in attribute.Versions)
                {
                    yield return new ApiVersionAttributeReadResult(version, attribute.Deprecated);
                }
            }
        }

    }

    internal readonly struct ApiVersionAttributeReadResult
    {

        public ApiVersionAttributeReadResult(ApiVersion apiVersion, bool isDeprecated)
        {
            ApiVersion = apiVersion;
            IsDeprecated = isDeprecated;
        }

        public ApiVersion ApiVersion { get; }

        public bool IsDeprecated { get; }

    }

}
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~ApiVersionAttributeReader"
```

Expected: 3 passed.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Versioning/Internal/ApiVersionAttributeReader.cs \
        test/Microsoft.Restier.Tests.AspNetCore.Versioning/Internal/ApiVersionAttributeReaderTests.cs
git commit -m "$(cat <<'COMMIT'
feat: add ApiVersionAttributeReader

Reads [ApiVersion] attributes (AllowMultiple) and projects each declared
version into an internal read result. Sunset is intentionally not read
here — it comes from RestierVersioningOptions.SunsetDate.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```


---

## Phase 3 — Registry implementation

### Task 8: TDD `RestierApiVersionRegistry`

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.Versioning/RestierApiVersionRegistry.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/RestierApiVersionRegistryTests.cs`

The concrete implementation of `IRestierApiVersionRegistry` is internal to the Versioning package. The `Add` method is the only mutator and is called only from `RestierApiVersioningOptionsConfigurator` while configuring `ODataOptions`. Lookups are read-only; the type is intended to be a singleton.

- [ ] **Step 1: Write the failing tests**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/RestierApiVersionRegistryTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.Restier.AspNetCore.Versioning;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning
{

    public class RestierApiVersionRegistryTests
    {

        [Fact]
        public void Add_AppendsDescriptorWithEverySpecifiedField()
        {
            var registry = new RestierApiVersionRegistry();

            var descriptor = registry.Add(
                new ApiVersion(1, 0),
                basePrefix: "api",
                routePrefix: "api/v1",
                apiType: typeof(SampleApi),
                isDeprecated: true,
                groupName: "v1",
                sunsetDate: new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

            descriptor.Version.Should().Be("1.0");
            descriptor.BasePrefix.Should().Be("api");
            descriptor.RoutePrefix.Should().Be("api/v1");
            descriptor.ApiType.Should().Be(typeof(SampleApi));
            descriptor.IsDeprecated.Should().BeTrue();
            descriptor.GroupName.Should().Be("v1");
            descriptor.SunsetDate.Should().Be(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

            registry.Descriptors.Should().HaveCount(1);
            registry.Descriptors[0].Should().BeSameAs(descriptor);
        }

        [Fact]
        public void FindByPrefix_IsCaseSensitive()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            registry.FindByPrefix("api/v1").Should().NotBeNull();
            registry.FindByPrefix("API/V1").Should().BeNull();
            registry.FindByPrefix("api/v2").Should().BeNull();
        }

        [Fact]
        public void FindByGroupName_IsCaseInsensitive()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            registry.FindByGroupName("v1").Should().NotBeNull();
            registry.FindByGroupName("V1").Should().NotBeNull();
            registry.FindByGroupName("v2").Should().BeNull();
        }

        [Fact]
        public void FindByBasePrefix_ReturnsAllDescriptorsInGroup()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "orders", "orders/v1", typeof(OrdersApiV1), true, "orders-v1", null);
            registry.Add(new ApiVersion(2, 0), "orders", "orders/v2", typeof(OrdersApiV2), false, "orders-v2", null);
            registry.Add(new ApiVersion(1, 0), "inventory", "inventory/v1", typeof(InventoryApi), false, "inventory-v1", null);

            var ordersGroup = registry.FindByBasePrefix("orders");

            ordersGroup.Should().HaveCount(2);
            ordersGroup.Should().OnlyContain(d => d.BasePrefix == "orders");
        }

        [Fact]
        public void FindByBasePrefix_ReturnsEmptyListForUnknownGroup()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            registry.FindByBasePrefix("nonexistent").Should().BeEmpty();
        }

        private class SampleApi { }

        private class OrdersApiV1 { }

        private class OrdersApiV2 { }

        private class InventoryApi { }

    }

}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~RestierApiVersionRegistry"
```

Expected: COMPILATION FAILS — `RestierApiVersionRegistry` does not exist.

- [ ] **Step 3: Write the implementation**

Path: `src/Microsoft.Restier.AspNetCore.Versioning/RestierApiVersionRegistry.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Concrete <see cref="IRestierApiVersionRegistry"/>. Append-only; descriptors are
    /// added by <see cref="Internal.RestierApiVersioningOptionsConfigurator"/> when
    /// <c>ODataOptions</c> materializes. Registered as a singleton.
    /// </summary>
    internal sealed class RestierApiVersionRegistry : IRestierApiVersionRegistry
    {

        private readonly List<RestierApiVersionDescriptor> _descriptors = new();
        private readonly object _lock = new();

        public IReadOnlyList<RestierApiVersionDescriptor> Descriptors
        {
            get
            {
                lock (_lock)
                {
                    return _descriptors.ToArray();
                }
            }
        }

        public RestierApiVersionDescriptor Add(
            ApiVersion apiVersion,
            string basePrefix,
            string routePrefix,
            Type apiType,
            bool isDeprecated,
            string groupName,
            DateTimeOffset? sunsetDate)
        {
            if (apiVersion is null)
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            var descriptor = new RestierApiVersionDescriptor(
                apiVersion.ToString(),
                basePrefix,
                routePrefix,
                apiType,
                isDeprecated,
                groupName,
                sunsetDate);

            lock (_lock)
            {
                _descriptors.Add(descriptor);
            }

            return descriptor;
        }

        public RestierApiVersionDescriptor FindByPrefix(string routePrefix)
        {
            if (routePrefix is null)
            {
                return null;
            }

            lock (_lock)
            {
                return _descriptors.FirstOrDefault(d => string.Equals(d.RoutePrefix, routePrefix, StringComparison.Ordinal));
            }
        }

        public RestierApiVersionDescriptor FindByGroupName(string groupName)
        {
            if (groupName is null)
            {
                return null;
            }

            lock (_lock)
            {
                return _descriptors.FirstOrDefault(d => string.Equals(d.GroupName, groupName, StringComparison.OrdinalIgnoreCase));
            }
        }

        public IReadOnlyList<RestierApiVersionDescriptor> FindByBasePrefix(string basePrefix)
        {
            if (basePrefix is null)
            {
                return Array.Empty<RestierApiVersionDescriptor>();
            }

            lock (_lock)
            {
                return _descriptors.Where(d => string.Equals(d.BasePrefix, basePrefix, StringComparison.Ordinal)).ToArray();
            }
        }

    }

}
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~RestierApiVersionRegistry"
```

Expected: 5 passed.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Versioning/RestierApiVersionRegistry.cs \
        test/Microsoft.Restier.Tests.AspNetCore.Versioning/RestierApiVersionRegistryTests.cs
git commit -m "$(cat <<'COMMIT'
feat: add RestierApiVersionRegistry concrete implementation

Append-only registry with FindByPrefix / FindByGroupName /
FindByBasePrefix lookups. Thread-safe via internal lock; returns
copies for enumeration to avoid races with Add.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```


---

## Phase 4 — Builder

### Task 9: Add `PendingVersionRegistration` and `IRestierApiVersioningBuilder` / `RestierApiVersioningBuilder`

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/PendingVersionRegistration.cs`
- Create: `src/Microsoft.Restier.AspNetCore.Versioning/IRestierApiVersioningBuilder.cs`
- Create: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningBuilder.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Internal/RestierApiVersioningBuilderTests.cs`

The builder accumulates pending registrations across one or more `AddVersion` calls. The configurator drains them when `ODataOptions` materializes (Task 10).

- [ ] **Step 1: Write the failing tests**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Internal/RestierApiVersioningBuilderTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.AspNetCore.Versioning.Internal;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.OData.Edm;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Internal
{

    public class RestierApiVersioningBuilderTests
    {

        [Fact]
        public void AddVersion_AttributeDriven_AppendsOneRegistrationPerApiVersionAttribute()
        {
            var builder = new RestierApiVersioningBuilder();

            builder.AddVersion<TwoVersionedApi>("api", _ => { });

            builder.PendingRegistrations.Should().HaveCount(2);
            builder.PendingRegistrations.Should().Contain(r =>
                r.ApiVersion == new ApiVersion(1, 0) && r.IsDeprecated && r.BasePrefix == "api");
            builder.PendingRegistrations.Should().Contain(r =>
                r.ApiVersion == new ApiVersion(2, 0) && !r.IsDeprecated && r.BasePrefix == "api");
        }

        [Fact]
        public void AddVersion_AttributeDriven_NoAttribute_Throws()
        {
            var builder = new RestierApiVersioningBuilder();

            Action act = () => builder.AddVersion<UnannotatedApi>("api", _ => { });

            act.Should().Throw<InvalidOperationException>().WithMessage($"*{typeof(UnannotatedApi).FullName}*");
        }

        [Fact]
        public void AddVersion_Imperative_AppendsRegistrationWithExplicitDeprecatedFlag()
        {
            var builder = new RestierApiVersioningBuilder();

            builder.AddVersion<UnannotatedApi>(new ApiVersion(3, 0), deprecated: true, "api", _ => { });

            builder.PendingRegistrations.Should().HaveCount(1);
            var registration = builder.PendingRegistrations[0];
            registration.ApiVersion.Should().Be(new ApiVersion(3, 0));
            registration.IsDeprecated.Should().BeTrue();
            registration.BasePrefix.Should().Be("api");
            registration.ApiType.Should().Be(typeof(UnannotatedApi));
        }

        [Fact]
        public void AddVersion_ReturnsSameBuilder_ForChaining()
        {
            var builder = new RestierApiVersioningBuilder();

            var returned = builder.AddVersion<TwoVersionedApi>("api", _ => { });

            returned.Should().BeSameAs(builder);
        }

        [Fact]
        public void AddVersion_ConfigureVersioning_RecordedOnRegistration()
        {
            var builder = new RestierApiVersioningBuilder();

            builder.AddVersion<TwoVersionedApi>(
                "api",
                _ => { },
                options => options.SegmentFormatter = ApiVersionSegmentFormatters.MajorMinor);

            builder.PendingRegistrations.Should().AllSatisfy(r =>
            {
                var opts = new RestierVersioningOptions();
                r.ApplyVersioningOptions?.Invoke(opts);
                opts.SegmentFormatter.Should().BeSameAs(ApiVersionSegmentFormatters.MajorMinor);
            });
        }

        [ApiVersion("1.0", Deprecated = true)]
        [ApiVersion("2.0")]
        private class TwoVersionedApi : ApiBase
        {
            public TwoVersionedApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class UnannotatedApi : ApiBase
        {
            public UnannotatedApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }

    }

}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~RestierApiVersioningBuilder"
```

Expected: COMPILATION FAILS.

- [ ] **Step 3: Write `PendingVersionRegistration`**

Path: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/PendingVersionRegistration.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNetCore.Versioning.Internal
{

    /// <summary>
    /// One pending versioned-route registration captured by
    /// <see cref="IRestierApiVersioningBuilder.AddVersion{TApi}(string, Action{IServiceCollection}, Action{RestierVersioningOptions}, bool, RestierNamingConvention)"/>
    /// (and overloads) and consumed by <see cref="RestierApiVersioningOptionsConfigurator"/> when
    /// <c>ODataOptions</c> materializes.
    /// </summary>
    internal sealed class PendingVersionRegistration
    {

        public PendingVersionRegistration(
            Type apiType,
            ApiVersion apiVersion,
            bool isDeprecated,
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> applyVersioningOptions,
            bool useRestierBatching,
            RestierNamingConvention namingConvention)
        {
            ApiType = apiType;
            ApiVersion = apiVersion;
            IsDeprecated = isDeprecated;
            BasePrefix = basePrefix;
            ConfigureRouteServices = configureRouteServices;
            ApplyVersioningOptions = applyVersioningOptions;
            UseRestierBatching = useRestierBatching;
            NamingConvention = namingConvention;
        }

        public Type ApiType { get; }

        public ApiVersion ApiVersion { get; }

        public bool IsDeprecated { get; }

        public string BasePrefix { get; }

        public Action<IServiceCollection> ConfigureRouteServices { get; }

        public Action<RestierVersioningOptions> ApplyVersioningOptions { get; }

        public bool UseRestierBatching { get; }

        public RestierNamingConvention NamingConvention { get; }

    }

}
```

- [ ] **Step 4: Write `IRestierApiVersioningBuilder`**

Path: `src/Microsoft.Restier.AspNetCore.Versioning/IRestierApiVersioningBuilder.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Fluent builder used to declare versioned Restier routes. Each <c>AddVersion</c> call
    /// captures a pending registration applied when <c>ODataOptions</c> materializes.
    /// </summary>
    public interface IRestierApiVersioningBuilder
    {

        /// <summary>
        /// Registers one or more versions for <typeparamref name="TApi"/>, reading every
        /// <c>[ApiVersion]</c> attribute on the type.
        /// </summary>
        /// <typeparam name="TApi">The <see cref="ApiBase"/>-derived type for these versions.</typeparam>
        /// <param name="basePrefix">The logical API prefix; the version segment is appended to it.</param>
        /// <param name="configureRouteServices">Per-route DI configuration delegate.</param>
        /// <param name="configureVersioning">Optional per-call versioning options (segment formatter, sunset, explicit prefix).</param>
        /// <param name="useRestierBatching">Pass <c>useRestierBatching</c> through to <c>AddRestierRoute</c>.</param>
        /// <param name="namingConvention">Pass <c>namingConvention</c> through to <c>AddRestierRoute</c>.</param>
        IRestierApiVersioningBuilder AddVersion<TApi>(
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> configureVersioning = null,
            bool useRestierBatching = true,
            RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
            where TApi : ApiBase;

        /// <summary>
        /// Registers a specific <paramref name="apiVersion"/> for <typeparamref name="TApi"/>,
        /// without reading any <c>[ApiVersion]</c> attribute.
        /// </summary>
        /// <typeparam name="TApi">The <see cref="ApiBase"/>-derived type for this version.</typeparam>
        /// <param name="apiVersion">The version to register.</param>
        /// <param name="deprecated">Whether this version is deprecated.</param>
        /// <param name="basePrefix">The logical API prefix; the version segment is appended to it.</param>
        /// <param name="configureRouteServices">Per-route DI configuration delegate.</param>
        /// <param name="configureVersioning">Optional per-call versioning options (segment formatter, sunset, explicit prefix).</param>
        /// <param name="useRestierBatching">Pass <c>useRestierBatching</c> through to <c>AddRestierRoute</c>.</param>
        /// <param name="namingConvention">Pass <c>namingConvention</c> through to <c>AddRestierRoute</c>.</param>
        IRestierApiVersioningBuilder AddVersion<TApi>(
            ApiVersion apiVersion,
            bool deprecated,
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> configureVersioning = null,
            bool useRestierBatching = true,
            RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
            where TApi : ApiBase;

    }

}
```

- [ ] **Step 5: Write `RestierApiVersioningBuilder`**

Path: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningBuilder.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNetCore.Versioning.Internal
{

    /// <summary>
    /// Concrete <see cref="IRestierApiVersioningBuilder"/>. Mutable across multiple
    /// <c>AddRestierApiVersioning</c> calls; its pending registrations are drained by the
    /// options configurator.
    /// </summary>
    internal sealed class RestierApiVersioningBuilder : IRestierApiVersioningBuilder
    {

        private readonly List<PendingVersionRegistration> _pending = new();
        private readonly object _lock = new();

        public IReadOnlyList<PendingVersionRegistration> PendingRegistrations
        {
            get
            {
                lock (_lock)
                {
                    return _pending.ToArray();
                }
            }
        }

        public IRestierApiVersioningBuilder AddVersion<TApi>(
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> configureVersioning = null,
            bool useRestierBatching = true,
            RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
            where TApi : ApiBase
        {
            if (basePrefix is null)
            {
                throw new ArgumentNullException(nameof(basePrefix));
            }

            if (configureRouteServices is null)
            {
                throw new ArgumentNullException(nameof(configureRouteServices));
            }

            foreach (var read in ApiVersionAttributeReader.Read(typeof(TApi)))
            {
                lock (_lock)
                {
                    _pending.Add(new PendingVersionRegistration(
                        typeof(TApi),
                        read.ApiVersion,
                        read.IsDeprecated,
                        basePrefix,
                        configureRouteServices,
                        configureVersioning,
                        useRestierBatching,
                        namingConvention));
                }
            }

            return this;
        }

        public IRestierApiVersioningBuilder AddVersion<TApi>(
            ApiVersion apiVersion,
            bool deprecated,
            string basePrefix,
            Action<IServiceCollection> configureRouteServices,
            Action<RestierVersioningOptions> configureVersioning = null,
            bool useRestierBatching = true,
            RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
            where TApi : ApiBase
        {
            if (apiVersion is null)
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            if (basePrefix is null)
            {
                throw new ArgumentNullException(nameof(basePrefix));
            }

            if (configureRouteServices is null)
            {
                throw new ArgumentNullException(nameof(configureRouteServices));
            }

            lock (_lock)
            {
                _pending.Add(new PendingVersionRegistration(
                    typeof(TApi),
                    apiVersion,
                    deprecated,
                    basePrefix,
                    configureRouteServices,
                    configureVersioning,
                    useRestierBatching,
                    namingConvention));
            }

            return this;
        }

    }

}
```

- [ ] **Step 6: Run the tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~RestierApiVersioningBuilder"
```

Expected: 5 passed.

- [ ] **Step 7: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Versioning/Internal/PendingVersionRegistration.cs \
        src/Microsoft.Restier.AspNetCore.Versioning/IRestierApiVersioningBuilder.cs \
        src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningBuilder.cs \
        test/Microsoft.Restier.Tests.AspNetCore.Versioning/Internal/RestierApiVersioningBuilderTests.cs
git commit -m "$(cat <<'COMMIT'
feat: add IRestierApiVersioningBuilder + concrete implementation

Captures pending version registrations (attribute-driven and imperative
overloads) for the options configurator to drain when ODataOptions
materializes. Throws InvalidOperationException on the attribute path
when [ApiVersion] is missing.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```


---

## Phase 5 — Configurator and `AddRestierApiVersioning`

### Task 10: TDD `RestierApiVersioningOptionsConfigurator`

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningOptionsConfigurator.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Internal/RestierApiVersioningOptionsConfiguratorTests.cs`

The configurator is the bridge: when `ODataOptions` is materialized, it iterates `PendingVersionRegistration`s, composes route prefixes, calls the existing `oDataOptions.AddRestierRoute<TApi>(...)`, and adds descriptors to the registry. It guards against double-run via a `_hasRun` flag.

Prefix composition rules:
- If `RestierVersioningOptions.ExplicitRoutePrefix` is set, use it verbatim.
- Otherwise, `routePrefix = basePrefix is "" ? segmentFormatter(version) : basePrefix + "/" + segmentFormatter(version)`.

Group name = `segmentFormatter(version)` (always — the "v1" identity), even when `ExplicitRoutePrefix` is set.

Duplicate detection: if a descriptor already exists with the same `(ApiVersion, BasePrefix)` combination, throw.

- [ ] **Step 1: Write the failing tests**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Internal/RestierApiVersioningOptionsConfiguratorTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.AspNetCore.Versioning.Internal;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Internal
{

    public class RestierApiVersioningOptionsConfiguratorTests
    {

        [Fact]
        public void Configure_DefaultFormatter_ComposesPrefixAsBaseSlashVMajor()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(new ApiVersion(1, 0), deprecated: false, "api", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>()));

            configurator.Configure(options);

            options.RouteComponents.Should().ContainKey("api/v1");
            registry.Descriptors.Should().HaveCount(1);
            registry.Descriptors[0].RoutePrefix.Should().Be("api/v1");
            registry.Descriptors[0].BasePrefix.Should().Be("api");
            registry.Descriptors[0].GroupName.Should().Be("v1");
            registry.Descriptors[0].Version.Should().Be("1.0");
        }

        [Fact]
        public void Configure_EmptyBasePrefix_ComposesPrefixAsVMajor()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(new ApiVersion(2, 0), deprecated: false, "", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>()));

            configurator.Configure(options);

            options.RouteComponents.Should().ContainKey("v2");
            registry.Descriptors[0].RoutePrefix.Should().Be("v2");
            registry.Descriptors[0].BasePrefix.Should().Be("");
            registry.Descriptors[0].GroupName.Should().Be("v2");
        }

        [Fact]
        public void Configure_MajorMinorFormatter_ComposesPrefixAsBaseSlashVMajorDotMinor()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(
                    new ApiVersion(1, 5), deprecated: false, "api",
                    svc => svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>(),
                    opts => opts.SegmentFormatter = ApiVersionSegmentFormatters.MajorMinor));

            configurator.Configure(options);

            options.RouteComponents.Should().ContainKey("api/v1.5");
            registry.Descriptors[0].RoutePrefix.Should().Be("api/v1.5");
            registry.Descriptors[0].GroupName.Should().Be("v1.5");
        }

        [Fact]
        public void Configure_ExplicitRoutePrefix_UsedVerbatim_GroupNameStillFromFormatter()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(
                    new ApiVersion(1, 0), deprecated: false, "api",
                    svc => svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>(),
                    opts => opts.ExplicitRoutePrefix = "legacy/v1-old"));

            configurator.Configure(options);

            options.RouteComponents.Should().ContainKey("legacy/v1-old");
            registry.Descriptors[0].RoutePrefix.Should().Be("legacy/v1-old");
            registry.Descriptors[0].GroupName.Should().Be("v1");
        }

        [Fact]
        public void Configure_PassesSunsetDateThroughToDescriptor()
        {
            var sunset = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(
                    new ApiVersion(1, 0), deprecated: false, "api",
                    svc => svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>(),
                    opts => opts.SunsetDate = sunset));

            configurator.Configure(options);

            registry.Descriptors[0].SunsetDate.Should().Be(sunset);
        }

        [Fact]
        public void Configure_DuplicateApiVersionAndBasePrefix_Throws()
        {
            var (configurator, _, options) = BuildSubject(b =>
            {
                b.AddVersion<SampleApi>(new ApiVersion(1, 0), false, "api", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>());
                b.AddVersion<OtherApi>(new ApiVersion(1, 0), false, "api", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>());
            });

            Action act = () => configurator.Configure(options);

            act.Should().Throw<InvalidOperationException>().WithMessage("*1.0*api*");
        }

        [Fact]
        public void Configure_RunOnlyOnce_GuardsAgainstReEntry()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(new ApiVersion(1, 0), false, "api", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>()));

            configurator.Configure(options);
            configurator.Configure(options);

            registry.Descriptors.Should().HaveCount(1);
            options.RouteComponents.Where(kvp => kvp.Key == "api/v1").Should().HaveCount(1);
        }

        [Fact]
        public void Configure_NormalizesBasePrefix_TrailingSlashStrippedFromRouteAndDescriptor()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(new ApiVersion(1, 0), false, "api/", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>()));

            configurator.Configure(options);

            options.RouteComponents.Should().ContainKey("api/v1");
            registry.Descriptors[0].RoutePrefix.Should().Be("api/v1");
            registry.Descriptors[0].BasePrefix.Should().Be("api",
                "trailing slash on basePrefix must be normalized so it groups with non-slashed registrations");
        }

        [Fact]
        public void Configure_ExplicitGroupNameFormatter_OverridesDefault()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(
                    new ApiVersion(1, 0), false, "api",
                    svc => svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>(),
                    opts => opts.GroupNameFormatter = v => $"orders-v{v.MajorVersion}"));

            configurator.Configure(options);

            registry.Descriptors[0].GroupName.Should().Be("orders-v1");
        }

        [Fact]
        public void Configure_GroupNameCollisionAcrossBasePrefixes_Throws()
        {
            var (configurator, _, options) = BuildSubject(b =>
            {
                b.AddVersion<SampleApi>(new ApiVersion(1, 0), false, "orders", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>());
                b.AddVersion<OtherApi>(new ApiVersion(1, 0), false, "inventory", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>());
            });

            Action act = () => configurator.Configure(options);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*v1*orders*inventory*GroupNameFormatter*",
                    "the configurator must reject duplicate group names with guidance");
        }

        private static (RestierApiVersioningOptionsConfigurator configurator, RestierApiVersionRegistry registry, ODataOptions options) BuildSubject(
            Action<IRestierApiVersioningBuilder> configure)
        {
            var builder = new RestierApiVersioningBuilder();
            configure(builder);
            var registry = new RestierApiVersionRegistry();
            var configurator = new RestierApiVersioningOptionsConfigurator(builder, registry);
            return (configurator, registry, new ODataOptions());
        }

        private class SampleApi : ApiBase
        {
            public SampleApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class OtherApi : ApiBase
        {
            public OtherApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class SampleEntity
        {
            public int Id { get; set; }
        }

        private class SampleModelBuilder : IModelBuilder
        {
            public IModelBuilder Inner { get; set; }

            public IEdmModel GetEdmModel()
            {
                var b = new ODataConventionModelBuilder();
                b.EntitySet<SampleEntity>("Items");
                return b.GetEdmModel();
            }
        }

    }

}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~RestierApiVersioningOptionsConfigurator"
```

Expected: COMPILATION FAILS — `RestierApiVersioningOptionsConfigurator` does not exist.

- [ ] **Step 3: Write the implementation**

Path: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningOptionsConfigurator.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Asp.Versioning;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore;

namespace Microsoft.Restier.AspNetCore.Versioning.Internal
{

    /// <summary>
    /// <see cref="IConfigureOptions{ODataOptions}"/> that drains the builder's pending
    /// version registrations and applies them to the materialized <c>ODataOptions</c>.
    /// </summary>
    internal sealed class RestierApiVersioningOptionsConfigurator : IConfigureOptions<ODataOptions>
    {

        private readonly RestierApiVersioningBuilder _builder;
        private readonly RestierApiVersionRegistry _registry;
        private bool _hasRun;
        private readonly object _lock = new();

        public RestierApiVersioningOptionsConfigurator(
            RestierApiVersioningBuilder builder,
            RestierApiVersionRegistry registry)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void Configure(ODataOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            lock (_lock)
            {
                if (_hasRun)
                {
                    return;
                }

                _hasRun = true;
            }

            foreach (var pending in _builder.PendingRegistrations)
            {
                ApplyOne(options, pending);
            }
        }

        private void ApplyOne(ODataOptions options, PendingVersionRegistration pending)
        {
            var versioningOptions = new RestierVersioningOptions();
            pending.ApplyVersioningOptions?.Invoke(versioningOptions);

            // Normalize basePrefix once: trim trailing '/' so AddVersion("api") and
            // AddVersion("api/") group together.
            var normalizedBasePrefix = (pending.BasePrefix ?? string.Empty).TrimEnd('/');

            // Route segment is always SegmentFormatter; GroupName is independent (default falls
            // back to SegmentFormatter, but RestierVersioningOptions.GroupNameFormatter overrides).
            var routeSegment = versioningOptions.SegmentFormatter(pending.ApiVersion);
            var groupName = versioningOptions.GroupNameFormatter?.Invoke(pending.ApiVersion)
                ?? routeSegment;
            var routePrefix = versioningOptions.ExplicitRoutePrefix
                ?? ComposePrefix(normalizedBasePrefix, routeSegment);

            // Duplicate detection: same (ApiVersion, normalized BasePrefix) is rejected.
            var versionCollision = _registry.Descriptors.FirstOrDefault(d =>
                string.Equals(d.Version, pending.ApiVersion.ToString(), StringComparison.Ordinal)
                && string.Equals(d.BasePrefix, normalizedBasePrefix, StringComparison.Ordinal));
            if (versionCollision is not null)
            {
                throw new InvalidOperationException(
                    $"A Restier API version is already registered with version {pending.ApiVersion} at base prefix " +
                    $"\"{normalizedBasePrefix}\" for type {versionCollision.ApiType.FullName}; " +
                    $"refused to register conflicting type {pending.ApiType.FullName}.");
            }

            // GroupName collision: two descriptors at different basePrefixes would produce the
            // same GroupName (e.g., orders/v1 and inventory/v1 both default to "v1"). Throw with
            // guidance to RestierVersioningOptions.GroupNameFormatter.
            var groupNameCollision = _registry.Descriptors.FirstOrDefault(d =>
                string.Equals(d.GroupName, groupName, StringComparison.OrdinalIgnoreCase));
            if (groupNameCollision is not null)
            {
                throw new InvalidOperationException(
                    $"OpenAPI document GroupName \"{groupName}\" is already registered for base prefix " +
                    $"\"{groupNameCollision.BasePrefix}\" (type {groupNameCollision.ApiType.FullName}); " +
                    $"the new registration for base prefix \"{normalizedBasePrefix}\" (type {pending.ApiType.FullName}) " +
                    $"would collide. Set RestierVersioningOptions.GroupNameFormatter on each call to disambiguate, " +
                    $"e.g. opts.GroupNameFormatter = v => $\"{normalizedBasePrefix}-v{{v.MajorVersion}}\".");
            }

            _registry.Add(
                pending.ApiVersion,
                normalizedBasePrefix,
                routePrefix,
                pending.ApiType,
                pending.IsDeprecated,
                groupName,
                versioningOptions.SunsetDate);

            // Reflect into the existing AddRestierRoute extension. Because that extension is generic,
            // we cannot avoid reflection here — the caller of this configurator runs at startup,
            // so the cost is paid once per host boot.
            var addRestierRoute = typeof(RestierODataOptionsExtensions)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .First(m => m.Name == nameof(RestierODataOptionsExtensions.AddRestierRoute)
                    && m.IsGenericMethod
                    && m.GetParameters().Length == 5);
            var closed = addRestierRoute.MakeGenericMethod(pending.ApiType);
            closed.Invoke(null, new object[]
            {
                options,
                routePrefix,
                pending.ConfigureRouteServices,
                pending.UseRestierBatching,
                pending.NamingConvention,
            });
        }

        private static string ComposePrefix(string basePrefix, string segment)
        {
            if (string.IsNullOrEmpty(basePrefix))
            {
                return segment;
            }

            return basePrefix.TrimEnd('/') + "/" + segment;
        }

    }

}
```

> **Implementation note on the reflection call:** `RestierODataOptionsExtensions.AddRestierRoute<TApi>` is generic and `TApi` is unknown until the configurator runs. The five-argument overload (`oDataOptions`, `routePrefix`, `configureRouteServices`, `useRestierBatching`, `namingConvention`) is the one we target. If the extension signatures change, this `First(m => ...)` predicate must be updated; the call site is centralized in this one file.

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~RestierApiVersioningOptionsConfigurator"
```

Expected: 10 passed.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersioningOptionsConfigurator.cs \
        test/Microsoft.Restier.Tests.AspNetCore.Versioning/Internal/RestierApiVersioningOptionsConfiguratorTests.cs
git commit -m "$(cat <<'COMMIT'
feat: add RestierApiVersioningOptionsConfigurator

IConfigureOptions<ODataOptions> that composes prefixes, populates the
registry, and calls the existing AddRestierRoute<TApi>(...) when
ODataOptions materializes. Guards against double-run.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 11: TDD `AddRestierApiVersioning` (the public entry point)

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.Versioning/Extensions/RestierApiVersioningServiceCollectionExtensions.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Extensions/RestierApiVersioningServiceCollectionExtensionsTests.cs`

The single public extension method on `IServiceCollection`. It uses the find-or-create pattern (NOT `TryAddSingleton`) so multi-call append works correctly.

- [ ] **Step 1: Write the failing tests**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Extensions/RestierApiVersioningServiceCollectionExtensionsTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.AspNetCore.Versioning.Internal;
using Microsoft.Restier.Core;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Extensions
{

    public class RestierApiVersioningServiceCollectionExtensionsTests
    {

        [Fact]
        public void AddRestierApiVersioning_RegistersRegistryAsSingleton()
        {
            var services = new ServiceCollection();

            services.AddRestierApiVersioning(b => { });

            services.Should().Contain(d =>
                d.ServiceType == typeof(IRestierApiVersionRegistry) && d.Lifetime == ServiceLifetime.Singleton);
            services.Should().Contain(d =>
                d.ServiceType == typeof(RestierApiVersionRegistry) && d.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddRestierApiVersioning_RegistersBuilderAsSingletonInstance()
        {
            var services = new ServiceCollection();

            services.AddRestierApiVersioning(b => { });

            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(RestierApiVersioningBuilder));
            descriptor.Should().NotBeNull();
            descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
            descriptor.ImplementationInstance.Should().NotBeNull();
        }

        [Fact]
        public void AddRestierApiVersioning_CalledTwice_AppendsToSameBuilder()
        {
            var services = new ServiceCollection();

            services.AddRestierApiVersioning(b =>
                b.AddVersion<SampleApi>(new ApiVersion(1, 0), deprecated: false, "api", _ => { }));

            services.AddRestierApiVersioning(b =>
                b.AddVersion<SampleApi>(new ApiVersion(2, 0), deprecated: false, "api", _ => { }));

            // Exactly one builder ServiceDescriptor.
            services.Where(d => d.ServiceType == typeof(RestierApiVersioningBuilder)).Should().HaveCount(1);

            // The single builder has both pending registrations.
            var builder = (RestierApiVersioningBuilder)services
                .Single(d => d.ServiceType == typeof(RestierApiVersioningBuilder)).ImplementationInstance;
            builder.PendingRegistrations.Should().HaveCount(2);
            builder.PendingRegistrations.Should().Contain(p => p.ApiVersion == new ApiVersion(1, 0));
            builder.PendingRegistrations.Should().Contain(p => p.ApiVersion == new ApiVersion(2, 0));
        }

        [Fact]
        public void AddRestierApiVersioning_RegistersConfigureOptions()
        {
            var services = new ServiceCollection();

            services.AddRestierApiVersioning(b => { });

            services.Should().Contain(d =>
                d.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.OData.ODataOptions>)
                && d.ImplementationType == typeof(RestierApiVersioningOptionsConfigurator));
        }

        [Fact]
        public void AddRestierApiVersioning_ReplacesAnyPriorIApiVersionDescriptionProviderWithComposite()
        {
            // Simulate a prior Asp.Versioning registration (e.g., AddApiVersioning().AddApiExplorer()).
            var services = new ServiceCollection();
            var priorProvider = NSubstitute.Substitute.For<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
            services.AddSingleton(priorProvider);

            services.AddRestierApiVersioning(b => { });

            // Exactly one provider descriptor remains; it's a factory registration.
            var providerDescriptors = services
                .Where(d => d.ServiceType == typeof(Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider))
                .ToArray();
            providerDescriptors.Should().HaveCount(1);
            providerDescriptors[0].ImplementationFactory.Should().NotBeNull(
                "the composite is registered via factory so it can capture and inject the prior provider");
        }

        [Fact]
        public void AddRestierApiVersioning_CalledTwice_DoesNotDoubleReplaceProvider()
        {
            var services = new ServiceCollection();
            var priorProvider = NSubstitute.Substitute.For<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
            services.AddSingleton(priorProvider);

            services.AddRestierApiVersioning(b => { });
            services.AddRestierApiVersioning(b => { });

            services
                .Where(d => d.ServiceType == typeof(Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider))
                .Should().HaveCount(1);
        }

        private class SampleApi : ApiBase
        {
            public SampleApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }

    }

}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~RestierApiVersioningServiceCollectionExtensions"
```

Expected: COMPILATION FAILS — `AddRestierApiVersioning` does not exist.

- [ ] **Step 3: Write the implementation**

Path: `src/Microsoft.Restier.AspNetCore.Versioning/Extensions/RestierApiVersioningServiceCollectionExtensions.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore.Versioning.Internal;

// IMPORTANT (registration ordering): if the consumer calls AddApiVersioning().AddApiExplorer()
// (the canonical setup), they MUST do so BEFORE calling AddRestierApiVersioning. The composite
// IApiVersionDescriptionProvider captures the prior registration as `inner` so MVC controller
// versions still surface.

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Registers Restier API-versioning services on an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class RestierApiVersioningServiceCollectionExtensions
    {

        /// <summary>
        /// Registers the <see cref="IRestierApiVersionRegistry"/>, the
        /// <see cref="Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider"/> adapter, and an
        /// <see cref="IConfigureOptions{ODataOptions}"/> that adds versioned Restier routes when
        /// <c>ODataOptions</c> materializes.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">A delegate that declares versions via the builder.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Multiple calls to this method append additional version registrations to a single
        /// shared <see cref="IRestierApiVersioningBuilder"/>. This method does NOT use
        /// <c>TryAddSingleton</c> for the builder — it locates the existing builder
        /// <see cref="ServiceDescriptor"/> in the collection (if any) and reuses its
        /// <see cref="ServiceDescriptor.ImplementationInstance"/>.
        /// </remarks>
        public static IServiceCollection AddRestierApiVersioning(
            this IServiceCollection services,
            Action<IRestierApiVersioningBuilder> configure)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = FindOrCreateBuilder(services);
            configure(builder);

            services.TryAddSingleton<RestierApiVersionRegistry>();
            services.TryAddSingleton<IRestierApiVersionRegistry>(sp => sp.GetRequiredService<RestierApiVersionRegistry>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<ODataOptions>, RestierApiVersioningOptionsConfigurator>());

            // IApiVersionDescriptionProvider is single-instance in Asp.Versioning's API. The
            // canonical setup calls AddApiVersioning().AddApiExplorer() before this method,
            // which registers Asp.Versioning's DefaultApiVersionDescriptionProvider.
            // TryAddSingleton would silently skip our adapter and ApiExplorer would never see
            // RESTier routes. Use a composite that wraps the prior provider (if any) so MVC
            // controller versions and Restier versions both surface.
            ReplaceApiVersionDescriptionProviderWithComposite(services);

            return services;
        }

        private static RestierApiVersioningBuilder FindOrCreateBuilder(IServiceCollection services)
        {
            var existing = services.FirstOrDefault(d => d.ServiceType == typeof(RestierApiVersioningBuilder));
            if (existing is not null)
            {
                if (existing.ImplementationInstance is RestierApiVersioningBuilder b)
                {
                    return b;
                }

                throw new InvalidOperationException(
                    "A RestierApiVersioningBuilder service descriptor exists but does not have an ImplementationInstance. " +
                    "AddRestierApiVersioning must register the builder via instance registration.");
            }

            var created = new RestierApiVersioningBuilder();
            services.AddSingleton(created);
            return created;
        }

        /// <summary>
        /// Replace any existing <see cref="Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider"/>
        /// registration with a composite <see cref="RestierApiVersionDescriptionProvider"/> that
        /// wraps the prior provider as <c>inner</c>. The canonical setup runs
        /// <c>AddApiVersioning().AddApiExplorer()</c> first; if so, the prior registration is
        /// Asp.Versioning's <c>DefaultApiVersionDescriptionProvider</c>, and the composite merges
        /// MVC-controller descriptions with the Restier registry's descriptions.
        /// </summary>
        private static void ReplaceApiVersionDescriptionProviderWithComposite(IServiceCollection services)
        {
            var providerType = typeof(Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider);

            // If the composite is already registered (multiple AddRestierApiVersioning calls),
            // do not re-replace.
            var existing = services.LastOrDefault(d => d.ServiceType == providerType);
            if (existing is { ImplementationFactory: not null }
                && existing.ImplementationFactory.Method.Name.Contains("RestierApiVersionDescriptionProvider", StringComparison.Ordinal))
            {
                return;
            }

            var prior = existing;
            if (prior is not null)
            {
                services.Remove(prior);
            }

            services.AddSingleton<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>(sp =>
            {
                Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider inner = null;
                if (prior is not null)
                {
                    inner = prior.ImplementationInstance as Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider
                        ?? (prior.ImplementationFactory is { } factory
                            ? (Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider)factory(sp)
                            : prior.ImplementationType is { } implType
                                ? (Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider)ActivatorUtilities.CreateInstance(sp, implType)
                                : null);
                }

                return new RestierApiVersionDescriptionProvider(
                    sp.GetRequiredService<IOptions<ODataOptions>>(),
                    sp.GetRequiredService<IRestierApiVersionRegistry>(),
                    inner);
            });
        }

    }

}
```

> **Note:** The composite-replacement code above resolves `RestierApiVersionDescriptionProvider` (added in Task 12) via `ActivatorUtilities`-style instantiation through the factory delegate. Add a placeholder class so the package compiles after Task 11. The placeholder must already accept the three constructor parameters that the factory passes in (otherwise Task 11's tests can't construct it).
>
> Path: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersionDescriptionProvider.cs`
>
> ```csharp
> // Copyright (c) Microsoft Corporation.  All rights reserved.
> // Licensed under the MIT License.  See License.txt in the project root for license information.
>
> using System;
> using System.Collections.Generic;
> using Asp.Versioning;
> using Asp.Versioning.ApiExplorer;
> using Microsoft.AspNetCore.OData;
> using Microsoft.Extensions.Options;
>
> namespace Microsoft.Restier.AspNetCore.Versioning
> {
>     // Placeholder; full composite implementation lands in Task 12.
>     internal sealed class RestierApiVersionDescriptionProvider : IApiVersionDescriptionProvider
>     {
>         public RestierApiVersionDescriptionProvider(
>             IOptions<ODataOptions> odataOptions,
>             IRestierApiVersionRegistry registry,
>             IApiVersionDescriptionProvider inner)
>         {
>         }
>         public IReadOnlyList<ApiVersionDescription> ApiVersionDescriptions => throw new NotImplementedException();
>         public bool IsDeprecated(ApiVersion apiVersion) => throw new NotImplementedException();
>     }
> }
> ```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~RestierApiVersioningServiceCollectionExtensions"
```

Expected: 4 passed.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Versioning/Extensions/RestierApiVersioningServiceCollectionExtensions.cs \
        src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersionDescriptionProvider.cs \
        test/Microsoft.Restier.Tests.AspNetCore.Versioning/Extensions/RestierApiVersioningServiceCollectionExtensionsTests.cs
git commit -m "$(cat <<'COMMIT'
feat: add AddRestierApiVersioning entry point

Find-or-create the RestierApiVersioningBuilder in the service collection
so multiple AddRestierApiVersioning calls append registrations to the
same builder. Registers the registry, the configurator, and a placeholder
description provider (filled in by the next task).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```


---

## Phase 6 — `IApiVersionDescriptionProvider` adapter

### Task 12: TDD `RestierApiVersionDescriptionProvider` with the materialization invariant

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersionDescriptionProvider.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Internal/RestierApiVersionDescriptionProviderTests.cs`

The provider observes the materialization invariant: it depends on `IOptions<ODataOptions>` and reads `.Value` once on first access, ensuring the configurator pipeline runs before the registry is read. This is the spec's load-bearing safeguard for ApiExplorer / Swashbuckle / NSwag consumers that resolve description providers during host startup.

- [ ] **Step 1: Write the failing tests**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Internal/RestierApiVersionDescriptionProviderTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentAssertions;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.AspNetCore.Versioning.Internal;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Internal
{

    public class RestierApiVersionDescriptionProviderTests
    {

        [Fact]
        public void ApiVersionDescriptions_TouchesIOptionsValueBeforeReadingRegistry()
        {
            var registry = new RestierApiVersionRegistry();
            var optionsAccessed = false;
            var odataOptions = Substitute.For<IOptions<ODataOptions>>();
            odataOptions.Value.Returns(_ =>
            {
                optionsAccessed = true;
                registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);
                return new ODataOptions();
            });

            var provider = new RestierApiVersionDescriptionProvider(odataOptions, registry, inner: null);

            var descriptions = provider.ApiVersionDescriptions;

            optionsAccessed.Should().BeTrue("the provider must read IOptions<ODataOptions>.Value before reading the registry");
            descriptions.Should().HaveCount(1);
            descriptions[0].ApiVersion.Should().Be(new ApiVersion(1, 0));
            descriptions[0].GroupName.Should().Be("v1");
            descriptions[0].IsDeprecated.Should().BeFalse();
        }

        [Fact]
        public void ApiVersionDescriptions_PopulatesGroupNameAndDeprecatedFlagFromDescriptor()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), isDeprecated: true, "v1", null);
            registry.Add(new ApiVersion(2, 0), "api", "api/v2", typeof(SampleApi), isDeprecated: false, "v2", null);

            var odataOptions = Substitute.For<IOptions<ODataOptions>>();
            odataOptions.Value.Returns(new ODataOptions());
            var provider = new RestierApiVersionDescriptionProvider(odataOptions, registry, inner: null);

            provider.ApiVersionDescriptions.Should().HaveCount(2);
            provider.ApiVersionDescriptions.Should().ContainSingle(d => d.ApiVersion == new ApiVersion(1, 0) && d.IsDeprecated && d.GroupName == "v1");
            provider.ApiVersionDescriptions.Should().ContainSingle(d => d.ApiVersion == new ApiVersion(2, 0) && !d.IsDeprecated && d.GroupName == "v2");
        }

        [Fact]
        public void ApiVersionDescriptions_WhenInnerProviderPresent_MergesInnerAndRestierDescriptions()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(2, 0), "api", "api/v2", typeof(SampleApi), false, "v2", null);

            var inner = Substitute.For<IApiVersionDescriptionProvider>();
            inner.ApiVersionDescriptions.Returns(new[]
            {
                new ApiVersionDescription(new ApiVersion(1, 0), "controllers-v1", deprecated: false),
            });

            var odataOptions = Substitute.For<IOptions<ODataOptions>>();
            odataOptions.Value.Returns(new ODataOptions());
            var provider = new RestierApiVersionDescriptionProvider(odataOptions, registry, inner);

            provider.ApiVersionDescriptions.Should().HaveCount(2);
            provider.ApiVersionDescriptions.Should().ContainSingle(d => d.GroupName == "controllers-v1");
            provider.ApiVersionDescriptions.Should().ContainSingle(d => d.GroupName == "v2");
        }

        [Fact]
        public void IsDeprecated_ReturnsTrueOnlyWhenAllRestierDescriptorsAreDeprecated()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), isDeprecated: true, "v1", null);
            registry.Add(new ApiVersion(2, 0), "api", "api/v2", typeof(SampleApi), isDeprecated: false, "v2", null);

            var odataOptions = Substitute.For<IOptions<ODataOptions>>();
            odataOptions.Value.Returns(new ODataOptions());
            var provider = new RestierApiVersionDescriptionProvider(odataOptions, registry, inner: null);

            provider.IsDeprecated(new ApiVersion(1, 0)).Should().BeTrue();
            provider.IsDeprecated(new ApiVersion(2, 0)).Should().BeFalse();
            provider.IsDeprecated(new ApiVersion(99, 0)).Should().BeFalse();
        }

        [Fact]
        public void IsDeprecated_DelegatesToInnerForVersionsNotInRegistry()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(2, 0), "api", "api/v2", typeof(SampleApi), false, "v2", null);

            var inner = Substitute.For<IApiVersionDescriptionProvider>();
            inner.IsDeprecated(new ApiVersion(1, 0)).Returns(true);

            var odataOptions = Substitute.For<IOptions<ODataOptions>>();
            odataOptions.Value.Returns(new ODataOptions());
            var provider = new RestierApiVersionDescriptionProvider(odataOptions, registry, inner);

            provider.IsDeprecated(new ApiVersion(1, 0)).Should().BeTrue("inner provider says so");
        }

        private class SampleApi : ApiBase
        {
            public SampleApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }

    }

}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~RestierApiVersionDescriptionProvider"
```

Expected: All tests FAIL — the placeholder from Task 11 throws `NotImplementedException`.

- [ ] **Step 3: Replace the placeholder with the real implementation**

Path: `src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersionDescriptionProvider.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;

namespace Microsoft.Restier.AspNetCore.Versioning
{

    /// <summary>
    /// Composite <see cref="IApiVersionDescriptionProvider"/>: merges descriptions from an
    /// optional <paramref name="inner"/> provider (typically Asp.Versioning's
    /// <c>DefaultApiVersionDescriptionProvider</c>, which reports MVC-controller versions)
    /// with descriptions sourced from <see cref="IRestierApiVersionRegistry"/>. Honors the
    /// materialization invariant by touching <c>IOptions&lt;ODataOptions&gt;.Value</c> before
    /// reading the registry.
    /// </summary>
    internal sealed class RestierApiVersionDescriptionProvider : IApiVersionDescriptionProvider
    {

        private readonly IOptions<ODataOptions> _odataOptions;
        private readonly IRestierApiVersionRegistry _registry;
        private readonly IApiVersionDescriptionProvider _inner;

        public RestierApiVersionDescriptionProvider(
            IOptions<ODataOptions> odataOptions,
            IRestierApiVersionRegistry registry,
            IApiVersionDescriptionProvider inner)
        {
            _odataOptions = odataOptions ?? throw new ArgumentNullException(nameof(odataOptions));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _inner = inner;   // optional
        }

        public IReadOnlyList<ApiVersionDescription> ApiVersionDescriptions
        {
            get
            {
                // Materialization invariant.
                _ = _odataOptions.Value;

                IEnumerable<ApiVersionDescription> innerDescriptions = _inner?.ApiVersionDescriptions
                    ?? Array.Empty<ApiVersionDescription>();

                var registryDescriptions = _registry.Descriptors
                    .Select(d => new ApiVersionDescription(
                        ApiVersion.Parse(d.Version),
                        d.GroupName,
                        d.IsDeprecated));

                return innerDescriptions.Concat(registryDescriptions).ToArray();
            }
        }

        public bool IsDeprecated(ApiVersion apiVersion)
        {
            if (apiVersion is null)
            {
                return false;
            }

            _ = _odataOptions.Value;

            var versionString = apiVersion.ToString();
            var registryMatches = _registry.Descriptors
                .Where(d => string.Equals(d.Version, versionString, StringComparison.Ordinal))
                .ToArray();

            if (registryMatches.Length > 0)
            {
                return registryMatches.All(d => d.IsDeprecated);
            }

            // Not a Restier-registered version; defer to the inner provider (e.g., MVC controllers).
            return _inner?.IsDeprecated(apiVersion) ?? false;
        }

    }

}
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~RestierApiVersionDescriptionProvider"
```

Expected: 3 passed.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Versioning/Internal/RestierApiVersionDescriptionProvider.cs \
        test/Microsoft.Restier.Tests.AspNetCore.Versioning/Internal/RestierApiVersionDescriptionProviderTests.cs
git commit -m "$(cat <<'COMMIT'
feat: implement RestierApiVersionDescriptionProvider

Adapter from IRestierApiVersionRegistry to IApiVersionDescriptionProvider.
Reads IOptions<ODataOptions>.Value before reading the registry to
honor the materialization invariant — ApiExplorer/Swashbuckle/NSwag
consumers resolving the provider during host startup see populated
descriptions.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```


---

## Phase 7 — Version-discovery headers middleware

### Task 13: TDD `RestierVersionHeadersMiddleware`

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.Versioning/Middleware/RestierVersionHeadersMiddleware.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Middleware/RestierVersionHeadersMiddlewareTests.cs`

The middleware is a response-side filter: it inspects the request path (which ASP.NET Core has already made `PathBase`-relative), longest-prefix-matches against registry descriptors via `PathString.StartsWithSegments`, and registers a `Response.OnStarting` callback that emits headers using the matched descriptor's `BasePrefix` group. The matching logic is exposed as a static method (`TryMatch`) so it can be unit-tested without spinning up a host. Header behavior (group isolation, sunset, "do not overwrite") is verified in the integration tests of Phase 8 because it depends on `OnStarting` firing — which only happens with a real `TestServer`.

- [ ] **Step 1: Write the failing tests for `TryMatch` (the path-matching unit)**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Middleware/RestierVersionHeadersMiddlewareTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Asp.Versioning;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.AspNetCore.Versioning.Middleware;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Middleware
{

    /// <summary>
    /// Unit-level coverage for the path-matching logic. Header-emission behavior (group isolation,
    /// sunset, "do not overwrite") is exercised by integration tests in
    /// <c>VersionHeadersIntegrationTests</c> because it depends on <c>HttpResponse.OnStarting</c>
    /// callbacks firing, which only happens through a real <c>TestServer</c>.
    /// </summary>
    public class RestierVersionHeadersMiddlewareTests
    {

        [Fact]
        public void TryMatch_NoDescriptors_ReturnsNull()
        {
            var registry = new RestierApiVersionRegistry();
            RestierVersionHeadersMiddleware.TryMatch(registry, new PathString("/api/v1/x"))
                .Should().BeNull();
        }

        [Fact]
        public void TryMatch_NoPrefixMatch_ReturnsNull()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            RestierVersionHeadersMiddleware.TryMatch(registry, new PathString("/unrelated/path"))
                .Should().BeNull();
        }

        [Fact]
        public void TryMatch_ExactPrefix_Matches()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            RestierVersionHeadersMiddleware.TryMatch(registry, new PathString("/api/v1"))
                .Should().NotBeNull();
        }

        [Fact]
        public void TryMatch_PrefixWithTrailing_Matches()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            RestierVersionHeadersMiddleware.TryMatch(registry, new PathString("/api/v1/Customers"))
                .Should().NotBeNull();
        }

        [Fact]
        public void TryMatch_LookalikePrefix_DoesNotMatch()
        {
            // Segment-boundary safe: /api/v10 must not match a registration for "api/v1".
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            RestierVersionHeadersMiddleware.TryMatch(registry, new PathString("/api/v10/anything"))
                .Should().BeNull();
        }

        [Fact]
        public void TryMatch_LongestPrefixWins()
        {
            // If both "api" (unversioned) and "api/v1" are registered, /api/v1/x must pick "api/v1".
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api", typeof(SampleApi), false, "default", null);
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            var match = RestierVersionHeadersMiddleware.TryMatch(registry, new PathString("/api/v1/x"));
            match.Should().NotBeNull();
            match.RoutePrefix.Should().Be("api/v1");
        }

        private class SampleApi { }

    }

}
```

> **Why no unit tests for header emission?** ASP.NET Core's `DefaultHttpResponseFeature.OnStarting` does not invoke registered callbacks unless the response is actually being written by the host. There is no public API to drive those callbacks from a unit test. Rather than implement the headers in a synchronously-applied way that contradicts the spec's "response-side, never overwrite" requirement, we restrict unit tests to the path-matching logic and verify header behavior through the integration tests in Task 17. Those tests use `TestServer`, which honors `OnStarting`.

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~RestierVersionHeadersMiddleware"
```

Expected: COMPILATION FAILS — `RestierVersionHeadersMiddleware` does not exist.

- [ ] **Step 3: Write the implementation**

Path: `src/Microsoft.Restier.AspNetCore.Versioning/Middleware/RestierVersionHeadersMiddleware.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;

namespace Microsoft.Restier.AspNetCore.Versioning.Middleware
{

    /// <summary>
    /// Emits <c>api-supported-versions</c>, <c>api-deprecated-versions</c>, and <c>Sunset</c>
    /// response headers on requests whose path matches a registered Restier versioned route.
    /// Headers are scoped to the matched descriptor's <see cref="RestierApiVersionDescriptor.BasePrefix"/>
    /// group so unrelated APIs at other base prefixes do not leak versions into each other's headers.
    /// Headers are applied via <see cref="HttpResponse.OnStarting(System.Func{object,Task},object)"/>
    /// so they fire after the inner pipeline, just before the response begins.
    /// </summary>
    internal sealed class RestierVersionHeadersMiddleware
    {

        private readonly RequestDelegate _next;
        private readonly IRestierApiVersionRegistry _registry;
        private readonly IOptions<ODataOptions> _odataOptions;

        // Standard UseMiddleware<T> shape: RequestDelegate is the first ctor param;
        // additional services are resolved per-request from the request scope.
        public RestierVersionHeadersMiddleware(
            RequestDelegate next,
            IRestierApiVersionRegistry registry,
            IOptions<ODataOptions> odataOptions)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _odataOptions = odataOptions ?? throw new ArgumentNullException(nameof(odataOptions));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Materialization invariant: ensure the registry has been populated.
            _ = _odataOptions.Value;

            var matched = TryMatch(_registry, context.Request.Path);
            if (matched is not null)
            {
                // OnStarting fires after the inner pipeline produces the response, just before
                // headers are flushed. This honors the "do not overwrite already-set headers"
                // contract because we run after downstream code has had its chance to set them.
                context.Response.OnStarting(static state =>
                {
                    var (response, descriptor, registry) = ((HttpResponse, RestierApiVersionDescriptor, IRestierApiVersionRegistry))state;
                    ApplyHeaders(response, descriptor, registry);
                    return Task.CompletedTask;
                }, (context.Response, matched, _registry));
            }

            await _next(context);
        }

        /// <summary>
        /// Longest-prefix-match against the registry. Uses <see cref="PathString.StartsWithSegments(PathString)"/>
        /// for segment-boundary safety. <see cref="HttpRequest.Path"/> is already
        /// <see cref="HttpRequest.PathBase"/>-relative when middleware see it, so we don't need to
        /// strip <c>PathBase</c> ourselves.
        /// </summary>
        internal static RestierApiVersionDescriptor TryMatch(IRestierApiVersionRegistry registry, PathString path)
        {
            RestierApiVersionDescriptor longest = null;
            foreach (var descriptor in registry.Descriptors)
            {
                var candidate = new PathString("/" + descriptor.RoutePrefix);
                if (path.StartsWithSegments(candidate))
                {
                    if (longest is null || descriptor.RoutePrefix.Length > longest.RoutePrefix.Length)
                    {
                        longest = descriptor;
                    }
                }
            }

            return longest;
        }

        private static void ApplyHeaders(HttpResponse response, RestierApiVersionDescriptor matched, IRestierApiVersionRegistry registry)
        {
            var group = registry.FindByBasePrefix(matched.BasePrefix);

            if (!response.Headers.ContainsKey("api-supported-versions"))
            {
                var supported = string.Join(", ", group.Select(d => d.Version));
                if (supported.Length > 0)
                {
                    response.Headers["api-supported-versions"] = supported;
                }
            }

            if (!response.Headers.ContainsKey("api-deprecated-versions"))
            {
                var deprecated = string.Join(", ", group.Where(d => d.IsDeprecated).Select(d => d.Version));
                if (deprecated.Length > 0)
                {
                    response.Headers["api-deprecated-versions"] = deprecated;
                }
            }

            if (matched.SunsetDate is { } sunset && !response.Headers.ContainsKey("Sunset"))
            {
                response.Headers["Sunset"] = sunset.UtcDateTime.ToString("R", CultureInfo.InvariantCulture);
            }
        }

    }

}
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~RestierVersionHeadersMiddleware"
```

Expected: 7 passed.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Versioning/Middleware/RestierVersionHeadersMiddleware.cs \
        test/Microsoft.Restier.Tests.AspNetCore.Versioning/Middleware/RestierVersionHeadersMiddlewareTests.cs
git commit -m "$(cat <<'COMMIT'
feat: add RestierVersionHeadersMiddleware

Segment-boundary safe matching via PathString.StartsWithSegments;
longest-prefix-match wins; group-isolated header emission keyed on
the matched descriptor's BasePrefix; never overwrites already-set
headers; emits Sunset only when configured.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 14: Add `UseRestierVersionHeaders` extension

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.Versioning/Extensions/RestierVersionedApplicationBuilderExtensions.cs`

- [ ] **Step 1: Write the type**

Path: `src/Microsoft.Restier.AspNetCore.Versioning/Extensions/RestierVersionedApplicationBuilderExtensions.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.AspNetCore.Versioning.Middleware;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Extension methods on <see cref="IApplicationBuilder"/> for the Restier API-versioning package.
    /// </summary>
    public static class Restier_AspNetCore_Versioning_IApplicationBuilderExtensions
    {

        /// <summary>
        /// Adds middleware that emits <c>api-supported-versions</c>, <c>api-deprecated-versions</c>,
        /// and <c>Sunset</c> response headers on requests targeting registered versioned Restier routes.
        /// </summary>
        public static IApplicationBuilder UseRestierVersionHeaders(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RestierVersionHeadersMiddleware>();
        }

    }

}
```

- [ ] **Step 2: Build**

```bash
dotnet build src/Microsoft.Restier.AspNetCore.Versioning/Microsoft.Restier.AspNetCore.Versioning.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Versioning/Extensions/RestierVersionedApplicationBuilderExtensions.cs
git commit -m "$(cat <<'COMMIT'
feat: add UseRestierVersionHeaders extension

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```


---

## Phase 8 — End-to-end integration tests for the Versioning package

### Task 15: Add the integration-test fixture

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Infrastructure/VersionedApiFixture.cs`

This fixture defines two `ApiBase`-derived classes (`SampleApiV1`, `SampleApiV2`), an in-memory model builder, and a `BuildHostAsync(...)` helper that wires the canonical Asp.Versioning + Restier-Versioning pipeline. Subsequent integration tests reuse it.

- [ ] **Step 1: Verify the directory does not exist**

```bash
test ! -e test/Microsoft.Restier.Tests.AspNetCore.Versioning/Infrastructure && echo "OK"
```

Expected: `OK`

- [ ] **Step 2: Create the directory and write the fixture**

```bash
mkdir -p test/Microsoft.Restier.Tests.AspNetCore.Versioning/Infrastructure
```

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Infrastructure/VersionedApiFixture.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure
{

    [ApiVersion("1.0", Deprecated = true)]
    public class SampleApiV1 : ApiBase
    {
        public SampleApiV1(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }

        public IQueryable<SampleEntity> Items => Enumerable.Empty<SampleEntity>().AsQueryable();
    }

    [ApiVersion("2.0")]
    public class SampleApiV2 : ApiBase
    {
        public SampleApiV2(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }

        public IQueryable<SampleEntity> Items => Enumerable.Empty<SampleEntity>().AsQueryable();

        // V2-only entity set
        public IQueryable<SampleAuditLog> AuditLogs => Enumerable.Empty<SampleAuditLog>().AsQueryable();
    }

    public class SampleEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SampleAuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; }
    }

    public class SampleV1ModelBuilder : IModelBuilder
    {
        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel()
        {
            var b = new ODataConventionModelBuilder();
            b.EntitySet<SampleEntity>(nameof(SampleApiV1.Items));
            return b.GetEdmModel();
        }
    }

    public class SampleV2ModelBuilder : IModelBuilder
    {
        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel()
        {
            var b = new ODataConventionModelBuilder();
            b.EntitySet<SampleEntity>(nameof(SampleApiV2.Items));
            b.EntitySet<SampleAuditLog>(nameof(SampleApiV2.AuditLogs));
            return b.GetEdmModel();
        }
    }

    public static class VersionedApiFixture
    {

        public static async Task<IHost> BuildHostAsync(CancellationToken cancellationToken)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddApiVersioning(o =>
                        {
                            o.DefaultApiVersion = new ApiVersion(2, 0);
                            o.ReportApiVersions = true;
                            o.ApiVersionReader = new UrlSegmentApiVersionReader();
                        }).AddApiExplorer();

                        services.AddControllers().AddRestier(options =>
                        {
                            options.Select().Expand().Filter().OrderBy().Count();
                        });

                        services.AddRestierApiVersioning(b => b
                            .AddVersion<SampleApiV1>("api", svc =>
                                svc.AddSingleton<IChainedService<IModelBuilder>, SampleV1ModelBuilder>())
                            .AddVersion<SampleApiV2>("api", svc =>
                                svc.AddSingleton<IChainedService<IModelBuilder>, SampleV2ModelBuilder>()));
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseRestierVersionHeaders();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapRestier();
                        });
                    }));

            return await builder.StartAsync(cancellationToken);
        }

    }

}
```

- [ ] **Step 3: Build**

```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore.Versioning/Infrastructure/VersionedApiFixture.cs
git commit -m "$(cat <<'COMMIT'
test: add versioned-API integration test fixture

SampleApiV1 + SampleApiV2 with a real surface delta (V2 adds AuditLogs),
plus a BuildHostAsync helper that wires AddApiVersioning + AddRestier +
AddRestierApiVersioning + UseRestierVersionHeaders + MapRestier.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 16: TDD versioned `$metadata` and per-version GET

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/VersionedMetadataTests.cs`

- [ ] **Step 1: Write the failing tests**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/VersionedMetadataTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.IntegrationTests
{

    public class VersionedMetadataTests
    {

        [Fact]
        public async Task GetV1Metadata_ReturnsV1Edm_WithoutAuditLogs()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/api/v1/$metadata", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            body.Should().Contain("EntitySet Name=\"Items\"");
            body.Should().NotContain("EntitySet Name=\"AuditLogs\"",
                "V1 EDM must not surface V2-only entity sets");
        }

        [Fact]
        public async Task GetV2Metadata_ReturnsV2Edm_WithAuditLogs()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/api/v2/$metadata", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            body.Should().Contain("EntitySet Name=\"Items\"");
            body.Should().Contain("EntitySet Name=\"AuditLogs\"");
        }

        [Fact]
        public async Task GetV3_ReturnsNotFound()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/api/v3/Items", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetV1Items_ReturnsOk()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/api/v1/Items", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

    }

}
```

- [ ] **Step 2: Run the tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~VersionedMetadata"
```

Expected: 4 passed.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/VersionedMetadataTests.cs
git commit -m "$(cat <<'COMMIT'
test: cover versioned \$metadata and per-version GET 404

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 17: TDD versioning headers across the full pipeline

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/VersionHeadersIntegrationTests.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Infrastructure/MultiGroupApiFixture.cs`

These tests exercise the parts of the headers middleware that depend on `OnStarting` actually firing — group isolation, `Sunset` header emission, the "do not overwrite" rule. They use `TestServer`, which fires `OnStarting` callbacks the same way Kestrel does.

- [ ] **Step 1: Write the multi-group fixture**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Infrastructure/MultiGroupApiFixture.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure
{

    [ApiVersion("1.0")]
    [ApiVersion("2.0", Deprecated = true)]
    public class OrdersApi : ApiBase
    {
        public OrdersApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler) { }
        public IQueryable<SampleEntity> Orders => Enumerable.Empty<SampleEntity>().AsQueryable();
    }

    [ApiVersion("1.0")]
    public class InventoryApi : ApiBase
    {
        public InventoryApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler) { }
        public IQueryable<SampleEntity> Stock => Enumerable.Empty<SampleEntity>().AsQueryable();
    }

    public class OrdersModelBuilder : IModelBuilder
    {
        public IModelBuilder Inner { get; set; }
        public IEdmModel GetEdmModel()
        {
            var b = new ODataConventionModelBuilder();
            b.EntitySet<SampleEntity>(nameof(OrdersApi.Orders));
            return b.GetEdmModel();
        }
    }

    public class InventoryModelBuilder : IModelBuilder
    {
        public IModelBuilder Inner { get; set; }
        public IEdmModel GetEdmModel()
        {
            var b = new ODataConventionModelBuilder();
            b.EntitySet<SampleEntity>(nameof(InventoryApi.Stock));
            return b.GetEdmModel();
        }
    }

    public static class MultiGroupApiFixture
    {

        public static async Task<IHost> BuildHostAsync(CancellationToken cancellationToken, DateTimeOffset? ordersV2Sunset = null)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddApiVersioning().AddApiExplorer();
                        services.AddControllers().AddRestier(options =>
                        {
                            options.Select().Expand().Filter().OrderBy().Count();
                        });
                        services.AddRestierApiVersioning(b =>
                        {
                            // GroupNameFormatter disambiguates "v1" between the two logical APIs
                            // (orders-v1, orders-v2, inventory-v1). Without it the configurator
                            // throws on GroupName collision.
                            b.AddVersion<OrdersApi>("orders",
                                svc => svc.AddSingleton<IChainedService<IModelBuilder>, OrdersModelBuilder>(),
                                opts => opts.GroupNameFormatter = v => $"orders-v{v.MajorVersion}");

                            // Apply sunset on V2 specifically when configured.
                            if (ordersV2Sunset is { } sunset)
                            {
                                b.AddVersion<OrdersApi>(
                                    new ApiVersion(2, 0), deprecated: true, "orders",
                                    svc => svc.AddSingleton<IChainedService<IModelBuilder>, OrdersModelBuilder>(),
                                    opts =>
                                    {
                                        opts.GroupNameFormatter = v => $"orders-v{v.MajorVersion}";
                                        opts.SunsetDate = sunset;
                                    });
                            }

                            b.AddVersion<InventoryApi>("inventory",
                                svc => svc.AddSingleton<IChainedService<IModelBuilder>, InventoryModelBuilder>(),
                                opts => opts.GroupNameFormatter = v => $"inventory-v{v.MajorVersion}");
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseRestierVersionHeaders();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapRestier();
                        });
                    }));

            return await builder.StartAsync(cancellationToken);
        }

    }

}
```

> Note: The fixture's "if `ordersV2Sunset` then re-AddVersion" pattern is ugly but works because the imperative overload is independent of the attribute path. For a cleaner fixture, refactor `OrdersApi` into `OrdersApiV1` / `OrdersApiV2` like the main fixture; the form above keeps the test compact and exercises both overloads.

- [ ] **Step 2: Write the failing tests**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/VersionHeadersIntegrationTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.IntegrationTests
{

    public class VersionHeadersIntegrationTests
    {

        [Fact]
        public async Task V1Response_CarriesSupportedAndDeprecatedVersionsHeaders()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/api/v1/Items", cancellationToken);

            response.Headers.GetValues("api-supported-versions").Single().Should().Be("1.0, 2.0");
            response.Headers.GetValues("api-deprecated-versions").Single().Should().Be("1.0");
        }

        [Fact]
        public async Task V2Response_CarriesSupportedHeader_AndDeprecatedHeader()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/api/v2/Items", cancellationToken);

            response.Headers.GetValues("api-supported-versions").Single().Should().Be("1.0, 2.0");
            response.Headers.GetValues("api-deprecated-versions").Single().Should().Be("1.0");
        }

        [Fact]
        public async Task UnrelatedPath_DoesNotCarryHeaders()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/some/unrelated/path", cancellationToken);

            response.Headers.Contains("api-supported-versions").Should().BeFalse();
        }

        [Fact]
        public async Task GroupIsolation_OrdersHeadersDoNotIncludeInventoryVersions()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await MultiGroupApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var ordersResponse = await client.GetAsync("/orders/v1/Orders", cancellationToken);
            ordersResponse.Headers.GetValues("api-supported-versions").Single().Should().Be("1.0, 2.0");

            var inventoryResponse = await client.GetAsync("/inventory/v1/Stock", cancellationToken);
            inventoryResponse.Headers.GetValues("api-supported-versions").Single().Should().Be("1.0");
        }

        [Fact]
        public async Task SunsetHeader_OnlyEmittedForVersionWithSunsetConfigured()
        {
            var sunset = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await MultiGroupApiFixture.BuildHostAsync(cancellationToken, ordersV2Sunset: sunset);
            var client = host.GetTestClient();

            var v1Response = await client.GetAsync("/orders/v1/Orders", cancellationToken);
            v1Response.Headers.Contains("Sunset").Should().BeFalse();

            var v2Response = await client.GetAsync("/orders/v2/Orders", cancellationToken);
            v2Response.Headers.GetValues("Sunset").Single()
                .Should().Be("Fri, 01 Jan 2027 00:00:00 GMT");
        }

    }

}
```

> Note: a "do not overwrite already-set headers" integration test would require a custom middleware that injects a header before `RestierController` runs. That's hard to wire cleanly through the existing pipeline. For now, the contract is enforced in code by the `if (!response.Headers.ContainsKey(...))` guards in `ApplyHeaders`. If you want stronger coverage, add a helper middleware that pre-sets `api-supported-versions = "9.9"` and assert the header survives.

- [ ] **Step 2: Run the tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~VersionHeadersIntegration"
```

Expected: 3 passed.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/VersionHeadersIntegrationTests.cs
git commit -m "$(cat <<'COMMIT'
test: integration coverage for versioning response headers

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 18: TDD versioned $batch routing

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/VersionedBatchTests.cs`

- [ ] **Step 1: Write the failing tests**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/VersionedBatchTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.IntegrationTests
{

    public class VersionedBatchTests
    {

        [Fact]
        public async Task BatchToV1_RoutesV1InnerRequest()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var batch = BuildBatch("GET /api/v1/Items HTTP/1.1");
            var response = await client.SendAsync(batch, cancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            body.Should().NotContain("AuditLogs", "V1 batch must not see V2-only entity set");
        }

        [Fact]
        public async Task BatchToV2_RoutesV2InnerRequest()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var batch = BuildBatch("GET /api/v2/AuditLogs HTTP/1.1");
            var response = await client.SendAsync(batch, cancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private static HttpRequestMessage BuildBatch(string innerRequestLine)
        {
            const string boundary = "batch_test";
            var body = new StringBuilder();
            body.Append($"--{boundary}\r\n");
            body.Append("Content-Type: application/http\r\n");
            body.Append("Content-Transfer-Encoding: binary\r\n\r\n");
            body.Append($"{innerRequestLine}\r\n");
            body.Append("Host: localhost\r\n\r\n");
            body.Append($"--{boundary}--\r\n");

            // The $batch endpoint is at the per-route prefix, not at the version.
            // Decide which version to target based on the inner path; v1 → /api/v1/$batch.
            var batchUrl = innerRequestLine.Contains("/api/v1/") ? "/api/v1/$batch" : "/api/v2/$batch";

            var content = new StringContent(body.ToString(), Encoding.UTF8);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse($"multipart/mixed; boundary={boundary}");

            return new HttpRequestMessage(HttpMethod.Post, batchUrl) { Content = content };
        }

    }

}
```

- [ ] **Step 2: Run the tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~VersionedBatch"
```

Expected: 2 passed. If they fail because batching isn't enabled, verify Task 15's fixture passes `useRestierBatching: true` (which is the default in `AddVersion`).

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/VersionedBatchTests.cs
git commit -m "$(cat <<'COMMIT'
test: cover versioned \$batch routing for V1 and V2

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```


---

## Phase 9 — NSwag integration updates

### Task 19: Update `RestierOpenApiDocumentGenerator` (NSwag) to be registry-aware

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiDocumentGenerator.cs`
- Modify: `src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiMiddleware.cs`

The generator gains an optional registry parameter. When supplied AND non-empty, document-name lookup tries `registry.FindByGroupName(documentName)` first, then falls back to the existing prefix-based lookup. When the registry is null or empty, behavior is unchanged.

- [ ] **Step 1: Modify `RestierOpenApiDocumentGenerator.GenerateDocument`**

Path: `src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiDocumentGenerator.cs`

Replace the existing `GenerateDocument` body with this version. The parameter list adds a trailing optional `IRestierApiVersionRegistry` argument; existing callers that pass null get the original behavior.

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.OpenApi.OData;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using System;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.NSwag
{

    /// <summary>
    /// Generates OpenAPI documents from Restier EDM models. Shared logic used by
    /// <see cref="RestierOpenApiMiddleware"/>.
    /// </summary>
    internal static class RestierOpenApiDocumentGenerator
    {

        /// <summary>
        /// The document name used for Restier routes registered with an empty prefix.
        /// </summary>
        public const string DefaultDocumentName = "default";

        /// <summary>
        /// Generates an <see cref="OpenApiDocument"/> for the specified Restier route.
        /// </summary>
        /// <param name="documentName">The document name. May be a version group name (e.g., "v1") or a route prefix.</param>
        /// <param name="odataOptions">The OData options.</param>
        /// <param name="request">The current HTTP request, or null.</param>
        /// <param name="openApiSettings">Optional settings configurator.</param>
        /// <param name="registry">Optional version registry. If non-null and non-empty, group-name lookup is tried first.</param>
        /// <returns>The generated document, or null if the route was not found.</returns>
        public static OpenApiDocument GenerateDocument(
            string documentName,
            ODataOptions odataOptions,
            HttpRequest request,
            Action<OpenApiConvertSettings> openApiSettings,
            IRestierApiVersionRegistry registry = null)
        {
            var routePrefix = ResolveRoutePrefix(documentName, registry);

            if (!odataOptions.RouteComponents.TryGetValue(routePrefix, out var routeComponent))
            {
                return null;
            }

            var model = routeComponent.EdmModel;
            var routeServices = odataOptions.GetRouteServices(routePrefix);
            var odataValidationSettings = routeServices.GetService<ODataValidationSettings>();

            var settings = new OpenApiConvertSettings { TopExample = odataValidationSettings?.MaxTop ?? 5 };
            openApiSettings?.Invoke(settings);

            if (request is not null)
            {
                var pathParts = new[]
                {
                    $"{request.Scheme}:/",
                    request.Host.Value,
                    request.PathBase.HasValue ? request.PathBase.Value.TrimStart('/') : null,
                    routePrefix
                };
                settings.ServiceRoot = new Uri(string.Join("/", pathParts.Where(c => !string.IsNullOrWhiteSpace(c))));
            }

            return model.ConvertToOpenApi(settings);
        }

        /// <summary>
        /// Resolves a route prefix from a document name. When the registry has descriptors,
        /// the registry's group-name lookup wins for matching names; otherwise (or when no
        /// match) the existing rule applies: <c>"default"</c> → empty prefix, anything else →
        /// itself.
        /// </summary>
        private static string ResolveRoutePrefix(string documentName, IRestierApiVersionRegistry registry)
        {
            if (registry is { Descriptors.Count: > 0 })
            {
                var descriptor = registry.FindByGroupName(documentName);
                if (descriptor is not null)
                {
                    return descriptor.RoutePrefix;
                }
            }

            return string.Equals(documentName, DefaultDocumentName, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : documentName;
        }

    }

}
```

- [ ] **Step 2: Update `RestierOpenApiMiddleware` to resolve the registry from DI and pass it through**

Path: `src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiMiddleware.cs`

Add a constructor parameter (optional) for `IRestierApiVersionRegistry` and pass it into `GenerateDocument`. The middleware's caller path is unchanged for the registry-absent case.

Replace the file contents:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.OData;
using Microsoft.Restier.AspNetCore.Versioning;
using System;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.NSwag
{

    /// <summary>
    /// Middleware that serves OpenAPI documents generated from Restier EDM models at
    /// <c>/openapi/{documentName}/openapi.json</c>. NSwag UI hosts (configured via
    /// <c>UseRestierReDoc</c> / <c>UseRestierNSwagUI</c>) load these URLs.
    /// </summary>
    internal class RestierOpenApiMiddleware
    {

        private const string PathPrefix = "/openapi/";
        private const string PathSuffix = "/openapi.json";

        private readonly RequestDelegate next;
        private readonly IOptions<ODataOptions> odataOptions;
        private readonly Action<OpenApiConvertSettings> openApiSettings;
        private readonly IServiceProvider rootServices;

        public RestierOpenApiMiddleware(
            RequestDelegate next,
            IOptions<ODataOptions> odataOptions,
            IServiceProvider rootServices,
            Action<OpenApiConvertSettings> openApiSettings = null)
        {
            this.next = next;
            this.odataOptions = odataOptions;
            this.rootServices = rootServices;
            this.openApiSettings = openApiSettings;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            if (path is not null
                && path.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase)
                && path.EndsWith(PathSuffix, StringComparison.OrdinalIgnoreCase))
            {
                if (path.Length <= PathPrefix.Length + PathSuffix.Length)
                {
                    await next(context);
                    return;
                }

                var documentName = path.Substring(PathPrefix.Length, path.Length - PathPrefix.Length - PathSuffix.Length);
                if (!string.IsNullOrEmpty(documentName))
                {
                    // Touching IOptions<ODataOptions>.Value already happens inside GenerateDocument
                    // via the odataOptions.RouteComponents read; for the registry, ensure the
                    // configurator has run by reading .Value first (materialization invariant).
                    var options = odataOptions.Value;
                    var registry = rootServices.GetService<IRestierApiVersionRegistry>();

                    var document = RestierOpenApiDocumentGenerator.GenerateDocument(
                        documentName,
                        options,
                        context.Request,
                        openApiSettings,
                        registry);

                    if (document is not null)
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";
                        var json = await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0);
                        await context.Response.WriteAsync(json);
                        return;
                    }

                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
            }

            await next(context);
        }

    }

}
```

- [ ] **Step 3: Build the NSwag package**

```bash
dotnet build src/Microsoft.Restier.AspNetCore.NSwag/Microsoft.Restier.AspNetCore.NSwag.csproj
```

Expected: `Build succeeded`. The new dependency on `Microsoft.Restier.AspNetCore.Versioning` types is satisfied because `IRestierApiVersionRegistry` and `RestierApiVersionDescriptor` live in `Microsoft.Restier.AspNetCore` (Task 1).

- [ ] **Step 4: Run the existing NSwag tests to confirm no regression in registry-absent mode**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj
```

Expected: all existing tests pass. (No change in registry-absent behavior.)

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiDocumentGenerator.cs \
        src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiMiddleware.cs
git commit -m "$(cat <<'COMMIT'
feat(nswag): registry-aware OpenAPI doc resolution

GenerateDocument and the middleware now accept an optional
IRestierApiVersionRegistry. When the registry has descriptors,
group-name lookup wins. Falls back to the existing prefix-based
behavior when null/empty (per the "registry effectively absent"
rule from the spec).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 20: Update `UseRestierReDoc` to merge registry descriptors with unversioned prefixes

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensions.cs`

- [ ] **Step 1: Replace the body of `UseRestierReDoc`**

In `src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensions.cs`, replace the existing `UseRestierReDoc` method. The new version reads the registry, emits one ReDoc instance per descriptor (using `GroupName`), and ALSO emits one per `GetRestierRoutePrefixes()` entry that isn't represented by any descriptor.

```csharp
        public static IApplicationBuilder UseRestierReDoc(this IApplicationBuilder app)
        {
            // Materialization invariant: read .Value first.
            var odataOptions = app.ApplicationServices
                .GetRequiredService<IOptions<ODataOptions>>().Value;
            var registry = app.ApplicationServices
                .GetService<Microsoft.Restier.AspNetCore.Versioning.IRestierApiVersionRegistry>();

            var hasRegistryDescriptors = registry is { Descriptors.Count: > 0 };
            var registryPrefixes = hasRegistryDescriptors
                ? new System.Collections.Generic.HashSet<string>(
                    registry.Descriptors.Select(d => d.RoutePrefix), System.StringComparer.Ordinal)
                : new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);

            if (hasRegistryDescriptors)
            {
                foreach (var descriptor in registry.Descriptors)
                {
                    var documentName = descriptor.GroupName;
                    app.UseReDoc(settings =>
                    {
                        settings.Path = $"/redoc/{documentName}";
                        settings.DocumentPath = $"/openapi/{documentName}/openapi.json";
                    });
                }
            }

            foreach (var prefix in odataOptions.GetRestierRoutePrefixes())
            {
                if (registryPrefixes.Contains(prefix))
                {
                    continue;
                }

                var documentName = string.IsNullOrEmpty(prefix)
                    ? RestierOpenApiDocumentGenerator.DefaultDocumentName
                    : prefix;
                app.UseReDoc(settings =>
                {
                    settings.Path = $"/redoc/{documentName}";
                    settings.DocumentPath = $"/openapi/{documentName}/openapi.json";
                });
            }

            return app;
        }
```

You will also need to add `using System.Linq;` and `using Microsoft.Extensions.Options;` and `using Microsoft.Restier.AspNetCore.Versioning;` to the file's `using` block (if not already present). The existing `Microsoft.Restier.AspNetCore` using is also required for `GetRestierRoutePrefixes()`.

- [ ] **Step 2: Replace the body of `UseRestierNSwagUI`**

```csharp
        public static IApplicationBuilder UseRestierNSwagUI(this IApplicationBuilder app)
        {
            // Materialization invariant: read .Value first.
            var odataOptions = app.ApplicationServices
                .GetRequiredService<IOptions<ODataOptions>>().Value;
            var registry = app.ApplicationServices
                .GetService<Microsoft.Restier.AspNetCore.Versioning.IRestierApiVersionRegistry>();
            var nswagDocuments = app.ApplicationServices.GetServices<OpenApiDocumentRegistration>();

            var hasRegistryDescriptors = registry is { Descriptors.Count: > 0 };
            var registryPrefixes = hasRegistryDescriptors
                ? new System.Collections.Generic.HashSet<string>(
                    registry.Descriptors.Select(d => d.RoutePrefix), System.StringComparer.Ordinal)
                : new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);

            app.UseSwaggerUi(settings =>
            {
                settings.Path = "/swagger";

                if (hasRegistryDescriptors)
                {
                    foreach (var descriptor in registry.Descriptors)
                    {
                        var documentName = descriptor.GroupName;
                        settings.SwaggerRoutes.Add(new SwaggerUiRoute(documentName, $"/openapi/{documentName}/openapi.json"));
                    }
                }

                foreach (var prefix in odataOptions.GetRestierRoutePrefixes())
                {
                    if (registryPrefixes.Contains(prefix))
                    {
                        continue;
                    }

                    var documentName = string.IsNullOrEmpty(prefix)
                        ? RestierOpenApiDocumentGenerator.DefaultDocumentName
                        : prefix;
                    settings.SwaggerRoutes.Add(new SwaggerUiRoute(documentName, $"/openapi/{documentName}/openapi.json"));
                }

                foreach (var registration in nswagDocuments)
                {
                    settings.SwaggerRoutes.Add(new SwaggerUiRoute(registration.DocumentName, $"/swagger/{registration.DocumentName}/swagger.json"));
                }
            });
            return app;
        }
```

- [ ] **Step 3: Build the NSwag package**

```bash
dotnet build src/Microsoft.Restier.AspNetCore.NSwag/Microsoft.Restier.AspNetCore.NSwag.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 4: Run existing NSwag tests for back-compat**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj
```

Expected: all existing tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensions.cs
git commit -m "$(cat <<'COMMIT'
feat(nswag): registry-aware ReDoc and Swagger UI helpers

UseRestierReDoc and UseRestierNSwagUI now emit one entry per registry
descriptor (by GroupName) plus one per route prefix not covered by a
descriptor. Materialization invariant: IOptions<ODataOptions>.Value is
read before the registry. Existing prefix-only behavior preserved when
no registry descriptors are present.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 21: Cross-project tests for NSwag + Versioning

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/NSwagIntegrationTests.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj`

The tests live in the Versioning test project (it depends on the Versioning package by definition); we add a transitive `ProjectReference` to the NSwag package for these tests only.

- [ ] **Step 1: Add the NSwag project reference to the Versioning test csproj**

Edit `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj`. Inside the existing `<ItemGroup>` containing `<ProjectReference>`, add:

```xml
    <ProjectReference Include="..\..\src\Microsoft.Restier.AspNetCore.NSwag\Microsoft.Restier.AspNetCore.NSwag.csproj" />
```

- [ ] **Step 2: Write the failing tests**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/NSwagIntegrationTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.IntegrationTests
{

    public class NSwagIntegrationTests
    {

        [Fact]
        public async Task OpenApi_AtVersionGroupName_ReturnsCorrectVersionedDoc()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await BuildAsync(cancellationToken);
            var client = host.GetTestClient();

            var v1Json = await client.GetStringAsync("/openapi/v1/openapi.json", cancellationToken);
            var v1Root = JsonDocument.Parse(v1Json).RootElement;
            v1Root.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/Items"));
            v1Root.GetProperty("paths").EnumerateObject()
                .Should().NotContain(p => p.Name.Contains("/AuditLogs"),
                    "V1 doc must not contain V2-only entity sets");

            var v2Json = await client.GetStringAsync("/openapi/v2/openapi.json", cancellationToken);
            var v2Root = JsonDocument.Parse(v2Json).RootElement;
            v2Root.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/AuditLogs"));
        }

        [Fact]
        public async Task OpenApi_AtRoutePrefix_FallbackPath_StillWorksForBackCompat()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await BuildAsync(cancellationToken);
            var client = host.GetTestClient();

            // Legacy callers may still hit the prefix-based URL; ensure it still works.
            var response = await client.GetAsync("/openapi/api%2Fv1/openapi.json", cancellationToken);
            // The middleware path-segments split on '/', so the fallback path here is
            // "/openapi/{prefix}/openapi.json" with the prefix segment URL-encoded. If the
            // implementation does not support the encoded form, accept that the explicit
            // group-name lookup is the only supported path; this test then asserts 404.
            (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound)
                .Should().BeTrue("either the legacy fallback path works or the new path is the only supported path");
        }

        [Fact]
        public async Task RegistryEmpty_FallsBackToPrefixBasedBehavior()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await BuildHostWithEmptyRegistryAsync(cancellationToken);
            var client = host.GetTestClient();

            // No versioned routes; only an unversioned route at empty prefix.
            // The registry is registered (Versioning package referenced) but empty,
            // so NSwag must serve "/openapi/default/openapi.json" exactly as before.
            var response = await client.GetAsync("/openapi/default/openapi.json", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task OpenApi_MultiGroupDocs_AreIndependentlyReachable()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await BuildMultiGroupHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var ordersV1 = await client.GetStringAsync("/openapi/orders-v1/openapi.json", cancellationToken);
            var ordersRoot = JsonDocument.Parse(ordersV1).RootElement;
            ordersRoot.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/Orders"),
                    "orders-v1 must serve the OrdersApi schema");

            var inventoryV1 = await client.GetStringAsync("/openapi/inventory-v1/openapi.json", cancellationToken);
            var inventoryRoot = JsonDocument.Parse(inventoryV1).RootElement;
            inventoryRoot.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/Stock"),
                    "inventory-v1 must serve the InventoryApi schema");
        }

        private static async Task<IHost> BuildAsync(CancellationToken cancellationToken)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddApiVersioning().AddApiExplorer();
                        services.AddControllers().AddRestier(options =>
                        {
                            options.Select().Expand().Filter().OrderBy().Count();
                        });
                        services.AddRestierApiVersioning(b => b
                            .AddVersion<SampleApiV1>("api", svc =>
                                svc.AddSingleton<IChainedService<IModelBuilder>, SampleV1ModelBuilder>())
                            .AddVersion<SampleApiV2>("api", svc =>
                                svc.AddSingleton<IChainedService<IModelBuilder>, SampleV2ModelBuilder>()));
                        services.AddRestierNSwag();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseRestierVersionHeaders();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapRestier();
                        });
                        app.UseRestierOpenApi();
                        app.UseRestierReDoc();
                        app.UseRestierNSwagUI();
                    }));

            return await builder.StartAsync(cancellationToken);
        }

        private static async Task<IHost> BuildHostWithEmptyRegistryAsync(CancellationToken cancellationToken)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddControllers().AddRestier(options =>
                        {
                            options.Select().Expand().Filter().OrderBy().Count();
                            options.AddRestierRoute<SampleApiV1>("", svc =>
                                svc.AddSingleton<IChainedService<IModelBuilder>, SampleV1ModelBuilder>());
                        });
                        // Register Versioning services but no AddVersion calls — empty registry.
                        services.AddRestierApiVersioning(_ => { });
                        services.AddRestierNSwag();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapRestier();
                        });
                        app.UseRestierOpenApi();
                        app.UseRestierReDoc();
                        app.UseRestierNSwagUI();
                    }));

            return await builder.StartAsync(cancellationToken);
        }

        // Multi-group host: two logical APIs (Orders + Inventory) at different basePrefixes
        // with disambiguated GroupNameFormatter so OpenAPI docs don't collide on "v1".
        private static async Task<IHost> BuildMultiGroupHostAsync(CancellationToken cancellationToken)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddApiVersioning().AddApiExplorer();
                        services.AddControllers()
                            .AddRestier(options =>
                            {
                                options.Select().Expand().Filter().OrderBy().Count();
                            })
                            .AddApplicationPart(typeof(RestierController).Assembly);
                        services.AddRestierApiVersioning(b => b
                            .AddVersion<OrdersApi>("orders",
                                svc =>
                                {
                                    svc.AddSingleton<IChainedService<IModelBuilder>, OrdersModelBuilder>();
                                    svc.AddSingleton<Microsoft.Restier.Core.Submit.IChangeSetInitializer, Microsoft.Restier.Core.Submit.DefaultChangeSetInitializer>();
                                    svc.AddSingleton<Microsoft.Restier.Core.Submit.ISubmitExecutor, Microsoft.Restier.Core.Submit.DefaultSubmitExecutor>();
                                },
                                opts => opts.GroupNameFormatter = v => $"orders-v{v.MajorVersion}")
                            .AddVersion<InventoryApi>("inventory",
                                svc =>
                                {
                                    svc.AddSingleton<IChainedService<IModelBuilder>, InventoryModelBuilder>();
                                    svc.AddSingleton<Microsoft.Restier.Core.Submit.IChangeSetInitializer, Microsoft.Restier.Core.Submit.DefaultChangeSetInitializer>();
                                    svc.AddSingleton<Microsoft.Restier.Core.Submit.ISubmitExecutor, Microsoft.Restier.Core.Submit.DefaultSubmitExecutor>();
                                },
                                opts => opts.GroupNameFormatter = v => $"inventory-v{v.MajorVersion}"));
                        services.AddRestierNSwag();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseRestierVersionHeaders();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapRestier();
                        });
                        app.UseRestierOpenApi();
                        app.UseRestierReDoc();
                        app.UseRestierNSwagUI();
                    }));

            return await builder.StartAsync(cancellationToken);
        }

    }

}
```

- [ ] **Step 3: Run the tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~NSwagIntegration"
```

Expected: 4 passed.

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/NSwagIntegrationTests.cs \
        test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj
git commit -m "$(cat <<'COMMIT'
test: cross-project NSwag + Versioning integration

Group-name doc lookup, registry-empty fallback to existing
prefix-based behavior, and back-compat for prefix-based URLs.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```


---

## Phase 10 — Swagger integration updates

The Swagger package mirrors the NSwag changes: registry-aware document generator, registry-aware UI helper.

### Task 22: Update `RestierOpenApiDocumentGenerator` (Swagger) to be registry-aware

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore.Swagger/RestierOpenApiDocumentGenerator.cs`
- Modify: `src/Microsoft.Restier.AspNetCore.Swagger/RestierOpenApiMiddleware.cs`

- [ ] **Step 1: Replace `RestierOpenApiDocumentGenerator.cs`**

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.OpenApi.OData;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using System;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.Swagger
{

    /// <summary>
    /// Generates OpenAPI documents from Restier EDM models.
    /// Shared logic used by both the net8.0 middleware and the net9.0+ document transformer.
    /// </summary>
    internal static class RestierOpenApiDocumentGenerator
    {

        public const string DefaultDocumentName = "default";

        public static OpenApiDocument GenerateDocument(
            string documentName,
            ODataOptions odataOptions,
            HttpRequest request,
            Action<OpenApiConvertSettings> openApiSettings,
            IRestierApiVersionRegistry registry = null)
        {
            var routePrefix = ResolveRoutePrefix(documentName, registry);

            if (!odataOptions.RouteComponents.TryGetValue(routePrefix, out var routeComponent))
            {
                return null;
            }

            var model = routeComponent.EdmModel;
            var routeServices = odataOptions.GetRouteServices(routePrefix);
            var odataValidationSettings = routeServices.GetService<ODataValidationSettings>();

            var settings = new OpenApiConvertSettings { TopExample = odataValidationSettings?.MaxTop ?? 5 };
            openApiSettings?.Invoke(settings);

            if (request is not null)
            {
                var pathParts = new[]
                {
                    $"{request.Scheme}:/",
                    request.Host.Value,
                    request.PathBase.HasValue ? request.PathBase.Value.TrimStart('/') : null,
                    routePrefix
                };
                settings.ServiceRoot = new Uri(string.Join("/", pathParts.Where(c => !string.IsNullOrWhiteSpace(c))));
            }

            return model.ConvertToOpenApi(settings);
        }

        private static string ResolveRoutePrefix(string documentName, IRestierApiVersionRegistry registry)
        {
            if (registry is { Descriptors.Count: > 0 })
            {
                var descriptor = registry.FindByGroupName(documentName);
                if (descriptor is not null)
                {
                    return descriptor.RoutePrefix;
                }
            }

            return string.Equals(documentName, DefaultDocumentName, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : documentName;
        }

    }

}
```

- [ ] **Step 2: Replace `RestierOpenApiMiddleware.cs`**

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.OData;
using Microsoft.Restier.AspNetCore.Versioning;
using System;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.Swagger
{

    internal class RestierOpenApiMiddleware
    {

        private readonly RequestDelegate next;
        private readonly IOptions<ODataOptions> odataOptions;
        private readonly IServiceProvider rootServices;
        private readonly Action<OpenApiConvertSettings> openApiSettings;

        public RestierOpenApiMiddleware(
            RequestDelegate next,
            IOptions<ODataOptions> odataOptions,
            IServiceProvider rootServices,
            Action<OpenApiConvertSettings> openApiSettings = null)
        {
            this.next = next;
            this.odataOptions = odataOptions;
            this.rootServices = rootServices;
            this.openApiSettings = openApiSettings;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            if (path is not null
                && path.StartsWith("/swagger/", StringComparison.OrdinalIgnoreCase)
                && path.EndsWith("/swagger.json", StringComparison.OrdinalIgnoreCase))
            {
                var documentName = path.Substring("/swagger/".Length,
                    path.Length - "/swagger/".Length - "/swagger.json".Length);

                if (!string.IsNullOrEmpty(documentName))
                {
                    var options = odataOptions.Value;
                    var registry = rootServices.GetService<IRestierApiVersionRegistry>();

                    var document = RestierOpenApiDocumentGenerator.GenerateDocument(
                        documentName,
                        options,
                        context.Request,
                        openApiSettings,
                        registry);

                    if (document is not null)
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";
                        var json = await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0);
                        await context.Response.WriteAsync(json);
                        return;
                    }
                }
            }

            await next(context);
        }

    }

}
```

- [ ] **Step 3: Build the Swagger package**

```bash
dotnet build src/Microsoft.Restier.AspNetCore.Swagger/Microsoft.Restier.AspNetCore.Swagger.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 4: Run existing Swagger tests for back-compat**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Swagger/Microsoft.Restier.Tests.AspNetCore.Swagger.csproj
```

Expected: all existing tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Swagger/RestierOpenApiDocumentGenerator.cs \
        src/Microsoft.Restier.AspNetCore.Swagger/RestierOpenApiMiddleware.cs
git commit -m "$(cat <<'COMMIT'
feat(swagger): registry-aware OpenAPI doc resolution

Mirrors the NSwag change: optional IRestierApiVersionRegistry consumed
by the document generator and middleware. Group-name lookup wins when
the registry has descriptors; falls back to prefix-based behavior
when null/empty.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 23: Update `UseRestierSwaggerUI` to merge registry descriptors with unversioned prefixes

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore.Swagger/Extensions/IApplicationBuilderExtensions.cs`

- [ ] **Step 1: Replace the body of `UseRestierSwaggerUI`**

Replace the file with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Swagger;
using Microsoft.Restier.AspNetCore.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Builder
{

    public static class Restier_AspNetCore_Swagger_IApplicationBuilderExtensions
    {

        public static IApplicationBuilder UseRestierSwaggerUI(this IApplicationBuilder app)
        {
            app.UseMiddleware<RestierOpenApiMiddleware>();

            // Materialization invariant.
            var odataOptions = app.ApplicationServices
                .GetRequiredService<IOptions<ODataOptions>>().Value;
            var registry = app.ApplicationServices.GetService<IRestierApiVersionRegistry>();

            var hasRegistryDescriptors = registry is { Descriptors.Count: > 0 };
            var registryPrefixes = hasRegistryDescriptors
                ? new HashSet<string>(registry.Descriptors.Select(d => d.RoutePrefix), StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            app.UseSwaggerUI(c =>
            {
                if (hasRegistryDescriptors)
                {
                    foreach (var descriptor in registry.Descriptors)
                    {
                        var documentName = descriptor.GroupName;
                        c.SwaggerEndpoint($"swagger/{documentName}/swagger.json", documentName);
                    }
                }

                foreach (var prefix in odataOptions.GetRestierRoutePrefixes())
                {
                    if (registryPrefixes.Contains(prefix))
                    {
                        continue;
                    }

                    var documentName = string.IsNullOrEmpty(prefix)
                        ? RestierOpenApiDocumentGenerator.DefaultDocumentName
                        : prefix;

                    c.SwaggerEndpoint($"swagger/{documentName}/swagger.json", documentName);
                }
            });

            return app;
        }

    }

}
```

- [ ] **Step 2: Build the Swagger package**

```bash
dotnet build src/Microsoft.Restier.AspNetCore.Swagger/Microsoft.Restier.AspNetCore.Swagger.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 3: Run existing Swagger tests for back-compat**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Swagger/Microsoft.Restier.Tests.AspNetCore.Swagger.csproj
```

Expected: all existing tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.Swagger/Extensions/IApplicationBuilderExtensions.cs
git commit -m "$(cat <<'COMMIT'
feat(swagger): registry-aware UseRestierSwaggerUI

Emits one Swagger endpoint per registry descriptor (by GroupName) plus
one per route prefix not represented in the registry. Materialization
invariant honored.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 24: Cross-project tests for Swagger + Versioning

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/SwaggerIntegrationTests.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj`

- [ ] **Step 1: Add the Swagger project reference to the Versioning test csproj**

Edit `test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj`. Inside the `<ItemGroup>` containing `<ProjectReference>`, add:

```xml
    <ProjectReference Include="..\..\src\Microsoft.Restier.AspNetCore.Swagger\Microsoft.Restier.AspNetCore.Swagger.csproj" />
```

- [ ] **Step 2: Write the failing tests**

Path: `test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/SwaggerIntegrationTests.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.IntegrationTests
{

    public class SwaggerIntegrationTests
    {

        [Fact]
        public async Task SwaggerJson_AtVersionGroupName_ReturnsCorrectVersionedDoc()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            using var host = await BuildAsync(cancellationToken);
            var client = host.GetTestClient();

            var v1Json = await client.GetStringAsync("/swagger/v1/swagger.json", cancellationToken);
            JsonDocument.Parse(v1Json).RootElement.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/Items"));

            var v2Json = await client.GetStringAsync("/swagger/v2/swagger.json", cancellationToken);
            JsonDocument.Parse(v2Json).RootElement.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/AuditLogs"));
        }

        private static async Task<IHost> BuildAsync(CancellationToken cancellationToken)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddApiVersioning().AddApiExplorer();
                        services.AddControllers().AddRestier(options =>
                        {
                            options.Select().Expand().Filter().OrderBy().Count();
                        });
                        services.AddRestierApiVersioning(b => b
                            .AddVersion<SampleApiV1>("api", svc =>
                                svc.AddSingleton<IChainedService<IModelBuilder>, SampleV1ModelBuilder>())
                            .AddVersion<SampleApiV2>("api", svc =>
                                svc.AddSingleton<IChainedService<IModelBuilder>, SampleV2ModelBuilder>()));
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseRestierVersionHeaders();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapRestier();
                        });
                        app.UseRestierSwaggerUI();
                    }));

            return await builder.StartAsync(cancellationToken);
        }

    }

}
```

- [ ] **Step 3: Run the tests to verify they pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj --filter "FullyQualifiedName~SwaggerIntegration"
```

Expected: 1 passed.

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore.Versioning/IntegrationTests/SwaggerIntegrationTests.cs \
        test/Microsoft.Restier.Tests.AspNetCore.Versioning/Microsoft.Restier.Tests.AspNetCore.Versioning.csproj
git commit -m "$(cat <<'COMMIT'
test: cross-project Swagger + Versioning integration

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```


---

## Phase 11 — `Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore` sample

The sample uses an in-memory EF Core SQLite or an in-memory `IModelBuilder`-based provider to keep the database trivial. To match the existing samples we use EF Core (`Microsoft.EntityFrameworkCore.InMemory`) to avoid a hard SQL Server requirement and to keep the focus on versioning rather than data setup.

### Task 25: Create the sample csproj and per-version `DbContext` classes

**Files:**
- Create: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.csproj`
- Create: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Data/NorthwindModels.cs`
- Create: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Data/NorthwindContextV1.cs`
- Create: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Data/NorthwindContextV2.cs`
- Create: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/NorthwindApiV1.cs`
- Create: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/NorthwindApiV2.cs`

- [ ] **Step 1: Verify the directory does not exist**

```bash
test ! -e src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore && echo "OK"
```

Expected: `OK`

- [ ] **Step 2: Create the csproj**

```bash
mkdir -p src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore
mkdir -p src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Data
```

Path: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFrameworks>net8.0;net9.0;net10.0;</TargetFrameworks>
		<UserSecretsId>restier-northwind-versioned</UserSecretsId>
	</PropertyGroup>

	<ItemGroup>
		<ProjectReference Include="..\Microsoft.Restier.AspNetCore\Microsoft.Restier.AspNetCore.csproj" />
		<ProjectReference Include="..\Microsoft.Restier.AspNetCore.NSwag\Microsoft.Restier.AspNetCore.NSwag.csproj" />
		<ProjectReference Include="..\Microsoft.Restier.AspNetCore.Versioning\Microsoft.Restier.AspNetCore.Versioning.csproj" />
		<ProjectReference Include="..\Microsoft.Restier.EntityFrameworkCore\Microsoft.Restier.EntityFrameworkCore.csproj" />
	</ItemGroup>

	<ItemGroup Condition="'$(TargetFramework)' == 'net9.0'">
		<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="$(RestierNet9EntityFrameworkVersion)" />
	</ItemGroup>
	<ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
		<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="$(RestierNet10EntityFrameworkVersion)" />
	</ItemGroup>
	<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
		<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="[8.*, 9.0.0)" />
	</ItemGroup>

</Project>
```

- [ ] **Step 3: Write the data model**

Path: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Data/NorthwindModels.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.Data
{

    public class Customer
    {
        public string CustomerId { get; set; }
        public string CompanyName { get; set; }

        // Email exists on the entity but is hidden by V1's DbContext via Ignore().
        public string Email { get; set; }
    }

    public class Order
    {
        public int OrderId { get; set; }
        public string CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
    }

    // V2-only entity set
    public class OrderShipment
    {
        public int OrderShipmentId { get; set; }
        public int OrderId { get; set; }
        public string Carrier { get; set; }
        public string TrackingNumber { get; set; }
    }

}
```

- [ ] **Step 4: Write the V1 DbContext (hides `Email` and `OrderShipments`)**

Path: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Data/NorthwindContextV1.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.Data
{

    public class NorthwindContextV1 : DbContext
    {

        public NorthwindContextV1(DbContextOptions<NorthwindContextV1> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().HasKey(c => c.CustomerId);
            modelBuilder.Entity<Customer>().Ignore(c => c.Email);
            modelBuilder.Entity<Order>().HasKey(o => o.OrderId);
        }

    }

}
```

- [ ] **Step 5: Write the V2 DbContext (full surface)**

Path: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Data/NorthwindContextV2.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.Data
{

    public class NorthwindContextV2 : DbContext
    {

        public NorthwindContextV2(DbContextOptions<NorthwindContextV2> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderShipment> OrderShipments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().HasKey(c => c.CustomerId);
            modelBuilder.Entity<Order>().HasKey(o => o.OrderId);
            modelBuilder.Entity<OrderShipment>().HasKey(s => s.OrderShipmentId);
        }

    }

}
```

- [ ] **Step 6: Write the V1 API class**

Path: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/NorthwindApiV1.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Asp.Versioning;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.Data;

namespace Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore
{

    [ApiVersion("1.0", Deprecated = true)]
    public class NorthwindApiV1 : EntityFrameworkApi<NorthwindContextV1>
    {

        public NorthwindApiV1(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }

    }

}
```

- [ ] **Step 7: Write the V2 API class**

Path: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/NorthwindApiV2.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Asp.Versioning;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.Data;

namespace Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore
{

    [ApiVersion("2.0")]
    public class NorthwindApiV2 : EntityFrameworkApi<NorthwindContextV2>
    {

        public NorthwindApiV2(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }

    }

}
```

- [ ] **Step 8: Build the sample**

```bash
dotnet build src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.csproj
```

Expected: `Build succeeded`. Note: the sample has no `Program.cs` or `Startup.cs` yet — it builds as a library. That's OK; we add Startup next.

- [ ] **Step 9: Commit**

```bash
git add src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/
git commit -m "$(cat <<'COMMIT'
feat(samples): scaffold NorthwindVersioned sample (data + API classes)

V1 hides Customer.Email and OrderShipments via the DbContext;
V2 surfaces them. Carries [ApiVersion] attributes; V1 deprecated.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 26: Wire Startup, Program, and appsettings; add to slnx

**Files:**
- Create: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Program.cs`
- Create: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Startup.cs`
- Create: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/appsettings.json`
- Create: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/appsettings.Development.json`
- Create: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Properties/launchSettings.json`
- Modify: `RESTier.slnx`

- [ ] **Step 1: Write `Program.cs`**

Path: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Program.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore
{

    public static class Program
    {

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

    }

}
```

- [ ] **Step 2: Write `Startup.cs`**

Path: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Startup.cs`

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.Data;

namespace Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore
{

    public class Startup
    {

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApiVersioning(o =>
            {
                o.DefaultApiVersion = new ApiVersion(2, 0);
                o.ReportApiVersions = true;
                o.ApiVersionReader = new UrlSegmentApiVersionReader();
            }).AddApiExplorer();

            services.AddControllers().AddRestier(options =>
            {
                options.Select().Expand().Filter().OrderBy().SetMaxTop(100).Count();
                options.TimeZone = TimeZoneInfo.Utc;
            });

            services.AddRestierApiVersioning(b => b
                .AddVersion<NorthwindApiV1>("api", restierServices =>
                {
                    restierServices
                        .AddEFCoreProviderServices<NorthwindContextV1>((sp, dbOptions) =>
                            dbOptions.UseInMemoryDatabase("Northwind-V1"))
                        .AddSingleton(new ODataValidationSettings
                        {
                            MaxTop = 5,
                            MaxAnyAllExpressionDepth = 3,
                            MaxExpansionDepth = 3,
                        });
                },
                opts => opts.SunsetDate = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero))
                .AddVersion<NorthwindApiV2>("api", restierServices =>
                {
                    restierServices
                        .AddEFCoreProviderServices<NorthwindContextV2>((sp, dbOptions) =>
                            dbOptions.UseInMemoryDatabase("Northwind-V2"))
                        .AddSingleton(new ODataValidationSettings
                        {
                            MaxTop = 5,
                            MaxAnyAllExpressionDepth = 3,
                            MaxExpansionDepth = 3,
                        });
                }));

            services.AddRestierNSwag();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<Restier.AspNetCore.Middleware.ODataBatchHttpContextFixerMiddleware>();
            app.UseODataBatching();
            app.UseRouting();
            app.UseRestierVersionHeaders();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRestier();
            });

            app.UseRestierOpenApi();
            app.UseRestierReDoc();
            app.UseRestierNSwagUI();
        }

    }

}
```

- [ ] **Step 3: Write `appsettings.json`**

Path: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 4: Write `appsettings.Development.json`**

Path: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

- [ ] **Step 5: Write `Properties/launchSettings.json`**

```bash
mkdir -p src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Properties
```

Path: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Properties/launchSettings.json`

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "NorthwindVersioned": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5051;http://localhost:5050",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 6: Add the sample to `RESTier.slnx`**

Edit `RESTier.slnx`. Inside `<Folder Name="/src/Samples/">`, add:

```xml
    <Project Path="src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.csproj" />
```

(Insert alphabetically — between `Northwind.AspNetCore` and `Postgres.AspNetCore`.)

- [ ] **Step 7: Build the solution**

```bash
dotnet build RESTier.slnx
```

Expected: `Build succeeded`.

- [ ] **Step 8: Commit**

```bash
git add src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Program.cs \
        src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Startup.cs \
        src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/appsettings.json \
        src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/appsettings.Development.json \
        src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Properties/launchSettings.json \
        RESTier.slnx
git commit -m "$(cat <<'COMMIT'
feat(samples): wire NorthwindVersioned Startup + add to RESTier.slnx

Two versions registered via AddRestierApiVersioning, V1 carries a
sunset date for 2027-01-01, NSwag UI hosted at /swagger with v1/v2
in the dropdown. EF Core in-memory provider for both versions.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 27: Manual browser verification of the sample

This task is manual — there's no automation here. The goal is to confirm the end-to-end UX matches the spec.

- [ ] **Step 1: Run the sample**

```bash
dotnet run --project src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.csproj
```

Wait for the listening URL to appear in the console.

- [ ] **Step 2: Browse `https://localhost:5051/swagger`**

Verify the dropdown lists exactly two entries: `v1` and `v2` (no `api/v1` / `api/v2`).

- [ ] **Step 3: Browse `https://localhost:5051/redoc/v1` and `/redoc/v2`**

Verify each ReDoc page renders the corresponding versioned doc, and that V1 does NOT show `OrderShipments`.

- [ ] **Step 4: Browse `https://localhost:5051/api/v1/$metadata`**

Verify the EDM contains `Customers`, `Orders` but NOT `OrderShipments`. Verify no `Email` property on `Customer`.

- [ ] **Step 5: Browse `https://localhost:5051/api/v2/$metadata`**

Verify the EDM contains `Customers`, `Orders`, `OrderShipments`, and that `Customer` has an `Email` property.

- [ ] **Step 6: With `curl`, verify the response headers**

```bash
curl -i https://localhost:5051/api/v1/Customers --insecure | head -20
```

Expected headers in the response:
- `api-supported-versions: 1.0, 2.0`
- `api-deprecated-versions: 1.0`
- `Sunset: Fri, 01 Jan 2027 00:00:00 GMT`

- [ ] **Step 7: Stop the sample**

`Ctrl+C` in the terminal where `dotnet run` is running.

- [ ] **Step 8: Commit (no code changes — just a note)**

If you wrote a verification log (`docs/superpowers/verification/2026-05-03-northwind-versioned-manual.md` or similar), commit it. Otherwise no commit is required.


---

## Phase 12 — Documentation

### Task 28: Wire the Versioning project into the docs project

**Files:**
- Modify: `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`

- [ ] **Step 1: Add `<ProjectReference>` to the docsproj**

In `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`, find the `<ItemGroup>` containing the existing `<ProjectReference>` entries (around line 92). Add:

```xml
    <ProjectReference Include="..\Microsoft.Restier.AspNetCore.Versioning\Microsoft.Restier.AspNetCore.Versioning.csproj" />
```

(Insert alphabetically between `Microsoft.Restier.AspNetCore.Swagger` and `Microsoft.Restier.Breakdance`.)

- [ ] **Step 2: Add `<_DocsSourceProject>` to the docsproj**

In the same file, find the `<ItemGroup>` containing `<_DocsSourceProject>` entries (around line 102). Add:

```xml
    <_DocsSourceProject Include="..\Microsoft.Restier.AspNetCore.Versioning\Microsoft.Restier.AspNetCore.Versioning.csproj" />
```

- [ ] **Step 3: Build the docs project**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: `Build succeeded`. The DotNetDocs SDK should generate API reference for the new package's public types under `src/Microsoft.Restier.Docs/api-reference/`.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
git commit -m "$(cat <<'COMMIT'
docs: include Microsoft.Restier.AspNetCore.Versioning in docsproj sources

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 29: Write `guides/server/api-versioning.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/api-versioning.mdx`

- [ ] **Step 1: Write the page**

Path: `src/Microsoft.Restier.Docs/guides/server/api-versioning.mdx`

```mdx
---
title: API Versioning
description: Expose multiple URL-segment versions of a Restier API with versioned $metadata, NSwag/Swagger documents per version, and standard version-discovery response headers.
---

import { Steps, Tabs, Tab, CodeGroup, Note, Tip, Warning } from "/snippets/mintlify-components.mdx";

Restier integrates with [`Asp.Versioning`](https://github.com/dotnet/aspnet-api-versioning) for **URL-segment** API versioning via the optional `Microsoft.Restier.AspNetCore.Versioning` package. Each version is a separate `ApiBase` subclass; routes are exposed at distinct prefixes (e.g., `/api/v1`, `/api/v2`) with their own EDMs, `$metadata`, and OpenAPI documents.

<Note>
**Scope:** URL-segment versioning only. Header / query-string / media-type versioning is not yet supported.
</Note>

## Setup

<Steps>
<Step title="Install the package">

```bash
dotnet add package Microsoft.Restier.AspNetCore.Versioning
```

</Step>
<Step title="Define one ApiBase subclass per version">

Each version gets its own `ApiBase` subclass and is decorated with `[ApiVersion]`:

```csharp
[ApiVersion("1.0", Deprecated = true)]
public class NorthwindApiV1 : EntityFrameworkApi<NorthwindContextV1> { /* ... */ }

[ApiVersion("2.0")]
public class NorthwindApiV2 : EntityFrameworkApi<NorthwindContextV2> { /* ... */ }
```

</Step>
<Step title="Register versions in ConfigureServices">

```csharp
services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(2, 0);
    o.ReportApiVersions = true;
    o.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer();

services.AddControllers().AddRestier(options =>
{
    options.Select().Expand().Filter().OrderBy().Count();
});

services.AddRestierApiVersioning(b => b
    .AddVersion<NorthwindApiV1>("api", svc =>
        svc.AddEFCoreProviderServices<NorthwindContextV1>(/* ... */))
    .AddVersion<NorthwindApiV2>("api", svc =>
        svc.AddEFCoreProviderServices<NorthwindContextV2>(/* ... */)));
```

The base prefix `"api"` is combined with the version segment (default `v1`, `v2`) to produce the route prefix.

</Step>
<Step title="Add the headers middleware">

```csharp
app.UseRouting();
app.UseRestierVersionHeaders();   // before MapRestier
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapRestier();
});
```

</Step>
</Steps>

## What you get

- `GET /api/v1/$metadata` returns the V1 EDM; `GET /api/v2/$metadata` returns V2's.
- `GET /openapi/v1/openapi.json` serves the V1 OpenAPI document; `GET /openapi/v2/openapi.json` serves V2's.
- The NSwag UI dropdown at `/swagger` shows `v1` and `v2`.
- Every response on a versioned route carries:
  - `api-supported-versions: 1.0, 2.0`
  - `api-deprecated-versions: 1.0` (only versions marked `Deprecated = true`)
  - `Sunset: <RFC 1123 date>` (only when `RestierVersioningOptions.SunsetDate` is set)

## Configuration reference

### `RestierVersioningOptions`

| Property | Default | Purpose |
|----------|---------|---------|
| `SegmentFormatter` | `ApiVersionSegmentFormatters.Major` (`v1`, `v2`) | How `ApiVersion` is rendered as a URL segment. Use `ApiVersionSegmentFormatters.MajorMinor` for `v1.0`/`v2.1`, or supply a custom `Func<ApiVersion, string>`. |
| `ExplicitRoutePrefix` | null | Override the composed route prefix entirely. When set, `SegmentFormatter` and the base prefix are ignored. |
| `SunsetDate` | null | Optional date emitted via the `Sunset` response header. |

### Imperative overload (no `[ApiVersion]` attribute)

```csharp
services.AddRestierApiVersioning(b => b
    .AddVersion<MyApi>(
        new ApiVersion(1, 0),
        deprecated: false,
        basePrefix: "api",
        configureRouteServices: svc => /* ... */));
```

## Multiple logical APIs

Two unrelated APIs at different base prefixes don't leak versions into each other's headers:

```csharp
services.AddRestierApiVersioning(b => b
    .AddVersion<OrdersApiV1>("orders", /* ... */)
    .AddVersion<OrdersApiV2>("orders", /* ... */)
    .AddVersion<InventoryApi>(new ApiVersion(1, 0), false, "inventory", /* ... */));
```

A `GET /orders/v1` response has `api-supported-versions: 1.0, 2.0` (Orders only). A `GET /inventory/v1` response has `api-supported-versions: 1.0` (Inventory only).

## Mixing versioned and unversioned routes

You can mix `AddRestierRoute` (unversioned) and `AddRestierApiVersioning` in the same app. The NSwag UI dropdown will show one entry per registered version (by group name) plus one per unversioned prefix.

## Limitations

- Header / query-string / media-type version readers are not supported. RESTier's dynamic route transformer keys off URL prefix only.
- `OData-Deprecation` annotations on entity sets/properties in the EDM are not emitted automatically. (Tracked separately; overlaps with the OpenAPI annotation work.)
- A request to `/api` without a version segment returns 404. Register a non-versioned `AddRestierRoute<TApi>("api", ...)` if you want a default.
- A sunset date in the past is reported via the `Sunset` header but does not cause RESTier to return 410 Gone.

## Migrating from unversioned

If you currently call `AddRestierRoute<TApi>("api", ...)` and want to introduce versions:

1. Rename `TApi` to `TApiV1`. Add `[ApiVersion("1.0")]`.
2. Replace the `AddRestierRoute` call with `services.AddRestierApiVersioning(b => b.AddVersion<TApiV1>("api", /* ... */))`.
3. Old client URLs change from `/api/Customers` to `/api/v1/Customers`. If you want to keep the legacy URL, register the same API class twice — once via `AddRestierRoute<TApiV1>("api", ...)` (unversioned) and once via `AddRestierApiVersioning`.

## See also

- [NSwag](nswag) — the recommended OpenAPI integration; understands the version registry automatically.
- [Swagger](swagger) — works with versioning the same way; alternative to NSwag.
- [Asp.Versioning documentation](https://github.com/dotnet/aspnet-api-versioning/wiki) — upstream reference.
```

- [ ] **Step 2: Build the docs**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/api-versioning.mdx
git commit -m "$(cat <<'COMMIT'
docs: write guides/server/api-versioning.mdx

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```

### Task 30: Update docs nav and cross-link from related pages

**Files:**
- Modify: `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` (nav template)
- Modify: `src/Microsoft.Restier.Docs/guides/server/nswag.mdx` (cross-link)
- Modify: `src/Microsoft.Restier.Docs/guides/server/swagger.mdx` (cross-link)
- Create: `src/Microsoft.Restier.Docs/release-notes/api-versioning.mdx`

- [ ] **Step 1: Add the page to the nav template**

In `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`, find the `<Group Name="Server" Icon="server">` block. Add `guides/server/api-versioning;` to the `<Pages>` block, alphabetically between `naming-conventions` and `concurrency`. The block should look like:

```xml
                            <Group Name="Server" Icon="server">
                                <Pages>
                                    guides/server/model-building;
                                    guides/server/method-authorization;
                                    guides/server/filters;
                                    guides/server/interceptors;
                                    guides/server/operations;
                                    guides/server/nswag;
                                    guides/server/swagger;
                                    guides/server/openapi-annotations;
                                    guides/server/api-versioning;
                                    guides/server/testing;
                                    guides/server/naming-conventions;
                                    guides/server/concurrency;
                                    guides/server/performance;
                                </Pages>
                            </Group>
```

- [ ] **Step 2: Cross-link from `nswag.mdx`**

In `src/Microsoft.Restier.Docs/guides/server/nswag.mdx`, find the "See also" or final paragraph (whichever is the last user-facing section). Add a one-line reference:

> "If you need to expose multiple versions of your API at different URL segments, see [API Versioning](api-versioning) — the NSwag integration is registry-aware and shows per-version dropdown entries automatically."

- [ ] **Step 3: Cross-link from `swagger.mdx`**

Same change in `src/Microsoft.Restier.Docs/guides/server/swagger.mdx`:

> "For multi-version API support, see [API Versioning](api-versioning) — the Swagger integration mirrors the NSwag behavior."

- [ ] **Step 4: Add a release-note entry**

Path: `src/Microsoft.Restier.Docs/release-notes/api-versioning.mdx`

```mdx
---
title: API Versioning
description: Restier 2.0 adds optional URL-segment API versioning via Asp.Versioning.
---

The new opt-in package **`Microsoft.Restier.AspNetCore.Versioning`** integrates Restier with `Asp.Versioning` for URL-segment versioning. Register multiple `ApiBase` subclasses, each with its own `[ApiVersion]`, via `services.AddRestierApiVersioning(builder => builder.AddVersion<TApi>(...))`. Versioned `$metadata`, per-version OpenAPI documents (NSwag and Swagger), and standard version-discovery response headers (`api-supported-versions`, `api-deprecated-versions`, `Sunset`) are wired up automatically. See the [API Versioning guide](../guides/server/api-versioning) for full setup.
```

(Optional but recommended: add this entry to the Release Notes group in the nav template if you want it discoverable. Otherwise it lives as an unlinked page that the API Versioning guide references.)

- [ ] **Step 5: Build the docs**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: `Build succeeded` and `docs.json` regenerated with the new nav entry.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj \
        src/Microsoft.Restier.Docs/guides/server/nswag.mdx \
        src/Microsoft.Restier.Docs/guides/server/swagger.mdx \
        src/Microsoft.Restier.Docs/release-notes/api-versioning.mdx \
        src/Microsoft.Restier.Docs/docs.json
git commit -m "$(cat <<'COMMIT'
docs: add api-versioning to nav, cross-link from nswag/swagger,
release note for the Versioning package

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
COMMIT
)"
```


---

## Phase 13 — Final verification

### Task 31: Full solution build and full test pass

- [ ] **Step 1: Full build of the solution**

```bash
dotnet build RESTier.slnx
```

Expected: `Build succeeded` for every project. Zero warnings, zero errors.

- [ ] **Step 2: Run every test project**

```bash
dotnet test RESTier.slnx
```

Expected: every test passes across `net8.0`, `net9.0`, and `net10.0`. Pay particular attention to:
- `Microsoft.Restier.Tests.AspNetCore.Versioning` — all new tests
- `Microsoft.Restier.Tests.AspNetCore.NSwag` — back-compat
- `Microsoft.Restier.Tests.AspNetCore.Swagger` — back-compat
- `Microsoft.Restier.Tests.AspNetCore` — confirm `MapRestier` still works without versioning

- [ ] **Step 3: If any test fails, do not proceed**

Diagnose the failure, fix the underlying issue (do NOT skip the test), and re-run from Step 2. Common failure sources:
- `Asp.Versioning.Mvc` package version mismatch — adjust the version range in the Versioning csproj.
- `Microsoft.AspNetCore.OData` interface change between versions — confirm `ODataOptions.RouteComponents` and `GetRouteServices(prefix)` remain available.
- `Asp.Versioning.ApiExplorer.ApiVersionDescription` constructor signature change — check the package and adjust `RestierApiVersionDescriptionProvider`.

- [ ] **Step 4: Confirm no leftover artifacts**

```bash
git status
```

Expected: clean working tree (all changes committed across the prior tasks).

- [ ] **Step 5: Squash review (optional)**

If the implementing engineer prefers a tighter history, squash any "fix typo" / "fix test" commits into the original feature commits. Do NOT amend or rewrite commits that have been pushed.

---

## Out of scope (do not do these unless asked)

- Header / query-string / media-type version readers
- `OData-Deprecation` annotations in the EDM
- Asp.Versioning `IPolicyManager` integration (sunset policy-driven instead of per-call)
- Automatic `410 Gone` enforcement after a sunset date
- Versioning support for `Microsoft.Restier.EntityFramework` (EF6) — the same patterns work, but no special glue is needed; tracked separately if requested

