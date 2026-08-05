# Keyless EF Views — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Auto-map keyless EF Core (`[Keyless]` / `HasNoKey()` / `ToView`) types to a `ComplexType<T>` + unbound `FunctionImport` returning `Collection(<ComplexType>)`, dispatched at request time by a registry-based fallback in `RestierOperationExecutor`. Views become accessible read-only via `GET /odata/<ViewName>()`. **EFCore-only.** EF6 throws an explicit "not supported" error on any keyless EntitySet; the EDMX path is out of scope. Closes [#741](https://github.com/OData/RESTier/issues/741).

**Architecture:** A new host-agnostic `KeylessViewRegistry` (in `Microsoft.Restier.Core`) maps function-import name → `(CLR type, Func<object, IQueryable>)`. The EFCore partial of `EFModelBuilder` detects keyless types via `FindPrimaryKey() == null`, registers them as `ComplexType<T>`, adds the function import on the EDM container (in a `<namespace>.Views` sub-namespace to avoid colliding with the ComplexType name), and `Register(...)`s them in the registry. The EF6 partial of `EFModelBuilder` throws an early `InvalidOperationException` for any entity set with empty `KeyProperties` so EF6 users get a clear "not supported" signal. `RestierODataOptionsExtensions.AddRestierRoute` bridges the registry across the model-building service provider's `Dispose()` boundary by capturing the populated instance locally before disposal and re-registering it into the per-route services lambda, mirroring the existing `RestierWebApiModelExtender` pattern. `RestierOperationExecutor` gets a `KeylessViewRegistry` constructor parameter; when its existing method lookup returns null, it consults the registry and returns the source factory's `IQueryable` directly (no `api.QueryAsync` — that's deferred follow-up work). AspNetCore.OData applies `$filter`/`$select`/`$orderby`/`$top`/`$skip` to the returned queryable at the OData layer. Writes return 405 via guards in `RestierController.Post` / `Delete` / `Update`.

**Tech Stack:** C# (.NET 8/9/10), Microsoft.OData.Edm (`EdmModel`, `EdmComplexType`, `EdmFunction`, `EdmEntityContainer.AddFunctionImport`), Microsoft.OData.ModelBuilder 2.x (`ODataConventionModelBuilder.ComplexType<T>`), Microsoft.AspNetCore.OData 9.x, Entity Framework 6.5.x (`IObjectContextAdapter`, `ObjectContext.CreateQuery<T>`), Entity Framework Core 8/9/10 (`IEntityType.FindPrimaryKey()`), xUnit v3, AwesomeAssertions (imported as `FluentAssertions`), NSubstitute, DotNetDocs SDK + Mintlify MDX.

**Spec:** `docs/superpowers/specs/2026-05-19-keyless-views-design.md`.

---

## Conventions

- **Targets:** net8.0, net9.0, net10.0 (solution-wide; EF6 packages add net48 separately but production code we touch is multi-TFM).
- **Brace style:** Allman. `var` preferred. Curly braces even for single-line blocks.
- **Warnings as errors:** enabled globally — code must be warning-clean.
- **Implicit usings disabled:** every `using` directive must be explicit.
- **Tabs** for indentation in every file you create or edit (existing convention; check each file).
- **Test framework:** xUnit v3 (`[Fact]`, `[Theory]`, `[InlineData]`), AwesomeAssertions (`Should()`), NSubstitute.
- **Commits:** small and focused; one per task. End each commit message with `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>`.
- **EF6 / EF Core symmetry:** the shared file in `src/Microsoft.Restier.EntityFramework.Shared/` is compiled by both EF6 (`Microsoft.Restier.EntityFramework`) and EFCore (`Microsoft.Restier.EntityFrameworkCore`) projects. Each EF flavour also has a partial class in its own project. Changes to the shared partial affect both flavours; verify by building both.

---

## File Inventory

| File | Action | Purpose |
|------|--------|---------|
| `src/Microsoft.Restier.Core/Model/KeylessViewRegistry.cs` | Create | New host-agnostic registry. `Register(name, clrType, factory)` / `TryGet(name, out entry)`. Throws on duplicate name. |
| `src/Microsoft.Restier.Core/Model/KeylessViewEntry.cs` | Create | DTO holding name, CLR type, source factory. |
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` | Modify | Lifetime bridge: register `KeylessViewRegistry` in `modelBuildingServices`, capture after `GetEdmModel`, re-register inside `AddRouteComponents`. |
| `src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs` | Modify | Take `KeylessViewRegistry` ctor param. `BuildEdmModelFromEntitySetMaps` splits `entitySetMap` into keyed and keyless dictionaries, registers keyless as `ComplexType<T>`, adds function imports (in a `<namespace>.Views` sub-namespace), populates registry. |
| `src/Microsoft.Restier.EntityFrameworkCore/Model/EFModelBuilder.cs` | Modify | `EntityFrameworkCoreGetEntities` also emits `Dictionary<string, Func<object, IQueryable>>` of source factories keyed by DbSet property name (reflection on the DbSet property). Adjust shared method signature. |
| `src/Microsoft.Restier.EntityFramework/Model/EfModelBuilder.cs` | Modify | EF6 throws an explicit `InvalidOperationException` on any entity set with empty `KeyProperties` (keyless not supported on EF6 — code-first model validation rejects, EDMX path out of scope). Shared `GetEdmModel` provides an empty `sourceFactoryMap` for the EF6 branch. |
| `src/Microsoft.Restier.AspNetCore/Operation/RestierOperationExecutor.cs` | Modify | Add `KeylessViewRegistry` ctor parameter. After reflective method lookup returns null, consult registry; on hit, return `entry.SourceFactory(api)` as `IQueryable`. |
| `src/Microsoft.Restier.AspNetCore/RestierController.cs` | Modify | Add `OperationImportSegment + IsFunctionImport` 405 guards to `Delete` and the private `Update` method (POST already had the guard). |
| `test/Microsoft.Restier.Tests.Core/Model/KeylessViewRegistryTests.cs` | Create | Unit tests for `Register` / `TryGet` / duplicate-throws. |
| `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/BooksByPublisher.cs` | Create | View CLR type (TFM-agnostic, in shared Library scenario). |
| `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryContext.cs` | Modify | Under `#if EFCore`, add `DbSet<BooksByPublisher>` + fluent `HasNoKey().ToView("BooksByPublisher")`. |
| `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs` | Modify | Under `#if EFCore`, after the main seed, run `CREATE VIEW BooksByPublisher` via `ExecuteSqlRaw` (guarded by `IsRelational()` so in-memory tests aren't tripped). |
| `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsApi.cs` | Create | Thin `EntityFrameworkApi<LibraryContext>` derived class hosting the instrumented `OnFilteringBooksByPublisher` probe. |
| `test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelBuilderTests.cs` | Modify | Flip the existing keyless test from "throws" to "produces ComplexType + FunctionImport". Add mixed-model test. |
| `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/EFCore/Issue741_KeylessViews.cs` | Create | EFCore end-to-end: GET rows, $filter, OnFiltering doesn't fire, write verbs return 405. |
| `test/Microsoft.Restier.Tests.AspNetCore/Baselines/LibraryApi-EFCore-ApiMetadata.txt` | Modify | Refresh baseline to include the new ComplexType + FunctionImport + Views schema. |
| `test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial/EFModelBuilderSpatialIntegrationTests.cs` | Modify | Update the two direct `new EFModelBuilder<>(...)` call sites to pass a fresh `KeylessViewRegistry`. |
| `test/Microsoft.Restier.Tests.AspNetCore/Operation/RestierOperationExecutorTests.cs` | Modify | Update the executor-construction helper to pass a fresh `KeylessViewRegistry`. |
| `src/Microsoft.Restier.Docs/guides/server/keyless-views.mdx` | Create | New user-facing docs page. |
| `src/Microsoft.Restier.Docs/guides/server/model-building.mdx` | Modify | Cross-link to keyless-views page. |
| `src/Microsoft.Restier.Docs/guides/server/operations.mdx` | Modify | Note about auto-generated function imports. |
| `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` | Modify | Add `<MintlifyTemplate>` entry for the new page. |
| `src/Microsoft.Restier.Docs/docs.json` | Regenerate (build) | Do not hand-edit; the SDK rewrites it from the template. |
| `src/Microsoft.Restier.Docs/release-notes/<latest>.mdx` | Modify (or create the next entry) | Summarise the new capability and v1 limitations. |

---

## Phase 1 — Foundation: KeylessViewRegistry + lifetime bridge

### Task 1: Create `KeylessViewEntry` DTO

**Files:**
- Create: `src/Microsoft.Restier.Core/Model/KeylessViewEntry.cs`

- [ ] **Step 1: Write the file**

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// A single entry in the <see cref="KeylessViewRegistry"/>. Carries enough information to
    /// dispatch a request for a keyless-view function import back to its underlying IQueryable
    /// source at request time.
    /// </summary>
    public sealed class KeylessViewEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeylessViewEntry"/> class.
        /// </summary>
        /// <param name="functionImportName">The unbound function-import name as it appears in $metadata.</param>
        /// <param name="clrType">The CLR type of the view's element (registered as an EDM ComplexType).</param>
        /// <param name="sourceFactory">Builds an <see cref="IQueryable"/> over the underlying view, given the live API instance.</param>
        public KeylessViewEntry(string functionImportName, Type clrType, Func<object, IQueryable> sourceFactory)
        {
            Ensure.NotNullOrWhiteSpace(functionImportName, nameof(functionImportName));
            Ensure.NotNull(clrType, nameof(clrType));
            Ensure.NotNull(sourceFactory, nameof(sourceFactory));

            FunctionImportName = functionImportName;
            ClrType = clrType;
            SourceFactory = sourceFactory;
        }

        /// <summary>
        /// Gets the unbound function-import name as it appears in <c>$metadata</c>.
        /// </summary>
        public string FunctionImportName { get; }

        /// <summary>
        /// Gets the CLR type of the view's element (registered as an EDM <c>ComplexType</c>).
        /// </summary>
        public Type ClrType { get; }

        /// <summary>
        /// Gets the factory that builds an <see cref="IQueryable"/> over the underlying view.
        /// </summary>
        /// <remarks>
        /// The argument is the live API instance (cast to <c>IEntityFrameworkApi</c> by EF-flavour factories).
        /// </remarks>
        public Func<object, IQueryable> SourceFactory { get; }
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build src/Microsoft.Restier.Core/Microsoft.Restier.Core.csproj`
Expected: success, no warnings.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.Core/Model/KeylessViewEntry.cs
git commit -m "feat(core): add KeylessViewEntry DTO for keyless-view dispatch

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Create `KeylessViewRegistry` with TDD

**Files:**
- Create: `src/Microsoft.Restier.Core/Model/KeylessViewRegistry.cs`
- Test: `test/Microsoft.Restier.Tests.Core/Model/KeylessViewRegistryTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Restier.Core.Model;
using Xunit;

namespace Microsoft.Restier.Tests.Core.Model;

public class KeylessViewRegistryTests
{
    [Fact]
    public void Register_StoresEntry_RetrievableByName()
    {
        var registry = new KeylessViewRegistry();
        Func<object, IQueryable> factory = _ => Enumerable.Empty<string>().AsQueryable();

        registry.Register("MyView", typeof(string), factory);

        registry.TryGet("MyView", out var entry).Should().BeTrue();
        entry.Should().NotBeNull();
        entry.FunctionImportName.Should().Be("MyView");
        entry.ClrType.Should().Be(typeof(string));
        entry.SourceFactory.Should().BeSameAs(factory);
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForUnknownName()
    {
        var registry = new KeylessViewRegistry();

        registry.TryGet("NotRegistered", out var entry).Should().BeFalse();
        entry.Should().BeNull();
    }

    [Fact]
    public void Register_Throws_OnDuplicateName()
    {
        var registry = new KeylessViewRegistry();
        registry.Register("MyView", typeof(string), _ => Enumerable.Empty<string>().AsQueryable());

        var act = () => registry.Register("MyView", typeof(int), _ => Enumerable.Empty<int>().AsQueryable());

        act.Should().Throw<InvalidOperationException>()
            .Where(e => e.Message.Contains("MyView"));
    }

    [Fact]
    public void Register_RejectsNullName()
    {
        var registry = new KeylessViewRegistry();
        var act = () => registry.Register(null, typeof(string), _ => Enumerable.Empty<string>().AsQueryable());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Register_RejectsNullType()
    {
        var registry = new KeylessViewRegistry();
        var act = () => registry.Register("X", null, _ => Enumerable.Empty<string>().AsQueryable());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Register_RejectsNullFactory()
    {
        var registry = new KeylessViewRegistry();
        var act = () => registry.Register("X", typeof(string), null);
        act.Should().Throw<ArgumentNullException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~KeylessViewRegistryTests"`
Expected: FAIL — `KeylessViewRegistry` does not exist.

- [ ] **Step 3: Implement the registry**

Create `src/Microsoft.Restier.Core/Model/KeylessViewRegistry.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// Maps an unbound function-import name to the CLR type and source factory needed to dispatch
    /// a request for a keyless EF view (or other ComplexType-backed read-only collection).
    /// </summary>
    /// <remarks>
    /// Populated by <c>EFModelBuilder</c> during model construction inside the temporary
    /// model-building service provider used by <c>RestierODataOptionsExtensions.AddRestierRoute</c>.
    /// The populated instance is captured locally before that service provider is disposed and
    /// re-registered into the per-route services lambda, so request-time consumers
    /// (notably <c>RestierOperationExecutor</c>) resolve the same populated instance.
    /// </remarks>
    public sealed class KeylessViewRegistry
    {
        private readonly ConcurrentDictionary<string, KeylessViewEntry> entries
            = new(StringComparer.Ordinal);

        /// <summary>
        /// Registers a keyless view's dispatch metadata. Throws if <paramref name="functionImportName"/>
        /// has already been registered.
        /// </summary>
        public void Register(string functionImportName, Type clrType, Func<object, IQueryable> sourceFactory)
        {
            Ensure.NotNullOrWhiteSpace(functionImportName, nameof(functionImportName));
            Ensure.NotNull(clrType, nameof(clrType));
            Ensure.NotNull(sourceFactory, nameof(sourceFactory));

            var entry = new KeylessViewEntry(functionImportName, clrType, sourceFactory);
            if (!entries.TryAdd(functionImportName, entry))
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "A keyless view named '{0}' is already registered.",
                    functionImportName));
            }
        }

        /// <summary>
        /// Attempts to find the dispatch metadata for an unbound function-import name.
        /// </summary>
        public bool TryGet(string functionImportName, out KeylessViewEntry entry)
        {
            if (string.IsNullOrEmpty(functionImportName))
            {
                entry = null;
                return false;
            }

            return entries.TryGetValue(functionImportName, out entry);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~KeylessViewRegistryTests"`
Expected: 6 passed, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Core/Model/KeylessViewRegistry.cs test/Microsoft.Restier.Tests.Core/Model/KeylessViewRegistryTests.cs
git commit -m "feat(core): add KeylessViewRegistry with duplicate-name guard

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Lifetime bridge in `AddRestierRoute`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs:111-181`

- [ ] **Step 1: Add registry to model-building services and capture after model build**

In `AddRestierRoute`, around line 115 (where the model-building services are populated), add the registry:

```csharp
modelBuildingServices.AddSingleton(typeof(RestierNamingConvention), (object)namingConvention);
modelBuildingServices.AddSingleton<KeylessViewRegistry>();
modelBuildingServices.AddSingleton< IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
    .AddSingleton(new RestierWebApiModelExtender(type))
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new RestierWebApiOperationModelBuilder(type, sp.GetRequiredService<RestierWebApiModelExtender>()))
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new ConventionBasedAnnotationModelBuilder(type));

IEdmModel model;
RestierWebApiModelExtender modelExtender;
KeylessViewRegistry keylessViewRegistry;
ServiceProvider modelBuildingServiceProvider = null;

try
{
    modelBuildingServiceProvider = modelBuildingServices.BuildServiceProvider();
    var modelBuilderFactory = modelBuildingServiceProvider
        .GetRequiredService<IChainOfResponsibilityFactory<IModelBuilder>>();
    var modelBuilder = modelBuilderFactory.Create();
    model = modelBuilder.GetEdmModel();
    modelExtender = modelBuildingServiceProvider.GetRequiredService<RestierWebApiModelExtender>();
    keylessViewRegistry = modelBuildingServiceProvider.GetRequiredService<KeylessViewRegistry>();
}
catch (Exception exception)
{
    throw new InvalidOperationException($"Model building failed with exception {exception.Message}", exception);
}
finally
{
    modelBuildingServiceProvider?.Dispose();
}
```

- [ ] **Step 2: Add `using` for `Microsoft.Restier.Core.Model`**

At the top of the file, add:

```csharp
using Microsoft.Restier.Core.Model;
```

- [ ] **Step 3: Re-register the captured instance into the route services**

In the `oDataOptions.AddRouteComponents` services lambda (around line 181 where `modelExtender` is re-registered), add the registry:

```csharp
services.AddSingleton<IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
    .AddSingleton(modelExtender)
    .AddSingleton(keylessViewRegistry)
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new RestierWebApiOperationModelBuilder(type, sp.GetRequiredService<RestierWebApiModelExtender>()))
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new ConventionBasedAnnotationModelBuilder(type))
    .AddSingleton<IChainedService<IModelMapper>, RestierWebApiModelMapper>()
    .AddSingleton<IChainedService<IQueryExpressionExpander>, RestierQueryExpressionExpander>()
    .AddSingleton<IChainedService<IQueryExpressionSourcer>, RestierQueryExpressionSourcer>();
```

- [ ] **Step 4: Build**

Run: `dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj`
Expected: success, no warnings.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs
git commit -m "feat(aspnetcore): bridge KeylessViewRegistry across model-build SP

Registers KeylessViewRegistry in the temporary model-building service
provider, captures the populated instance before disposal, then
re-registers the same instance into the per-route services lambda.
Mirrors the existing RestierWebApiModelExtender pattern.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 2 — Shared model builder + EFCore partial

### Task 4: Pass `KeylessViewRegistry` into shared `EFModelBuilder`

**Files:**
- Modify: `src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs`

- [ ] **Step 1: Add `KeylessViewRegistry` constructor parameter**

The current ctor signature is at line 51. Update to:

```csharp
public EFModelBuilder(
    TDbContext dbContext,
    ModelMerger modelMerger,
    KeylessViewRegistry keylessViewRegistry,
    RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase,
    IEnumerable<ISpatialModelMetadataProvider> spatialMetadataProviders = null)
{
    Ensure.NotNull(dbContext, nameof(dbContext));
    Ensure.NotNull(modelMerger, nameof(modelMerger));
    Ensure.NotNull(keylessViewRegistry, nameof(keylessViewRegistry));
    this._dbContext = dbContext;
    this._modelMerger = modelMerger;
    this._keylessViewRegistry = keylessViewRegistry;
    this._namingConvention = namingConvention;
    this._spatialConvention = new SpatialModelConvention(spatialMetadataProviders);
}
```

Add the field at line 38:

```csharp
private readonly KeylessViewRegistry _keylessViewRegistry;
```

Add the using at the top:

```csharp
using Microsoft.Restier.Core.Model;
```

- [ ] **Step 2: Update the two direct call sites in the EFCore Spatial integration tests**

`EFModelBuilder<TDbContext>` is constructed directly (no DI) in two places. Open `test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial/EFModelBuilderSpatialIntegrationTests.cs`.

At line 42, replace:

```csharp
var builder = new EFModelBuilder<IntegrationContext>(ctx, modelMerger, RestierNamingConvention.PascalCase, providers);
```

with:

```csharp
var builder = new EFModelBuilder<IntegrationContext>(ctx, modelMerger, new KeylessViewRegistry(), RestierNamingConvention.PascalCase, providers);
```

At line 61, replace:

```csharp
var builder = new EFModelBuilder<IntegrationContext>(ctx, modelMerger);
```

with:

```csharp
var builder = new EFModelBuilder<IntegrationContext>(ctx, modelMerger, new KeylessViewRegistry());
```

Add `using Microsoft.Restier.Core.Model;` to the file's usings.

These are the only two non-DI call sites in the repository (verified by `grep "new EFModelBuilder"` — only these two files match).

- [ ] **Step 3: Build both EF projects**

Run: `dotnet build src/Microsoft.Restier.EntityFrameworkCore/Microsoft.Restier.EntityFrameworkCore.csproj src/Microsoft.Restier.EntityFramework/Microsoft.Restier.EntityFramework.csproj test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial.csproj`
Expected: success. DI will resolve `KeylessViewRegistry` because we registered it in Task 3.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial/EFModelBuilderSpatialIntegrationTests.cs
git commit -m "feat(ef): inject KeylessViewRegistry into shared EFModelBuilder

Updates the two direct-construction call sites in the EFCore Spatial
integration tests to pass a fresh KeylessViewRegistry. Production code
gets the registry via DI through the lifetime bridge in
AddRestierRoute (see prior commit).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Source-factory pipe in EFCore partial

**Files:**
- Modify: `src/Microsoft.Restier.EntityFrameworkCore/Model/EFModelBuilder.cs`

- [ ] **Step 1: Change `EntityFrameworkCoreGetEntities` signature to emit factories**

Current signature returns `entitySetMap` and `entitySetKeyMap`. Change to also return a factories dictionary:

```csharp
private void EntityFrameworkCoreGetEntities(
    out Dictionary<string, Type> entitySetMap,
    out Dictionary<Type, ICollection<PropertyInfo>> entitySetKeyMap,
    out Dictionary<string, Func<object, IQueryable>> sourceFactoryMap)
{
    // @robertmclaws: Validate that no Owned Types are mapped to DbSet<>. If there are, EFCore calls to GetModel will fail.
    var ownedTypes = _dbContext.Model.GetEntityTypes().Where(c => c.IsOwned()).ToList();
    var dbSetMappedTypes = ownedTypes.Where(c => _dbContext.IsDbSetMapped(c.ClrType)).ToList();

    if (dbSetMappedTypes.Count > 0)
    {
        throw new EdmModelValidationException($"The '{_dbContext.GetType().Name}' DbContext has 'Owned Types' (the EFCore equivalent of EF6's 'Complex Types') mapped to DbSets. " +
                                              $"You must remove the following DbSet mappings for EFCore to function properly with Restier: {string.Join(",", dbSetMappedTypes.Select(c => c.ShortName()))}");
    }

    // Map { DbSet property name -> CLR type }.
    var dbSetProperties = _dbContext.GetType().GetProperties()
        .Where(e => e.PropertyType.FindGenericType(typeof(DbSet<>)) is not null)
        .ToList();

    entitySetMap = dbSetProperties.ToDictionary(e => e.Name, e => e.PropertyType.GetGenericArguments()[0]);

    // Map { entity-set name -> source factory } via reflection on the DbSet property captured here.
    sourceFactoryMap = dbSetProperties.ToDictionary(
        p => p.Name,
        p =>
        {
            var capturedProp = p;
            Func<object, IQueryable> factory = api =>
            {
                var ctx = ((IEntityFrameworkApi)api).DbContext;
                return (IQueryable)capturedProp.GetValue(ctx);
            };
            return factory;
        });

    entitySetKeyMap = _dbContext.Model.GetEntityTypes().Where(c => !c.IsOwned() && !IsImplicitManyToManyJoinEntity(c)).ToDictionary(
                    e => e.ClrType,
                    e => ((ICollection<PropertyInfo>)e.FindPrimaryKey()?.Properties.Select(p => e.ClrType?.GetProperty(p.Name)).ToList()));
}
```

- [ ] **Step 2: Build**

Run: `dotnet build src/Microsoft.Restier.EntityFrameworkCore/Microsoft.Restier.EntityFrameworkCore.csproj`
Expected: success **except** for the shared `EFModelBuilder.GetEdmModel` call site, which still calls the 2-out version. We fix that in Task 6.

- [ ] **Step 3: Adjust the call site in shared `GetEdmModel`**

Open `src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs` and update the `GetEdmModel` method (around line 71). The `#if EFCore` branch becomes:

```csharp
#if EFCore
    EntityFrameworkCoreGetEntities(out var entitySetMap, out var entitySetKeyMap, out var sourceFactoryMap);
#endif
#if EF6
    EntityFramework6GetEntitySets(out var entitySetMap, out var entitySetKeyMap, out var sourceFactoryMap);
#endif
```

And the `BuildEdmModelFromEntitySetMaps` call (around line 85) becomes:

```csharp
var result = BuildEdmModelFromEntitySetMaps(entitySetMap, entitySetKeyMap, sourceFactoryMap, _namingConvention, _spatialConvention, _dbContext, _keylessViewRegistry);
```

We'll wire EF6's signature in Phase 4. For now the EF6 build will break — that's expected and resolved later.

- [ ] **Step 4: Build EFCore only**

Run: `dotnet build src/Microsoft.Restier.EntityFrameworkCore/Microsoft.Restier.EntityFrameworkCore.csproj`
Expected: success.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.EntityFrameworkCore/Model/EFModelBuilder.cs src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs
git commit -m "feat(efcore): emit source-factory map alongside entity-set maps

Pipes a Dictionary<string, Func<object, IQueryable>> from the EFCore
partial through the shared GetEdmModel; consumed by the keyless branch
added in the next task.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Detect keyless, demote to ComplexType + FunctionImport, register

**Files:**
- Modify: `src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs`

- [ ] **Step 1: Update the shared `BuildEdmModelFromEntitySetMaps` to accept the new inputs and split keyed vs keyless**

Replace the method body. The current method starts at line 96; replace it entirely with:

```csharp
private static EdmModel BuildEdmModelFromEntitySetMaps(
    Dictionary<string, Type> entitySetMap,
    Dictionary<Type, ICollection<PropertyInfo>> entitySetKeyMap,
    Dictionary<string, Func<object, IQueryable>> sourceFactoryMap,
    RestierNamingConvention namingConvention,
    SpatialModelConvention spatialConvention,
    object spatialProviderContext,
    KeylessViewRegistry keylessViewRegistry)
{
    if (!entitySetMap.Any())
    {
        return new EdmModel();
    }

    // Split: keyed entity sets become EntitySet<T>; keyless DbSets/EntitySets become ComplexType<T> + FunctionImport.
    // A type is keyless if its key collection is null OR empty (EF Core reports null, EF6 reports an empty list).
    var keyedEntitySets = new Dictionary<string, Type>();
    var keylessViewSets = new Dictionary<string, Type>();
    foreach (var pair in entitySetMap)
    {
        var keyList = entitySetKeyMap.TryGetValue(pair.Value, out var keys) ? keys : null;
        if (keyList is null || keyList.Count == 0)
        {
            keylessViewSets.Add(pair.Key, pair.Value);
        }
        else
        {
            keyedEntitySets.Add(pair.Key, pair.Value);
        }
    }

    var builder = new ODataConventionModelBuilder
    {
        // This namespace is used by container
        Namespace = entitySetMap.First().Value.Namespace
    };

    var entitySetMethod = typeof(ODataConventionModelBuilder).GetMethod("EntitySet", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
    var complexTypeMethod = typeof(ODataConventionModelBuilder).GetMethod("ComplexType", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy, Type.EmptyTypes);

    foreach (var pair in keyedEntitySets)
    {
        var specifiedMethod = entitySetMethod.MakeGenericMethod(pair.Value);
        var parameters = new object[] { pair.Key };
        specifiedMethod.Invoke(builder, parameters);
    }

    foreach (var pair in keylessViewSets)
    {
        var specifiedMethod = complexTypeMethod.MakeGenericMethod(pair.Value);
        specifiedMethod.Invoke(builder, Array.Empty<object>());
    }

    foreach (var pair in entitySetKeyMap)
    {
        if (builder.GetTypeConfigurationOrNull(pair.Key) is not EntityTypeConfiguration edmTypeConfiguration)
        {
            continue;
        }

        if (pair.Value is null || pair.Value.Count == 0)
        {
            // Keyless types are handled above (registered as ComplexType, not EntityType).
            continue;
        }

        foreach (var property in pair.Value)
        {
            edmTypeConfiguration.HasKey(property);
        }
    }
    switch (namingConvention)
    {
        case RestierNamingConvention.LowerCamelCase:
            builder.EnableLowerCamelCase();
            break;
        case RestierNamingConvention.LowerCamelCaseWithEnumMembers:
            builder.EnableLowerCamelCaseForPropertiesAndEnums();
            break;
    }

    var entityClrTypes = entitySetMap.Values.Distinct().ToList();
    var spatialCaptures = spatialConvention.CapturePhase(builder, entityClrTypes, spatialProviderContext);

    var edmModel = (EdmModel)builder.GetEdmModel();

    spatialConvention.AugmentPhase(edmModel, spatialCaptures, namingConvention);

    AddKeylessViewFunctionImports(edmModel, keylessViewSets, sourceFactoryMap, keylessViewRegistry);

    return edmModel;
}

private static void AddKeylessViewFunctionImports(
    EdmModel edmModel,
    Dictionary<string, Type> keylessViewSets,
    Dictionary<string, Func<object, IQueryable>> sourceFactoryMap,
    KeylessViewRegistry keylessViewRegistry)
{
    if (keylessViewSets.Count == 0)
    {
        return;
    }

    var container = edmModel.EntityContainer as EdmEntityContainer
        ?? throw new InvalidOperationException("Keyless view registration requires a writable EdmEntityContainer.");

    foreach (var pair in keylessViewSets)
    {
        var viewName = pair.Key;
        var clrType = pair.Value;
        var edmComplexType = edmModel.SchemaElements.OfType<IEdmComplexType>().FirstOrDefault(c => c.Name == clrType.Name)
            ?? throw new InvalidOperationException(
                $"Could not find ComplexType '{clrType.Name}' in the EDM model for keyless view '{viewName}'.");

        var complexTypeReference = new EdmComplexTypeReference(edmComplexType, isNullable: false);
        var collectionTypeReference = new EdmCollectionTypeReference(new EdmCollectionType(complexTypeReference));

        var function = new EdmFunction(
            container.Namespace,
            viewName,
            collectionTypeReference,
            isBound: false,
            entitySetPathExpression: null,
            isComposable: false);

        edmModel.AddElement(function);
        container.AddFunctionImport(viewName, function, entitySet: null);

        if (!sourceFactoryMap.TryGetValue(viewName, out var sourceFactory))
        {
            throw new InvalidOperationException(
                $"No source factory was supplied for keyless view '{viewName}'. " +
                $"This is an internal bug in the EF model builder.");
        }

        keylessViewRegistry.Register(viewName, clrType, sourceFactory);
    }
}
```

- [ ] **Step 2: Ensure the `using`s cover the new types**

At the top of the file, ensure these are present (Allman-add any missing):

```csharp
using System.Collections.Generic;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core.Model;
```

- [ ] **Step 3: Build EFCore**

Run: `dotnet build src/Microsoft.Restier.EntityFrameworkCore/Microsoft.Restier.EntityFrameworkCore.csproj`
Expected: success. (EF6 still broken; we fix it in Phase 4.)

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs
git commit -m "feat(ef): demote keyless types to ComplexType + FunctionImport

Splits the EF entity-set map into keyed entity sets and keyless view
sets. Keyed entries proceed through the existing EntitySet<T> path;
keyless entries are registered as ComplexType<T>, get an unbound
FunctionImport added to the container post-build, and are recorded
in KeylessViewRegistry alongside their source factory. Empty key
lists are normalised to 'keyless' so the EF6 path lands in the
same branch.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: Flip the existing EFCore keyless test from "throws" to "produces ComplexType + FunctionImport"

**Files:**
- Modify: `test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelBuilderTests.cs`

- [ ] **Step 1: Replace the existing `EFModelBuilder_Should_HandleViews` test**

Find the test (currently asserts `ThrowAsync<InvalidOperationException>` with message containing `[Keyless]`) and replace its body:

```csharp
[Fact]
public async Task EFModelBuilder_Should_HandleViews()
{
    var metadata = await RestierTestHelpers.GetApiMetadataAsync<LibraryWithViewsApi>(
        serviceCollection: services => services.AddEFCoreProviderServices<LibraryWithViewsContext>((Action<DbContextOptionsBuilder>)null));

    metadata.Should().NotBeNull();
    var metadataString = metadata.ToString();

    // The keyless view appears as a ComplexType, not an EntityType.
    metadataString.Should().Contain("ComplexType Name=\"BooksByPublisher\"");
    metadataString.Should().NotContain("EntityType Name=\"BooksByPublisher\"");

    // And as an unbound FunctionImport returning a Collection of that ComplexType.
    metadataString.Should().Contain("FunctionImport Name=\"BooksByPublisher\"");
    metadataString.Should().MatchRegex("Function Name=\"BooksByPublisher\".*ReturnType.*Type=\"Collection\\(.*BooksByPublisher\\)\"");
}
```

- [ ] **Step 2: Add a mixed-model test**

```csharp
[Fact]
public async Task EFModelBuilder_Should_HandleMixedModel()
{
    var metadata = await RestierTestHelpers.GetApiMetadataAsync<LibraryWithViewsApi>(
        serviceCollection: services => services.AddEFCoreProviderServices<LibraryWithViewsContext>((Action<DbContextOptionsBuilder>)null));

    var metadataString = metadata.ToString();

    // Regular entity sets coexist with the keyless view.
    metadataString.Should().Contain("EntityType Name=\"Book\"");
    metadataString.Should().Contain("EntityType Name=\"Publisher\"");
    metadataString.Should().Contain("EntitySet Name=\"Books\"");
    metadataString.Should().Contain("EntitySet Name=\"Publishers\"");

    metadataString.Should().Contain("ComplexType Name=\"BooksByPublisher\"");
    metadataString.Should().Contain("FunctionImport Name=\"BooksByPublisher\"");
}
```

- [ ] **Step 3: Run the tests**

Run: `dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj --filter "FullyQualifiedName~EFModelBuilder_Should_HandleViews|FullyQualifiedName~EFModelBuilder_Should_HandleMixedModel"`
Expected: 2 passed (per TFM).

If the regex doesn't match, dump the metadata string to a file and inspect:

```csharp
System.IO.File.WriteAllText("/tmp/metadata.xml", metadataString);
```

Adjust the regex to match the actual ODL output shape.

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelBuilderTests.cs
git commit -m "test(efcore): verify keyless views become ComplexType + FunctionImport

Flips the existing 'should throw on keyless' assertion to verify the new
auto-mapping behaviour. Adds a mixed-model test asserting regular entity
sets coexist with a keyless view.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 3 — Executor dispatch + EFCore end-to-end

### Task 8: Inject `KeylessViewRegistry` into `RestierOperationExecutor`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Operation/RestierOperationExecutor.cs`

- [ ] **Step 1: Add ctor parameter, field, and dispatch fallback**

At the top of the file, add:

```csharp
using Microsoft.Restier.Core.Model;
```

Update the class field area (after line 32):

```csharp
private readonly IOperationAuthorizer operationAuthorizer;
private readonly IOperationFilter operationFilter;
private readonly KeylessViewRegistry keylessViewRegistry;
```

Update the constructor (line 39):

```csharp
public RestierOperationExecutor(
    IChainOfResponsibilityFactory<IOperationAuthorizer> operationAuthorizerFactory,
    IChainOfResponsibilityFactory<IOperationFilter> operationFilterFactory,
    KeylessViewRegistry keylessViewRegistry)
{
    Ensure.NotNull(operationAuthorizerFactory, nameof(operationAuthorizerFactory));
    Ensure.NotNull(operationFilterFactory, nameof(operationFilterFactory));
    Ensure.NotNull(keylessViewRegistry, nameof(keylessViewRegistry));

    this.operationAuthorizer = operationAuthorizerFactory.Create();
    this.operationFilter = operationFilterFactory.Create();
    this.keylessViewRegistry = keylessViewRegistry;
}
```

In `ExecuteOperationAsync`, after the existing reflective method lookup (around line 78-85), change the null branch:

```csharp
var method = context.Api.GetType().GetMethod(
    restierOperationContext.OperationName,
    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

if (method is null)
{
    // Fallback: is this an auto-generated keyless-view function import?
    if (keylessViewRegistry.TryGet(restierOperationContext.OperationName, out var viewEntry))
    {
        // Authorisation check still applies (operation-level).
        await InvokeAuthorizers(restierOperationContext, cancellationToken).ConfigureAwait(false);

        var viewQueryable = viewEntry.SourceFactory(context.Api);
        return viewQueryable;
    }

    throw new NotImplementedException(AspNetResources.OperationNotImplemented);
}
```

Note: the authorisation call is duplicated from the existing path on purpose — the existing path runs `InvokeAuthorizers` near the top of the method (line 73). We want the same authoriser check for view dispatch, but we exit before the rest of the method runs. If the authoriser was already called above this branch, do not double-invoke — instead, position the null-check BEFORE the existing `InvokeAuthorizers` call and call it only inside the early-return.

Re-read the existing method carefully and structure the changes so `InvokeAuthorizers` runs exactly once per request.

- [ ] **Step 2: Build**

Run: `dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj`
Expected: success.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Operation/RestierOperationExecutor.cs
git commit -m "feat(aspnetcore): dispatch keyless-view function imports via registry

When the reflective method lookup on the API returns null, consult
KeylessViewRegistry. On hit, invoke the source factory and return its
IQueryable directly so AspNetCore.OData can apply query options at the
OData layer. On miss, throw the existing NotImplementedException
unchanged.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 8b: Return HTTP 405 for DELETE / PUT / PATCH on function-import URLs

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

The existing controller returns 405 from `Post` for function-import paths (line 178-182) but `Delete` (line 311) and `Update` (line 435 — handles both PUT and PATCH) throw `NotImplementedException` for non-entity-set paths, which surfaces as HTTP 500. For keyless views to be honestly read-only, all four write verbs need to return 405.

- [ ] **Step 1: Add the function-import guard to `Delete`**

In `RestierController.Delete` (line 311), insert the guard immediately after `GetPath()`:

```csharp
public async Task<IActionResult> Delete(CancellationToken cancellationToken)
{
    EnsureInitialized();
    var path = GetPath();
    var lastSegment = path.Last();

    if (lastSegment is OperationSegment opSeg && opSeg.Operations.FirstOrDefault().IsFunction())
    {
        return MethodNotAllowed();
    }

    if (lastSegment is OperationImportSegment opImpSeg && opImpSeg.OperationImports.FirstOrDefault().IsFunctionImport())
    {
        return MethodNotAllowed();
    }

    if (path.NavigationSource() is not IEdmEntitySet entitySet)
    {
        throw new NotImplementedException(Resources.DeleteOnlySupportedOnEntitySet);
    }
    // ... existing body continues unchanged ...
}
```

- [ ] **Step 2: Add the same guard to `Update`**

In the private `Update` method (line 435 — called by both PUT and PATCH endpoints), insert immediately after `GetPath()`:

```csharp
private async Task<IActionResult> Update(
    EdmEntityObject edmEntityObject,
    bool isFullReplaceUpdate,
    CancellationToken cancellationToken)
{
    var path = GetPath();
    var lastSegment = path.Last();

    if (lastSegment is OperationSegment opSeg && opSeg.Operations.FirstOrDefault().IsFunction())
    {
        return MethodNotAllowed();
    }

    if (lastSegment is OperationImportSegment opImpSeg && opImpSeg.OperationImports.FirstOrDefault().IsFunctionImport())
    {
        return MethodNotAllowed();
    }

    var entitySet = path.NavigationSource() as IEdmEntitySet;
    if (entitySet is null)
    {
        throw new NotImplementedException(Resources.UpdateOnlySupportedOnEntitySet);
    }
    // ... existing body continues unchanged ...
}
```

- [ ] **Step 3: Build**

Run: `dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj`
Expected: success.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/RestierController.cs
git commit -m "feat(aspnetcore): return 405 for DELETE/PUT/PATCH on function imports

Mirrors the existing 405 branch in Post. Without this, DELETE/PUT/PATCH
on a function-import URL (e.g. a keyless-view import) threw
NotImplementedException, surfacing as HTTP 500. Now all four write verbs
return 405 Method Not Allowed consistently — the desired UX for a
read-only resource.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 9: Move EFCore view test fixtures into `Tests.Shared.EntityFrameworkCore`

**Files:**
- Create: `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Views/BooksByPublisher.cs`
- Create: `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsContext.cs`
- Create: `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsApi.cs`
- Delete: `test/Microsoft.Restier.Tests.EntityFrameworkCore/Scenarios/Views/BooksByPublisher.cs`
- Delete: `test/Microsoft.Restier.Tests.EntityFrameworkCore/Scenarios/Views/LibaryWithViewsContext.cs`
- Delete: `test/Microsoft.Restier.Tests.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsApi.cs`

- [ ] **Step 1: Create the moved view CLR type**

```csharp
// test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Views/BooksByPublisher.cs
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views
{
    [Keyless]
    public partial class BooksByPublisher
    {
        // Publisher.Id is a string in the shared Library fixture (e.g. "Publisher1").
        public string PublisherId { get; set; }
        public string BookName { get; set; }
        public int BookCount { get; set; }
    }
}
```

- [ ] **Step 2: Create the moved DbContext (real-SQL, NOT in-memory) with a CREATE VIEW seed**

```csharp
// test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsContext.cs
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views
{
    /// <summary>
    /// LibraryContext + a single keyless view (BooksByPublisher) for keyless-view tests.
    /// </summary>
    public class LibraryWithViewsContext : LibraryContext
    {
        public virtual DbSet<BooksByPublisher> BooksByPublisher { get; set; }

        public LibraryWithViewsContext(DbContextOptions<LibraryWithViewsContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<BooksByPublisher>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("BooksByPublisher");
            });
        }
    }
}
```

- [ ] **Step 3: Create the API class with a *probe* `OnFilteringBooksByPublisher` for the v1-limitation test**

```csharp
// test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsApi.cs
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views
{
    public class LibraryWithViewsApi : EntityFrameworkApi<LibraryWithViewsContext>
    {
        /// <summary>
        /// Static counter incremented when the convention processor invokes this method.
        /// In v1 it stays at 0; flipping when the follow-up lands will be a deliberate test change.
        /// </summary>
        public static int OnFilteringBooksByPublisherCallCount;

        public LibraryWithViewsApi(LibraryWithViewsContext dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(dbContext, model, queryHandler, submitHandler)
        {
        }

        protected internal IQueryable<BooksByPublisher> OnFilteringBooksByPublisher(IQueryable<BooksByPublisher> entitySet)
        {
            System.Threading.Interlocked.Increment(ref OnFilteringBooksByPublisherCallCount);
            return entitySet;
        }
    }
}
```

- [ ] **Step 4: Delete the old files in `Tests.EntityFrameworkCore`**

```bash
git rm test/Microsoft.Restier.Tests.EntityFrameworkCore/Scenarios/Views/BooksByPublisher.cs
git rm test/Microsoft.Restier.Tests.EntityFrameworkCore/Scenarios/Views/LibaryWithViewsContext.cs
git rm test/Microsoft.Restier.Tests.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsApi.cs
```

- [ ] **Step 5: Update the existing EFModelBuilderTests to use the new namespace**

Open `test/Microsoft.Restier.Tests.EntityFrameworkCore/EFModelBuilderTests.cs` and update the `using`:

```csharp
// Replace:
using Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.Views;
// With:
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views;
```

- [ ] **Step 6: Build both EFCore test projects**

Run: `dotnet build test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Microsoft.Restier.Tests.Shared.EntityFrameworkCore.csproj`
Expected: success.

- [ ] **Step 7: Re-run Task 7 tests to verify they still pass after the move**

Run: `dotnet test test/Microsoft.Restier.Tests.EntityFrameworkCore/Microsoft.Restier.Tests.EntityFrameworkCore.csproj --filter "FullyQualifiedName~EFModelBuilder_Should_HandleViews|FullyQualifiedName~EFModelBuilder_Should_HandleMixedModel"`
Expected: 2 passed per TFM.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(tests): move LibraryWithViews fixtures to shared

Promotes BooksByPublisher, LibraryWithViewsContext, and LibraryWithViewsApi
into Tests.Shared.EntityFrameworkCore so the AspNetCore regression tests
can reference them. Replaces the previous in-memory DbContext with a
relational one for the upcoming end-to-end tests. Adds an instrumented
OnFilteringBooksByPublisher method to assert the v1 limitation.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 9b: Wire `LibraryWithViewsContext` seeding into the shared EF test helper

**Files:**
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Extensions/EntityFrameworkServiceCollectionExtensions.cs` (both `#if EF6` and `#if EFCore` blocks)

The existing helper only seeds `LibraryContext` or `MarvelContext` by literal type comparison. Without a branch for `LibraryWithViewsContext` the end-to-end tests get an empty database and the view DDL never runs.

- [ ] **Step 1: Create EFCore `LibraryWithViewsTestInitializer`**

Create `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsTestInitializer.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views
{
    /// <summary>
    /// Reuses LibraryTestInitializer to populate publishers/books, then creates the
    /// BooksByPublisher SQL view on top of the seeded data.
    /// </summary>
    public class LibraryWithViewsTestInitializer : IDatabaseInitializer
    {
        public void Seed(DbContext dbContext)
        {
            // Seed publishers + books via the base initialiser (same data the
            // LibraryContext tests use).
            new LibraryTestInitializer().Seed(dbContext);

            // Create the view on top. ExecuteSqlRaw because DbContext.Database
            // doesn't expose a CREATE VIEW API.
            dbContext.Database.ExecuteSqlRaw(@"
                IF OBJECT_ID('BooksByPublisher', 'V') IS NOT NULL DROP VIEW BooksByPublisher;
                EXEC('CREATE VIEW BooksByPublisher AS
                       SELECT p.Id AS PublisherId,
                              b.Title AS BookName,
                              CAST(COUNT(b.Id) OVER(PARTITION BY p.Id) AS INT) AS BookCount
                       FROM Publishers p
                       INNER JOIN Books b ON b.PublisherId = p.Id;');
            ");
        }
    }
}
```

(Verify the existing `IDatabaseInitializer` shape and `LibraryTestInitializer.Seed` signature; adjust the override accordingly.)

- [ ] **Step 2: Add the EFCore branch to the helper**

Open `test/Microsoft.Restier.Tests.Shared.EntityFramework/Extensions/EntityFrameworkServiceCollectionExtensions.cs`.

In the `#if EFCore` block, after the existing `MarvelContext` branch (line ~185), add:

```csharp
else if (typeof(TDbContext) == typeof(LibraryWithViewsContext))
{
    services.SeedDatabase<LibraryWithViewsContext, LibraryWithViewsTestInitializer>();
}
```

Add the using at the top of the EFCore section:

```csharp
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views;
```

- [ ] **Step 3: Add the EF6 branch to the helper**

In the `#if EF6` block, the seeding model is different — EF6 uses `Database.SetInitializer` on the context itself. The `LibraryWithViewsContext` (created in Task 12) already sets `LibraryWithViewsTestInitializer` in its constructor, which runs on first connection and creates the view. So the EF6 path needs no explicit `else if` branch — the existing `services.AddEF6ProviderServices<TDbContext>(builder.ConnectionString)` line picks up the initialiser automatically.

However, the `SeedDatabase<TContext>(connectionString)` call (line ~90 in EF6 block) is currently called unconditionally for *every* TDbContext and uses `Activator.CreateInstance(typeof(TContext), connectionString)`. Verify that `LibraryWithViewsContext`'s `(string)` constructor exists and is reachable. If it doesn't, add it (Task 12 already includes this constructor).

No additional EF6 branch is needed if the constructor pattern matches. If it doesn't, add the same shape:

```csharp
// EF6 path — only if SeedDatabase doesn't already handle it
```

- [ ] **Step 4: Build all touched projects**

Run: `dotnet build test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Microsoft.Restier.Tests.Shared.EntityFrameworkCore.csproj test/Microsoft.Restier.Tests.Shared.EntityFramework/Microsoft.Restier.Tests.Shared.EntityFramework.csproj`
Expected: success.

- [ ] **Step 5: Commit**

```bash
git add test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Views/LibraryWithViewsTestInitializer.cs test/Microsoft.Restier.Tests.Shared.EntityFramework/Extensions/EntityFrameworkServiceCollectionExtensions.cs
git commit -m "test(infra): wire LibraryWithViewsContext seeding into shared EF helper

EFCore: SeedDatabase<LibraryWithViewsContext, LibraryWithViewsTestInitializer>
runs after AddEFCoreProviderServices, populating publishers/books from the
existing LibraryTestInitializer and then creating the BooksByPublisher
SQL view on top.

EF6: relies on Database.SetInitializer in the LibraryWithViewsContext
constructor (LibraryWithViewsTestInitializer), which the existing
SeedDatabase<TContext>(connectionString) call activates per process.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 10: EFCore end-to-end — GET returns rows + $filter + convention NOT firing + write verbs 405

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/EFCore/Issue741_KeylessViews.cs`

- [ ] **Step 1: Find an existing EFCore regression test to use as a template**

```bash
ls test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/EFCore/
```

Pick one (e.g. `Issue714_ComplexTypes.cs`) and read it for the `RestierTestHelpers.ExecuteTestRequest<LibraryApi>` pattern.

- [ ] **Step 2: Write the regression test class**

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.RegressionTests.EFCore;

public class Issue741_KeylessViews
{
    private static Action<IServiceCollection> ConfigureServices => services =>
        services.AddEntityFrameworkServices<LibraryWithViewsContext>();

    [Fact]
    public async Task Get_KeylessView_Returns200WithRows()
    {
        LibraryWithViewsApi.OnFilteringBooksByPublisherCallCount = 0;

        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            HttpMethod.Get,
            resource: "/BooksByPublisher()",
            serviceCollection: ConfigureServices);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"value\"");
    }

    [Fact]
    public async Task Get_KeylessView_WithFilter_AppliesFilter()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            HttpMethod.Get,
            resource: "/BooksByPublisher()?$filter=PublisherId eq 'Publisher1'",
            serviceCollection: ConfigureServices);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"PublisherId\":\"Publisher1\"");
        body.Should().NotContain("\"PublisherId\":\"Publisher2\"");
    }

    [Fact]
    public async Task Get_KeylessView_DoesNotInvokeOnFilteringConvention()
    {
        // v1 limitation pin: convention hooks do NOT fire on keyless-view function imports.
        // When the convention-processor follow-up lands, flip this test to assert the call count > 0.
        LibraryWithViewsApi.OnFilteringBooksByPublisherCallCount = 0;

        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            HttpMethod.Get,
            resource: "/BooksByPublisher()?$filter=PublisherId eq 'Publisher1'",
            serviceCollection: ConfigureServices);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LibraryWithViewsApi.OnFilteringBooksByPublisherCallCount.Should().Be(0,
            because: "v1 does not invoke OnFiltering<View> for keyless-view function imports; see Follow-up A");
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Write_KeylessView_Returns405(string verb)
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<LibraryWithViewsApi>(
            new HttpMethod(verb),
            resource: "/BooksByPublisher()",
            payload: verb == "DELETE" ? null : "{}",
            serviceCollection: ConfigureServices);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}
