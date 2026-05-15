# `[AllowAnonymous]` / `[Authorize]` on RESTier API Surfaces — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Honor ASP.NET Core's standard authorization attributes — `[AllowAnonymous]`, `[Authorize]`, `[Authorize(Policy=…)]`, `[Authorize(Roles=…)]` — on three surfaces of a RESTier `ApiBase` subclass (the class itself, `[Resource]`-decorated properties, and `[BoundOperation]` / `[UnboundOperation]` methods), so that a global `[Authorize]` filter can be overridden per-API or per-action exactly the way it works on any other ASP.NET Core controller.

**Architecture:** A single new `RestierAuthorizationMetadataPolicy` (an `IEndpointSelectorPolicy`) runs during routing — after `DynamicControllerEndpointMatcherPolicy` and before `AuthorizationMiddleware`. It identifies Restier endpoints via a `RestierRouteMarker` attached to endpoint metadata, reads `ODataFeature.Path` to find the target (class / resource property / operation method) on the user's `ApiBase` subclass, collects any `IAuthorizeData` / `IAllowAnonymous` attributes, caches them by `(apiType, targetKey) → object[]`, and replaces the candidate endpoint with a freshly-wrapped one carrying the augmented metadata. ASP.NET Core's `AuthorizationMiddleware` then reads the wrapped endpoint and applies its standard precedence rules. The policy is registered via `AddRestier` so consumers get it without any new `app.Use…` call. DbSet-backed entity sets fall through to class-level since they have no anchor on the API class.

**Tech Stack:** C# (.NET 8/9/10), ASP.NET Core routing (`MatcherPolicy`, `IEndpointSelectorPolicy`, `CandidateSet`, `RouteEndpoint`), `Microsoft.AspNetCore.Authorization` (`IAuthorizeData`, `IAllowAnonymous`, `AuthorizationMiddleware`), `Microsoft.AspNetCore.OData` 9.x (`ODataFeature`, `ODataPath`, `EntitySetSegment`, `OperationSegment`, `OperationImportSegment`, `MetadataSegment`), xUnit v3 (`[Fact]`, `[Theory]`), AwesomeAssertions (imported as `FluentAssertions`), NSubstitute (`Substitute.For<T>()`).

**Spec:** `docs/superpowers/specs/2026-05-15-restier-authorization-attributes-design.md`.

---

## Conventions

- **Targets:** net8.0, net9.0, net10.0 (solution-wide).
- **Brace style:** Allman. `var` preferred. Curly braces even for single-line blocks.
- **Warnings as errors:** enabled globally — code must be warning-clean.
- **Implicit usings disabled:** every `using` directive must be explicit.
- **Test framework:** xUnit v3, AwesomeAssertions (`Should()`), NSubstitute (`Substitute.For<T>()`).
- **`InternalsVisibleTo`:** auto-configured from `Microsoft.Restier.AspNetCore` to `Microsoft.Restier.Tests.AspNetCore`. The new policy stays `internal sealed`; tests access it directly.
- **Commits:** small and focused; one per task. Use the existing co-author footer.

---

## File Inventory

| File | Action | Purpose |
|------|--------|---------|
| `src/Microsoft.Restier.AspNetCore/Routing/RestierRouteMarker.cs` | Modify | Add `Type ApiType { get; }` and constructor parameter. |
| `src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs` | Create | The `IEndpointSelectorPolicy` — `AppliesToEndpoints`, `ApplyAsync`, `ComputeTargetKey`, `DiscoverAttributes`, `WrapEndpoint`. |
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` | Modify | Inside `AddRouteComponents` services lambda, pass `typeof(TApi)` to the `RestierRouteMarker` constructor (was `services.AddSingleton<RestierRouteMarker>()`). |
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierEndpointRouteBuilderExtensions.cs` | Modify | Resolve `RestierRouteMarker` from route services and attach it as endpoint metadata via `.WithMetadata(marker)`. |
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierIMvcBuilderExtensions.cs` | Modify | Factor a private `AddRestierServices(IServiceCollection)` helper called from all four `AddRestier` overloads; helper registers the matcher policy via `TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, RestierAuthorizationMetadataPolicy>())`. |
| `test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierAuthorizationMetadataPolicyTests.cs` | Create | Unit tests: `ComputeTargetKey` across path shapes, `DiscoverAttributes` across surfaces, `AppliesToEndpoints` filter, `ApplyAsync` cache-miss / cache-hit / no-attributes / wrap-builds-correctly. |
| `test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierRouteValueTransformerTests.cs` | Modify | Existing tests register `services.AddSingleton<RestierRouteMarker>()` (line 69) — change to `services.AddSingleton(new RestierRouteMarker(typeof(SomeApi)))`. |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/TestAuthHandler.cs` | Create | `AuthenticationHandler<AuthenticationSchemeOptions>` that reads `X-Test-User` header and builds a `ClaimsPrincipal`. |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessApis.cs` | Create | Test fixture API types: `AnonymousAtClassApi`, `RequireAuthApi`, `AnonymousAtResourceApi`, `AnonymousAtOperationApi`, `PolicyOnOperationApi`, `AuthorizeOnBaseApi` / `InheritedAnonymousApi`. |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs` | Create | Integration tests for the 12 scenarios in the spec. |
| `src/Microsoft.Restier.Docs/guides/server/method-authorization.mdx` | Modify | Add a new top section "Using `[AllowAnonymous]` and `[Authorize]`" with examples, layer table, precedence rules, DbSet limitation. |

---

## Phase 1 — Enrich `RestierRouteMarker` with the API type

### Task 1: Add `ApiType` property to `RestierRouteMarker`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Routing/RestierRouteMarker.cs`

- [ ] **Step 1: Replace the empty sentinel with a typed marker**

Replace the entire body of `src/Microsoft.Restier.AspNetCore/Routing/RestierRouteMarker.cs` with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// Marker registered in per-route DI services AND attached as endpoint metadata so RESTier-specific
/// matcher policies and middleware can identify Restier routes and look up the user's API type
/// without re-scanning <see cref="Microsoft.AspNetCore.OData.ODataOptions"/>.
/// </summary>
internal sealed class RestierRouteMarker
{
    public RestierRouteMarker(Type apiType)
    {
        ApiType = apiType ?? throw new ArgumentNullException(nameof(apiType));
    }

    /// <summary>
    /// The concrete <see cref="Core.ApiBase"/> subclass registered for this route.
    /// </summary>
    public Type ApiType { get; }
}
```

- [ ] **Step 2: Build — expect compile errors at marker construction sites**

Run:
```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
```

Expected: `error CS7036: There is no argument given that corresponds to the required parameter 'apiType'` at two places — `RestierODataOptionsExtensions.cs:151` and `RestierRouteValueTransformerTests.cs:69`. We'll fix both in subsequent tasks.

- [ ] **Step 3: Commit (deferred — combine with the call-site fixes in Task 2)**

Don't commit yet. The two callers must compile before the project builds.

### Task 2: Pass `typeof(TApi)` to the marker and update transformer-tests

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs:151`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierRouteValueTransformerTests.cs:69`

- [ ] **Step 1: Update the route-services registration to pass the API type**

In `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs`, find the line:

```csharp
            services.AddSingleton<RestierRouteMarker>();
```

(currently at line 151, inside the `AddRouteComponents` services lambda). Replace with:

```csharp
            services.AddSingleton(new RestierRouteMarker(type));
```

Here `type` is the `Type` parameter already in scope on the enclosing `AddRestierRoute(ODataOptions, Type, ...)` overload (see line 94).

- [ ] **Step 2: Update the transformer test fixture to pass an API type**

In `test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierRouteValueTransformerTests.cs`, find the line:

```csharp
                    routeServices.AddSingleton<RestierRouteMarker>();
```

(currently at line 69, inside `CreateTransformer`). Replace with:

```csharp
                    routeServices.AddSingleton(new RestierRouteMarker(typeof(object)));
```

We use `typeof(object)` here because the transformer never reads `ApiType` — only the matcher policy does. Tests that exercise the policy use real fixture API types.

- [ ] **Step 3: Build the source and the tests**

Run:
```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: both succeed with no errors.

- [ ] **Step 4: Run the existing transformer tests to confirm no regression**

Run:
```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierRouteValueTransformerTests"
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Routing/RestierRouteMarker.cs \
        src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierRouteValueTransformerTests.cs
git commit -m "$(cat <<'EOF'
refactor(routing): RestierRouteMarker carries the registered API type

Foundation for #717. The marker was a sentinel; now it exposes the
concrete ApiBase subclass registered for the route so downstream
matcher-policy / metadata work can look it up in O(1) without
re-scanning ODataOptions.RouteComponents.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 3: Attach `RestierRouteMarker` to endpoint metadata in `MapRestier`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Extensions/RestierEndpointRouteBuilderExtensions.cs`

