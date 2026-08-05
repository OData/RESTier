# Dual EF6/EF Core Testing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Run all EF-dependent integration tests in `Microsoft.Restier.Tests.AspNetCore` against both EF6 and EF Core providers using abstract base classes with concrete subclasses per provider.

**Architecture:** Shared scenario files (LibraryApi, MarvelApi, etc.) get conditional namespaces (`.EF6`/`.EFCore`). A new `Tests.Shared.EntityFrameworkCore` project compiles scenarios with the EFCore constant. Each EF-dependent test becomes an abstract base class with two small subclasses (one per provider) that provide the service registration delegate.

**Tech Stack:** .NET 8/9, xUnit v3, Entity Framework 6, Entity Framework Core 8/9, SQL Server LocalDB (when configured) or EF Core InMemory (fallback)

---

### Task 1: Create Microsoft.Restier.Tests.Shared.EntityFrameworkCore project

**Files:**
- Create: `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Microsoft.Restier.Tests.Shared.EntityFrameworkCore.csproj`
- Modify: `RESTier.slnx`

- [ ] **Step 1: Create the project file**

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFrameworks>net8.0;net9.0;</TargetFrameworks>
		<IsTestProject>false</IsTestProject>
		<DefineConstants>$(DefineConstants);EFCore</DefineConstants>
		<UserSecretsId>a3d6432c-d914-44a1-93d6-fa96f123ca2f</UserSecretsId>
	</PropertyGroup>

	<ItemGroup>
		<Compile Include="..\Microsoft.Restier.Tests.Shared.EntityFramework\Extensions\**\*.cs" LinkBase="Extensions" />
		<Compile Include="..\Microsoft.Restier.Tests.Shared.EntityFramework\Scenarios\**\*.cs" LinkBase="Scenarios" />
	</ItemGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="$(RestierNet9UserSecretsVersion)" />
	</ItemGroup>

	<ItemGroup Condition="'$(TargetFramework)' == 'net9.0'">
		<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="$(RestierNet9EntityFrameworkVersion)" />
		<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="$(RestierNet9EntityFrameworkVersion)" />
	</ItemGroup>

	<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
		<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.*" />
		<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.*" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\..\src\Microsoft.Restier.EntityFrameworkCore\Microsoft.Restier.EntityFrameworkCore.csproj" />
		<ProjectReference Include="..\Microsoft.Restier.Tests.Shared\Microsoft.Restier.Tests.Shared.csproj" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\..\src\Microsoft.Restier.AspNetCore\Microsoft.Restier.AspNetCore.csproj" Condition="'$(IsNetCore)' == 'true'" />
	</ItemGroup>

</Project>
```

Note: The `IDatabaseInitializer.cs` file is currently in `Tests.Shared.EntityFramework` but excluded via `<Compile Remove="IDatabaseInitializer.cs" />` in the EF6 csproj. Since we're linking `Scenarios\**\*.cs`, the file at the root won't be included. We need to explicitly include it:

Add after the Scenarios compile include:
```xml
<Compile Include="..\Microsoft.Restier.Tests.Shared.EntityFramework\IDatabaseInitializer.cs" Link="IDatabaseInitializer.cs" />
```

- [ ] **Step 2: Add to solution file**

Edit `RESTier.slnx` to add the new project to the `/test/EntityFramework/` folder:

```xml
  <Folder Name="/test/EntityFramework/" Id="b45d57df-5e57-4cf8-9ff8-86a03f39bb09">
    <Project Path="test/Microsoft.Restier.Tests.Shared.EntityFramework/Microsoft.Restier.Tests.Shared.EntityFramework.csproj" />
    <Project Path="test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Microsoft.Restier.Tests.Shared.EntityFrameworkCore.csproj" />
  </Folder>
```

- [ ] **Step 3: Verify it compiles (will fail until Task 2 is done)**

This task is completed before Task 2 to establish the project structure. The build will fail due to namespace conflicts until conditional namespaces are added in Task 2.

Run: `dotnet build test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Microsoft.Restier.Tests.Shared.EntityFrameworkCore.csproj`
Expected: Build errors (namespace conflicts, which Task 2 resolves)

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Microsoft.Restier.Tests.Shared.EntityFrameworkCore.csproj RESTier.slnx
git commit -m "feat: add Microsoft.Restier.Tests.Shared.EntityFrameworkCore project"
```

---

### Task 2: Add conditional namespaces to shared scenario source files

**Files:**
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryApi.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryContext.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Marvel/MarvelApi.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Marvel/MarvelContext.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Marvel/MarvelTestInitializer.cs`

All six files follow the same pattern: replace the single namespace with a conditional namespace.

- [ ] **Step 1: Update LibraryApi.cs**

Fix the unconditional `using System.Data.Entity;` on line 10 — wrap it in `#if EF6`:

```csharp
// Before (line 10):
using System.Data.Entity;

// After:
#if EF6
using System.Data.Entity;
#endif
```