```

- [ ] **Step 3: Add a user-secret for `LibraryWithViewsContext` connection string locally**

The connection-string lookup in `AddEntityFrameworkServices<T>` keys on `typeof(TDbContext).Name` → `"LibraryWithViewsContext"`. Add a secret:

```bash
cd test/Microsoft.Restier.Tests.Shared
dotnet user-secrets set "ConnectionStrings:LibraryWithViewsContext" "Server=(localdb)\mssqllocaldb;Database=LibraryWithViewsContext;Trusted_Connection=true;TrustServerCertificate=true"
```

On macOS without LocalDB, point at whatever SQL Server you use for the other Library tests (the existing `LibraryContext` connection string is a fine template — copy it and change the Initial Catalog).

- [ ] **Step 4: Verify the DB and view exist**

The seeding is wired in Task 9b. To confirm, run a one-shot test:

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName=Microsoft.Restier.Tests.AspNetCore.RegressionTests.EFCore.Issue741_KeylessViews.Get_KeylessView_Returns200WithRows" --logger "console;verbosity=normal"
```

If the test fails because the view doesn't exist, inspect the SQL log and revisit Task 9b's `LibraryWithViewsTestInitializer.Seed` implementation.

- [ ] **Step 5: Run the regression tests**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~Issue741_KeylessViews"`
Expected: 3 Facts + 4 Theory rows (one per verb) = 7 passed per TFM.

