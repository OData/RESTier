# EF & EFCore Test Project Migration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `Microsoft.Restier.Tests.EntityFramework` and `Microsoft.Restier.Tests.EntityFrameworkCore` compile and pass tests against the vnext codebase.

**Architecture:** Both test projects were excluded during the main→vnext migration and still reference old packages (OData 7.x), old frameworks (net48, MSTest), removed APIs (`ModelContext`, non-generic `EFModelBuilder`), and broken project references. We fix the .csproj files first, then convert test code from MSTest to xUnit v3, then fix API-level compilation errors, then verify.

**Tech Stack:** .NET 8/9, xUnit v3, FluentAssertions (via AwesomeAssertions), Entity Framework 6.5, EF Core 8/9, OData 8.x

---

### Task 1: Fix EntityFramework test project file

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj`
- Delete: `test/Microsoft.Restier.Tests.EntityFramework/App.config`

The current .csproj has these problems:
1. Targets `net48;net8.0;net9.0` — net48 is incompatible with all dependencies
2. References `Microsoft.OData.Core 7.*` and `Microsoft.OData.Edm 7.*` — conflicts with OData 8.x used everywhere else
3. References `Microsoft.AspNet.OData 7.*` / `Microsoft.AspNetCore.OData 7.*` — both wrong version
4. References `Breakdance.Assemblies` — no longer used (Breakdance is a project reference)
5. References `System.Text.RegularExpressions 4.*` — not needed
6. Project references use `test\` relative paths to projects that don't exist (`Microsoft.Restier.Breakdance`, `Microsoft.Restier.AspNet`, `Microsoft.Restier.AspNetCore`, `Microsoft.Restier.EntityFramework`) — these should point to `src\` like the working AspNetCore test project
7. `App.config` is an EF6/net48 artifact with SQL Server connection strings — not needed for net8.0+

The working `Microsoft.Restier.Tests.AspNetCore.csproj` is a good reference — it has minimal package references (test packages come from Directory.Build.props) and uses `..\..\src\` project reference paths.

- [ ] **Step 1: Rewrite the .csproj**

Replace the entire content of `test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFrameworks>net8.0;net9.0</TargetFrameworks>
		<IsPackable>false</IsPackable>
	</PropertyGroup>

	<ItemGroup>
		<ProjectReference Include="..\..\src\Microsoft.Restier.AspNetCore\Microsoft.Restier.AspNetCore.csproj" />
		<ProjectReference Include="..\..\src\Microsoft.Restier.EntityFramework\Microsoft.Restier.EntityFramework.csproj" />
		<ProjectReference Include="..\Microsoft.Restier.Tests.Shared.EntityFramework\Microsoft.Restier.Tests.Shared.EntityFramework.csproj" />
		<ProjectReference Include="..\Microsoft.Restier.Tests.Shared\Microsoft.Restier.Tests.Shared.csproj" />
	</ItemGroup>

</Project>
```

Key changes:
- Removed net48 target
- Removed all explicit package references (xUnit, FluentAssertions, coverlet, Test.Sdk all come from Directory.Build.props)
- Fixed project reference paths to use `..\..\src\` for source projects
- Removed Breakdance project reference (it's not directly needed — Tests.Shared brings it transitively)
- Removed `Microsoft.Restier.AspNet` reference (net48-only)

- [ ] **Step 2: Delete App.config**

Delete `test/Microsoft.Restier.Tests.EntityFramework/App.config` — it's an EF6/net48 artifact with SQL Server LocalDB connection strings. The EF6 tests on net8.0+ use the connection string from the shared test project's `AddEntityFrameworkServices` extension.

- [ ] **Step 3: Build to verify restore succeeds**

Run: `dotnet build test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj`

Expected: NuGet restore succeeds. There will likely be compilation errors in the test .cs file — that's fixed in Task 3.

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj
git rm test/Microsoft.Restier.Tests.EntityFramework/App.config
git commit -m "fix: update EntityFramework test project for vnext (net8/9, OData 8.x)"
```