- [ ] **Step 1: Update `MapRestier` to resolve the marker and attach it as endpoint metadata**

Open `src/Microsoft.Restier.AspNetCore/Extensions/RestierEndpointRouteBuilderExtensions.cs`. Replace the body of the `foreach` loop in `MapRestier` (currently lines 28–43) with:

```csharp
        foreach (var (prefix, _) in odataOptions.RouteComponents)
        {
            // Only map routes for Restier APIs (identified by the RestierRouteMarker sentinel).
            var routeServices = odataOptions.GetRouteServices(prefix);
            var marker = routeServices.GetService(typeof(RestierRouteMarker)) as RestierRouteMarker;
            if (marker is null)
            {
                continue;
            }

            var pattern = string.IsNullOrEmpty(prefix)
                ? "{**odataPath}"
                : prefix + "/{**odataPath}";

            endpoints.MapDynamicControllerRoute<RestierRouteValueTransformer>(pattern, state: prefix)
                .WithMetadata(marker);
        }
```

The `.WithMetadata(marker)` call attaches the same `RestierRouteMarker` instance to the dynamic-route endpoint's static metadata. `RestierAuthorizationMetadataPolicy.AppliesToEndpoints` reads this in its fast filter (no DI lookups in the hot path).

- [ ] **Step 2: Build the source**

Run:
```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
```

Expected: succeeds.

- [ ] **Step 3: Run the full AspNetCore test project (smoke check)**

Run:
```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: all tests pass. (Metadata attached to dynamic routes does not change any existing behavior; this is a smoke check that no test reads `endpoints.DataSources` and asserts the metadata is empty.)

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Extensions/RestierEndpointRouteBuilderExtensions.cs
git commit -m "$(cat <<'EOF'
feat(routing): attach RestierRouteMarker as endpoint metadata in MapRestier

Endpoint metadata is the right place for matcher policies to read
"is this a Restier route?" in the fast-filter path. The marker stays
registered in route services too (existing transformers depend on it).

Foundation for #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 2 — Target-key resolution

### Task 4: Write `ComputeTargetKey` unit tests (TDD red)

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierAuthorizationMetadataPolicyTests.cs`

- [ ] **Step 1: Write the failing test file**

Create `test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierAuthorizationMetadataPolicyTests.cs` with this content:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using Microsoft.Restier.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Routing;

public partial class RestierAuthorizationMetadataPolicyTests
{
    #region Test model

    private class TestPerson
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    private static IEdmModel BuildTestModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<TestPerson>("People");
        builder.Singleton<TestPerson>("Me");
        builder.EntityType<TestPerson>().Collection.Action("DiscontinuePeople");
        builder.Action("ResetData");
        return builder.GetEdmModel();
    }

    private static ODataPath ParsePath(IEdmModel model, string odataPath)
    {
        var parser = new ODataUriParser(model, new Uri(odataPath, UriKind.Relative));
        parser.Resolver = new UnqualifiedODataUriResolver { EnableCaseInsensitive = true };
        return parser.ParsePath();
    }

    #endregion

    #region ComputeTargetKey

    [Fact]
    public void ComputeTargetKey_NullPath_ReturnsClass()
    {
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path: null);
        key.Should().Be("class");
    }

    [Fact]
    public void ComputeTargetKey_EmptyPath_ReturnsClass()
    {
        var path = new ODataPath(new List<ODataPathSegment>());
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("class");
    }

    [Fact]
    public void ComputeTargetKey_MetadataSegment_ReturnsClass()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "$metadata");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("class");
    }

    [Fact]
    public void ComputeTargetKey_EntitySet_ReturnsResource()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "People");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("resource:People");
    }

    [Fact]
    public void ComputeTargetKey_EntitySetWithKey_ReturnsResource()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "People(1)");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("resource:People");
    }

    [Fact]
    public void ComputeTargetKey_Singleton_ReturnsResource()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "Me");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("resource:Me");
    }

    [Fact]
    public void ComputeTargetKey_OperationImport_ReturnsOperation()
    {
        var model = BuildTestModel();
        var path = ParsePath(model, "ResetData");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("operation:ResetData");
    }

    [Fact]
    public void ComputeTargetKey_BoundOperationOnEntitySet_ReturnsOperation()
    {
        var model = BuildTestModel();
        // Bound action: People/Default.DiscontinuePeople
        var path = ParsePath(model, "People/Default.DiscontinuePeople");
        var key = RestierAuthorizationMetadataPolicy.ComputeTargetKey(path);
        key.Should().Be("operation:DiscontinuePeople");
    }

    #endregion
}
```

- [ ] **Step 2: Run the tests — expect failures because the policy class doesn't exist yet**

Run:
```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: `error CS0246: The type or namespace name 'RestierAuthorizationMetadataPolicy' could not be found`. This is the TDD-red signal.

### Task 5: Implement `RestierAuthorizationMetadataPolicy.ComputeTargetKey`

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs`

- [ ] **Step 1: Create the policy file with just the helper method needed for tests**

Create `src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.OData.UriParser;
using System;
using System.Linq;

namespace Microsoft.Restier.AspNetCore.Routing;

/// <summary>
/// A <see cref="MatcherPolicy"/> that augments the matched <see cref="Endpoint"/> for a Restier route
/// with any <see cref="Microsoft.AspNetCore.Authorization.IAuthorizeData"/> or
/// <see cref="Microsoft.AspNetCore.Authorization.IAllowAnonymous"/> attributes found on the user's
/// <see cref="Core.ApiBase"/> subclass, its <see cref="Model.ResourceAttribute"/>-decorated
/// properties, or its <see cref="Model.BoundOperationAttribute"/> /
/// <see cref="Model.UnboundOperationAttribute"/> methods.
/// </summary>
internal sealed class RestierAuthorizationMetadataPolicy : MatcherPolicy
{
    private const string ClassKey = "class";
    private const string ResourcePrefix = "resource:";
    private const string OperationPrefix = "operation:";

    /// <summary>
    /// Maps an <see cref="ODataPath"/> to a stable string key identifying the user-code target
    /// whose attributes should be honored: the class, a named resource property, or a named
    /// operation method. The key doubles as a cache key for the discovered attribute list.
    /// </summary>
    internal static string ComputeTargetKey(ODataPath path)
    {
        if (path is null || path.Count == 0)
        {
            return ClassKey;
        }

        var lastSegment = path.LastOrDefault();
        if (lastSegment is MetadataSegment)
        {
            return ClassKey;
        }

        // Operations win because they are the actual action being invoked. A bound operation
        // (path ending in OperationSegment) overrides the entity-set's attributes.
        if (lastSegment is OperationImportSegment opImport)
        {
            var op = opImport.OperationImports.FirstOrDefault();
            return op is null ? ClassKey : OperationPrefix + op.Name;
        }
        if (lastSegment is OperationSegment opSeg)
        {
            var op = opSeg.Operations.FirstOrDefault();
            return op is null ? ClassKey : OperationPrefix + op.Name;
        }

        // Otherwise the first segment identifies the resource the request targets.
        var firstSegment = path.FirstOrDefault();
        if (firstSegment is EntitySetSegment esSeg)
        {
            return ResourcePrefix + esSeg.EntitySet.Name;
        }
        if (firstSegment is SingletonSegment singletonSeg)
        {
            return ResourcePrefix + singletonSeg.Singleton.Name;
        }

        return ClassKey;
    }

    /// <inheritdoc/>
    // DynamicControllerEndpointMatcherPolicy.Order == int.MinValue + 100. We run after it so the
    // OData path is already parsed and the candidate endpoint is the RestierController action.
    public override int Order => int.MinValue + 110;
}
```

Note: the class derives from `MatcherPolicy` only for now (no `IEndpointSelectorPolicy` yet — that's added in Task 11). This lets the file compile while we focus on the static helper.

- [ ] **Step 2: Build the source project**

Run:
```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
```

Expected: succeeds.

- [ ] **Step 3: Run only the `ComputeTargetKey` tests**

Run:
```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
  --filter "FullyQualifiedName~ComputeTargetKey"
```

Expected: 8 tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierAuthorizationMetadataPolicyTests.cs
git commit -m "$(cat <<'EOF'
feat(routing): add ComputeTargetKey helper for authorization metadata policy

Maps an ODataPath to a stable target key (class / resource:Name /
operation:Name). The key both identifies which member's attributes
to read and serves as a cache key.

Part of #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 3 — Attribute discovery

### Task 6: Write `DiscoverAttributes` unit tests (TDD red)

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierAuthorizationMetadataPolicyTests.cs`