- [ ] **Step 6: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/RegressionTests/EFCore/Issue741_KeylessViews.cs test/Microsoft.Restier.Tests.Shared.EntityFramework/Extensions/EntityFrameworkServiceCollectionExtensions.cs test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Views/
git commit -m "test(efcore): end-to-end coverage for keyless-view function imports

Issue741_KeylessViews: GET returns 200 with rows; \$filter narrows;
OnFilteringBooksByPublisher convention call count stays at 0 (v1
limitation pin); POST returns 405 via the existing function-import
branch in RestierController.Post.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 4 — EF6 (removed)

**EFCore-only scope.** EF6 was originally planned to ship the same keyless-views auto-mapping, but EF6's `DbModelBuilder.BuildAndValidate()` rejects any code-first entity without a key, and the EDMX-defined-keyless-entity-set path was explicitly removed from scope. The EF6 partial of `EFModelBuilder` instead throws an explicit `InvalidOperationException` for any entity set with empty `KeyProperties` — implemented and committed alongside the EFCore work. See spec **Out of scope** and **Follow-up C** for the rationale and the future re-scoping option (`[KeylessView]` attribute + `SqlQuery` escape hatch).

EF6 users wanting view-shaped read-only resources continue to hand-author `[UnboundOperation]` methods on their API class.