---

### Task 2: Fix EntityFrameworkCore test project file

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj`

The current .csproj has:
1. References `Microsoft.AspNetCore.OData 7.*` — conflicts with OData 8.x
2. References `Breakdance.Assemblies` — not needed
3. Explicit `Microsoft.Extensions.DependencyInjection` and `Microsoft.EntityFrameworkCore.Relational` — should come transitively
4. Project references use `test\` relative paths to non-existent projects

- [ ] **Step 1: Rewrite the .csproj**

Replace the entire content of `test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFrameworks>net8.0;net9.0</TargetFrameworks>
		<IsPackable>false</IsPackable>
	</PropertyGroup>

	<ItemGroup>
		<Compile Include="..\Microsoft.Restier.Tests.EntityFramework\ChangeSetPreparerTests.cs" Link="ChangeSetPreparerTests.cs" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\..\src\Microsoft.Restier.AspNetCore\Microsoft.Restier.AspNetCore.csproj" />
		<ProjectReference Include="..\..\src\Microsoft.Restier.EntityFrameworkCore\Microsoft.Restier.EntityFrameworkCore.csproj" />
		<ProjectReference Include="..\Microsoft.Restier.Tests.Shared.EntityFrameworkCore\Microsoft.Restier.Tests.Shared.EntityFrameworkCore.csproj" />
		<ProjectReference Include="..\Microsoft.Restier.Tests.Shared\Microsoft.Restier.Tests.Shared.csproj" />
	</ItemGroup>

</Project>
```

Key changes:
- Preserved the linked `ChangeSetPreparerTests.cs` compile item (shared between EF6 and EFCore)
- Removed all explicit package references
- Fixed project reference paths

- [ ] **Step 2: Build to verify restore succeeds**

Run: `dotnet build test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj`

Expected: NuGet restore succeeds. Compilation errors expected in test .cs files — fixed in Tasks 3-4.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj
git commit -m "fix: update EntityFrameworkCore test project for vnext (OData 8.x, fix refs)"
```

---

### Task 3: Convert ChangeSetPreparerTests.cs from MSTest to xUnit v3

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFramework/ChangeSetPreparerTests.cs`

This file is linked into both the EF6 and EFCore test projects. It currently uses MSTest and has obsolete `#if NET6_0_OR_GREATER` conditional compilation.

Current state:
```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
// ...
namespace Microsoft.Restier.EntityFramework.Tests
{
    [TestClass]
    public class ChangeSetPreparerTests : RestierTestBase
#if NET6_0_OR_GREATER
        <LibraryApi>
#endif
    {
        [TestMethod]
        public async Task ComplexTypeUpdate()
```

- [ ] **Step 1: Convert to xUnit v3**

Replace the full contents of `test/Microsoft.Restier.Tests.EntityFramework/ChangeSetPreparerTests.cs` with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Xunit;

#if EFCore
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;

namespace Microsoft.Restier.Tests.EntityFrameworkCore;
#else
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;

namespace Microsoft.Restier.Tests.EntityFramework;
#endif