- [ ] **Step 1: Add test fixture API types and the test cases**

Append to `test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierAuthorizationMetadataPolicyTests.cs` (inside the existing `partial class`, after the `#region ComputeTargetKey ... #endregion` block):

```csharp
    #region DiscoverAttributes fixtures

    private class PlainApi
    {
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    private class ClassAnonymousApi
    {
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    private class ClassAuthorizeApi
    {
    }

    private class ResourceApi
    {
        [Microsoft.Restier.AspNetCore.Model.Resource]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public System.Linq.IQueryable<TestPerson> PublicPeople { get; set; }

        [Microsoft.Restier.AspNetCore.Model.Resource]
        public System.Linq.IQueryable<TestPerson> PrivatePeople { get; set; }

        // Not a [Resource] — even though it has [AllowAnonymous], it must be ignored.
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public System.Linq.IQueryable<TestPerson> NotARealResource { get; set; }
    }

    private class OperationApi
    {
        [Microsoft.Restier.AspNetCore.Model.UnboundOperation]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "Admin")]
        public void RestrictedOp() { }

        [Microsoft.Restier.AspNetCore.Model.UnboundOperation]
        public void NormalOp() { }
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    private class BaseRestrictedApi
    {
    }

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    private class DerivedAnonymousApi : BaseRestrictedApi
    {
    }

    private class DerivedInheritsApi : BaseRestrictedApi
    {
    }

    #endregion

    #region DiscoverAttributes

    [Fact]
    public void DiscoverAttributes_PlainApi_ReturnsEmpty()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(PlainApi), "class");
        attrs.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverAttributes_ClassAllowAnonymous_ReturnsAllowAnonymous()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(ClassAnonymousApi), "class");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAllowAnonymous>();
    }

    [Fact]
    public void DiscoverAttributes_ClassAuthorize_ReturnsAuthorize()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(ClassAuthorizeApi), "class");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
    }

    [Fact]
    public void DiscoverAttributes_PublicResource_ReturnsAllowAnonymousFromProperty()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(ResourceApi), "resource:PublicPeople");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAllowAnonymous>();
    }

    [Fact]
    public void DiscoverAttributes_PrivateResource_ReturnsEmpty()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(ResourceApi), "resource:PrivatePeople");
        attrs.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverAttributes_NonResourceProperty_IsIgnored()
    {
        // [AllowAnonymous] on a property without [Resource] must be ignored to avoid surprising users.
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(ResourceApi), "resource:NotARealResource");
        attrs.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverAttributes_RestrictedOperation_ReturnsAuthorize()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(OperationApi), "operation:RestrictedOp");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
    }

    [Fact]
    public void DiscoverAttributes_NormalOperation_ReturnsEmpty()
    {
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(OperationApi), "operation:NormalOp");
        attrs.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverAttributes_DerivedClassAnonymous_OverridesBaseAuthorize()
    {
        // Both attributes flow through; AuthorizationMiddleware applies "AllowAnonymous wins" later.
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(DerivedAnonymousApi), "class");
        attrs.Should().HaveCount(2)
             .And.ContainItemsAssignableTo<object>();
        attrs.Should().Contain(a => a is Microsoft.AspNetCore.Authorization.IAllowAnonymous);
        attrs.Should().Contain(a => a is Microsoft.AspNetCore.Authorization.IAuthorizeData);
    }

    [Fact]
    public void DiscoverAttributes_InheritedAuthorize_IsDiscovered()
    {
        // Subclass with no attributes inherits [Authorize] from the base class.
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(DerivedInheritsApi), "class");
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
    }

    [Fact]
    public void DiscoverAttributes_ClassAndResourceCombined_ReturnsBoth()
    {
        // Class + member attributes both flow through.
        var attrs = RestierAuthorizationMetadataPolicy.DiscoverAttributes(typeof(ClassAuthorizeApi), "resource:Anything");
        // ClassAuthorizeApi has [Authorize]; no resource property of name "Anything" exists, so only class-level.
        attrs.Should().ContainSingle()
             .Which.Should().BeAssignableTo<Microsoft.AspNetCore.Authorization.IAuthorizeData>();
    }

    #endregion
```

- [ ] **Step 2: Run the build to confirm tests don't compile yet (method doesn't exist)**

Run:
```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: errors referencing `DiscoverAttributes` — this is TDD red.

### Task 7: Implement `DiscoverAttributes`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs`

- [ ] **Step 1: Add `DiscoverAttributes` to the policy class**