Change the namespace (line 28):

```csharp
// Before:
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library

// After:
#if EF6
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6
#elif EFCore
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore
#endif
```

Also add a using for the entity model types which now live in a different namespace:

```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
```

Add this after the existing using directives (before the namespace).

- [ ] **Step 2: Update LibraryContext.cs**

Change the namespace (line 10):

```csharp
// Before:
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library

// After:
using Microsoft.Restier.Tests.Shared.Scenarios.Library;

#if EF6
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6
#elif EFCore
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore
#endif
```

- [ ] **Step 3: Update LibraryTestInitializer.cs**

Change the namespace (line 16):

```csharp
// Before:
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library

// After:
using Microsoft.Restier.Tests.Shared.Scenarios.Library;

#if EF6
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6
#elif EFCore
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore
#endif
```

- [ ] **Step 4: Update MarvelApi.cs**

Change `using Microsoft.Restier.Tests.Shared.Scenarios.Library;` (line 8) to point at the provider-specific namespace (MarvelApi references `LibraryCard` from the Library scenario? No — check usages. Actually, `MarvelApi` imports `Library` for the `LibraryCard` type used in cross-references. Keep this using since entity types stay in the base namespace).

Change the namespace (line 23):

```csharp
// Before:
namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel

// After:
#if EF6
namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EF6
#elif EFCore
namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EFCore
#endif
```

Add using for Marvel entity types:

```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
```

- [ ] **Step 5: Update MarvelContext.cs**

Change the namespace (line 10):

```csharp
// Before:
namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel

// After:
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;

#if EF6
namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EF6
#elif EFCore
namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EFCore
#endif
```

- [ ] **Step 6: Update MarvelTestInitializer.cs**

Change the namespace (line 14):

```csharp
// Before:
namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel

// After:
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;

#if EF6
namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EF6
#elif EFCore
namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EFCore
#endif
```

- [ ] **Step 7: Build both shared projects**

Run: `dotnet build test/Microsoft.Restier.Tests.Shared.EntityFramework/Microsoft.Restier.Tests.Shared.EntityFramework.csproj`
Expected: SUCCESS

Run: `dotnet build test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Microsoft.Restier.Tests.Shared.EntityFrameworkCore.csproj`
Expected: SUCCESS

- [ ] **Step 8: Commit**

```bash
git add test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/ 
git commit -m "feat: add conditional namespaces to shared EF scenario files"
```

---

### Task 3: Update EFCore service registration for SQL Server + in-memory fallback

**Files:**
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Extensions/EntityFrameworkServiceCollectionExtensions.cs`

- [ ] **Step 1: Update the EFCore section**

Replace the entire `#if EFCore` block (lines 76-126) with:

```csharp
#if EFCore

        private static IConfiguration _configuration;

        /// <summary>
        /// Gets the test configuration, loading user secrets if available.
        /// </summary>
        private static IConfiguration Configuration
        {
            get
            {
                if (_configuration is null)
                {
                    _configuration = new ConfigurationBuilder()
                        .AddUserSecrets(typeof(EFServiceCollectionExtensions).Assembly, optional: true)
                        .Build();
                }
                return _configuration;
            }
        }

        /// <summary>
        /// Adds Entity Framework Core provider services for the specified DbContext.
        /// Uses SQL Server when a connection string is configured; falls back to in-memory.
        /// </summary>
        /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddEntityFrameworkServices<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
        {
            var connectionString = Configuration.GetConnectionString(typeof(TDbContext).Name);

            if (!string.IsNullOrEmpty(connectionString))
            {
                var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
                if (builder.ContainsKey("Initial Catalog"))
                {
                    builder["Initial Catalog"] = $"{builder["Initial Catalog"]}_{Environment.Version.Major}_EFCore";
                }
                else if (builder.ContainsKey("Database"))
                {
                    builder["Database"] = $"{builder["Database"]}_{Environment.Version.Major}_EFCore";
                }

                services.AddDbContext<TDbContext>(options =>
                    options.UseSqlServer(builder.ConnectionString));
            }
            else
            {
                services.AddDbContext<TDbContext>(options =>
                    options.UseInMemoryDatabase(typeof(TDbContext).Name));
            }

            services.AddEFCoreProviderServices<TDbContext>();

            if (typeof(TDbContext) == typeof(LibraryContext))
            {
                services.SeedDatabase<LibraryContext, LibraryTestInitializer>();
            }
            else if (typeof(TDbContext) == typeof(MarvelContext))
            {
                services.SeedDatabase<MarvelContext, MarvelTestInitializer>();
            }

            return services;
        }

        /// <summary>
        /// Seeds the database using the specified initializer.
        /// </summary>
        public static void SeedDatabase<TContext, TInitializer>(this IServiceCollection services)
            where TContext : DbContext
            where TInitializer : IDatabaseInitializer, new()
        {
            using var tempServices = services.BuildServiceProvider();

            var scopeFactory = tempServices.GetService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<TContext>();

            if (dbContext.Database.EnsureCreated())
            {
                var initializer = new TInitializer();
                initializer.Seed(dbContext);
            }

        }

#endif
```