public class ChangeSetPreparerTests : RestierTestBase<LibraryApi>
{
    [Fact]
    public async Task ComplexTypeUpdate()
    {
        var provider = await RestierTestHelpers.GetTestableInjectionContainer<LibraryApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        provider.Should().NotBeNull();

        var api = provider.GetTestableApiInstance<LibraryApi>();
        api.Should().NotBeNull();

        var item = new DataModificationItem(
            "Readers",
            typeof(Employee),
            null,
            RestierEntitySetOperation.Update,
            new Dictionary<string, object> { { "Id", new Guid("53162782-EA1B-4712-AF26-8AA1D2AC0461") } },
            new Dictionary<string, object>(),
            new Dictionary<string, object> { { "Addr", new Dictionary<string, object> { { "Zip", "332" } } } });
        var changeSet = new ChangeSet(new[] { item });
        var sc = new SubmitContext(api, changeSet);

        var changeSetPreparer = api.GetApiService<IChangeSetInitializer>();
        changeSetPreparer.Should().NotBeNull();

        await changeSetPreparer.InitializeAsync(sc, CancellationToken.None).ConfigureAwait(false);
        var person = item.Resource as Employee;

        person.Should().NotBeNull();
        person.Addr.Zip.Should().Be("332");
    }
}
```

Key changes:
- Replaced `using Microsoft.VisualStudio.TestTools.UnitTesting` with `using Xunit`
- Removed `[TestClass]` (not needed in xUnit)
- Replaced `[TestMethod]` with `[Fact]`
- Removed `#if NET6_0_OR_GREATER` — always use generic `RestierTestBase<LibraryApi>`
- Used `#if EFCore` / `#else` for namespace and using directives (matching the shared project's conditional compilation pattern)
- Changed namespace from `Microsoft.Restier.EntityFramework.Tests` to `Microsoft.Restier.Tests.EntityFramework` (follows project naming convention)
- Added `using Microsoft.Restier.EntityFrameworkCore` in the EFCore block for `AddEntityFrameworkServices` extension (the EFCore shared test project defines this as `AddEntityFrameworkServices`, same name as EF6)

- [ ] **Step 2: Build both projects to verify compilation**

Run: `dotnet build test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj && dotnet build test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj`

Expected: ChangeSetPreparerTests compiles in both projects. EFCore project may still have errors in EFModelBuilderTests/EFCoreDbContextExtensionsTests — that's Task 4.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFramework/ChangeSetPreparerTests.cs
git commit -m "refactor: convert ChangeSetPreparerTests from MSTest to xUnit v3"
```

---

### Task 4: Convert EFCore-only test files from MSTest to xUnit v3

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFrameworkCore/EFCoreDbContextExtensionsTests.cs`
- Modify: `test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelBuilderTests.cs`

#### EFCoreDbContextExtensionsTests.cs

Current state uses MSTest and directly instantiates DbContexts. The test itself is straightforward — just needs MSTest→xUnit conversion.

- [ ] **Step 1: Convert EFCoreDbContextExtensionsTests.cs**

Replace the full contents of `test/Microsoft.Restier.Tests.EntityFrameworkCore/EFCoreDbContextExtensionsTests.cs` with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.IncorrectLibrary;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.EntityFrameworkCore;

public class EFCoreDbContextExtensionsTests
{
    [Fact]
    public void IsDbSetMapped_CanFind_MappedDbSets()
    {
        using var context = new LibraryContext(new DbContextOptions<LibraryContext> { });
        context.Should().NotBeNull();

        context.IsDbSetMapped(typeof(Address)).Should().BeFalse();

        using var incorrectContext = new IncorrectLibraryContext(new DbContextOptions<IncorrectLibraryContext>());
        incorrectContext.Should().NotBeNull();

        incorrectContext.IsDbSetMapped(typeof(Address)).Should().BeTrue();
    }
}
```

Key changes:
- Replaced MSTest usings/attributes with xUnit
- Used file-scoped namespace

#### EFModelBuilderTests.cs

This file has a more complex problem: it references `new EFModelBuilder()` (non-generic) and `new ModelContext(api)` — both of which no longer exist in vnext. The `EFModelBuilder` is now `EFModelBuilder<TDbContext>` and takes `(TDbContext dbContext, ModelMerger modelMerger)` in its constructor. `ModelContext` has been removed entirely.

However, looking at what the test actually tests:
1. `DbSetOnComplexType_Should_ThrowException()` — tests that mapping an owned type as a DbSet causes `EdmModelValidationException`. The validation is done inside `EFModelBuilder.EntityFrameworkCoreGetEntities()` which is called by `GetEdmModel()`.
2. `EFModelBuilder_Should_HandleViews()` — tests that [Keyless] entities cause `InvalidOperationException`.

Both tests can be rewritten to use `RestierTestHelpers.GetApiMetadataAsync` which triggers model building through the full pipeline, or we can directly instantiate `EFModelBuilder<TDbContext>` with the right constructor args.

The simpler approach: use `GetApiMetadataAsync` for both (it invokes the model builder internally). Test 2 already does this. Test 1 should be adapted to match.

- [ ] **Step 2: Convert EFModelBuilderTests.cs**

Replace the full contents of `test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelBuilderTests.cs` with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.IncorrectLibrary;
using Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.Views;
using Xunit;

namespace Microsoft.Restier.Tests.EntityFrameworkCore;

public class EFModelBuilderTests
{
    [Fact]
    public async Task DbSetOnComplexType_Should_ThrowException()
    {
        var getModelAction = async () =>
        {
            _ = await RestierTestHelpers.GetApiMetadataAsync<IncorrectLibraryApi>(
                serviceCollection: services => services.AddEFCoreProviderServices<IncorrectLibraryContext>());
        };
        await getModelAction.Should().ThrowAsync<EdmModelValidationException>()
            .Where(c => c.Message.Contains("Address") && c.Message.Contains("Universe"));
    }

    [Fact]
    public async Task EFModelBuilder_Should_HandleViews()
    {
        var getModelAction = async () =>
        {
            _ = await RestierTestHelpers.GetApiMetadataAsync<LibraryWithViewsApi>(
                serviceCollection: services => services.AddEFCoreProviderServices<LibraryWithViewsContext>());
        };
        await getModelAction.Should().ThrowAsync<InvalidOperationException>()
            .Where(c => c.Message.Contains("[Keyless]"));
    }
}
```

Key changes:
- Replaced MSTest usings/attributes with xUnit
- Replaced `new EFModelBuilder().GetModel(new ModelContext(api))` with `RestierTestHelpers.GetApiMetadataAsync` — both removed APIs, and the metadata path exercises the same model builder
- First test previously used `GetTestableInjectionContainer` + manual `EFModelBuilder` invocation; now uses the same pattern as the second test
- Both tests are now `async Task` and use `ThrowAsync` consistently
- Used file-scoped namespaces

- [ ] **Step 3: Build the EFCore test project**

Run: `dotnet build test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj`

Expected: Clean compilation with no errors.

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFrameworkCore/EFCoreDbContextExtensionsTests.cs
git add test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelBuilderTests.cs
git commit -m "refactor: convert EFCore test files from MSTest to xUnit v3, fix removed APIs"
```

---

### Task 5: Build and run all tests

**Files:** None (verification only)

- [ ] **Step 1: Build the full solution**

Run: `dotnet build RESTier.slnx`

Expected: Clean build with zero errors.

- [ ] **Step 2: Run EntityFramework tests**

Run: `dotnet test test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj -v normal`

Expected: 1 test passes (ComplexTypeUpdate).

- [ ] **Step 3: Run EntityFrameworkCore tests**

Run: `dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj -v normal`

Expected: 4 tests pass (ChangeSetPreparerTests.ComplexTypeUpdate, EFCoreDbContextExtensionsTests.IsDbSetMapped_CanFind_MappedDbSets, EFModelBuilderTests.DbSetOnComplexType_Should_ThrowException, EFModelBuilderTests.EFModelBuilder_Should_HandleViews).

- [ ] **Step 4: Run full solution tests to verify no regressions**

Run: `dotnet test RESTier.slnx`

Expected: All existing tests continue to pass, plus the new ones.

- [ ] **Step 5: Commit any fixups needed**

If any test failures required code adjustments, commit those fixes.

---

### Task 6: Fix compilation issues (contingency)

This task exists as a catch-all for compilation or runtime errors discovered during Tasks 3-5. Common issues that may surface:

1. **`AddEntityFrameworkServices` not found in EF6 project** — The extension is defined in `Microsoft.Restier.Tests.Shared.EntityFramework/Extensions/EntityFrameworkServiceCollectionExtensions.cs` with `#if EF6` / `#if EFCore` conditional compilation. Both the EF6 and EFCore shared test projects define this same extension name. If the EF6 test project can't resolve it, check that the `Microsoft.Restier.Tests.Shared.EntityFramework` project reference is correct and that `EF6` is defined as a constant.

2. **`RestierTestHelpers` methods not found** — These are in `src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs`. The project reference chain should be: Test project → Tests.Shared → Breakdance. If not, add a direct project reference to `..\..\src\Microsoft.Restier.Breakdance\Microsoft.Restier.Breakdance.csproj`.

3. **`GetTestableApiInstance` extension method not found** — This is also in Breakdance. Same fix as above.

4. **`LibraryWithViewsContext` constructor mismatch** — The constructor takes `DbContextOptions<LibraryWithViewsContext>` but inherits from `LibraryContext` which takes `DbContextOptions`. This should work via covariance, but if it doesn't, change the constructor parameter to `DbContextOptions options`.

5. **`IsDbSetMapped` extension not found** — Defined in `src/Microsoft.Restier.EntityFrameworkCore/`. Make sure the EFCore test project references `Microsoft.Restier.EntityFrameworkCore.csproj`.

- [ ] **Steps: diagnose and fix as needed based on actual errors**

This task has no predefined steps — it's completed when Tasks 1-5 all pass.

---

### Task 7: Add tests for untested public classes

**Coverage analysis** found these public classes in the EF/EFCore source have no or insufficient direct test coverage:

| Class | Location | Status |
|---|---|---|
| `EFModelMapper` | Shared (EF6 & EFCore) | 0% — 2 public methods untested |
| `EFChangeSetInitializer.ConvertToEfValue` | EF6 | 0% — EFCore version has 4 tests in AspNetCore, EF6 has none |
| `EFModelBuilder<T>.GetEdmModel` | Shared | Partial — only error cases tested, no happy-path |
| `GeographyConverter` | EF6-only | 0% — 4 public static methods untested |

Internal pipeline classes (`EFQueryExecutor`, `EFQueryExpressionSourcer`, `EFQueryExpressionProcessor`, `EFSubmitExecutor`) are excluded — they require heavy mocking of EF internals and are already exercised through integration tests in `Microsoft.Restier.Tests.AspNetCore`.

#### 7a: Add EF6 ConvertToEfValue tests

**Files:**
- Create: `test/Microsoft.Restier.Tests.EntityFramework/EFChangeSetInitializerTests.cs`

Mirrors the existing `test/Microsoft.Restier.Tests.AspNetCore/EFChangeSetInitializerTests.cs` pattern but tests the EF6-specific conversions (Date→DateTime, DateTimeOffset→DateTime, TimeOfDay→TimeSpan, Enum, int→long).

- [ ] **Step 1: Write the test file**

Create `test/Microsoft.Restier.Tests.EntityFramework/EFChangeSetInitializerTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.EntityFramework;
using Xunit;

#pragma warning disable CS0618 // Date and TimeOfDay are obsolete but still used by OData
namespace Microsoft.Restier.Tests.EntityFramework;

public class EFChangeSetInitializerTests
{
    private readonly EFChangeSetInitializer _initializer = new();

    public enum SampleEnum
    {
        Value1,
        Value2,
    }

    [Fact]
    public void ConvertToEfValue_ShouldReturnDateTime_ForEdmDate()
    {
        var edmDate = new Date(2025, 4, 21);

        var result = _initializer.ConvertToEfValue(typeof(DateTime), edmDate);

        result.Should().BeOfType<DateTime>().Which.Should().Be(new DateTime(2025, 4, 21));
    }

    [Fact]
    public void ConvertToEfValue_ShouldReturnDateTime_ForDateTimeOffset()
    {
        var dateTimeOffset = new DateTimeOffset(2025, 4, 21, 10, 30, 0, TimeSpan.FromHours(2));

        var result = _initializer.ConvertToEfValue(typeof(DateTime), dateTimeOffset);

        result.Should().BeOfType<DateTime>().Which.Should().Be(new DateTime(2025, 4, 21, 10, 30, 0));
    }

    [Fact]
    public void ConvertToEfValue_ShouldReturnTimeSpan_ForEdmTimeOfDay()
    {
        var edmTimeOfDay = new TimeOfDay(10, 30, 45, 0);

        var result = _initializer.ConvertToEfValue(typeof(TimeSpan), edmTimeOfDay);

        result.Should().BeOfType<TimeSpan>().Which.Should().Be(new TimeSpan(10, 30, 45));
    }

    [Fact]
    public void ConvertToEfValue_ShouldParseEnum_ForStringValue()
    {
        var result = _initializer.ConvertToEfValue(typeof(SampleEnum), "Value2");

        result.Should().Be(SampleEnum.Value2);
    }

    [Fact]
    public void ConvertToEfValue_ShouldReturnLong_ForIntValue()
    {
        var result = _initializer.ConvertToEfValue(typeof(long), 42);

        result.Should().BeOfType<long>().Which.Should().Be(42L);
    }

    [Fact]
    public void ConvertToEfValue_ShouldReturnOriginalValue_ForUnmappedType()
    {
        var result = _initializer.ConvertToEfValue(typeof(string), "hello");

        result.Should().Be("hello");
    }
}
```

- [ ] **Step 2: Build and run**

Run: `dotnet test test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj --filter "EFChangeSetInitializerTests" -v normal`

Expected: 6 tests pass.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFramework/EFChangeSetInitializerTests.cs
git commit -m "test: add EF6 ConvertToEfValue unit tests"
```

#### 7b: Add EFModelMapper tests

**Files:**
- Create: `test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelMapperTests.cs`

Tests `TryGetRelevantType` which resolves entity set names to CLR types by inspecting DbSet properties on the DbContext. Uses the existing `LibraryContext` which has `Books`, `LibraryCards`, `Publishers`, `Readers` DbSets.

- [ ] **Step 4: Write the test file**

Create `test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelMapperTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.EntityFrameworkCore;

public class EFModelMapperTests : RestierTestBase<LibraryApi>
{
    [Fact]
    public async Task TryGetRelevantType_ShouldResolve_KnownEntitySet()
    {
        var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        var mapper = api.GetApiService<IModelMapper>();
        mapper.Should().NotBeNull();

        var context = new InvocationContext(api);
        mapper.TryGetRelevantType(context, "Books", out var relevantType).Should().BeTrue();
        relevantType.Should().Be(typeof(Book));
    }

    [Fact]
    public async Task TryGetRelevantType_ShouldNotResolve_UnknownEntitySet()
    {
        var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        var mapper = api.GetApiService<IModelMapper>();

        var context = new InvocationContext(api);
        mapper.TryGetRelevantType(context, "NonExistent", out var relevantType).Should().BeFalse();
        relevantType.Should().BeNull();
    }

    [Fact]
    public async Task TryGetRelevantType_WithNamespace_ShouldReturnFalse()
    {
        var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<LibraryContext>());
        var mapper = api.GetApiService<IModelMapper>();

        var context = new InvocationContext(api);
        mapper.TryGetRelevantType(context, "Microsoft.Restier", "Books", out var relevantType).Should().BeFalse();
        relevantType.Should().BeNull();
    }
}
```

- [ ] **Step 5: Build and run**

Run: `dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj --filter "EFModelMapperTests" -v normal`

Expected: 3 tests pass.

- [ ] **Step 6: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelMapperTests.cs
git commit -m "test: add EFModelMapper unit tests"
```