In `src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs`, add these `using` directives at the top of the file (after the existing ones):

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.Restier.AspNetCore.Model;
using System.Collections.Generic;
using System.Reflection;
```

Then add this method inside the class (after `ComputeTargetKey`):

```csharp
    private static readonly object[] EmptyAttributes = Array.Empty<object>();

    /// <summary>
    /// Reflects on <paramref name="apiType"/> and the target identified by <paramref name="targetKey"/>
    /// (one of <c>"class"</c>, <c>"resource:Name"</c>, or <c>"operation:Name"</c>) to collect every
    /// <see cref="IAuthorizeData"/> and <see cref="IAllowAnonymous"/> attribute placed on the API class
    /// and (where applicable) on a <see cref="ResourceAttribute"/>-decorated property or a
    /// <see cref="BoundOperationAttribute"/> / <see cref="UnboundOperationAttribute"/>-decorated method.
    /// Class attributes come first, member attributes second; ASP.NET Core's
    /// <c>AuthorizationMiddleware</c> applies its standard "AllowAnonymous wins" precedence later.
    /// Returns <see cref="EmptyAttributes"/> when nothing is found, so callers can fast-path-skip.
    /// </summary>
    internal static object[] DiscoverAttributes(Type apiType, string targetKey)
    {
        if (apiType is null) throw new ArgumentNullException(nameof(apiType));
        if (targetKey is null) throw new ArgumentNullException(nameof(targetKey));

        var classAttrs = CollectAuthAttributes(apiType.GetCustomAttributes(inherit: true));
        var memberAttrs = CollectMemberAttributes(apiType, targetKey);

        if (classAttrs.Count == 0 && memberAttrs.Count == 0)
        {
            return EmptyAttributes;
        }

        var combined = new object[classAttrs.Count + memberAttrs.Count];
        classAttrs.CopyTo(combined, 0);
        memberAttrs.CopyTo(combined, classAttrs.Count);
        return combined;
    }

    private static List<object> CollectMemberAttributes(Type apiType, string targetKey)
    {
        if (targetKey.StartsWith(ResourcePrefix, StringComparison.Ordinal))
        {
            var name = targetKey.Substring(ResourcePrefix.Length);
            var prop = apiType.GetProperty(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            // The property must actually be a registered Restier resource — otherwise we'd be honoring
            // attributes on arbitrary properties, which would surprise users.
            if (prop is null || !prop.IsDefined(typeof(ResourceAttribute), inherit: true))
            {
                return new List<object>(0);
            }

            return CollectAuthAttributes(prop.GetCustomAttributes(inherit: true));
        }

        if (targetKey.StartsWith(OperationPrefix, StringComparison.Ordinal))
        {
            var name = targetKey.Substring(OperationPrefix.Length);
            var method = apiType.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            // Same defensive check — must be a real Restier operation method.
            if (method is null
                || (!method.IsDefined(typeof(BoundOperationAttribute), inherit: true)
                    && !method.IsDefined(typeof(UnboundOperationAttribute), inherit: true)))
            {
                return new List<object>(0);
            }

            return CollectAuthAttributes(method.GetCustomAttributes(inherit: true));
        }

        return new List<object>(0);
    }

    private static List<object> CollectAuthAttributes(object[] attributes)
    {
        var result = new List<object>(attributes.Length);
        foreach (var attr in attributes)
        {
            if (attr is IAuthorizeData || attr is IAllowAnonymous)
            {
                result.Add(attr);
            }
        }
        return result;
    }
```

- [ ] **Step 2: Build the source project**

Run:
```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
```

Expected: succeeds.

- [ ] **Step 3: Run the `DiscoverAttributes` tests**

Run:
```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
  --filter "FullyQualifiedName~DiscoverAttributes"
```

Expected: 11 tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierAuthorizationMetadataPolicyTests.cs
git commit -m "$(cat <<'EOF'
feat(routing): add DiscoverAttributes for authorization metadata policy

Walks the API class, its [Resource]-decorated properties, and its
[Bound/Unbound]Operation methods looking for IAuthorizeData /
IAllowAnonymous attributes. Defensively ignores attributes on properties
without [Resource] / methods without [Operation] so user surprise stays low.

Part of #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 4 — Policy plumbing + registration

### Task 8: Write `AppliesToEndpoints` unit tests (TDD red)

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierAuthorizationMetadataPolicyTests.cs`

- [ ] **Step 1: Append the `AppliesToEndpoints` tests**

Add inside the existing `partial class`:

```csharp
    #region AppliesToEndpoints

    private static Endpoint MakeEndpoint(params object[] metadata)
    {
        return new Endpoint(
            requestDelegate: _ => System.Threading.Tasks.Task.CompletedTask,
            metadata: new EndpointMetadataCollection(metadata),
            displayName: "test");
    }

    [Fact]
    public void AppliesToEndpoints_NoRestierEndpoint_ReturnsFalse()
    {
        var policy = new RestierAuthorizationMetadataPolicy();
        var endpoints = new[] { MakeEndpoint(), MakeEndpoint("some-other-marker") };

        var applies = ((Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy)policy)
            .AppliesToEndpoints(endpoints);

        applies.Should().BeFalse();
    }

    [Fact]
    public void AppliesToEndpoints_OneRestierEndpoint_ReturnsTrue()
    {
        var policy = new RestierAuthorizationMetadataPolicy();
        var endpoints = new[]
        {
            MakeEndpoint(),
            MakeEndpoint(new RestierRouteMarker(typeof(PlainApi))),
        };

        var applies = ((Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy)policy)
            .AppliesToEndpoints(endpoints);

        applies.Should().BeTrue();
    }

    #endregion
```

`MakeEndpoint` plus the `IEndpointSelectorPolicy` cast hint at the next step: the class must implement that interface.

- [ ] **Step 2: Run the build to confirm interface is not yet implemented**

Run:
```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: `error CS0030: Cannot convert type 'RestierAuthorizationMetadataPolicy' to 'IEndpointSelectorPolicy'`. TDD-red.

### Task 9: Implement `IEndpointSelectorPolicy` and `AppliesToEndpoints`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs`

- [ ] **Step 1: Add the interface declaration and the `AppliesToEndpoints` method**

In `src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs`, change the class declaration from:

```csharp
internal sealed class RestierAuthorizationMetadataPolicy : MatcherPolicy
```

to:

```csharp
internal sealed class RestierAuthorizationMetadataPolicy : MatcherPolicy, IEndpointSelectorPolicy
```

Add a new `using` if not present:
```csharp
using System.Collections.Generic;
```

Add the `AppliesToEndpoints` method after `Order`:

```csharp
    /// <inheritdoc/>
    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        // Fast path: cheap metadata scan, no DI lookups.
        for (var i = 0; i < endpoints.Count; i++)
        {
            if (endpoints[i].Metadata.GetMetadata<RestierRouteMarker>() is not null)
            {
                return true;
            }
        }
        return false;
    }
```

Also add a stub `ApplyAsync` so the interface is satisfied — Task 11 fills it in:

```csharp
    /// <inheritdoc/>
    public System.Threading.Tasks.Task ApplyAsync(Microsoft.AspNetCore.Http.HttpContext httpContext, CandidateSet candidates)
    {
        return System.Threading.Tasks.Task.CompletedTask;
    }
```

- [ ] **Step 2: Build the source and tests**

Run:
```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: succeeds.

- [ ] **Step 3: Run the `AppliesToEndpoints` tests**

Run:
```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
  --filter "FullyQualifiedName~AppliesToEndpoints"
```

Expected: 2 tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierAuthorizationMetadataPolicyTests.cs
git commit -m "$(cat <<'EOF'
feat(routing): implement IEndpointSelectorPolicy.AppliesToEndpoints

Cheap metadata scan that engages the policy only for Restier routes.
ApplyAsync is a stub for now; filled in next.

Part of #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 10: Write `ApplyAsync` unit tests (TDD red)

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierAuthorizationMetadataPolicyTests.cs`

- [ ] **Step 1: Append the `ApplyAsync` tests**

Add inside the existing `partial class`:

```csharp
    #region ApplyAsync

    private static Microsoft.AspNetCore.Http.HttpContext MakeHttpContextWithODataPath(IEdmModel model, string odataPath)
    {
        var ctx = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        var feature = ctx.ODataFeature();
        feature.Path = ParsePath(model, odataPath);
        feature.Model = model;
        return ctx;
    }

    private static CandidateSet MakeCandidateSet(Endpoint endpoint)
    {
        var candidates = new Endpoint[] { endpoint };
        var values = new[] { new RouteValueDictionary() };
        var validities = new[] { true };
        return new CandidateSet(candidates, values, validities);
    }

    [Fact]
    public async System.Threading.Tasks.Task ApplyAsync_NonRestierCandidate_LeavesEndpointUnchanged()
    {
        var model = BuildTestModel();
        var policy = new RestierAuthorizationMetadataPolicy();
        var original = MakeEndpoint(); // no marker
        var candidates = MakeCandidateSet(original);
        var http = MakeHttpContextWithODataPath(model, "People");

        await ((Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy)policy).ApplyAsync(http, candidates);

        candidates[0].Endpoint.Should().BeSameAs(original);
    }

    [Fact]
    public async System.Threading.Tasks.Task ApplyAsync_NoAttributes_LeavesEndpointUnchanged()
    {
        var model = BuildTestModel();
        var policy = new RestierAuthorizationMetadataPolicy();
        var marker = new RestierRouteMarker(typeof(PlainApi));
        var original = MakeEndpoint(marker);
        var candidates = MakeCandidateSet(original);
        var http = MakeHttpContextWithODataPath(model, "People");

        await ((Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy)policy).ApplyAsync(http, candidates);

        candidates[0].Endpoint.Should().BeSameAs(original);
    }

    [Fact]
    public async System.Threading.Tasks.Task ApplyAsync_ClassAllowAnonymous_ReplacesEndpointWithAugmentedMetadata()
    {
        var model = BuildTestModel();
        var policy = new RestierAuthorizationMetadataPolicy();
        var marker = new RestierRouteMarker(typeof(ClassAnonymousApi));
        var original = MakeEndpoint(marker);
        var candidates = MakeCandidateSet(original);
        var http = MakeHttpContextWithODataPath(model, "People");

        await ((Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy)policy).ApplyAsync(http, candidates);

        var wrapped = candidates[0].Endpoint;
        wrapped.Should().NotBeSameAs(original);
        wrapped.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>().Should().NotBeNull();
        // Original metadata is preserved.
        wrapped.Metadata.GetMetadata<RestierRouteMarker>().Should().BeSameAs(marker);
    }

    [Fact]
    public async System.Threading.Tasks.Task ApplyAsync_DifferentEndpointsForSameApiAndTarget_BothGetWrappedIndividually()
    {
        // Regression for the cache-key bug: same (apiType, targetKey) can be requested for
        // different candidate endpoints (e.g., RestierController.Get vs RestierController.Post).
        // Each must be wrapped independently — never substituted for the cached wrapper of another.
        var model = BuildTestModel();
        var policy = new RestierAuthorizationMetadataPolicy();
        var marker = new RestierRouteMarker(typeof(ClassAnonymousApi));

        var firstOriginal = MakeEndpoint(marker, "FirstAction");
        var firstCandidates = MakeCandidateSet(firstOriginal);
        var http1 = MakeHttpContextWithODataPath(model, "People");
        await ((Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy)policy).ApplyAsync(http1, firstCandidates);
        var firstWrapped = firstCandidates[0].Endpoint;

        var secondOriginal = MakeEndpoint(marker, "SecondAction");
        var secondCandidates = MakeCandidateSet(secondOriginal);
        var http2 = MakeHttpContextWithODataPath(model, "People");
        await ((Microsoft.AspNetCore.Routing.Matching.IEndpointSelectorPolicy)policy).ApplyAsync(http2, secondCandidates);
        var secondWrapped = secondCandidates[0].Endpoint;

        firstWrapped.Should().NotBeSameAs(secondWrapped);
        // Each wrapped endpoint must preserve the metadata of its specific original candidate.
        firstWrapped.Metadata.Should().Contain(m => "FirstAction".Equals(m));
        secondWrapped.Metadata.Should().Contain(m => "SecondAction".Equals(m));
        firstWrapped.Metadata.Should().NotContain(m => "SecondAction".Equals(m));
        secondWrapped.Metadata.Should().NotContain(m => "FirstAction".Equals(m));
    }

    #endregion
```

- [ ] **Step 2: Build the tests to confirm compile errors / failures**

Run:
```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: build succeeds, but running tests will fail because `ApplyAsync` is currently a stub returning immediately.

- [ ] **Step 3: Run the tests to verify they fail**

Run:
```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
  --filter "FullyQualifiedName~ApplyAsync"
```

Expected: 2 tests pass (`NonRestierCandidate`, `NoAttributes` — by accident, since the stub doesn't change anything); 2 tests fail (the augmenting / per-candidate-wrap ones).

### Task 11: Implement `ApplyAsync` with attribute cache + per-candidate `WrapEndpoint`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs`

- [ ] **Step 1: Add the cache field, `using`s, and rewrite `ApplyAsync` + `WrapEndpoint`**

In `src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs`:

Add `using`s at the top:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Extensions;
using System.Collections.Concurrent;
using System.Threading.Tasks;
```

Add the cache field inside the class, near `EmptyAttributes`:

```csharp
    private readonly ConcurrentDictionary<(Type apiType, string targetKey), object[]> attributeCache = new();
```

Replace the stub `ApplyAsync` with:

```csharp
    /// <inheritdoc/>
    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        var path = httpContext.ODataFeature().Path;

        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i))
            {
                continue;
            }

            var candidate = candidates[i];
            var marker = candidate.Endpoint.Metadata.GetMetadata<RestierRouteMarker>();
            if (marker is null)
            {
                continue;
            }

            var targetKey = ComputeTargetKey(path);
            var cacheKey = (marker.ApiType, targetKey);

            var attributes = attributeCache.GetOrAdd(
                cacheKey,
                static key => DiscoverAttributes(key.apiType, key.targetKey));

            if (attributes.Length == 0)
            {
                // No auth metadata to add — fastest path: skip allocation entirely.
                continue;
            }

            var wrapped = WrapEndpoint(candidate.Endpoint, attributes);
            candidates.ReplaceEndpoint(i, wrapped, candidate.Values);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Builds a fresh <see cref="Endpoint"/> whose metadata is the original's metadata
    /// concatenated with the discovered auth attributes. A fresh endpoint per candidate
    /// is required: the same (apiType, targetKey) tuple can map to different
    /// <c>RestierController</c> actions (Get / Post / Put / …) and different route prefixes,
    /// so we cannot reuse a cached wrapped endpoint across candidates.
    /// </summary>
    internal static Endpoint WrapEndpoint(Endpoint original, object[] extraAttributes)
    {
        // Concatenate without LINQ to keep the hot path allocation-aware.
        var originalMetadata = original.Metadata;
        var combined = new object[originalMetadata.Count + extraAttributes.Length];
        var index = 0;
        foreach (var item in originalMetadata)
        {
            combined[index++] = item;
        }
        for (var i = 0; i < extraAttributes.Length; i++)
        {
            combined[index++] = extraAttributes[i];
        }
        var combinedMetadata = new EndpointMetadataCollection(combined);

        if (original is RouteEndpoint routeEndpoint)
        {
            return new RouteEndpoint(
                routeEndpoint.RequestDelegate,
                routeEndpoint.RoutePattern,
                routeEndpoint.Order,
                combinedMetadata,
                routeEndpoint.DisplayName);
        }

        return new Endpoint(original.RequestDelegate, combinedMetadata, original.DisplayName);
    }