Update the EFCore usings at the top of the file (lines 11-16) to add the needed namespaces:

```csharp
#if EFCore
using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Restier.Tests.Shared.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EFCore;
#endif
```

And update the EF6 usings (line 3) to reference the EF6-specific namespace:

After `using Microsoft.Restier.EntityFramework;` add:

```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
```

Wait — the EF6 block doesn't currently need Library/Marvel usings because the types were in the same namespace. Now they're in `.EF6`. But looking at the EF6 block, it doesn't reference LibraryContext/MarvelContext directly — the method is generic `AddEntityFrameworkServices<TDbContext>`. So no EF6 using changes are needed here.

For EFCore, the `SeedDatabase` method references `LibraryContext`, `LibraryTestInitializer`, `MarvelContext`, `MarvelTestInitializer` — these are now in `.EFCore` namespaces. Add the usings shown above.

- [ ] **Step 2: Build**

Run: `dotnet build test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Microsoft.Restier.Tests.Shared.EntityFrameworkCore.csproj`
Expected: SUCCESS

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.Shared.EntityFramework/Extensions/EntityFrameworkServiceCollectionExtensions.cs
git commit -m "feat: add SQL Server + in-memory fallback for EFCore test registration"
```

---

### Task 4: Update Microsoft.Restier.Tests.AspNetCore project references and collection definitions

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/LibraryApiTestCollection.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/LibraryApiEF6TestCollection.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/LibraryApiEFCoreTestCollection.cs`

- [ ] **Step 1: Update csproj to add EFCore references**

Add to the `<ItemGroup>` with ProjectReferences:

```xml
<ProjectReference Include="..\Microsoft.Restier.Tests.Shared.EntityFrameworkCore\Microsoft.Restier.Tests.Shared.EntityFrameworkCore.csproj" />
<ProjectReference Include="..\..\src\Microsoft.Restier.EntityFrameworkCore\Microsoft.Restier.EntityFrameworkCore.csproj" />
```

- [ ] **Step 2: Update usings in existing test files that reference Library/Marvel EF6 namespaces**

All feature tests and regression tests currently have:
```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
```

These must change to:
```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
```

Similarly for Marvel:
```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel;
// becomes:
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EF6;
```

**Important:** Only change files that reference EF-specific types (`LibraryApi`, `LibraryContext`, `MarvelApi`, `MarvelContext`). Files that only use entity types (`Book`, `Publisher`, `Employee`, etc.) keep `using Microsoft.Restier.Tests.Shared.Scenarios.Library;`.

In practice, all the FeatureTests and RegressionTests files reference both entity types AND EF-specific types, so they need **both** usings:
```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Library;      // for Book, Publisher, etc.
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;   // for LibraryApi, LibraryContext
```

- [ ] **Step 3: Create EF6 collection definition**

Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/LibraryApiEF6TestCollection.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

/// <summary>
/// Defines a test collection for EF6 feature tests that share the LibraryApi database.
/// Tests within this collection run sequentially to avoid data contention.
/// </summary>
[CollectionDefinition("LibraryApiEF6")]
public class LibraryApiEF6TestCollection;
```

- [ ] **Step 4: Create EFCore collection definition**

Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/LibraryApiEFCoreTestCollection.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore;

/// <summary>
/// Defines a test collection for EF Core feature tests that share the LibraryApi database.
/// Tests within this collection run sequentially to avoid data contention.
/// </summary>
[CollectionDefinition("LibraryApiEFCore")]
public class LibraryApiEFCoreTestCollection;
```

- [ ] **Step 5: Build to verify references resolve**

Run: `dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj`
Expected: SUCCESS (all tests still compile with EF6 namespaces)

- [ ] **Step 6: Run existing tests to verify nothing is broken**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj`
Expected: All existing tests still pass

- [ ] **Step 7: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/
git commit -m "feat: add EFCore project references and test collection definitions"
```

---

### Task 5: Refactor simple feature tests (pattern: only `ConfigureServices` needed)

This task covers the 8 simple feature tests that only call static `RestierTestHelpers` methods with `LibraryApi` and `LibraryContext` type parameters. No helper methods reference provider-specific types directly.

**Files to refactor:** ActionTests, ExpandTests, FunctionTests, InTests, InsertTests, PagingTests, QueryTests, ValidationTests

The pattern for each is identical. Showing `QueryTests` as the canonical example:

- [ ] **Step 1: Convert QueryTests.cs to abstract base class**