#### 7c: Add EFModelBuilder happy-path test

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelBuilderTests.cs`

The existing tests only cover error cases (complex types mapped as DbSets, keyless entities). Add a test that verifies a valid context produces a correct EdmModel.

- [ ] **Step 7: Add happy-path test to EFModelBuilderTests.cs**

Add the following test method to the `EFModelBuilderTests` class in `test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelBuilderTests.cs`:

```csharp
    [Fact]
    public async Task GetEdmModel_ShouldBuildValidModel_ForStandardContext()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<LibraryApi>(
            serviceCollection: services => services.AddEFCoreProviderServices<LibraryContext>());

        metadata.Should().NotBeNull();
        var metadataString = metadata.ToString();
        metadataString.Should().Contain("Books");
        metadataString.Should().Contain("Publishers");
        metadataString.Should().Contain("Readers");
    }
```

This requires adding the following usings to the top of the file:

```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
```

- [ ] **Step 8: Build and run**

Run: `dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj --filter "EFModelBuilderTests" -v normal`

Expected: 3 tests pass (2 existing error-case + 1 new happy-path).

- [ ] **Step 9: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelBuilderTests.cs
git commit -m "test: add EFModelBuilder happy-path test for standard context"
```

#### 7d: Add GeographyConverter tests (EF6-only)