```

- [ ] **Step 2: Build the source and tests**

Run:
```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: succeeds.

- [ ] **Step 3: Run the full `RestierAuthorizationMetadataPolicyTests` class**

Run:
```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
  --filter "FullyQualifiedName~RestierAuthorizationMetadataPolicyTests"
```

Expected: all 25 tests pass (8 ComputeTargetKey + 11 DiscoverAttributes + 2 AppliesToEndpoints + 4 ApplyAsync).

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Routing/RestierAuthorizationMetadataPolicy.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierAuthorizationMetadataPolicyTests.cs
git commit -m "$(cat <<'EOF'
feat(routing): implement RestierAuthorizationMetadataPolicy.ApplyAsync

ApplyAsync walks the candidate set, identifies Restier candidates via
RestierRouteMarker, looks up the user-code attributes (cached by
(apiType, targetKey)), and replaces each matching candidate with a
freshly-wrapped endpoint carrying the augmented metadata.

Wrapping is per-candidate because the same (apiType, targetKey) maps
to different RestierController actions (Get / Post / …) depending on
HTTP method — the cache holds only attribute lists, never endpoints.

Part of #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 12: Register the policy via `AddRestier`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Extensions/RestierIMvcBuilderExtensions.cs`

- [ ] **Step 1: Factor a private helper and register the matcher policy**

Open `src/Microsoft.Restier.AspNetCore/Extensions/RestierIMvcBuilderExtensions.cs`. Add `using`s at the top if not present:

```csharp
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Restier.AspNetCore.Routing;
```

Add a private helper after the class definition opens:

```csharp
    private static void AddRestierServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<Routing.RestierRouteValueTransformer>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<MatcherPolicy, RestierAuthorizationMetadataPolicy>());
    }
```

Then update each of the four `AddRestier` overloads to call the helper. Replace lines that look like:

```csharp
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<Routing.RestierRouteValueTransformer>();
```

with:

```csharp
        AddRestierServices(builder.Services);
```

That covers four overloads (lines ~56, ~72, ~88, ~107). Leave the `RestierMvcOptionsSetup` registration in the two `alternateBaseUri` overloads as-is — it's specific to those.

- [ ] **Step 2: Build the source**

Run:
```bash
dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj
```

Expected: succeeds.

- [ ] **Step 3: Run the full AspNetCore test suite (smoke check)**

Run:
```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: all tests pass. The matcher policy is now wired into every `AddRestier`-built host. Existing tests that have no auth attributes anywhere see `DiscoverAttributes` return `EmptyAttributes` for class-level and the policy short-circuits.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Extensions/RestierIMvcBuilderExtensions.cs
git commit -m "$(cat <<'EOF'
feat(di): register RestierAuthorizationMetadataPolicy from AddRestier

Factor a private AddRestierServices helper called from all four
AddRestier overloads. It registers the matcher policy via
TryAddEnumerable, so existing AddRestier callers get [AllowAnonymous]
/ [Authorize] honoring with no app.Use… change required.

Resolves #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 5 — Integration test infrastructure

### Task 13: Add the `TestAuthHandler`

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/TestAuthHandler.cs`

- [ ] **Step 1: Create the handler**

Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/TestAuthHandler.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

/// <summary>
/// Minimal authentication handler for integration tests. Reads the <c>X-Test-User</c> request
/// header: when present, constructs a <see cref="ClaimsPrincipal"/> with <c>Name == "TestUser"</c>
/// and a <c>Role == "admin"</c> claim if the header value is <c>"Admin"</c>. Anonymous requests
/// (no header) produce an authentication failure, which leaves <see cref="Microsoft.AspNetCore.Http.HttpContext.User"/>
/// unauthenticated — the standard <see cref="Microsoft.AspNetCore.Authorization.AuthorizationMiddleware"/>
/// then enforces or skips authorization per endpoint metadata.
/// </summary>
internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string HeaderName = "X-Test-User";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValues) || headerValues.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var role = headerValues[0];
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "TestUser"),
        };
        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToLowerInvariant()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

- [ ] **Step 2: Build the tests**

Run:
```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/TestAuthHandler.cs
git commit -m "$(cat <<'EOF'
test: add TestAuthHandler for integration-test middleware auth wiring

Reads X-Test-User request header and constructs a ClaimsPrincipal
with optional admin role. No header => anonymous (NoResult). Used by
AnonymousAccessTests scenarios that exercise [Authorize(Policy=...)]
end to end.

Part of #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 14: Add anonymous-access API test fixtures

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessApis.cs`

- [ ] **Step 1: Create the fixture file**

Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessApis.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using System.Linq;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

/// <summary>
/// Plain entity type used as the row type for all anonymous-access fixture APIs.
/// </summary>
public class AnonPerson
{
    public int Id { get; set; }
    public string Name { get; set; }
}