Modify `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/QueryTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

/// <summary>
/// Restier tests that cover the general queryability of the service.
/// </summary>
public abstract class QueryTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    [Fact]
    public async Task EmptyEntitySetQueryReturns200Not404()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/LibraryCards",
            serviceCollection: ConfigureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EmptyFilterQueryReturns200Not404()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$filter=Title eq 'Sesame Street'",
            serviceCollection: ConfigureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NonExistentEntitySetReturns404()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Subscribers",
            serviceCollection: ConfigureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ObservableCollectionsAsCollectionNavigationProperties()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Publishers('Publisher2')/Books",
            serviceCollection: ConfigureServices);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

Key changes from original:
- Remove `[Collection("LibraryApi")]`
- Class becomes `abstract class QueryTests<TApi, TContext>` with constraints
- Add `protected abstract Action<IServiceCollection> ConfigureServices { get; }`
- Replace `LibraryApi` type references in `ExecuteTestRequest<LibraryApi>` with `TApi`
- Replace `services.AddEntityFrameworkServices<LibraryContext>()` with `ConfigureServices`
- Remove `using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;` (only entity types needed)
- Add `using System;` for `Action<>`
- Keep `using Microsoft.Restier.Tests.Shared.Scenarios.Library;` for entity types (Book, Publisher, etc.)

- [ ] **Step 2: Create EF6 subclass**

Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/QueryTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

[Collection("LibraryApiEF6")]
public class QueryTests : QueryTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}
```

- [ ] **Step 3: Create EFCore subclass**

Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/QueryTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore;

[Collection("LibraryApiEFCore")]
public class QueryTests : QueryTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}
```

- [ ] **Step 4: Apply the same pattern to the remaining 7 simple feature tests**

For each of these files, apply the same three transformations:
1. Convert to `abstract class XxxTests<TApi, TContext>` — remove collection attribute, add `ConfigureServices` abstract property, replace `LibraryApi`/`LibraryContext` type args with `TApi`, replace service lambda with `ConfigureServices`
2. Create `FeatureTests/EF6/XxxTests.cs` with `[Collection("LibraryApiEF6")]`
3. Create `FeatureTests/EFCore/XxxTests.cs` with `[Collection("LibraryApiEFCore")]`

Files:
- `ActionTests.cs` — also has `ITestOutputHelper outputHelper` primary constructor parameter. Keep it in the abstract base, pass through in subclasses: `public class ActionTests(ITestOutputHelper outputHelper) : ActionTests<LibraryApi, LibraryContext>(outputHelper)`
- `ExpandTests.cs` — straightforward
- `FunctionTests.cs` — also has `ITestOutputHelper outputHelper` primary constructor parameter (same treatment as ActionTests)
- `InTests.cs` — straightforward
- `InsertTests.cs` — straightforward
- `PagingTests.cs` — straightforward
- `ValidationTests.cs` — straightforward

- [ ] **Step 5: Build**

Run: `dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj`
Expected: SUCCESS

- [ ] **Step 6: Run tests**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~FeatureTests.EF6"`
Expected: All EF6 tests pass

- [ ] **Step 7: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/
git commit -m "feat: refactor simple feature tests for dual EF6/EFCore testing"
```

---

### Task 6: Refactor feature tests with helper methods

This task covers tests that have private helper methods referencing provider-specific types (`LibraryContext`, `LibraryApi`). These helpers become abstract methods in the base class, implemented by each subclass.

**Files:** AuthorizationTests, BatchTests, NavigationPropertyTests, UpdateTests

- [ ] **Step 1: Refactor AuthorizationTests**

`AuthorizationTests` is almost simple — it only uses `LibraryApi`/`LibraryContext` via `ExecuteTestRequest` and `AddEntityFrameworkServices`. The `ConfigureServices` pattern handles it. However, the `Authorization_UpdateEmployee_ShouldReturn400` test builds a custom `services` action that chains `AddEntityFrameworkServices` with `AddSingleton<ODataValidationSettings>`. This still works with the abstract `ConfigureServices` approach if the subclass provides the base EF registration and the test adds extras on top.

Actually, looking more carefully: the tests pass inline lambdas to `serviceCollection:` that call `AddEntityFrameworkServices<LibraryContext>()`. Since both subclasses provide a `ConfigureServices` delegate, we can use that. But `Authorization_UpdateEmployee_ShouldReturn400` builds a custom delegate. Solution: the base class defines a `ConfigureServicesWithExtras` helper:

```csharp
protected Action<IServiceCollection> WithExtras(Action<IServiceCollection> extras)
    => services => { ConfigureServices(services); extras(services); };
