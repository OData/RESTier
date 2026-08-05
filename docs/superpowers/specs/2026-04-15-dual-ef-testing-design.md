# Dual EF6/EF Core Testing for Microsoft.Restier.Tests.AspNetCore

**Date:** 2026-04-15
**Status:** Design approved

## Goal

Run the EF-dependent integration tests in `Microsoft.Restier.Tests.AspNetCore` against both Entity Framework 6 and Entity Framework Core, within the same test project, using an abstract base class pattern.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Parameterization mechanism | Abstract base class + 2 concrete subclasses per test file | Clear test names, type-safe, trivial subclasses |
| EF Core database backend | SQL Server when connection string configured; in-memory fallback | Maximum fidelity when SQL Server available; still works without it |
| Database isolation | Separate database names with runtime version + provider suffix (e.g., `LibraryContext_9_EFCore`) | Avoids collisions during parallel TFM and provider test runs |
| Type name collision resolution | Conditional namespaces: `.Library.EF6` / `.Library.EFCore` | More explicit than `extern alias` |
| Pure unit tests | Untouched — no dual testing | They mock everything; running twice adds no value |

## Architecture

### Conditional Namespaces

The shared EF scenario source files (LibraryApi, LibraryContext, etc.) already use `#if EF6` / `#if EFCore` conditional compilation. The namespaces become provider-specific:

```csharp
#if EF6
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6
#elif EFCore
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore
#endif
```

Entity model types (Book, Publisher, Employee, etc.) stay in `Microsoft.Restier.Tests.Shared.Scenarios.Library` — they are provider-independent.

### New Project: Microsoft.Restier.Tests.Shared.EntityFrameworkCore

Mirrors `Microsoft.Restier.Tests.Shared.EntityFramework` but compiled with `DefineConstants: EFCore`. References `Microsoft.Restier.EntityFrameworkCore` instead of `Microsoft.Restier.EntityFramework`. Links to the same source files (LibraryApi.cs, LibraryContext.cs, etc.) from the EF6 project directory via `<Compile Include="...">` items, so there is a single copy of each source file.

### Pre-existing Issues to Fix

`LibraryApi.cs` has `using System.Data.Entity;` on line 10 outside any `#if` block. This will break EFCore compilation and must be wrapped in `#if EF6`.

### Test Class Pattern

Each EF-dependent test class becomes a generic abstract base:

```csharp
// FeatureTests/QueryTests.cs
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
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
```

Two concrete subclasses per provider:

```csharp
// FeatureTests/EF6/QueryTests.cs
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6;

[Collection("LibraryApiEF6")]
public class QueryTests : QueryTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}
```

```csharp
// FeatureTests/EFCore/QueryTests.cs
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore;

[Collection("LibraryApiEFCore")]
public class QueryTests : QueryTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}
```

### EF Core Service Registration

The EFCore `AddEntityFrameworkServices` extension supports both SQL Server and in-memory:

```csharp
#if EFCore
public static IServiceCollection AddEntityFrameworkServices<TDbContext>(
    this IServiceCollection services) where TDbContext : DbContext
{
    var connectionString = Configuration.GetConnectionString(typeof(TDbContext).Name);

    if (!string.IsNullOrEmpty(connectionString))
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        AppendDatabaseSuffix(builder, $"_{Environment.Version.Major}_EFCore");
        services.AddDbContext<TDbContext>(options =>
            options.UseSqlServer(builder.ConnectionString));
    }
    else
    {
        services.AddDbContext<TDbContext>(options =>
            options.UseInMemoryDatabase(typeof(TDbContext).Name));
    }

    services.AddEFCoreProviderServices<TDbContext>();
    SeedDatabase<TDbContext>(services);
    return services;
}
#endif
```

### Test Collections

Two collection definitions to allow EF6 and EF Core tests to run in parallel (different databases), while tests within each collection run sequentially (shared database state):

- `[CollectionDefinition("LibraryApiEF6")]` — all EF6 feature tests
- `[CollectionDefinition("LibraryApiEFCore")]` — all EF Core feature tests

## Scope

### New projects
- `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/` — EFCore-compiled shared scenarios

### Modified shared source files (conditional namespace)
- `Scenarios/Library/LibraryApi.cs`
- `Scenarios/Library/LibraryContext.cs`
- `Scenarios/Library/LibraryTestInitializer.cs`
- `Scenarios/Marvel/MarvelApi.cs`
- `Scenarios/Marvel/MarvelContext.cs`
- `Scenarios/Marvel/MarvelTestInitializer.cs`
- `Extensions/EntityFrameworkServiceCollectionExtensions.cs` — EFCore SQL Server + in-memory fallback

### Modified test project
- `Microsoft.Restier.Tests.AspNetCore.csproj` — add references to `Tests.Shared.EntityFrameworkCore` and `Microsoft.Restier.EntityFrameworkCore`

### Feature tests refactored to base + subclasses (~13 files)
ActionTests, AuthorizationTests, BatchTests, ExpandTests, FunctionTests, InsertTests, InTests, MetadataTests, NavigationPropertyTests, PagingTests, QueryTests, UpdateTests, ValidationTests

### Regression tests refactored to base + subclasses (~3 files)
Issue541_CountPlusParametersFails, Issue671_MultipleContexts, Issue714_ComplexTypes

### New subclass files
- `FeatureTests/EF6/` — ~13 EF6 subclass files + collection definition
- `FeatureTests/EFCore/` — ~13 EFCore subclass files + collection definition
- `RegressionTests/EF6/` — ~3 EF6 subclass files
- `RegressionTests/EFCore/` — ~3 EFCore subclass files

### Other projects affected by namespace change (using updates only)
- `Microsoft.Restier.Tests.EntityFramework` — update usings to `.EF6`
- `Microsoft.Restier.Tests.EntityFrameworkCore` — update usings to `.EFCore`
- `Microsoft.Restier.Tests.AspNetCorePlusEF6` — update usings to `.EF6`

### Untouched
- All pure unit tests (Batch/, Filters/, Formatter/, Model/, MiddleWare/, etc.)
- FallbackTests (uses `ApiBase`, not EF)
- `RestierTestBase<TApi>` and Breakdance infrastructure
- Entity model types (Book, Publisher, Employee, etc.)