---


## Phase 5 — Documentation

### Task 15: Create `keyless-views.mdx` user-facing page

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/keyless-views.mdx`

- [ ] **Step 1: Write the page**

```mdx
---
title: 'Keyless Views'
description: 'Expose EF Core and EF6 keyless types — typically database views — as read-only RESTier resources.'
---

<Info>
RESTier auto-maps keyless EF Core (`[Keyless]` / `HasNoKey()` / `ToView()`) and EF6 keyless `DbSet<T>` / `DbQuery<T>` types — typically database views — to read-only OData function imports. No hand-authored `[UnboundOperation]` wrappers, no synthetic keys.
</Info>

## What gets auto-mapped

The EF model builder detects any entity type whose key collection is empty (EF6) or `null` (EF Core) and:

1. Registers it as an EDM **`ComplexType`** (not an entity type — entity types in OData v4 require keys).
2. Adds an unbound **`FunctionImport`** named after the DbSet/EntitySet, returning `Collection(<ComplexType>)`.

So a `DbSet<BooksByPublisher> BooksByPublisher` on a keyless type shows up in `$metadata` like:

```xml
<ComplexType Name="BooksByPublisher">
  <Property Name="PublisherId" Type="Edm.String" />
  <Property Name="BookName" Type="Edm.String" />
  <Property Name="BookCount" Type="Edm.Int32" Nullable="false" />
</ComplexType>

<Function Name="BooksByPublisher" IsBound="false">
  <ReturnType Type="Collection(Restier.Sample.BooksByPublisher)" />
</Function>

<EntityContainer Name="Container">
  <FunctionImport Name="BooksByPublisher" Function="Restier.Sample.BooksByPublisher" />
</EntityContainer>
```