```

Then the test uses:
```csharp
var services = WithExtras(s => s.AddSingleton(new ODataValidationSettings { ... }));
```

Apply this pattern. The base class:

```csharp
public abstract class AuthorizationTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    // helper for tests that need additional service registrations
    protected Action<IServiceCollection> WithExtras(Action<IServiceCollection> extras)
        => services => { ConfigureServices(services); extras(services); };

    // ... tests with LibraryApi→TApi, service lambdas→ConfigureServices or WithExtras(...)
}
```

EF6/EFCore subclasses: same 10-line pattern as Task 5.

- [ ] **Step 2: Refactor BatchTests**

`BatchTests` has two private helpers:
- `GetHttpClientAsync()` — calls `RestierTestHelpers.GetTestableHttpClient<LibraryApi>(serviceCollection: ...)`
- `CleanupBatchBooksAsync()` — calls `RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(...)`

Both can be parameterized with `TApi`/`TContext` and `ConfigureServices` in the base class:

```csharp
public abstract class BatchTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    // CleanupBatchBooksAsync needs to remove books from the context.
    // Since TContext doesn't have a .Books property in the base, make it abstract.
    protected abstract Task CleanupBatchBooksAsync();

    private async Task<HttpClient> GetHttpClientAsync()
    {
        var httpClient = await RestierTestHelpers.GetTestableHttpClient<TApi>(
            serviceCollection: ConfigureServices);
        httpClient.BaseAddress = new Uri($"{WebApiConstants.Localhost}{WebApiConstants.RoutePrefix}");
        return httpClient;
    }

    // ... all test methods using TApi and ConfigureServices
}
```

EF6 subclass adds the cleanup implementation:

```csharp
[Collection("LibraryApiEF6")]
public class BatchTests : BatchTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();

    protected override async Task CleanupBatchBooksAsync()
    {
        var context = await RestierTestHelpers.GetTestableInjectedService<LibraryApi, LibraryContext>(
            serviceCollection: ConfigureServices);
        var books = context.Books.Where(book => book.Title.StartsWith("Batch Test")).ToList();
        foreach (var book in books)
        {
            context.Books.Remove(book);
        }
        await context.SaveChangesAsync();
    }
}
```

EFCore subclass: identical structure but with EFCore namespace usings.

- [ ] **Step 3: Refactor NavigationPropertyTests**

Has a `CleanupPublisher(LibraryContext context, Publisher publisher)` helper. Make it abstract:

```csharp
protected abstract void CleanupPublisher(object context, Publisher publisher);
```

Actually, the test also calls `GetTestableInjectedService<LibraryApi, LibraryContext>` to get the context. Better approach: make a single abstract method that gets the context and does cleanup:

```csharp
protected abstract Task<object> GetContextAsync();
protected abstract void CleanupPublisher(object context, Publisher publisher);
```

Simpler: just make the whole cleanup operation abstract:

```csharp
protected abstract void CleanupTestPublisher(Publisher publisher);
```

But this loses the ability to share setup/teardown patterns. Let's keep it pragmatic — the setup calls `GetTestableInjectedService` which needs `TApi`/`TContext`, and the cleanup calls methods on the context. Make both abstract:

Base class:
```csharp
protected abstract Task<dynamic> GetLibraryContextAsync();
protected abstract void RemovePublisher(dynamic context, Publisher publisher);
```

No, using `dynamic` is ugly. Better approach: just make the entire test setup+cleanup abstract and use template method pattern:

Actually, the simplest approach: since the context access pattern is `RestierTestHelpers.GetTestableInjectedService<TApi, TContext>(serviceCollection: ConfigureServices)`, and the cleanup uses `.Books.Remove()` / `.Publishers.Remove()` / `.SaveChanges()`, the EF6 and EFCore contexts both have these members. The issue is the base class can't call them without knowing the type.

**Decision: Make the context-dependent helpers abstract.** Each subclass (~15 lines) implements them with their provider's concrete types.

Base class:
```csharp
public abstract class NavigationPropertyTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }
    protected abstract Task<SetupContext> SetupPublisherAsync(Publisher publisher);
    protected abstract Task<SetupContext> SetupPublishersAsync(Publisher publisher1, Publisher publisher2);
    protected abstract void CleanupPublisher(SetupContext ctx, Publisher publisher);

    // SetupContext is a simple wrapper that subclasses populate with their typed context
    protected class SetupContext : IDisposable
    {
        public object Context { get; set; }
        public Action DisposeAction { get; set; }
        public void Dispose() => DisposeAction?.Invoke();
    }
    // ... tests
}
```

Hmm, this is getting overly complex. Let me simplify. The subclasses are small — let's just make `CleanupPublisher` take no arguments and have the subclass manage state via a field:

Actually, the cleanest approach for NavigationPropertyTests: extract the context-access into a virtual method and the cleanup into an abstract. Looking at the actual test code, each test:
1. Gets a context via `GetTestableInjectedService<LibraryApi, LibraryContext>`
2. Adds publishers to context
3. Runs HTTP assertions
4. Cleans up publishers from context

Since (1) and (4) need provider-specific types, make them abstract:

```csharp
protected abstract Task<IPublisherTestHelper> CreatePublisherHelperAsync();