/// <summary>
/// Plain entity type used to demonstrate a non-anonymous resource alongside an anonymous one.
/// </summary>
public class AnonOrder
{
    public int Id { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// API where the entire class is anonymous-allowed. With a global [Authorize] filter, every
/// route this API serves should bypass authentication.
/// </summary>
[AllowAnonymous]
public class AnonymousAtClassApi : ApiBase
{
    public AnonymousAtClassApi(IEdmModel model, IQueryHandler q, ISubmitHandler s) : base(model, q, s) { }

    [Resource]
    public IQueryable<AnonPerson> People => System.Linq.Enumerable.Empty<AnonPerson>().AsQueryable();
}

/// <summary>
/// API that does NOT declare [AllowAnonymous]. Used as the control case: with a global
/// [Authorize] filter, every route should require authentication.
/// </summary>
public class RequireAuthApi : ApiBase
{
    public RequireAuthApi(IEdmModel model, IQueryHandler q, ISubmitHandler s) : base(model, q, s) { }

    [Resource]
    public IQueryable<AnonPerson> People => System.Linq.Enumerable.Empty<AnonPerson>().AsQueryable();
}

/// <summary>
/// API where one resource property is [AllowAnonymous] while another is not.
/// </summary>
public class AnonymousAtResourceApi : ApiBase
{
    public AnonymousAtResourceApi(IEdmModel model, IQueryHandler q, ISubmitHandler s) : base(model, q, s) { }

    [Resource]
    [AllowAnonymous]
    public IQueryable<AnonPerson> PublicPeople => System.Linq.Enumerable.Empty<AnonPerson>().AsQueryable();

    [Resource]
    public IQueryable<AnonOrder> PrivateOrders => System.Linq.Enumerable.Empty<AnonOrder>().AsQueryable();
}

/// <summary>
/// API where one operation method is [AllowAnonymous] while another is restricted by [Authorize(Policy=...)]
/// and a third has no attribute (inherits class-level).
/// </summary>
public class AnonymousAtOperationApi : ApiBase
{
    public AnonymousAtOperationApi(IEdmModel model, IQueryHandler q, ISubmitHandler s) : base(model, q, s) { }

    [Resource]
    public IQueryable<AnonPerson> People => System.Linq.Enumerable.Empty<AnonPerson>().AsQueryable();

    [UnboundOperation]
    [AllowAnonymous]
    public AnonPerson Hello() => new AnonPerson { Id = 1, Name = "Hello" };

    [UnboundOperation]
    [Authorize(Policy = "AdminOnly")]
    public AnonPerson AdminGreeting() => new AnonPerson { Id = 2, Name = "Hi Admin" };

    [UnboundOperation]
    public AnonPerson DefaultGreeting() => new AnonPerson { Id = 3, Name = "Default" };
}

/// <summary>
/// API class with class-level [Authorize] AND a [Resource] property carrying [AllowAnonymous].
/// Used to verify that AllowAnonymous on the member wins over class-level Authorize.
/// </summary>
[Authorize]
public class MixedAuthorizationApi : ApiBase
{
    public MixedAuthorizationApi(IEdmModel model, IQueryHandler q, ISubmitHandler s) : base(model, q, s) { }

    [Resource]
    [AllowAnonymous]
    public IQueryable<AnonPerson> PublicPeople => System.Linq.Enumerable.Empty<AnonPerson>().AsQueryable();

    [Resource]
    public IQueryable<AnonOrder> RestrictedOrders => System.Linq.Enumerable.Empty<AnonOrder>().AsQueryable();
}

/// <summary>
/// Base API class with [Authorize]. Used together with <see cref="InheritsAuthApi"/> to verify inheritance.
/// </summary>
[Authorize]
public class BaseAuthApi : ApiBase
{
    public BaseAuthApi(IEdmModel model, IQueryHandler q, ISubmitHandler s) : base(model, q, s) { }

    [Resource]
    public IQueryable<AnonPerson> People => System.Linq.Enumerable.Empty<AnonPerson>().AsQueryable();
}

/// <summary>
/// Subclass with no attributes — inherits [Authorize] from <see cref="BaseAuthApi"/>.
/// </summary>
public class InheritsAuthApi : BaseAuthApi
{
    public InheritsAuthApi(IEdmModel model, IQueryHandler q, ISubmitHandler s) : base(model, q, s) { }
}
```

- [ ] **Step 2: Build the tests**

Run:
```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
```

Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessApis.cs
git commit -m "$(cat <<'EOF'
test: add AnonymousAccessApis fixture types

Seven ApiBase subclasses covering the matrix of class-level, resource-property,
operation-method, mixed, and inheritance scenarios for #717 integration tests.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 6 — Integration tests

### Task 15: Add the integration test class with class-level scenarios

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs`

- [ ] **Step 1: Create the test file with class-level scenarios (1, 2, 8, 9)**

Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public class AnonymousAccessTests
{
    /// <summary>
    /// Configures the test pipeline with a global [Authorize] filter, the test authentication
    /// scheme, and (optionally) the "AdminOnly" policy required by the
    /// <see cref="AnonymousAtOperationApi"/> fixture. The "Test" scheme is registered as the
    /// default so the filter has a scheme to challenge.
    /// </summary>
    private static Action<IServiceCollection> ConfigureAuthServices(bool addAdminPolicy = true)
        => services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.AddAuthorization(o =>
            {
                if (addAdminPolicy)
                {
                    o.AddPolicy("AdminOnly", p => p.RequireRole("admin"));
                }
            });
            services.Configure<MvcOptions>(o => o.Filters.Add(new AuthorizeFilter()));
        };

    /// <summary>
    /// Hook into Breakdance's ApplicationBuilderAction (which runs before UseRouting in the harness)
    /// to wire UseAuthentication() before the routing/authorization middleware.
    /// </summary>
    private static Action<IApplicationBuilder> UseAuthenticationHook
        => builder => builder.UseAuthentication();

    private static async Task<HttpResponseMessage> SendAsync<TApi>(
        HttpMethod method,
        string resource,
        string asUser = null,
        bool addAdminPolicy = true)
        where TApi : ApiBase
    {
        return await RestierTestHelpers.ExecuteTestRequest<TApi>(
            method,
            resource: resource,
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureAuthServices(addAdminPolicy),
            applicationBuilderAction: UseAuthenticationHook,
            customHeaders: asUser is null
                ? null
                : new[] { (TestAuthHandler.HeaderName, asUser) });
    }

    #region Class-level

    [Fact]
    public async Task ClassAllowAnonymous_BypassesGlobalAuthorizeFilter()
    {
        // Scenario 1: global [Authorize] + class [AllowAnonymous] + anonymous GET /People → 200.
        var response = await SendAsync<AnonymousAtClassApi>(HttpMethod.Get, "/People");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NoClassAttribute_AnonymousRequest_Returns401()
    {
        // Scenario 2 (control case): global [Authorize], no class attribute, anonymous GET /People → 401.
        var response = await SendAsync<RequireAuthApi>(HttpMethod.Get, "/People");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClassAllowAnonymous_MetadataAccessible()
    {
        // Scenario 8: $metadata + class [AllowAnonymous] + global Authorize → 200.
        var response = await SendAsync<AnonymousAtClassApi>(HttpMethod.Get, "/$metadata");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClassAllowAnonymous_ServiceDocumentAccessible()
    {
        // Scenario 9: service document (GET /) + class [AllowAnonymous] → 200.
        var response = await SendAsync<AnonymousAtClassApi>(HttpMethod.Get, "/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
```

- [ ] **Step 2: Check that `RestierTestHelpers.ExecuteTestRequest` supports `applicationBuilderAction` and `customHeaders`**

Run:
```bash
grep -n "applicationBuilderAction\|customHeaders" src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs src/Microsoft.Restier.Breakdance/RestierBreakdanceTestBase.cs
```

If both parameters are present on `ExecuteTestRequest`, proceed. If `applicationBuilderAction` exists but `customHeaders` does not (or vice versa), inspect the actual signature with:

```bash
grep -A 20 "public static .* ExecuteTestRequest" src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs | head -40
```

and adjust the test helper call accordingly. The two parameters this plan assumes:

| Parameter | Purpose |
|-----------|---------|
| `applicationBuilderAction: Action<IApplicationBuilder>` | Lambda passed to `RestierBreakdanceTestBase.ApplicationBuilderAction` so it runs before `UseRouting` in the harness (see `RestierBreakdanceTestBase.cs:106`). |
| `customHeaders: IEnumerable<(string, string)>` | Headers attached to the outbound `HttpRequestMessage` for the test request. |

If the helper does not expose one of these, the implementer must:
- For `applicationBuilderAction` missing: set it directly on the underlying test base, or add a small overload to the helper that accepts and forwards it. Prefer adding the overload since the test pattern will repeat across this test class.
- For `customHeaders` missing: construct the `HttpRequestMessage` manually with the header and use `HttpClient.SendAsync` directly inside each test.

- [ ] **Step 3: Build and run the new tests**

Run:
```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
  --filter "FullyQualifiedName~AnonymousAccessTests"
```

Expected: 4 tests pass.

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs
git commit -m "$(cat <<'EOF'
test(feature): class-level [AllowAnonymous] integration tests

Wires UseAuthentication via the existing ApplicationBuilderAction hook
(runs before UseRouting in the Breakdance harness). Adds four class-level
scenarios — anonymous-allowed API works, control case still 401s,
metadata + service document follow class-level attribute.

Part of #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 16: Add resource-property integration tests

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs`

- [ ] **Step 1: Append the resource-property `#region` to the test class**

Append inside the `AnonymousAccessTests` class (after the `#region Class-level ... #endregion` block):

```csharp
    #region Resource property

    [Fact]
    public async Task ResourceAllowAnonymous_AccessibleAnonymously()
    {
        // Scenario 3: [AllowAnonymous] on [Resource] property → anonymous GET /PublicPeople → 200.
        var response = await SendAsync<AnonymousAtResourceApi>(HttpMethod.Get, "/PublicPeople");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResourceWithoutAttribute_AnonymousRequest_Returns401()
    {
        // Scenario 4: same fixture, anonymous GET /PrivateOrders (no attribute on this resource,
        // none on class) → 401.
        var response = await SendAsync<AnonymousAtResourceApi>(HttpMethod.Get, "/PrivateOrders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClassAuthorize_ResourceAllowAnonymous_ResourceBypassesAuth()
    {
        // Scenario 11: class [Authorize], member [AllowAnonymous] → that resource bypasses auth.
        var response = await SendAsync<MixedAuthorizationApi>(HttpMethod.Get, "/PublicPeople");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClassAuthorize_RestrictedResource_AnonymousRequestReturns401()
    {
        // Scenario 11 control: same fixture, /RestrictedOrders inherits class-level [Authorize] → 401.
        var response = await SendAsync<MixedAuthorizationApi>(HttpMethod.Get, "/RestrictedOrders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
```

- [ ] **Step 2: Run the new tests**

Run:
```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
  --filter "FullyQualifiedName~AnonymousAccessTests"
```

Expected: 8 tests pass.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs
git commit -m "$(cat <<'EOF'
test(feature): [AllowAnonymous] / [Authorize] on [Resource] property

Four scenarios covering per-resource auth: anonymous-allowed resource
bypasses auth, sibling restricted resource still requires it; class-level
[Authorize] is overridden by member [AllowAnonymous].

Part of #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 17: Add operation-method integration tests (with policy)

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs`

- [ ] **Step 1: Append the operation `#region`**

Append inside the `AnonymousAccessTests` class (after the resource-property region):

```csharp
    #region Operation method

    [Fact]
    public async Task OperationAllowAnonymous_AccessibleAnonymously()
    {
        // Scenario 5: [AllowAnonymous] on [UnboundOperation] → anonymous /Hello() → 200.
        var response = await SendAsync<AnonymousAtOperationApi>(HttpMethod.Get, "/Hello()");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OperationWithAdminPolicy_AdminUser_Allowed()
    {
        // Scenario 7: [Authorize(Policy = "AdminOnly")] on operation, authenticated admin → 200.
        var response = await SendAsync<AnonymousAtOperationApi>(HttpMethod.Get, "/AdminGreeting()", asUser: "Admin");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OperationWithAdminPolicy_NonAdminUser_Returns403()
    {
        // Scenario 6: same operation, authenticated non-admin user → 403.
        var response = await SendAsync<AnonymousAtOperationApi>(HttpMethod.Get, "/AdminGreeting()", asUser: "User");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OperationWithAdminPolicy_AnonymousUser_Returns401()
    {
        // Scenario 6 alt: same operation, no auth header → 401 (challenge).
        var response = await SendAsync<AnonymousAtOperationApi>(HttpMethod.Get, "/AdminGreeting()");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OperationWithoutAttribute_InheritsGlobalAuth_AnonymousReturns401()
    {
        // Sanity: operation with no attributes inherits the global [Authorize] filter.
        var response = await SendAsync<AnonymousAtOperationApi>(HttpMethod.Get, "/DefaultGreeting()");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
```

- [ ] **Step 2: Run the new tests**

Run:
```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
  --filter "FullyQualifiedName~AnonymousAccessTests"
```

Expected: 13 tests pass.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs
git commit -m "$(cat <<'EOF'
test(feature): [AllowAnonymous] / [Authorize(Policy=...)] on operations

Five scenarios: anonymous-allowed operation, admin-only policy granted
to admin user, denied to non-admin, challenged for anonymous, plus a
sanity check that non-attributed operations inherit the global filter.

Part of #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 18: Add inheritance integration test

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs`

- [ ] **Step 1: Append the inheritance `#region`**

Append inside the `AnonymousAccessTests` class:

```csharp
    #region Inheritance

    [Fact]
    public async Task InheritedAuthorize_AnonymousReturns401()
    {
        // Scenario 12: base class [Authorize], subclass without override → subclass inherits.
        var response = await SendAsync<InheritsAuthApi>(HttpMethod.Get, "/People");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InheritedAuthorize_AuthenticatedUserSucceeds()
    {
        // Scenario 12 control: same inheritance, authenticated user → 200.
        var response = await SendAsync<InheritsAuthApi>(HttpMethod.Get, "/People", asUser: "User");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
```

- [ ] **Step 2: Run the new tests**

Run:
```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
  --filter "FullyQualifiedName~AnonymousAccessTests"
```

Expected: 15 tests pass.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs
git commit -m "$(cat <<'EOF'
test(feature): [Authorize] inheritance scenario

Verifies a subclass with no attributes inherits the base class's
[Authorize] — anonymous denied, authenticated allowed.

Part of #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

### Task 19: Add `$batch` integration test

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs`

- [ ] **Step 1: Append the batch `#region`**

Append inside the `AnonymousAccessTests` class:

```csharp
    #region Batch

    [Fact]
    public async Task BatchWithMixedAuth_EachChildHonoredIndependently()
    {
        // Scenario 10: $batch containing two children — one targeting an anonymous resource,
        // one targeting a restricted resource. Anonymous client → first child 200, second 401.
        // We test against MixedAuthorizationApi which has [Authorize] at the class level and
        // [AllowAnonymous] on PublicPeople.
        var batchBody =
            "--batch_test\r\n" +
            "Content-Type: application/http\r\n" +
            "Content-Transfer-Encoding: binary\r\n" +
            "\r\n" +
            "GET PublicPeople HTTP/1.1\r\n" +
            "Accept: application/json\r\n" +
            "\r\n" +
            "\r\n" +
            "--batch_test\r\n" +
            "Content-Type: application/http\r\n" +
            "Content-Transfer-Encoding: binary\r\n" +
            "\r\n" +
            "GET RestrictedOrders HTTP/1.1\r\n" +
            "Accept: application/json\r\n" +
            "\r\n" +
            "\r\n" +
            "--batch_test--\r\n";

        var request = new HttpRequestMessage(HttpMethod.Post, "/$batch")
        {
            Content = new StringContent(batchBody, System.Text.Encoding.UTF8, "multipart/mixed"),
        };
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("multipart/mixed")
        {
            Parameters = { new System.Net.Http.Headers.NameValueHeaderValue("boundary", "batch_test") },
        };

        var response = await RestierTestHelpers.ExecuteTestRequestAsync<MixedAuthorizationApi>(
            request,
            serviceCollection: ConfigureAuthServices(addAdminPolicy: false),
            applicationBuilderAction: UseAuthenticationHook);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        // Per-child status lines appear in the batch response body.
        body.Should().Contain("HTTP/1.1 200")
            .And.Contain("HTTP/1.1 401");
    }

    #endregion
```

If `ExecuteTestRequestAsync(HttpRequestMessage, …)` is not available on `RestierTestHelpers`, look for the closest matching overload (`SendRequestAsync` or similar) and use it. If none takes a raw `HttpRequestMessage`, the implementer must construct the test client directly via `RestierBreakdanceTestBase.TestServer.CreateClient()` and send the request through it.

- [ ] **Step 2: Run the new test**

Run:
```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj \
  --filter "FullyQualifiedName~AnonymousAccessTests.BatchWithMixedAuth"
```

Expected: passes. If the body assertions fail, the matcher policy may not be firing for each batch child — check that `ODataFeature.Path` is populated per child by `ODataBatchHttpContextFixerMiddleware` before the matcher policy runs.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs
git commit -m "$(cat <<'EOF'
test(feature): per-child auth in $batch requests

Submits a $batch with one anonymous-allowed child and one restricted
child. Anonymous client gets 200 for the first, 401 for the second
— confirms the matcher policy fires per child operation.

Part of #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 7 — Documentation

### Task 20: Update `method-authorization.mdx`

**Files:**
- Modify: `src/Microsoft.Restier.Docs/guides/server/method-authorization.mdx`

- [ ] **Step 1: Insert the new top section**

Open `src/Microsoft.Restier.Docs/guides/server/method-authorization.mdx`. Immediately after the introductory paragraphs (after line 18, before the line `## Convention-Based Authorization` at line 19), insert this new section:

```mdx
## Using `[AllowAnonymous]` and `[Authorize]`

RESTier honors the standard ASP.NET Core authorization attributes
(`[AllowAnonymous]`, `[Authorize]`, `[Authorize(Policy = "...")]`, `[Authorize(Roles = "...")]`)
on three surfaces of your API class. They behave exactly like they do on any other
ASP.NET Core controller or action — they participate in `AuthorizationMiddleware` via
endpoint metadata, including standard precedence (`AllowAnonymous` wins over `Authorize`).

### Where attributes can go

```csharp TrippinApi.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.EntityFrameworkCore;

// 1. On the API class itself — applies to every route served by this API.
[AllowAnonymous]
public class TrippinApi : EntityFrameworkApi<TrippinContext>
{
    public TrippinApi(TrippinContext db, IEdmModel m, IQueryHandler q, ISubmitHandler s)
        : base(db, m, q, s) { }
}

public class LibraryApi : EntityFrameworkApi<LibraryContext>
{
    public LibraryApi(LibraryContext db, IEdmModel m, IQueryHandler q, ISubmitHandler s)
        : base(db, m, q, s) { }

    // 2. On a [Resource] property — applies to that resource only.
    [AllowAnonymous]
    [Resource]
    public IQueryable<Book> BooksWithPublisher => DbContext.Books.Include(b => b.Publisher);

    // 3. On a [BoundOperation] or [UnboundOperation] method — applies to that operation only.
    [UnboundOperation]
    [Authorize(Policy = "Admin")]
    public void ResetDataSource() { /* ... */ }
}
```

### How RESTier authorization relates to ASP.NET Core authorization

Think of them as two complementary layers:

| Layer | What it controls | How you opt in |
|-------|------------------|----------------|
| **ASP.NET Core authentication / authorization** | Whether the request reaches RESTier at all (authentication scheme, policy, role, anonymous override) | `[AllowAnonymous]` / `[Authorize]` attributes, evaluated by `AuthorizationMiddleware` |
| **RESTier authorization** | Whether an authenticated request is allowed to perform a specific entity-set or operation action (`Can{Op}{EntitySet}`, custom `IChangeSetItemAuthorizer`) | Convention methods or chained services on your API class |

`[AllowAnonymous]` *only* tells `AuthorizationMiddleware` to skip the standard auth check. It
does not bypass RESTier's `Can*` methods. Use the convention methods (`CanDelete{EntitySet}`,
etc.) when you need RESTier-level authorization to behave differently for anonymous vs
authenticated users.

### Precedence

RESTier delegates to the standard ASP.NET Core precedence rules:

- `[AllowAnonymous]` always wins over `[Authorize]`, regardless of which is on the class
  vs the member.
- `[Authorize]` attributes are combined (all roles, schemes, policies must be satisfied).

<Info>
Inherited attributes are honored too. If a base API class declares `[Authorize]` and a
subclass doesn't override it, the subclass inherits the requirement.
</Info>

### Limitation: DbSet-backed entity sets

Entity sets that come from a `DbContext`'s `DbSet<T>` properties (the canonical Entity
Framework case) have no anchor on your `ApiBase` subclass — so you can't attach
`[AllowAnonymous]` to just `Books`. The class-level attribute always covers them. For
per-DbSet-entity-set granularity, use RESTier's existing `Can{Op}{EntitySet}` convention
methods (described below), which can inspect `ClaimsPrincipal.Current` directly.

```

- [ ] **Step 2: Add a one-line cross-reference at the top of "Convention-Based Authorization"**

Immediately after the line `## Convention-Based Authorization`, before the existing paragraph, insert:

```mdx

<Tip>
For controlling **whether ASP.NET Core auth runs at all** (e.g. overriding a global `[Authorize]`
filter), use `[AllowAnonymous]` / `[Authorize]` as described in the section above. The
convention-based methods here run *after* ASP.NET Core authorization has admitted the request.
</Tip>

```

- [ ] **Step 3: Build the docs project**

Run:
```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: succeeds. (The docs SDK regenerates `docs.json` from the template; if it diffs significantly, commit the regenerated file.)

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/method-authorization.mdx \
        src/Microsoft.Restier.Docs/docs.json
git commit -m "$(cat <<'EOF'
docs: document [AllowAnonymous] / [Authorize] on RESTier API surfaces

Adds a new top section explaining placement (class / [Resource] /
operation), the relationship to RESTier's convention-based and
centralized authorizers, precedence rules, and the DbSet-backed
entity-set limitation.

Resolves docs portion of #717.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 8 — Final verification

### Task 21: Run the full RESTier solution test pass

**Files:** none

- [ ] **Step 1: Build everything**

Run:
```bash
dotnet build RESTier.slnx
```

Expected: succeeds with no errors and no new warnings.

- [ ] **Step 2: Run the full test suite**

Run:
```bash
dotnet test RESTier.slnx
```

Expected: all tests pass. Pay particular attention to:
- `Microsoft.Restier.Tests.AspNetCore.Routing.RestierRouteValueTransformerTests` — must still pass after the marker enrichment.
- `Microsoft.Restier.Tests.AspNetCore.Routing.RestierAuthorizationMetadataPolicyTests` — all 25 unit tests.
- `Microsoft.Restier.Tests.AspNetCore.FeatureTests.AnonymousAccessTests` — all 16 integration tests.
- `Microsoft.Restier.Tests.AspNetCore.FeatureTests.AuthorizationTests` (existing, RESTier-level) — must still pass.
- `Microsoft.Restier.Tests.AspNetCore.FeatureTests.BatchTests` — must still pass (the batch path is touched by the matcher-policy work).
- The full `Microsoft.Restier.Tests.AspNetCore.NSwag` project (it exercises a different `AddRestier` overload path).

- [ ] **Step 3: If everything passes, the feature is complete**

The implementation is done. The next workflow step is updating the changelog / release notes and opening a PR; that's outside this plan.

---

## Self-Review

After writing the plan, I scanned against the spec:

- **Spec § Goal** → covered by Tasks 1, 2, 3, 12 (class-level surface wired end-to-end).
- **Spec § Decisions row "Surfaces"** → covered by Tasks 5 (target key), 7 (attribute discovery).
- **Spec § Decisions row "Mechanism / pipeline ordering / caching"** → covered by Tasks 9–11 (policy structure + per-candidate wrap + attribute-only cache).
- **Spec § Decisions row "Inheritance"** → covered by Task 7 test `DiscoverAttributes_InheritedAuthorize_IsDiscovered` and Task 18 integration tests.
- **Spec § Decisions row "$batch"** → covered by Task 19.
- **Spec § Decisions row "Bound operations on entity sets"** → covered by Task 4 test `ComputeTargetKey_BoundOperationOnEntitySet_ReturnsOperation`.
- **Spec § Architecture / Components 1–4** → Tasks 5, 7, 9, 11 (the policy itself) + Task 12 (registration) + Tasks 1, 3 (marker enrichment + endpoint metadata).
- **Spec § Data Flow golden path** → exercised by Task 15.
- **Spec § Data Flow per-operation policy** → exercised by Task 17.
- **Spec § Data Flow resource property** → exercised by Task 16.
- **Spec § Error Handling rows** → no-attribute fast path (Task 11), conflicting attributes (covered by integration), batch (Task 19), bound op (Task 4), inheritance (Tasks 7 + 18), schemes/roles (Task 17 uses Role; Policy is exercised via `AdminOnly`).
- **Spec § Testing Strategy unit tests** → all 12 listed scenarios covered by Tasks 4, 6, 8, 10 (with two additional ones for completeness).
- **Spec § Testing Strategy integration tests** → 12 listed; this plan implements 16 (combinations of class/resource/operation/inheritance/batch).
- **Spec § Documentation** → Task 20.

Placeholder scan: no "TBD", no "etc.", no "similar to". Each code block contains the actual file content; each command shows expected output.

Type/name consistency:
- `RestierAuthorizationMetadataPolicy` — consistent throughout.
- `RestierRouteMarker.ApiType` — consistent (added in Task 1, used in Tasks 3, 9, 11).
- Target keys `"class"` / `"resource:..."` / `"operation:..."` — consistent.
- Helper names `ComputeTargetKey`, `DiscoverAttributes`, `WrapEndpoint` — match spec.

One implementation-time check called out: Task 15 Step 2 verifies the existing `RestierTestHelpers.ExecuteTestRequest` signature for `applicationBuilderAction` / `customHeaders`. If absent, the implementer adapts inline. This is the only signature dependency the plan can't pre-verify without reading another file.