## Querying

The URL shape is a function-call (parentheses required):

```http
GET /odata/BooksByPublisher()
```

OData query options work as usual on the returned collection:

```http
GET /odata/BooksByPublisher()?$filter=PublisherId eq 'Publisher1'
GET /odata/BooksByPublisher()?$select=BookName,BookCount
GET /odata/BooksByPublisher()?$orderby=BookCount desc&$top=10
```

```csharp
public class LibraryContext : DbContext
{
    public DbSet<Book> Books { get; set; }
    public DbSet<BooksByPublisher> BooksByPublisher { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<BooksByPublisher>(e =>
        {
            e.HasNoKey();
            e.ToView("BooksByPublisher");
        });
    }
}

[Keyless]
public class BooksByPublisher
{
    public string PublisherId { get; set; }
    public string BookName { get; set; }
    public int BookCount { get; set; }
}
```

<Note>
**EF Core only.** EF6 doesn't support keyless entity types in code-first (model validation rejects entities without a key), and the EDMX-defined-keyless-entity-set path is explicitly out of scope. EF6 users who want view-shaped resources hand-author `[UnboundOperation]` methods on their API class.
</Note>

## Read-only by construction

Writes (POST, PATCH, PUT, DELETE) return **HTTP 405 Method Not Allowed**:

```http
POST /odata/BooksByPublisher()  →  405 Method Not Allowed
```

No submit-pipeline plumbing is involved — there's no entity set to write to.

## v1 limitations

<Warning>
**Convention interceptors do not fire for keyless views in this release.** `OnFiltering<View>`, `OnExecuting<View>`, `OnInserting<View>`, and the rest of the convention surface stay silent. The RESTier query pipeline (`IQueryExpressionAuthorizer`, `ConventionBasedQueryExpressionProcessor`) is not invoked.

For security:

- Apply `[Authorize]` to the function import via your standard ASP.NET Core authorization (the operation appears as a normal OData function-import endpoint).
- Or pre-filter inside the view's SQL definition (e.g. row-level security in SQL Server).

`RestierEFOptions.NoTracking` is also not applied to keyless-view queries in this release. EF Core defaults to tracking; the consequence is small because the result is serialised straight to the response (no entity-graph state is retained beyond the request), but watch out if you read large views in tight loops within a single DbContext lifetime.

Both limitations are tracked in the [keyless-views follow-up issue]() and will be lifted by widening the convention processor + adding a `KeylessViewQueryExpressionSourcer` to the chain.
</Warning>

## Mapping table

| Source | RESTier surface |
|---|---|
| EF Core `[Keyless]` + `DbSet<T>` | `ComplexType<T>` + `FunctionImport` named after the DbSet |
| EF Core `HasNoKey()` + `ToView("X")` + `DbSet<T>` | Same |
| EF Core keyless type with no DbSet (pure query type) | Not exposed — no entity-set-name to map to a function import |
| EF6 (any flavour) | Not supported — `EFModelBuilder` throws `InvalidOperationException` if it encounters an empty key list. Use `[UnboundOperation]` to hand-author a view-shaped resource on EF6. |
```

- [ ] **Step 2: Build the docs**

Run: `dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`
Expected: build picks up the new page. May produce warnings if MDX frontmatter or component usage is malformed — fix until clean.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/keyless-views.mdx
git commit -m "docs(server): add keyless-views guide

Introduces the auto-mapping of keyless EF types as ComplexType + unbound
FunctionImport. EFCore-only; EF6 callout explains the limitation.
Calls out v1 limitations (no convention hooks, no RestierEFOptions
no-tracking) under a Warning component pending the follow-up.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 16: Cross-link from `model-building.mdx` and `operations.mdx`

**Files:**
- Modify: `src/Microsoft.Restier.Docs/guides/server/model-building.mdx`
- Modify: `src/Microsoft.Restier.Docs/guides/server/operations.mdx`

- [ ] **Step 1: Open `model-building.mdx` and find the section that explains what RESTier auto-maps from EF (entity types, navigations, etc.). Add a short paragraph:**

```mdx
RESTier also auto-maps **keyless EF types** (database views) to read-only OData function imports — see [Keyless Views](./keyless-views) for the details.
```

- [ ] **Step 2: Open `operations.mdx` and add a note near where unbound operations are introduced:**

```mdx
<Note>
Unbound function imports for **keyless EF views** are auto-generated by the EF model builder — see [Keyless Views](./keyless-views). You don't write `[UnboundOperation]` for them.
</Note>
```

- [ ] **Step 3: Build the docs**

Run: `dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`
Expected: success.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/model-building.mdx src/Microsoft.Restier.Docs/guides/server/operations.mdx
git commit -m "docs(server): cross-link keyless-views from model-building and operations

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 17: Register the new page in the navigation template

**Files:**
- Modify: `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`
- Regenerate: `src/Microsoft.Restier.Docs/docs.json`

- [ ] **Step 1: Open the docsproj and find the `<MintlifyTemplate>` block**

Locate the "Server" group in the template and add the new page next to `model-building`:

```xml
<Page Path="guides/server/keyless-views.mdx" />
```

(Use the exact attribute/element names already in the template — they may differ.)

- [ ] **Step 2: Build the docs to regenerate `docs.json`**

Run: `dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`
Expected: `docs.json` is regenerated with the new page entry. Inspect the diff:

Run: `git diff src/Microsoft.Restier.Docs/docs.json`

Confirm the new page appears in the navigation tree exactly once.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj src/Microsoft.Restier.Docs/docs.json
git commit -m "docs(nav): register keyless-views page in the docs navigation

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 18: Release notes entry

**Files:**
- Modify or create: `src/Microsoft.Restier.Docs/release-notes/<latest>.mdx` (or follow the existing release-notes folder convention)

- [ ] **Step 1: Identify the right release-notes file**

Run: `ls src/Microsoft.Restier.Docs/release-notes/`
Pick the file representing the current vnext release (or create a new entry if conventions require one — match the existing pattern).

- [ ] **Step 2: Add an entry**

```mdx
### Keyless EF views as read-only function imports