protected interface IPublisherTestHelper
{
    void AddPublisher(Publisher publisher);
    void SaveChanges();
    void RemovePublisherAndBooks(Publisher publisher);
}
```

OK this is still over-engineered. The simplest correct approach:

**Each subclass overrides one method: `CreateContextAsync` which returns `dynamic`.** Tests use `dynamic` for the few context operations (Add, Remove, SaveChanges). These are standard EF methods on both providers. It's pragmatic and avoids abstractions:

No, `dynamic` breaks at runtime. Let me just accept that for the 3 tests with helpers (Batch, Navigation, Update), the subclasses will be ~25 lines instead of ~10 lines, duplicating the helper logic. This is fine — it's test code.

**Final decision for all tests with helpers: make the helpers abstract in the base class. Each subclass provides its own implementation using its provider's types.**

- [ ] **Step 4: Refactor UpdateTests**

Has `Cleanup(Guid bookId, string title)` which calls `GetTestableApiInstance<LibraryApi>()` and accesses `api.DbContext.Books`. Make it abstract:

Base class:
```csharp
protected abstract Task Cleanup(Guid bookId, string title);
```

Subclass:
```csharp
protected override async Task Cleanup(Guid bookId, string title)
{
    var api = await RestierTestHelpers.GetTestableApiInstance<LibraryApi>(
        serviceCollection: ConfigureServices);
    var book = api.DbContext.Books.First(candidate => candidate.Id == bookId);
    book.Title = title;
    await api.DbContext.SaveChangesAsync();
}
```

- [ ] **Step 5: Build and test**

Run: `dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj`
Expected: SUCCESS

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~FeatureTests.EF6"`
Expected: All EF6 tests pass

- [ ] **Step 6: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/
git commit -m "feat: refactor feature tests with helpers for dual EF6/EFCore"
```

---

### Task 7: Refactor MetadataTests (multi-API: Library, Marvel, Store)

MetadataTests is special — it tests 3 different APIs: LibraryApi (EF), MarvelApi (EF), and StoreApi (non-EF). Split into separate base classes.

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/MetadataTests.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/MetadataTests.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/MetadataTests.cs`

- [ ] **Step 1: Split MetadataTests into base class with abstract Marvel support**

The LibraryApi tests use the `TApi`/`TContext` pattern. The MarvelApi tests need their own type parameters. Rather than 4 type params, add abstract methods for the Marvel metadata tests:

```csharp
public abstract class MetadataTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }
    protected abstract Task<string> GetMarvelApiMetadataAsync();

    private const string RelativePath = "..//..//..//..//Microsoft.Restier.Tests.AspNetCore//";
    private const string BaselineFolder = "Baselines//";

    [Fact]
    public async Task LibraryApi_CompareCurrentApiMetadataToPriorRun()
    {
        var fileName = $"{Path.Combine(RelativePath, BaselineFolder)}{typeof(TApi).Name}-ApiMetadata.txt";
        File.Exists(fileName).Should().BeTrue();

        var oldReport = File.ReadAllText(fileName);
        var newReport = await RestierTestHelpers.GetApiMetadataAsync<TApi>(
            serviceCollection: ConfigureServices);

        oldReport.Should().BeEquivalentTo(newReport.ToString());
    }

    [Fact]
    public async Task MarvelApi_CompareCurrentApiMetadataToPriorRun()
    {
        var fileName = $"{Path.Combine(RelativePath, BaselineFolder)}MarvelApi-ApiMetadata.txt";
        File.Exists(fileName).Should().BeTrue();

        var oldReport = File.ReadAllText(fileName);
        var newReport = await GetMarvelApiMetadataAsync();

        oldReport.Should().BeEquivalentTo(newReport.ToString());
    }

    [Fact]
    public async Task StoreApi_CompareCurrentApiMetadataToPriorRun()
    {
        var fileName = $"{Path.Combine(RelativePath, BaselineFolder)}{nameof(StoreApi)}-ApiMetadata.txt";
        File.Exists(fileName).Should().BeTrue();

        var oldReport = File.ReadAllText(fileName);
        var newReport = await RestierTestHelpers.GetApiMetadataAsync<StoreApi>();

        oldReport.Should().BeEquivalentTo(newReport.ToString());
    }

    // BreakdanceManifestGenerator methods stay as abstract in subclasses
    // since they reference concrete API types
}
```

Note: The `LibraryApi-ApiMetadata.txt` baseline file name uses `nameof(LibraryApi)`. Since both EF6 and EFCore `LibraryApi` have the same name, the baseline file should be the same. The metadata should be identical regardless of provider since it's derived from the EDM model, not the database. Both subclasses can share the same baseline file. If metadata differs between providers, we'd need separate baseline files — but this is unlikely and can be addressed later.

- [ ] **Step 2: Create EF6 subclass**

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EF6;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

[Collection("LibraryApiEF6")]
public class MetadataTests : MetadataTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();

    protected override async Task<string> GetMarvelApiMetadataAsync()
    {
        return await RestierTestHelpers.GetApiMetadataAsync<MarvelApi>(
            serviceCollection: services => services.AddEntityFrameworkServices<MarvelContext>());
    }
}
```

- [ ] **Step 3: Create EFCore subclass**

Same structure with EFCore namespace usings.

- [ ] **Step 4: Build and test**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~MetadataTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/MetadataTests.cs test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EF6/MetadataTests.cs test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/MetadataTests.cs
git commit -m "feat: refactor MetadataTests for dual EF6/EFCore"
```

