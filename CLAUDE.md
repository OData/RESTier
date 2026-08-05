# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Microsoft RESTier is an OData V4 API development framework for building standardized RESTful services on .NET. It is the spiritual successor to WCF Data Services, providing convention-based query interception and data manipulation over Entity Framework.

## Build & Test Commands

```bash
# Build entire solution
dotnet build RESTier.slnx

# Run all tests
dotnet test RESTier.slnx

# Run a single test project
dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj

# Run a specific test by name
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Build a single project
dotnet build src/Microsoft.Restier.Core/Microsoft.Restier.Core.csproj
```

## Architecture

### Core Pipeline (Chain of Responsibility)

RESTier's central pattern is a **chain of responsibility** pipeline for both queries and submissions. Services implement `IChainedService<TService>` with an `Inner` property, composed via `IChainOfResponsibilityFactory`.

**Query pipeline** (`Microsoft.Restier.Core.Query`):
`IQueryExpressionSourcer` -> `IQueryExpressionAuthorizer` -> `IQueryExpressionExpander` -> `IQueryExpressionProcessor` -> `IQueryExecutor`
Orchestrated by `DefaultQueryHandler`.

**Submit pipeline** (`Microsoft.Restier.Core.Submit`):
`IChangeSetInitializer` -> `IChangeSetItemFilter` -> `IChangeSetItemAuthorizer` -> `IChangeSetItemValidator` -> `ISubmitExecutor`
Orchestrated by `DefaultSubmitHandler`.

### Convention-Based Interception

RESTier discovers interceptor methods by naming convention on `ApiBase` subclasses:
- `OnFiltering{EntitySet}()` / `OnInserting{Entity}()` / `OnValidating{Entity}()` etc.
- Implemented via `ConventionBasedQueryExpressionProcessor`, `ConventionBasedChangeSetItemFilter`, `ConventionBasedChangeSetItemValidator`

### Key Base Classes

- `ApiBase` - Base class for all RESTier APIs; subclass to define your API surface
- `EntityFrameworkApi<TContext>` - EF-specific base providing DbContext integration
- `RestierController : ODataController` - Handles OData HTTP requests in ASP.NET Core

### Project Layout

| Directory | Purpose |
|-----------|---------|
| `src/Microsoft.Restier.Core` | Core abstractions, pipelines, conventions, DI |
| `src/Microsoft.Restier.AspNetCore` | ASP.NET Core integration, routing, controller |
| `src/Microsoft.Restier.EntityFramework` | Entity Framework 6.x support |
| `src/Microsoft.Restier.EntityFrameworkCore` | Entity Framework Core support |
| `src/Microsoft.Restier.EntityFramework.Shared` | Shared EF code (shared project, not NuGet) |
| `src/Microsoft.Restier.Breakdance` | In-memory testing framework |
| `src/Microsoft.Restier.AspNetCore.Swagger` | Swagger/OpenAPI generation |

### Dependency Injection

Uses `Microsoft.Extensions.DependencyInjection` with per-route service containers. Service registration extensions are in `Microsoft.Restier.Core.DependencyInjection` and `Microsoft.Restier.AspNetCore.Extensions`.

## Code Conventions

- **Targets:** .NET 8.0, .NET 9.0, and .NET Framework 4.8
- **Warnings as errors** enabled globally
- **Implicit usings disabled** - all `using` directives must be explicit
- **Nullable reference types disabled**
- **Strong name signing** with `restier.snk`
- **Allman brace style**, prefer `var`, prefer curly braces even for single-line blocks
- **InternalsVisibleTo** is auto-configured from source to matching test project

## Test Conventions

- **Framework:** MSTest 3.x, FluentAssertions (AwesomeAssertions), NSubstitute
- **Project naming:** `X` -> `X.Tests` (e.g., `Microsoft.Restier.Core` -> `Microsoft.Restier.Tests.Core`)
- **File naming:** `X/Y/Z/A.cs` -> `X.Tests/Y/Z/ATests.cs`
- **Namespace:** must match folder path (e.g., `Microsoft.Restier.Tests.Core.Convention`)
- **Integration/scenario tests** go in `X.Tests/IntegrationTests` or `X.Tests/ScenarioTests`

## Documentation

Documentation lives in `src/Microsoft.Restier.Docs/` and is built with the **DotNetDocs SDK** (`<Project Sdk="DotNetDocs.Sdk/1.2.0">`), which generates Mintlify-flavored MDX.

```bash
# Build the docs project (regenerates api-reference/ and docs.json)
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

The docs project is part of `RESTier.slnx`, so a full solution build also builds the docs:

```bash
dotnet build RESTier.slnx
```

**Authoring conventions:**
- Hand-written content lives under `guides/`, `release-notes/`, and the project root (`index.mdx`, `quickstart.mdx`, `contribution-guidelines.mdx`, `why-restier.mdx`).
- API reference under `api-reference/` is auto-generated from XML doc comments (six assemblies at TFM `net9.0`) and gitignored — do NOT hand-edit it.
- Pages use Mintlify components: `<Info>`, `<Note>`, `<Tip>`, `<Warning>`, `<Steps>`, `<CodeGroup>`, `<Tabs>`, `<CardGroup>`. See existing pages for examples.

**Navigation source of truth:** the `<MintlifyTemplate>` block in `Microsoft.Restier.Docs.docsproj`. The SDK regenerates `docs.json` from this template on every build, so commit `docs.json` alongside any nav-affecting change but do not hand-edit it.

**Build-ordering note:** the docsproj has an explicit `BuildSourceProjectsForDocs` target that calls `<MSBuild>` on each documented source project for `net8.0` before doc generation runs. The DotNetDocs SDK resolves assembly paths against `bin/Debug/net8.0/`, and `<ProjectReference>` items alone don't reliably trigger a full Build of the multi-targeted source projects from this NoTargets-style docsproj.

`assembly-list.txt` under the docs project is a Debug-build debug dump written by the SDK (not configuration). It is gitignored.

## Key Dependencies

- Microsoft.OData.Core / Microsoft.OData.Edm (8.x)
- Microsoft.OData.ModelBuilder (2.x)
- Microsoft.AspNetCore.OData (9.x)
- EntityFramework 6.5.x / EntityFrameworkCore 8.x-10.x