EF Core `[Keyless]` / `HasNoKey()` / `ToView()` and EF6 keyless `DbSet<T>` / `DbQuery<T>` types are now exposed automatically as `ComplexType` + unbound `FunctionImport` returning `Collection(<ComplexType>)`. Query them via `GET /odata/<ViewName>()` with full `$filter` / `$select` / `$orderby` / `$top` / `$skip` support. Writes return HTTP 405.

**v1 limitations:** convention interceptors (`OnFiltering<View>` etc.) don't fire for keyless views, and `RestierEFOptions.NoTracking` is not applied. Use `[Authorize]` on the function import or row-filter in SQL for security. See [Keyless Views](/guides/server/keyless-views) for details and tracked follow-ups.

Closes [#741](https://github.com/OData/RESTier/issues/741).
```

- [ ] **Step 3: Build and commit**

Run: `dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`

```bash
git add src/Microsoft.Restier.Docs/release-notes/
git commit -m "docs(release-notes): keyless EF views (#741)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Phase 6 — Finalisation

### Task 19: Verify Swagger / NSwag output for the new function imports

**Files:** none (verification only; if a fix is needed, scope it as a new task or a separate issue)

- [ ] **Step 1: Wire `BooksByPublisher` into one of the sample apps (Northwind preferred)**

Add a `[Keyless]` class + `DbSet<BooksByPublisher>` + `.ToView(...)` mapping. Don't commit; this is just to inspect Swagger.

- [ ] **Step 2: Run the sample and hit the Swagger UI**

Run: `dotnet run --project src/Microsoft.Restier.Samples.Northwind.AspNetCore` and navigate to `/swagger`.

- [ ] **Step 3: Confirm the function import appears as a `GET /odata/BooksByPublisher()` path with a `Collection(BooksByPublisher)` response shape**

If it's missing or malformed, file a separate Swagger/NSwag issue and link to the keyless-views feature. Don't fix in this plan unless trivial.

- [ ] **Step 4: Revert the sample-app changes**

```bash
git checkout -- src/Microsoft.Restier.Samples.Northwind.AspNetCore/
```

- [ ] **Step 5: Note the result in the follow-up issue you'll file in Task 20**

---

### Task 20: File the follow-up tracking issue

**Action:** file an issue against OData/RESTier titled "Keyless views — query-pipeline integration (conventions + no-tracking)" linking it to #741 and pasting the Follow-up A + B sections of the spec verbatim.

- [ ] **Step 1: Use `gh` to file the issue**

```bash
gh issue create --repo OData/RESTier \
    --title "Keyless views — query-pipeline integration (conventions + no-tracking) [#741 follow-up]" \
    --body "$(cat <<'EOF'
Follow-up to #741. The v1 implementation (PR <link>) auto-maps keyless EF types as ComplexType + FunctionImport but does **not** integrate them into the RESTier query pipeline. This issue tracks lifting that limitation.

## Follow-up A — convention hooks + query-pipeline integration

[paste from docs/superpowers/specs/2026-05-19-keyless-views-design.md "Follow-up A" section]

## Follow-up B — RestierEFOptions no-tracking for keyless views

[paste from spec "Follow-up B" section]

## Open question (verify during work)

Microsoft.Restier.AspNetCore.Swagger / NSwag OpenAPI output for `FunctionImport` returning `Collection(<ComplexType>)` — verified during #741 implementation as [paste result].
EOF
)"
```

- [ ] **Step 2: Update the `<Warning>` block in `keyless-views.mdx` to link to the new issue number**

Edit the link: `[keyless-views follow-up issue](https://github.com/OData/RESTier/issues/<N>)`.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/keyless-views.mdx
git commit -m "docs: link Warning callout to the keyless-views follow-up issue

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 21: Run the full test suite

- [ ] **Step 1: Run all tests in the solution**

Run: `dotnet test RESTier.slnx`
Expected: all tests pass on all TFMs (net8.0, net9.0, net10.0). If a pre-existing unrelated test fails on your environment, capture the failure but don't fix it in this PR.

- [ ] **Step 2: Take a code-coverage snapshot**

```bash
dotnet test RESTier.slnx --collect:"XPlat Code Coverage" --results-directory TestResults/Coverage
~/.dotnet/tools/reportgenerator \
  "-reports:TestResults/Coverage/*/coverage.cobertura.xml" \
  "-targetdir:TestResults/CoverageReport" \
  -reporttypes:TextSummary
cat TestResults/CoverageReport/Summary.txt
```

Sanity-check that `KeylessViewRegistry`, the keyless branch in `EFModelBuilder.BuildEdmModelFromEntitySetMaps`, and the executor fallback have measurable coverage (≥ 80%).

- [ ] **Step 3: Final commit (if there are stray test-config changes)**

```bash
git status
# only commit if there are intentional config or seed-file changes outstanding
```

---

## Self-Review (run before declaring complete)

- [ ] Spec coverage: every section of `docs/superpowers/specs/2026-05-19-keyless-views-design.md` (Goal, Decisions, Components, Data flow, Edge cases, Documentation, Testing, Out-of-scope, Follow-ups) has a corresponding task above.
- [ ] No placeholders: search this plan for "TBD", "TODO", "...", "implement later". Fix any.
- [ ] Type consistency:
  - `KeylessViewRegistry.Register(string, Type, Func<object, IQueryable>)` matches every call site (Task 6, Task 8, Task 11).
  - `KeylessViewEntry` fields (`FunctionImportName`, `ClrType`, `SourceFactory`) match every consumer (Task 8).
  - Source factory signature `Func<object, IQueryable>` is identical in EFCore (Task 5), EF6 (Task 11), and the executor (Task 8).
- [ ] HTTP status: every test expecting "not allowed" asserts **405**, not 404 (Tasks 10, 14).
- [ ] Convention hooks: every test about `OnFiltering<View>` asserts the call count is **0** (Tasks 10, 14) — pinned as the v1 limitation.
- [ ] Bridge: `KeylessViewRegistry` registered in `modelBuildingServices` AND captured locally AND re-registered into the route services lambda (Task 3). All three steps present.
- [ ] EF6 partial throws an explicit `InvalidOperationException` for empty key lists (not silently no-op).
- [ ] Docs deliverable: new page (Task 15), cross-links (Task 16), navigation (Task 17), release notes (Task 18), all in scope.