---

### Task 8: Refactor regression tests

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/Issue541_CountPlusParametersFails.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/Issue671_MultipleContexts.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/Issue714_ComplexTypes.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/EF6/` (3 subclass files)
- Create: `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/EFCore/` (3 subclass files)

- [ ] **Step 1: Refactor Issue541**

Uses constructor-based `AddRestierAction` setup with `LibraryApi`/`LibraryContext`. Base class becomes:

```csharp
public abstract class Issue541_CountPlusParametersFails<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    protected Issue541_CountPlusParametersFails()
    {
        AddRestierAction = options =>
        {
            options.AddRestierRoute<TApi>(WebApiConstants.RoutePrefix, services =>
            {
                ConfigureServices(services);
            });
        };
        TestSetup();
    }

    // ... test methods unchanged (they use ExecuteTestRequest which inherits TApi)
}
```

Subclasses: same 10-line pattern.

- [ ] **Step 2: Refactor Issue671**

This file has 3 classes. Two are simple single-context tests; one uses both Library and Marvel.

`Issue671_MultipleContexts_SingleLibraryContext` — straightforward base + subclasses (same as Issue541 pattern).

`Issue671_MultipleContexts_SingleMarvelContext` — same but with `MarvelApi`/`MarvelContext`. Base class:

```csharp
public abstract class Issue671_MultipleContexts_SingleMarvelContext<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }
    // ...
}
```

`Issue671_MultipleContexts` — uses both Library and Marvel APIs. Base class needs both:

```csharp
public abstract class Issue671_MultipleContexts<TLibraryApi, TMarvelApi> : RestierTestBase<TLibraryApi>
    where TLibraryApi : ApiBase
    where TMarvelApi : ApiBase
{
    protected abstract Action<IServiceCollection> ConfigureLibraryServices { get; }
    protected abstract Action<IServiceCollection> ConfigureMarvelServices { get; }

    protected Issue671_MultipleContexts()
    {
        AddRestierAction = options =>
        {
            options.AddRestierRoute<TLibraryApi>("Library", services =>
            {
                ConfigureLibraryServices(services);
            });
            options.AddRestierRoute<TMarvelApi>("Marvel", services =>
            {
                ConfigureMarvelServices(services);
            });
        };
        TestSetup();
    }

    // test methods unchanged
}
```

EF6 subclass:
```csharp
using EF6Library = Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;
using EF6Marvel = Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EF6;

public class Issue671_MultipleContexts : Issue671_MultipleContexts<EF6Library.LibraryApi, EF6Marvel.MarvelApi>
{
    protected override Action<IServiceCollection> ConfigureLibraryServices
        => services => services.AddEntityFrameworkServices<EF6Library.LibraryContext>();
    protected override Action<IServiceCollection> ConfigureMarvelServices
        => services => services.AddEntityFrameworkServices<EF6Marvel.MarvelContext>();
}
```

- [ ] **Step 3: Refactor Issue714**

`ComplexTypesApi` extends `MarvelApi`. Since `MarvelApi` is now provider-specific, `ComplexTypesApi` must also be provider-specific. Define it in each subclass file:

Base class (no provider-specific types):
```csharp
public abstract class Issue714_ComplexTypes<TApi> : RestierTestBase<TApi>
    where TApi : ApiBase
{
    protected abstract void ConfigureRoute(ODataOptions options);

    protected Issue714_ComplexTypes()
    {
        AddRestierAction = ConfigureRoute;
        TestSetup();
    }

    [Fact]
    public async Task ComplexTypes_WorkAsExpected()
    {
        var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/ComplexTypeTest()");
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);
        content.Should().NotBeNullOrWhiteSpace();
    }
}
```

EF6 subclass:
```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;  // for MarvelContext not needed, use Marvel
using Microsoft.Restier.Tests.Shared.Scenarios.Marvel.EF6;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests.EF6;

public class Issue714_ComplexTypes : Issue714_ComplexTypes<ComplexTypesApiEF6>
{
    protected override void ConfigureRoute(ODataOptions options)
    {
        options.AddRestierRoute<ComplexTypesApiEF6>(WebApiConstants.RoutePrefix, routeServices =>
        {
            routeServices
                .AddEntityFrameworkServices<MarvelContext>()
                .AddSingleton<IChainedService<IModelBuilder>, ComplexTypesModelBuilder>();
        });
    }
}