**Files:**
- Create: `test/Microsoft.Restier.Tests.EntityFramework/GeographyConverterTests.cs`

Tests the 4 public conversion methods between `DbGeography` and OData `GeographyPoint`/`GeographyLineString`. These are EF6-only spatial types from `System.Data.Entity.Spatial`.

- [ ] **Step 10: Write the test file**

Create `test/Microsoft.Restier.Tests.EntityFramework/GeographyConverterTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Data.Entity.Spatial;
using FluentAssertions;
using Microsoft.Restier.EntityFramework;
using Microsoft.Spatial;
using Xunit;

namespace Microsoft.Restier.Tests.EntityFramework;

public class GeographyConverterTests
{
    [Fact]
    public void ToGeographyPoint_ShouldConvert_DbGeographyPoint()
    {
        var dbGeography = DbGeography.PointFromText("POINT(-122.12 47.67)", 4326);

        var result = dbGeography.ToGeographyPoint();

        result.Should().NotBeNull();
        result.Longitude.Should().BeApproximately(-122.12, 0.01);
        result.Latitude.Should().BeApproximately(47.67, 0.01);
    }

    [Fact]
    public void ToDbGeography_ShouldConvert_GeographyPoint()
    {
        var point = GeographyPoint.Create(47.67, -122.12, null, null);

        var result = point.ToDbGeography();

        result.Should().NotBeNull();
        result.Longitude.Should().BeApproximately(-122.12, 0.01);
        result.Latitude.Should().BeApproximately(47.67, 0.01);
    }

    [Fact]
    public void ToGeographyLineString_ShouldConvert_DbGeographyLineString()
    {
        var dbGeography = DbGeography.LineFromText("LINESTRING(-122.12 47.67, -122.13 47.68)", 4326);

        var result = dbGeography.ToGeographyLineString();

        result.Should().NotBeNull();
        result.Points.Should().HaveCount(2);
    }

    [Fact]
    public void ToDbGeography_ShouldConvert_GeographyLineString()
    {
        var factory = GeographyFactory.LineString(47.67, -122.12).LineTo(47.68, -122.13);
        var lineString = (GeographyLineString)factory.Build();

        var result = lineString.ToDbGeography();

        result.Should().NotBeNull();
        result.PointCount.Should().Be(2);
    }
}
```

**Note:** These tests depend on the EF6 `System.Data.Entity.Spatial.DbGeography` type, which requires SqlServer spatial types at runtime. If the tests fail because the spatial provider isn't available on the test machine (no SQL Server LocalDB), they should be marked with `[Fact(Skip = "Requires SQL Server spatial types")]` or wrapped with a runtime check. Evaluate at runtime.

- [ ] **Step 11: Build and run**

Run: `dotnet test test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj --filter "GeographyConverterTests" -v normal`

Expected: 4 tests pass (or skip if spatial provider unavailable).

- [ ] **Step 12: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFramework/GeographyConverterTests.cs
git commit -m "test: add GeographyConverter unit tests for EF6 spatial conversions"
```

- [ ] **Step 13: Run full test suite and verify**

Run: `dotnet test RESTier.slnx`

Expected: All tests pass, including all new tests from this task.