public class ComplexTypesApiEF6 : MarvelApi
{
    public ComplexTypesApiEF6(MarvelContext dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(dbContext, model, queryHandler, submitHandler)
    {
    }

    [UnboundOperation(OperationType = OperationType.Function)]
    public LibraryCard ComplexTypeTest()
    {
        return new() { Id = Guid.NewGuid() };
    }
}
```

The `ComplexTypesModelBuilder` class is provider-independent — keep it in the base file.

- [ ] **Step 4: Build and test**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RegressionTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/
git commit -m "feat: refactor regression tests for dual EF6/EFCore"
```

---

### Task 9: Remove old LibraryApi collection and update remaining references

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/LibraryApiTestCollection.cs` — delete (no longer used)

- [ ] **Step 1: Delete the old collection definition**

Delete `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/LibraryApiTestCollection.cs`.

- [ ] **Step 2: Verify no remaining references to `[Collection("LibraryApi")]`**

Run: `grep -r 'Collection("LibraryApi")' test/Microsoft.Restier.Tests.AspNetCore/`
Expected: No matches (all have been replaced with `LibraryApiEF6`/`LibraryApiEFCore`)

- [ ] **Step 3: Commit**

```bash
git rm test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/LibraryApiTestCollection.cs
git commit -m "chore: remove old LibraryApi test collection definition"
```

---

### Task 10: Update other projects affected by namespace change

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFramework/ChangeSetPreparerTests.cs`
- Modify: `test/Microsoft.Restier.Tests.EntityFrameworkCore/EFCoreDbContextExtensionsTests.cs`
- Modify: `test/Microsoft.Restier.Tests.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsContext.cs` (if it uses Library namespace)
- Modify: `test/Microsoft.Restier.Tests.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsApi.cs` (if it uses Library namespace)

- [ ] **Step 1: Update Tests.EntityFramework**

In `ChangeSetPreparerTests.cs`, change:
```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
// to:
using Microsoft.Restier.Tests.Shared.Scenarios.Library;      // for Book, etc.
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;   // for LibraryApi, LibraryContext
```

- [ ] **Step 2: Update Tests.EntityFrameworkCore**

In `EFCoreDbContextExtensionsTests.cs`, add:
```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore; // for LibraryContext
```
Keep `using Microsoft.Restier.Tests.Shared.Scenarios.Library;` for `Address`.

In `Scenarios/Views/LibraryWithViewsContext.cs` and `LibraryWithViewsApi.cs`, add:
```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
```

- [ ] **Step 3: Build affected projects**

Run: `dotnet build test/Microsoft.Restier.Tests.EntityFramework/Microsoft.Restier.Tests.EntityFramework.csproj`
Run: `dotnet build test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj`
Expected: Both succeed

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFramework/ test/Microsoft.Restier.Tests.EntityFrameworkCore/
git commit -m "fix: update namespace references for conditional EF namespaces"
```

---

### Task 11: Full build and test verification

- [ ] **Step 1: Build entire solution**

Run: `dotnet build RESTier.slnx`
Expected: SUCCESS with no errors

- [ ] **Step 2: Run all tests**

Run: `dotnet test RESTier.slnx`
Expected: All tests pass. EF6 tests run as before. EFCore tests with in-memory fallback also pass (no SQL Server connection string configured by default).

- [ ] **Step 3: Verify test count increased**

The EF-dependent tests should now appear twice in the test output — once under `.EF6` namespace, once under `.EFCore` namespace. Confirm that the total test count has increased by approximately the number of EF-dependent tests.

- [ ] **Step 4: Commit any remaining fixes**

```bash
git add -A
git commit -m "fix: resolve any remaining build or test issues"
```

---

### Task 12: Address AspNetCorePlusEF6 project (if needed)

The `Microsoft.Restier.Tests.AspNetCorePlusEF6` project links source files from `Microsoft.Restier.Tests.AspNet` (legacy ASP.NET). Those source files reference `Microsoft.Restier.Tests.Shared.Scenarios.Library` for EF6 types that are now in `.Library.EF6`. Since the linked source files live in the `Tests.AspNet` project directory (outside our refactoring scope), this project may break.

- [ ] **Step 1: Check if AspNetCorePlusEF6 builds**

Run: `dotnet build test/Microsoft.Restier.Tests.AspNetCorePlusEF6/Microsoft.Restier.Tests.AspNetCorePlusEF6.csproj`

If it fails with namespace errors in linked files from `Tests.AspNet`, those files need the same `using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;` update. Fix them if needed.

- [ ] **Step 2: Commit if changes were needed**

```bash
git add test/Microsoft.Restier.Tests.AspNetCorePlusEF6/ test/Microsoft.Restier.Tests.AspNet/
git commit -m "fix: update AspNetCorePlusEF6 linked file usings for EF6 namespace"
```

---

### Task 13: Clean up and final commit

- [ ] **Step 1: Remove any dead code**

Check for orphaned using directives, unreferenced files, etc.

- [ ] **Step 2: Run full test suite one final time**

Run: `dotnet test RESTier.slnx`
Expected: All tests pass

- [ ] **Step 3: Final commit**

```bash
git add -A
git commit -m "chore: clean up after dual EF6/EFCore test refactor"
```
