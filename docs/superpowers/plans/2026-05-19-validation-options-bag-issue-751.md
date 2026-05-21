# Validation Options Bag (Issue #751) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `RestierRouteOptions.Validation` the *only* public channel for query validation limits. Restier has no per-query layer, so the upstream per-action `ODataValidationSettings` class has no business being a user-facing configuration surface in Restier. Default unset bag values from the global `ODataOptions`; warn (via `Trace.TraceWarning`) when the two channels disagree; throw `InvalidOperationException` if the user tries to register `ODataValidationSettings` directly in route DI; migrate samples + tests + docs.

**Architecture:** Add a `RestierValidationOptions` POCO in `Microsoft.Restier.Core` exposing the six integer query-validation knobs (`MaxTop`, `MaxSkip`, `MaxExpansionDepth`, `MaxAnyAllExpressionDepth`, `MaxOrderByNodeCount`, `MaxNodeCount`). Hang it off `RestierRouteOptions.Validation`. Inside `AddRestierRoute`, register **only the bag** in route DI (mirroring the precedence pattern used for `DeepOperationSettings` and `RestierConformanceOptions`); call the resolver once for its `Trace.TraceWarning` side-effect on `MaxTop` conflict and discard the result. The upstream `ODataValidationSettings` class is **never DI-registered** — it becomes a pure implementation detail of the controller's `Validate(...)` call. `RestierController` resolves the bag + the app-level `IOptions<ODataOptions>`, builds an `ODataValidationSettings` once per request, and caches it. Swagger / NSwag generators resolve the bag and read `MaxTop` directly without ever materializing the upstream type. Any user attempt to DI-register `ODataValidationSettings` throws `InvalidOperationException` with a migration message.

**Tech Stack:** .NET 8 / 9 / net48 multi-target, xUnit v3, FluentAssertions (AwesomeAssertions), NSubstitute, ASP.NET Core OData 9.x, Mintlify-flavored MDX docs via DotNetDocs SDK.

---

## File Structure

| Action | File | Responsibility |
|---|---|---|
| Create | `src/Microsoft.Restier.Core/RestierValidationOptions.cs` | POCO exposing six nullable integer query-validation knobs. Lives in `Microsoft.Restier.Core` namespace so `RestierRouteOptions` can reference it without dragging the AspNetCore reference into Core. |
| Modify | `src/Microsoft.Restier.Core/RestierRouteOptions.cs` | Add `Validation` getter returning a default `RestierValidationOptions` instance. |
| Create | `src/Microsoft.Restier.AspNetCore/Routing/RestierValidationOptionsResolver.cs` | Two methods. `Resolve(bag, odataOptions, routePrefix)` builds an `ODataValidationSettings` and emits a `Trace.TraceWarning` on `MaxTop` conflict — called once at route-add for its warning side-effect. `Build(bag, odataOptions)` is the silent equivalent used per-request by `RestierController`. |
| Modify | `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs:148-215` | Detect and reject any user-registered `ODataValidationSettings` descriptor (throw `InvalidOperationException`). Register the bag via `services.AddSingleton(options.Validation)`. Call `Resolve(...)` for the conflict warning and discard. Delete the old `services.TryAddSingleton<ODataValidationSettings>()` — the upstream type is no longer in DI. |
| Modify | `src/Microsoft.Restier.AspNetCore/RestierController.cs:1034` | Replace `container.GetRequiredService<ODataValidationSettings>()` with: resolve `RestierValidationOptions` from route services, resolve `IOptions<ODataOptions>` from `HttpContext.RequestServices`, build settings via `RestierValidationOptionsResolver.Build(...)`. Cache result in the existing `validationSettings` field. |
| Modify | `src/Microsoft.Restier.AspNetCore.Swagger/RestierOpenApiDocumentGenerator.cs:43` | Replace `GetService<ODataValidationSettings>()` with `GetService<RestierValidationOptions>()`. Read `MaxTop` directly. Drop `using Microsoft.AspNetCore.OData.Query.Validator;` if no longer used. |
| Modify | `src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiDocumentGenerator.cs:55` | Same change as the Swagger generator. |
| Create | `test/Microsoft.Restier.Tests.Core/RestierValidationOptionsTests.cs` | Unit tests for the POCO defaults + property mutability. |
| Create | `test/Microsoft.Restier.Tests.AspNetCore/Options/RestierValidationOptionsResolverTests.cs` | Unit tests for the resolver (defaulting from global, `MaxTop` conflict warning, all bag fields flow through) and a separate guard test asserting `InvalidOperationException` when a user DI-registers `ODataValidationSettings`. |
| Create | `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/ValidationOptionsTests.cs` | HTTP-level tests using Breakdance that the bag's `MaxTop` actually causes a 400 when `$top` exceeds it. |
| Modify | `src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs:417-442` | **Required** by the same commit as the wiring (Task 4) — the helper currently registers `ODataValidationSettings`, which will throw under the new rules. Move to the bag. |
| Modify | `src/Microsoft.Restier.Samples.Northwind.AspNetCore/Startup.cs:45-66` | Migrate to bag-based validation. **Required** in the same commit as Task 4. |
| Modify | `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Startup.cs:32-73` | Migrate both versioned route registrations. **Required** in the same commit as Task 4. |
| Modify | `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Program.cs` | Migrate if it registers `ODataValidationSettings`. **Required** in the same commit as Task 4. |
| Create | `src/Microsoft.Restier.Docs/guides/server/validation-options.mdx` | Authoring guide for the new bag and the bag-only model. Explains that DI-registering `ODataValidationSettings` now throws and points to the migration. |
| Modify | `src/Microsoft.Restier.Docs/guides/server/conformance-options.mdx` | Add `Validation` row to the `RestierRouteOptions` table. |
| Modify | `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` | Add `guides/server/validation-options` to the Mintlify nav template. |
| Modify | `src/Microsoft.Restier.Docs/guides/server/nswag.mdx`, `swagger.mdx`, `naming-conventions.mdx`, `performance.mdx`, `multi-tenancy.mdx`, `testing.mdx` | Replace any inline `AddSingleton(new ODataValidationSettings{…})` examples with the bag pattern (only where they appear). |
| Create or Modify | `src/Microsoft.Restier.Docs/release-notes/2-0-0-beta.md` | Add a "Validation options" entry describing the new bag and the conflict-warning behavior. Create the file (and register it in the docsproj nav) if it does not yet exist. |

---

## Task 1: Add `RestierValidationOptions` POCO in Core

**Files:**
- Create: `src/Microsoft.Restier.Core/RestierValidationOptions.cs`
- Test: `test/Microsoft.Restier.Tests.Core/RestierValidationOptionsTests.cs`

- [ ] **Step 1: Write the failing test**

Create `test/Microsoft.Restier.Tests.Core/RestierValidationOptionsTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Restier.Core;
using Xunit;

namespace Microsoft.Restier.Tests.Core;

public class RestierValidationOptionsTests
{
    [Fact]
    public void Defaults_AreAllNull()
    {
        var options = new RestierValidationOptions();

        options.MaxTop.Should().BeNull();
        options.MaxSkip.Should().BeNull();
        options.MaxExpansionDepth.Should().BeNull();
        options.MaxAnyAllExpressionDepth.Should().BeNull();
        options.MaxOrderByNodeCount.Should().BeNull();
        options.MaxNodeCount.Should().BeNull();
    }

    [Fact]
    public void Properties_AreMutable()
    {
        var options = new RestierValidationOptions
        {
            MaxTop = 100,
            MaxSkip = 1000,
            MaxExpansionDepth = 3,
            MaxAnyAllExpressionDepth = 2,
            MaxOrderByNodeCount = 4,
            MaxNodeCount = 50,
        };

        options.MaxTop.Should().Be(100);
        options.MaxSkip.Should().Be(1000);
        options.MaxExpansionDepth.Should().Be(3);
        options.MaxAnyAllExpressionDepth.Should().Be(2);
        options.MaxOrderByNodeCount.Should().Be(4);
        options.MaxNodeCount.Should().Be(50);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~RestierValidationOptionsTests"`

Expected: BUILD FAIL with `The type or namespace name 'RestierValidationOptions' could not be found`.

- [ ] **Step 3: Create the POCO**

Create `src/Microsoft.Restier.Core/RestierValidationOptions.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Per-route query validation limits exposed through the
    /// <see cref="RestierRouteOptions"/> bag. Any property left <c>null</c>
    /// inherits its value from the global <c>ODataOptions</c> (where
    /// applicable) or the underlying OData framework default.
    /// </summary>
    /// <remarks>
    /// Restier owns a single shared <c>RestierController</c> per route, so
    /// there is no per-action layer on which to hang the upstream
    /// <c>ODataValidationSettings</c> object. This bag is the route-level
    /// substitute: values set here win over both the global
    /// <c>ODataOptions</c> ceilings and any caller-supplied
    /// <c>ODataValidationSettings</c> DI registration. Conflicts emit
    /// <see cref="System.Diagnostics.Trace.TraceWarning(string)"/> at
    /// route-add time naming the winning value.
    /// </remarks>
    public class RestierValidationOptions
    {
        /// <summary>
        /// Maximum value the client may supply for <c>$top</c>. When unset,
        /// inherits <c>ODataOptions.QuerySettings.MaxTop</c>.
        /// </summary>
        public int? MaxTop { get; set; }

        /// <summary>
        /// Maximum value the client may supply for <c>$skip</c>. When unset,
        /// the underlying OData framework default applies (no upper bound).
        /// </summary>
        public int? MaxSkip { get; set; }

        /// <summary>
        /// Maximum depth permitted in <c>$expand</c>. When unset, the
        /// underlying OData framework default applies (2).
        /// </summary>
        public int? MaxExpansionDepth { get; set; }

        /// <summary>
        /// Maximum nesting of <c>any</c>/<c>all</c> lambda expressions
        /// inside <c>$filter</c>. When unset, the underlying OData framework
        /// default applies (1).
        /// </summary>
        public int? MaxAnyAllExpressionDepth { get; set; }

        /// <summary>
        /// Maximum number of comma-separated nodes in <c>$orderby</c>. When
        /// unset, the underlying OData framework default applies (5).
        /// </summary>
        public int? MaxOrderByNodeCount { get; set; }

        /// <summary>
        /// Maximum total node count of a parsed <c>$filter</c> expression
        /// tree. When unset, the underlying OData framework default applies
        /// (100).
        /// </summary>
        public int? MaxNodeCount { get; set; }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~RestierValidationOptionsTests"`

Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Core/RestierValidationOptions.cs test/Microsoft.Restier.Tests.Core/RestierValidationOptionsTests.cs
git commit -m "feat(core): add RestierValidationOptions POCO for route-level query limits"
```

---

## Task 2: Expose `Validation` on `RestierRouteOptions`

**Files:**
- Modify: `src/Microsoft.Restier.Core/RestierRouteOptions.cs`
- Test: `test/Microsoft.Restier.Tests.Core/RestierValidationOptionsTests.cs`

- [ ] **Step 1: Add failing test**

Append to `test/Microsoft.Restier.Tests.Core/RestierValidationOptionsTests.cs` inside the existing class:

```csharp
    [Fact]
    public void RestierRouteOptions_Validation_DefaultsToNonNullEmptyBag()
    {
        var route = new RestierRouteOptions();

        route.Validation.Should().NotBeNull();
        route.Validation.MaxTop.Should().BeNull();
        route.Validation.MaxExpansionDepth.Should().BeNull();
    }

    [Fact]
    public void RestierRouteOptions_Validation_IsMutableViaPropertyAccess()
    {
        var route = new RestierRouteOptions();

        route.Validation.MaxTop = 25;
        route.Validation.MaxExpansionDepth = 3;

        route.Validation.MaxTop.Should().Be(25);
        route.Validation.MaxExpansionDepth.Should().Be(3);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~RestierValidationOptionsTests"`

Expected: BUILD FAIL with `'RestierRouteOptions' does not contain a definition for 'Validation'`.

- [ ] **Step 3: Add the property**

Edit `src/Microsoft.Restier.Core/RestierRouteOptions.cs` and insert the `Validation` property just after `Conformance` (around line 25):

```csharp
        /// <summary>
        /// Per-route query validation limits (<c>$top</c>, <c>$expand</c>
        /// depth, etc.). Any property left <c>null</c> defaults from the
        /// global <c>ODataOptions</c> or the OData framework default. Values
        /// set here take precedence over any caller-supplied
        /// <c>ODataValidationSettings</c> DI registration; conflicts with
        /// <c>ODataOptions.SetMaxTop</c> emit a Trace warning at route-add
        /// time.
        /// </summary>
        public RestierValidationOptions Validation { get; } = new();
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test test/Microsoft.Restier.Tests.Core/Microsoft.Restier.Tests.Core.csproj --filter "FullyQualifiedName~RestierValidationOptionsTests"`

Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Core/RestierRouteOptions.cs test/Microsoft.Restier.Tests.Core/RestierValidationOptionsTests.cs
git commit -m "feat(core): expose Validation bag on RestierRouteOptions"
```

---

## Task 3: Implement `RestierValidationOptionsResolver`

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore/Routing/RestierValidationOptionsResolver.cs`
- Test: `test/Microsoft.Restier.Tests.AspNetCore/Options/RestierValidationOptionsResolverTests.cs`

The resolver exposes **two** methods. `Resolve(bag, odataOptions, routePrefix)` builds an `ODataValidationSettings` and emits `Trace.TraceWarning` on `MaxTop` conflict — used at route-add time for its warning side-effect. `Build(bag, odataOptions)` is the silent equivalent used per-request by `RestierController` so that the conflict warning is emitted exactly once per app lifetime, not once per request. There is no user-instance channel; DI registration of `ODataValidationSettings` is rejected upstream (Task 4).

- [ ] **Step 1: Write the failing tests**

Create `test/Microsoft.Restier.Tests.AspNetCore/Options/RestierValidationOptionsResolverTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics;
using FluentAssertions;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.Restier.AspNetCore.Routing;
using Microsoft.Restier.Core;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Options;

public class RestierValidationOptionsResolverTests
{
    private sealed class CapturingTraceListener : TraceListener
    {
        public System.Collections.Generic.List<string> Warnings { get; } = new();

        public override void Write(string message) { }

        public override void WriteLine(string message) => Warnings.Add(message);

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (eventType == TraceEventType.Warning)
            {
                Warnings.Add(message);
            }
        }
    }

    private static (CapturingTraceListener listener, System.IDisposable scope) AttachListener()
    {
        var listener = new CapturingTraceListener();
        Trace.Listeners.Add(listener);
        return (listener, new TraceListenerScope(listener));
    }

    private sealed class TraceListenerScope : System.IDisposable
    {
        private readonly TraceListener listener;
        public TraceListenerScope(TraceListener listener) { this.listener = listener; }
        public void Dispose() => Trace.Listeners.Remove(listener);
    }

    [Fact]
    public void Resolve_EmptyBag_NoGlobalMaxTop_ProducesFrameworkDefaults()
    {
        var bag = new RestierValidationOptions();
        var globalOptions = new ODataOptions();

        var resolved = RestierValidationOptionsResolver.Resolve(bag, globalOptions, routePrefix: "api");

        resolved.Should().NotBeNull();
        resolved.MaxTop.Should().BeNull();
        resolved.MaxExpansionDepth.Should().Be(new ODataValidationSettings().MaxExpansionDepth);
    }

    [Fact]
    public void Resolve_EmptyBag_GlobalMaxTopSet_InheritsGlobalMaxTop()
    {
        var bag = new RestierValidationOptions();
        var globalOptions = new ODataOptions();
        globalOptions.SetMaxTop(50);

        var resolved = RestierValidationOptionsResolver.Resolve(bag, globalOptions, routePrefix: "api");

        resolved.MaxTop.Should().Be(50);
    }

    [Fact]
    public void Resolve_BagMaxTop_GlobalMaxTopDisagrees_BagWinsAndEmitsWarning()
    {
        var (listener, scope) = AttachListener();
        using var _ = scope;

        var bag = new RestierValidationOptions { MaxTop = 25 };
        var globalOptions = new ODataOptions();
        globalOptions.SetMaxTop(50);

        var resolved = RestierValidationOptionsResolver.Resolve(bag, globalOptions, routePrefix: "api");

        resolved.MaxTop.Should().Be(25);
        listener.Warnings.Should().ContainSingle(w =>
            w.Contains("MaxTop", System.StringComparison.Ordinal) &&
            w.Contains("api", System.StringComparison.Ordinal) &&
            w.Contains("25", System.StringComparison.Ordinal) &&
            w.Contains("50", System.StringComparison.Ordinal));
    }

    [Fact]
    public void Resolve_BagMaxTop_GlobalMaxTopAgrees_NoWarning()
    {
        var (listener, scope) = AttachListener();
        using var _ = scope;

        var bag = new RestierValidationOptions { MaxTop = 50 };
        var globalOptions = new ODataOptions();
        globalOptions.SetMaxTop(50);

        var resolved = RestierValidationOptionsResolver.Resolve(bag, globalOptions, routePrefix: "api");

        resolved.MaxTop.Should().Be(50);
        listener.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_BagSetsAllFields_AllFlowThrough()
    {
        var bag = new RestierValidationOptions
        {
            MaxTop = 10,
            MaxSkip = 1000,
            MaxExpansionDepth = 3,
            MaxAnyAllExpressionDepth = 2,
            MaxOrderByNodeCount = 4,
            MaxNodeCount = 50,
        };

        var resolved = RestierValidationOptionsResolver.Resolve(bag, new ODataOptions(), routePrefix: "api");

        resolved.MaxTop.Should().Be(10);
        resolved.MaxSkip.Should().Be(1000);
        resolved.MaxExpansionDepth.Should().Be(3);
        resolved.MaxAnyAllExpressionDepth.Should().Be(2);
        resolved.MaxOrderByNodeCount.Should().Be(4);
        resolved.MaxNodeCount.Should().Be(50);
    }

    [Fact]
    public void Build_BagMaxTop_GlobalDisagrees_BagWinsAndDoesNotEmitWarning()
    {
        var (listener, scope) = AttachListener();
        using var _ = scope;

        var bag = new RestierValidationOptions { MaxTop = 25 };
        var globalOptions = new ODataOptions();
        globalOptions.SetMaxTop(50);

        var built = RestierValidationOptionsResolver.Build(bag, globalOptions);

        built.MaxTop.Should().Be(25);
        listener.Warnings.Should().BeEmpty(
            because: "Build is the silent per-request path; the conflict warning is only emitted by Resolve at route-add time");
    }

    [Fact]
    public void Build_NullODataOptions_DoesNotThrow()
    {
        var bag = new RestierValidationOptions { MaxExpansionDepth = 4 };

        var built = RestierValidationOptionsResolver.Build(bag, globalOptions: null);

        built.MaxExpansionDepth.Should().Be(4);
        built.MaxTop.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierValidationOptionsResolverTests"`

Expected: BUILD FAIL with `The type or namespace name 'RestierValidationOptionsResolver' could not be found`.

- [ ] **Step 3: Implement the resolver**

Create `src/Microsoft.Restier.AspNetCore/Routing/RestierValidationOptionsResolver.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.AspNetCore.Routing
{
    /// <summary>
    /// Resolves a route's final <see cref="ODataValidationSettings"/> from
    /// the <see cref="RestierValidationOptions"/> bag and the global
    /// <see cref="ODataOptions"/>. The bag is the only public configuration
    /// channel; <see cref="ODataValidationSettings"/> is never read from DI.
    /// A bag <c>MaxTop</c> that disagrees with the global
    /// <c>SetMaxTop</c> wins and emits a <see cref="Trace.TraceWarning(string)"/>.
    /// </summary>
    internal static class RestierValidationOptionsResolver
    {
        private const string WarningPrefix = "Restier: ";

        /// <summary>
        /// Builds the route's <see cref="ODataValidationSettings"/> from the
        /// bag and the global <see cref="ODataOptions"/>, emitting a
        /// <see cref="Trace.TraceWarning(string)"/> when <c>MaxTop</c>
        /// disagrees between the two channels. Call this once at route-add
        /// time for its warning side-effect; use <see cref="Build"/> at
        /// request time to avoid duplicate warnings.
        /// </summary>
        public static ODataValidationSettings Resolve(
            RestierValidationOptions bag,
            ODataOptions globalOptions,
            string routePrefix)
        {
            var globalMaxTop = globalOptions?.QuerySettings?.MaxTop;
            if (bag.MaxTop.HasValue && globalMaxTop.HasValue && globalMaxTop != bag.MaxTop)
            {
                Trace.TraceWarning(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}Route '{1}': RestierValidationOptions.MaxTop = {2} overrides ODataOptions.SetMaxTop value {3}.",
                        WarningPrefix,
                        routePrefix,
                        bag.MaxTop.Value,
                        globalMaxTop.Value));
            }
            return Build(bag, globalOptions);
        }

        /// <summary>
        /// Builds the route's <see cref="ODataValidationSettings"/> silently
        /// (no warnings). Used per-request by <c>RestierController</c>.
        /// </summary>
        public static ODataValidationSettings Build(
            RestierValidationOptions bag,
            ODataOptions globalOptions)
        {
            var resolved = new ODataValidationSettings();

            var globalMaxTop = globalOptions?.QuerySettings?.MaxTop;
            resolved.MaxTop = bag.MaxTop ?? globalMaxTop;

            if (bag.MaxSkip.HasValue) resolved.MaxSkip = bag.MaxSkip;
            if (bag.MaxExpansionDepth.HasValue) resolved.MaxExpansionDepth = bag.MaxExpansionDepth.Value;
            if (bag.MaxAnyAllExpressionDepth.HasValue) resolved.MaxAnyAllExpressionDepth = bag.MaxAnyAllExpressionDepth.Value;
            if (bag.MaxOrderByNodeCount.HasValue) resolved.MaxOrderByNodeCount = bag.MaxOrderByNodeCount.Value;
            if (bag.MaxNodeCount.HasValue) resolved.MaxNodeCount = bag.MaxNodeCount.Value;

            return resolved;
        }
    }
}
```

- [ ] **Step 4: Run resolver tests to verify they pass**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierValidationOptionsResolverTests"`

Expected: PASS (7 tests — 5 `Resolve` tests + 2 `Build` tests). If any test fails, the most likely culprit is the property path on `ODataOptions`: adjust `globalOptions.QuerySettings.MaxTop` to whatever the installed `Microsoft.AspNetCore.OData` 9.x exposes (it may be `GetMaxTop()` or a property on `DefaultQuerySettings`).

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Routing/RestierValidationOptionsResolver.cs test/Microsoft.Restier.Tests.AspNetCore/Options/RestierValidationOptionsResolverTests.cs
git commit -m "feat(aspnetcore): add RestierValidationOptionsResolver (Resolve + Build)"
```

---

## Task 4: Atomic switchover — register the bag, drop `ODataValidationSettings` from DI, migrate every consumer

**Files (all in one commit):**
- Modify: `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`
- Modify: `src/Microsoft.Restier.AspNetCore.Swagger/RestierOpenApiDocumentGenerator.cs`
- Modify: `src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiDocumentGenerator.cs`
- Modify: `src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs`
- Modify: `src/Microsoft.Restier.Samples.Northwind.AspNetCore/Startup.cs`
- Modify: `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Startup.cs`
- Modify: `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Program.cs` (only if it registers `ODataValidationSettings`)
- Test: `test/Microsoft.Restier.Tests.AspNetCore/Options/RestierValidationOptionsResolverTests.cs` (add the throw test)

This is the breaking-change commit. `ODataValidationSettings` is removed from the route DI surface entirely. Every in-repo consumer (controller, Swagger, NSwag, Breakdance, samples) switches to resolving `RestierValidationOptions` in the same commit, or the test suite breaks.

- [ ] **Step 1: Add the failing throw test**

Append to `test/Microsoft.Restier.Tests.AspNetCore/Options/RestierValidationOptionsResolverTests.cs` a new test class:

```csharp
public class AddRestierRouteValidationGuardTests
{
    [Fact]
    public void AddRestierRoute_UserRegistersODataValidationSettings_Throws()
    {
        var act = () =>
        {
            using var server = Microsoft.Restier.Breakdance.RestierTestHelpers
                .GetTestableRestierServer<Microsoft.Restier.Tests.Shared.Scenarios.Library.LibraryApi>(
                    routeName: "api",
                    routePrefix: "api",
                    apiServiceCollection: services => services.AddSingleton(
                        new Microsoft.AspNetCore.OData.Query.Validator.ODataValidationSettings { MaxTop = 5 }));
        };

        act.Should().Throw<System.InvalidOperationException>()
            .WithMessage("*ODataValidationSettings*RestierRouteOptions.Validation*");
    }
}
```

(Adjust the helper-method invocation path if `GetTestableRestierServer` is on a different class — search `src/Microsoft.Restier.Breakdance` first to confirm the exact signature.)

- [ ] **Step 2: Run the throw test to verify it fails**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~AddRestierRouteValidationGuardTests"`

Expected: FAIL — currently the helper's `AddSingleton(new ODataValidationSettings{…})` succeeds without throwing.

- [ ] **Step 3: Wire the bag and add the rejection in `RestierODataOptionsExtensions`**

Open `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs`.

Find this snippet (around lines 163-170):

```csharp
            configureRouteServices?.Invoke(services);

            // Bag wins: applied *after* configureRouteServices so it overrides any
            // registrations of these types the caller may have made in DI.
            services.AddSingleton(typeof(RestierNamingConvention), (object)options.NamingConvention);
            services.AddSingleton(options.DeepOperations);
            services.AddSingleton(options.Conformance);
```

Replace with:

```csharp
            configureRouteServices?.Invoke(services);

            // Bag wins: applied *after* configureRouteServices so it overrides any
            // registrations of these types the caller may have made in DI.
            services.AddSingleton(typeof(RestierNamingConvention), (object)options.NamingConvention);
            services.AddSingleton(options.DeepOperations);
            services.AddSingleton(options.Conformance);
            services.AddSingleton(options.Validation);

            // ODataValidationSettings is a per-action object in upstream OData
            // (designed for use inside an [EnableQuery] controller method).
            // Restier has no per-action layer, so DI-registering it in route
            // services is meaningless. Reject the legacy pattern with a clear
            // migration message — the bag is the only supported channel.
            for (var i = 0; i < services.Count; i++)
            {
                if (services[i].ServiceType == typeof(ODataValidationSettings))
                {
                    throw new InvalidOperationException(
                        $"Route '{routePrefix}': registering ODataValidationSettings directly in route services " +
                        $"is not supported. Restier has no per-query/per-action layer for this upstream OData class to attach to. " +
                        $"Configure query validation limits via the RestierRouteOptions.Validation bag on AddRestierRoute instead.");
                }
            }

            // Call Resolve once for its conflict-warning side effect; we don't
            // store the result. RestierController and the OpenAPI generators
            // build/read settings on demand from the bag at request time.
            Routing.RestierValidationOptionsResolver.Resolve(
                options.Validation, oDataOptions, routePrefix);
```

Then find this line lower down (around line 186):

```csharp
            services.TryAddSingleton<ODataValidationSettings>();
```

**Delete it.** `ODataValidationSettings` is no longer a route-DI service. Every consumer that previously resolved it now resolves `RestierValidationOptions` instead (steps 4-7 below).

- [ ] **Step 4: Migrate `RestierController` to resolve the bag**

Open `src/Microsoft.Restier.AspNetCore/RestierController.cs`. Find the `EnsureInitialized` method (around line 999-1006):

```csharp
        private void EnsureInitialized()
        {
            var container = HttpContext.Request.GetRouteServices();
            api = container.GetRequiredService<ApiBase>();
            querySettings = container.GetRequiredService<ODataQuerySettings>();
            validationSettings = container.GetRequiredService<ODataValidationSettings>();
            operationExecutor = container.GetRequiredService<IOperationExecutor>();
        }
```

Replace with:

```csharp
        private void EnsureInitialized()
        {
            var container = HttpContext.Request.GetRouteServices();
            api = container.GetRequiredService<ApiBase>();
            querySettings = container.GetRequiredService<ODataQuerySettings>();
            operationExecutor = container.GetRequiredService<IOperationExecutor>();

            // ODataValidationSettings is no longer a DI service — build it
            // from the route's bag plus the app-level ODataOptions snapshot.
            var bag = container.GetRequiredService<RestierValidationOptions>();
            var odataOptions = HttpContext.RequestServices
                .GetService<IOptions<ODataOptions>>()?.Value;
            validationSettings = Routing.RestierValidationOptionsResolver.Build(bag, odataOptions);
        }
```

Add the following `using` directives near the top of the file if not already present:

```csharp
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;
using Microsoft.Restier.Core;
```

(`Microsoft.Restier.AspNetCore.Routing` may also be needed for `RestierValidationOptionsResolver` if the controller's existing usings don't already cover it via the file's namespace.)

The `validationSettings` field at line 51 stays as `private ODataValidationSettings validationSettings;` — only the assignment changes. The field is cached for the controller instance's lifetime (one request), so `Build` is called once per request.

- [ ] **Step 5: Migrate the Swagger generator**

Open `src/Microsoft.Restier.AspNetCore.Swagger/RestierOpenApiDocumentGenerator.cs`. Find line 43:

```csharp
            var odataValidationSettings = routeServices.GetService<ODataValidationSettings>();
```

Replace with:

```csharp
            var validationOptions = routeServices.GetService<RestierValidationOptions>();
```

Then update the only downstream reference (the line that reads `odataValidationSettings.MaxTop` to populate `TopExample`): change `odataValidationSettings.MaxTop` to `validationOptions?.MaxTop`. The `?.` is necessary because `GetService` may return null in pathological test setups.

Remove `using Microsoft.AspNetCore.OData.Query.Validator;` if no other reference remains. Add `using Microsoft.Restier.Core;`.

Verify by running `dotnet build src/Microsoft.Restier.AspNetCore.Swagger/Microsoft.Restier.AspNetCore.Swagger.csproj` — expected: BUILD SUCCEEDED.

- [ ] **Step 6: Migrate the NSwag generator**

Open `src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiDocumentGenerator.cs`. Find line 55:

```csharp
            var odataValidationSettings = routeServices.GetService<ODataValidationSettings>();
```

Apply the same edit as Step 5. Verify with `dotnet build src/Microsoft.Restier.AspNetCore.NSwag/Microsoft.Restier.AspNetCore.NSwag.csproj`.

- [ ] **Step 7: Migrate Breakdance**

Open `src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs`. Find lines 417-442 — the helper that registers `ODataValidationSettings`. Replace this block:

```csharp
            restierTests.AddRestierAction = (odataOptions) =>
            {
                odataOptions.AddRestierRoute<TApi>(routeName, restierServices =>
                {
                    restierServices
                        .AddSingleton(new ODataValidationSettings
                        {
                            MaxTop = 5,
                            MaxAnyAllExpressionDepth = 3,
                            MaxExpansionDepth = 3,
                        });
                    apiServiceCollection?.Invoke(restierServices);
                },
                options =>
                {
                    options.NamingConvention = namingConvention;
                    configureOptions?.Invoke(options);
                });
            };
```

With:

```csharp
            restierTests.AddRestierAction = (odataOptions) =>
            {
                odataOptions.AddRestierRoute<TApi>(routeName, restierServices =>
                {
                    apiServiceCollection?.Invoke(restierServices);
                },
                options =>
                {
                    options.NamingConvention = namingConvention;
                    options.Validation.MaxTop = 5;
                    options.Validation.MaxAnyAllExpressionDepth = 3;
                    options.Validation.MaxExpansionDepth = 3;
                    configureOptions?.Invoke(options);
                });
            };
```

(`configureOptions` is invoked **after** the defaults so test callers can still override the bag.)

- [ ] **Step 8: Migrate `Northwind.AspNetCore`**

Open `src/Microsoft.Restier.Samples.Northwind.AspNetCore/Startup.cs`. Find lines 45-66 and replace with:

```csharp
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
                .AddRestier(options =>
                {
                    options.Select().Expand().Filter().OrderBy().SetMaxTop(5).Count();
                    options.TimeZone = TimeZoneInfo.Utc;

                    options.AddRestierRoute<NorthwindApi>(
                        string.Empty,
                        restierServices => restierServices
                            .AddEFCoreProviderServices<NorthwindContext>((services, dbOptions) =>
                                dbOptions.UseSqlServer(Configuration.GetConnectionString("NorthwindEntities"))),
                        bag =>
                        {
                            bag.Validation.MaxAnyAllExpressionDepth = 3;
                            bag.Validation.MaxExpansionDepth = 3;
                        });
                })
                .AddApplicationPart(typeof(NorthwindApi).Assembly)
                .AddApplicationPart(typeof(RestierController).Assembly);
```

Notes:
- `SetMaxTop(100)` and `MaxTop=5` were the original conflict from issue #684. Collapse to the stricter value (5) in `SetMaxTop`; the bag inherits it.
- Remove the now-unused `using Microsoft.AspNetCore.OData.Query.Validator;` directive if nothing else references it.

- [ ] **Step 9: Migrate `NorthwindVersioned.AspNetCore`**

Open `src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Startup.cs`. Find lines 41-72 and replace with:

```csharp
            services.AddControllers().AddRestier(options =>
            {
                options.Select().Expand().Filter().OrderBy().SetMaxTop(5).Count();
                options.TimeZone = TimeZoneInfo.Utc;
            });

            services.AddRestierApiVersioning(b => b
                .AddVersion<NorthwindApiV1>(
                    "api",
                    restierServices => restierServices
                        .AddEFCoreProviderServices<NorthwindContextV1>((sp, dbOptions) =>
                            dbOptions.UseInMemoryDatabase("Northwind-V1")),
                    opts =>
                    {
                        opts.SunsetDate = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
                        opts.Validation.MaxAnyAllExpressionDepth = 3;
                        opts.Validation.MaxExpansionDepth = 3;
                    })
                .AddVersion<NorthwindApiV2>(
                    "api",
                    restierServices => restierServices
                        .AddEFCoreProviderServices<NorthwindContextV2>((sp, dbOptions) =>
                            dbOptions.UseInMemoryDatabase("Northwind-V2")),
                    opts =>
                    {
                        opts.Validation.MaxAnyAllExpressionDepth = 3;
                        opts.Validation.MaxExpansionDepth = 3;
                    }));
```

If `SunsetDate` lives on a versioning-specific options type wrapping `RestierRouteOptions`, navigate to the wrapping type's `.Validation` property; the access path may be `opts.RouteOptions.Validation.*` or similar. Look at the `AddVersion` signature in `src/Microsoft.Restier.AspNetCore.Versioning/` (the commit `80316665` introduced this).

- [ ] **Step 10: Migrate `Postgres.AspNetCore`**

Open `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Program.cs`. Run `grep -n "ODataValidationSettings" Program.cs`. For each `AddSingleton(new ODataValidationSettings{…})` you find, apply the same conversion: move integer fields to `bag.Validation.*` and put a single `SetMaxTop(...)` on the outer `AddRestier` options. If no registration exists (only `using` directives reference the type), remove the unused `using`.

- [ ] **Step 11: Build the whole solution**

```bash
dotnet build RESTier.slnx
```

Expected: BUILD SUCCEEDED. The samples and Breakdance compile cleanly without the now-banned `ODataValidationSettings` registrations.

- [ ] **Step 12: Run the full test suite**

```bash
dotnet test RESTier.slnx
```

Expected: All tests PASS, including:
- The new `AddRestierRouteValidationGuardTests.AddRestierRoute_UserRegistersODataValidationSettings_Throws` (verifies the rejection).
- `Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore.PagingTests` and `.EF6.PagingTests` (still rely on the helper-supplied `MaxTop=5`, now coming through the bag).
- All previously-passing resolver and `RestierValidationOptions` tests from Tasks 1-3.

If any test fails because a third-party in-tree caller still uses the legacy pattern, migrate it here and add a note to the commit message — don't paper over it.

- [ ] **Step 13: Commit**

```bash
git add \
  src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs \
  src/Microsoft.Restier.AspNetCore/RestierController.cs \
  src/Microsoft.Restier.AspNetCore.Swagger/RestierOpenApiDocumentGenerator.cs \
  src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiDocumentGenerator.cs \
  src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs \
  src/Microsoft.Restier.Samples.Northwind.AspNetCore/Startup.cs \
  src/Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore/Startup.cs \
  src/Microsoft.Restier.Samples.Postgres.AspNetCore/Program.cs \
  test/Microsoft.Restier.Tests.AspNetCore/Options/RestierValidationOptionsResolverTests.cs
git commit -m "feat(aspnetcore)!: RestierRouteOptions.Validation is the only query validation channel

ODataValidationSettings is an upstream per-action class and Restier has no
per-action layer. Remove it from the route DI surface entirely; the bag
(RestierValidationOptions) is now the only registered service. RestierController
builds an ODataValidationSettings on demand from the bag + IOptions<ODataOptions>;
the OpenAPI generators read MaxTop straight from the bag. Direct DI registration
of ODataValidationSettings throws InvalidOperationException at route-add time.

Closes #751, #684, #719."
```

---

## Task 5: HTTP-level integration test for the bag

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/ValidationOptionsTests.cs`

This proves that the bag actually flows end-to-end through Breakdance into `RestierController` and rejects an over-limit `$top`.

- [ ] **Step 1: Write the failing test**

Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/ValidationOptionsTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class ValidationOptionsTests<TApi, TContext> : RestierTestBase<TApi>
    where TApi : ApiBase
    where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    [Fact]
    public async Task Bag_MaxTop_RejectsOverLimitTopWithBadRequest()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$top=99",
            serviceCollection: ConfigureServices,
            configureOptions: o => o.Validation.MaxTop = 3);

        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeFalse();
        ((int)response.StatusCode).Should().Be(400);
        content.Should().Contain("Top");
    }

    [Fact]
    public async Task Bag_MaxTop_AllowsUnderLimitTop()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$top=2",
            serviceCollection: ConfigureServices,
            configureOptions: o => o.Validation.MaxTop = 3);

        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("@odata.context");
    }
}
```

Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/ValidationOptionsTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore;

[Collection("LibraryApiEFCore")]
public class ValidationOptionsTests : ValidationOptionsTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}
```

- [ ] **Step 2: Inspect `RestierTestHelpers.ExecuteTestRequest` to confirm it accepts `configureOptions`**

Open `src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs` and search for `ExecuteTestRequest`. Verify it has an `Action<RestierRouteOptions> configureOptions = null` parameter. (It does — pattern is used by `DeepInsertTests.cs:173`.) If not, add it following that signature.

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~ValidationOptionsTests"`

Expected: One of two outcomes:
- If Task 4 is fully complete, **PASS** — this becomes a regression test rather than a TDD test. Note the date and proceed.
- If Task 4 is incomplete, FAIL with "expected 400 but got 200" (because the bag isn't being read).

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/ValidationOptionsTests.cs test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/ValidationOptionsTests.cs
git commit -m "test(aspnetcore): HTTP-level coverage of RestierRouteOptions.Validation bag"
```

---

## Task 6: Authoring guide for the validation bag

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/validation-options.mdx`

- [ ] **Step 1: Write the guide**

Create `src/Microsoft.Restier.Docs/guides/server/validation-options.mdx`:

```mdx
---
title: 'Query Validation Options'
description: 'Per-route limits for $top, $expand, $filter and $orderby. The RestierRouteOptions.Validation bag is the only configuration channel — directly DI-registering ODataValidationSettings throws.'
---

In ASP.NET Core OData, query validation lives in two places:

- `ODataOptions` (the global `AddOData(...)` configuration) sets app-wide ceilings such as `SetMaxTop`, plus the boolean flags `Select()`, `Expand()`, `Filter()`, `OrderBy()`, `Count()`.
- `ODataValidationSettings` is a *per-action* object that upstream OData passes to `ODataQueryOptions.Validate(...)` inside an `[EnableQuery]` controller method.

Restier collapses every entity set on a route into a single shared `RestierController`. **There is no per-action layer.** Pre-2.0 versions registered `ODataValidationSettings` in route DI as a workaround; that pattern blurred the line between the per-action upstream model and Restier's per-route reality, and it produced silent conflicts with the global `SetMaxTop`. As of 2.0:

- The `RestierRouteOptions.Validation` bag is the only public configuration channel.
- Registering `ODataValidationSettings` directly in route services throws `InvalidOperationException` at route-add time.
- `ODataValidationSettings` is **no longer a route-DI service at all**. `RestierController` builds one on demand from the bag for the upstream `queryOptions.Validate(...)` call, and the OpenAPI generators (Swagger / NSwag) read `MaxTop` straight from the bag. Third-party code that previously resolved `ODataValidationSettings` from route services must switch to `RestierValidationOptions`.

## The bag

| Property | Type | Default when unset | Maps to |
|---|---|---|---|
| `MaxTop` | `int?` | `ODataOptions.QuerySettings.MaxTop` (the value you passed to `SetMaxTop`) | `ODataValidationSettings.MaxTop` |
| `MaxSkip` | `int?` | OData framework default (no upper bound) | `ODataValidationSettings.MaxSkip` |
| `MaxExpansionDepth` | `int?` | `2` | `ODataValidationSettings.MaxExpansionDepth` |
| `MaxAnyAllExpressionDepth` | `int?` | `1` | `ODataValidationSettings.MaxAnyAllExpressionDepth` |
| `MaxOrderByNodeCount` | `int?` | `5` | `ODataValidationSettings.MaxOrderByNodeCount` |
| `MaxNodeCount` | `int?` | `100` | `ODataValidationSettings.MaxNodeCount` |

The bag intentionally exposes only the six integer knobs that matter for Restier's collection-oriented model. The upstream enum knobs (`AllowedQueryOptions`, `AllowedFunctions`, `AllowedArithmeticOperators`, `AllowedLogicalOperators`, `AllowedOrderByProperties`) are designed to gate individual controller actions — a concept Restier does not expose. If you need fine-grained gating, implement it through Restier interceptors or the convention-based authorization pipeline, not through `ODataValidationSettings`.

## Configuring a route

```csharp
builder.Services
    .AddControllers()
    .AddRestier(options =>
    {
        options.Select().Expand().Filter().OrderBy().SetMaxTop(50).Count();
        options.TimeZone = TimeZoneInfo.Utc;

        options.AddRestierRoute<NorthwindApi>(
            "api",
            services => services.AddEFCoreProviderServices<NorthwindContext>(...),
            bag =>
            {
                bag.Validation.MaxExpansionDepth = 3;
                bag.Validation.MaxAnyAllExpressionDepth = 2;
                // Leave MaxTop unset — inherits 50 from SetMaxTop above.
            });
    });
```

The third argument to `AddRestierRoute` is the `RestierRouteOptions` bag. The `.Validation` property is mutable; set the limits you care about and leave the rest alone.

## `MaxTop` and the global `SetMaxTop`

`MaxTop` is the one validation knob that *can* be set in two places: on `RestierRouteOptions.Validation.MaxTop` and on the global `ODataOptions.SetMaxTop(...)`. When the bag's `MaxTop` is `null`, Restier inherits the global value. When the bag's `MaxTop` is set, the bag wins — and if it disagrees with the global, Restier emits a `Trace.TraceWarning` at route-add time:

```
Restier: Route 'api': RestierValidationOptions.MaxTop = 25 overrides ODataOptions.SetMaxTop value 100.
```

The warning is loud on purpose. Either set `MaxTop` in one place (the simpler choice — usually `SetMaxTop`), or set both to the same value. The historic confusion behind issue [#684](https://github.com/OData/RESTier/issues/684) was exactly two `MaxTop` values that disagreed silently.

## Migrating from the legacy pattern

Pre-2.0 code typically looked like this:

```csharp
options.AddRestierRoute<NorthwindApi>(string.Empty, restierServices =>
{
    restierServices
        .AddEFCoreProviderServices<NorthwindContext>(...)
        .AddSingleton(new ODataValidationSettings
        {
            MaxTop = 5,
            MaxAnyAllExpressionDepth = 3,
            MaxExpansionDepth = 3,
        });
});
```

As of 2.0, this **throws** at startup:

```
System.InvalidOperationException: Route '': registering ODataValidationSettings directly
in route services is not supported. Restier has no per-query/per-action layer for this
upstream OData class to attach to. Configure query validation limits via the
RestierRouteOptions.Validation bag on AddRestierRoute instead.
```

Migrate to the bag form:

```csharp
options.Select().Expand().Filter().OrderBy().SetMaxTop(5).Count();

options.AddRestierRoute<NorthwindApi>(
    string.Empty,
    restierServices => restierServices
        .AddEFCoreProviderServices<NorthwindContext>(...),
    bag =>
    {
        bag.Validation.MaxAnyAllExpressionDepth = 3;
        bag.Validation.MaxExpansionDepth = 3;
    });
```

`MaxTop` lives in one place (the global `SetMaxTop`) and the route-specific limits live on the bag. No conflict, no warning.

## Why not just allow `ODataValidationSettings` in DI?

The upstream class is designed for the per-action `[EnableQuery]` model. Restier has no per-action layer to attach it to, so any DI registration of it was always a workaround — and one that quietly conflicted with the global `SetMaxTop`. Removing that channel removes a whole class of "which value wins?" confusion. The bag is the only knob; the warning around `MaxTop` is the only reconciliation; the upstream per-action class stays in upstream where it belongs and never appears as a Restier-registered service. See issue [#751](https://github.com/OData/RESTier/issues/751) for the design rationale.
```

- [ ] **Step 2: Add the guide to the docsproj nav**

Open `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` and find this block (around line 44):

```xml
                                    guides/server/conformance-options;
                                    guides/server/performance;
```

Insert `guides/server/validation-options;` between them:

```xml
                                    guides/server/conformance-options;
                                    guides/server/validation-options;
                                    guides/server/performance;
```

- [ ] **Step 3: Build the docs project**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: BUILD SUCCEEDED. Verify that `src/Microsoft.Restier.Docs/docs.json` was regenerated and now includes the new page (search for `validation-options`).

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/validation-options.mdx src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj src/Microsoft.Restier.Docs/docs.json
git commit -m "docs: validation-options guide and bag precedence story"
```

---

## Task 7: Cross-reference the new guide from `conformance-options.mdx`

**Files:**
- Modify: `src/Microsoft.Restier.Docs/guides/server/conformance-options.mdx`

The conformance-options page describes the four-knob `RestierRouteOptions` bag. With this change there are five knobs — `Validation` needs a row.

- [ ] **Step 1: Add the row to the table**

Open `src/Microsoft.Restier.Docs/guides/server/conformance-options.mdx`. Find this table (around lines 8-13):

```mdx
| Property | Type | Default | Purpose |
|---|---|---|---|
| `DeepOperations` | `DeepOperationSettings` | `new() { MaxDepth = 5 }` | Maximum nesting depth for deep insert / deep update. |
| `Conformance` | `RestierConformanceOptions` | `new()` | Opt-in OData v4 spec strictness toggles. |
| `UseRestierBatching` | `bool` | `true` | Whether the Restier batch handler is registered. |
| `NamingConvention` | `RestierNamingConvention` | `PascalCase` | EDM-to-JSON property naming. |
```

Add a `Validation` row after `Conformance`:

```mdx
| Property | Type | Default | Purpose |
|---|---|---|---|
| `DeepOperations` | `DeepOperationSettings` | `new() { MaxDepth = 5 }` | Maximum nesting depth for deep insert / deep update. |
| `Conformance` | `RestierConformanceOptions` | `new()` | Opt-in OData v4 spec strictness toggles. |
| `Validation` | `RestierValidationOptions` | `new()` (all properties null → inherit from `ODataOptions`) | Per-route limits for `$top`, `$expand`, `$filter`, `$orderby`. See [Query Validation Options](/guides/server/validation-options). |
| `UseRestierBatching` | `bool` | `true` | Whether the Restier batch handler is registered. |
| `NamingConvention` | `RestierNamingConvention` | `PascalCase` | EDM-to-JSON property naming. |
```

- [ ] **Step 2: Build docs**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: BUILD SUCCEEDED.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/conformance-options.mdx
git commit -m "docs: cross-reference validation-options from conformance-options table"
```

---

## Task 8: Sweep other guide pages that show the legacy pattern

**Files:**
- Modify: `src/Microsoft.Restier.Docs/guides/server/nswag.mdx`
- Modify: `src/Microsoft.Restier.Docs/guides/server/swagger.mdx`
- Modify: `src/Microsoft.Restier.Docs/guides/server/naming-conventions.mdx`

These three pages show `AddSingleton(new ODataValidationSettings{…})` examples (per the earlier grep). Update each so the bag pattern leads.

- [ ] **Step 1: Update `nswag.mdx`**

Open `src/Microsoft.Restier.Docs/guides/server/nswag.mdx`. Find each block that registers `ODataValidationSettings` via `AddSingleton`. Replace with the bag form. For the `<Note>` block that says `RESTier automatically sets TopExample to your configured MaxTop value from ODataValidationSettings`, change it to read:

```mdx
<Note>RESTier automatically sets `TopExample` to the resolved `MaxTop` value (the bag's `RestierRouteOptions.Validation.MaxTop`, or — when the bag is silent — the value from `ODataOptions.SetMaxTop`). It also populates `ServiceRoot` from the incoming HTTP request. Any values you</Note>
```

(Trim the surrounding text minimally; only the source-of-truth wording needs updating.)

- [ ] **Step 2: Update `swagger.mdx`**

Apply the same edits as Step 1 but in `src/Microsoft.Restier.Docs/guides/server/swagger.mdx`.

- [ ] **Step 3: Update `naming-conventions.mdx`**

The two hits in `naming-conventions.mdx` are `SetMaxTop(100)` calls inside larger sample blocks. They are fine as-is *if* they do not also show an `AddSingleton(new ODataValidationSettings{…})`. Re-read both code blocks in context and:
- If the block shows only `SetMaxTop`, leave it.
- If it shows both, migrate the ODataValidationSettings half to the bag exactly as Task 4 did for the Northwind sample.

- [ ] **Step 4: Build docs**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: BUILD SUCCEEDED.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/nswag.mdx src/Microsoft.Restier.Docs/guides/server/swagger.mdx src/Microsoft.Restier.Docs/guides/server/naming-conventions.mdx
git commit -m "docs(guides): sweep legacy ODataValidationSettings examples to the bag form"
```

---

## Task 9: Release notes

**Files:**
- Create or Modify: `src/Microsoft.Restier.Docs/release-notes/2-0-0-beta.md`
- Modify: `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`

- [ ] **Step 1: Determine whether a 2-0-0-beta notes file exists**

Run: `ls src/Microsoft.Restier.Docs/release-notes/ | grep -i "2-0\|vnext\|beta" || true`

- If a `2-0-0-beta.md` (or similar `2-0-*.md`) exists, append to it.
- Otherwise, create a new file with the content below and register it in the docsproj nav (Step 3).

- [ ] **Step 2: Write the entry**

If creating the file, populate `src/Microsoft.Restier.Docs/release-notes/2-0-0-beta.md` with:

```markdown
---
title: '2.0.0-beta'
description: 'Pre-release notes for Restier 2.0.'
---

# 2.0.0-beta

## Query validation: bag-only, no more DI registration of `ODataValidationSettings` *(breaking change)*

Restier's per-route query validation knobs (`MaxTop`, `MaxSkip`, `MaxExpansionDepth`, `MaxAnyAllExpressionDepth`, `MaxOrderByNodeCount`, `MaxNodeCount`) now live on the new `RestierRouteOptions.Validation` property — and the bag is now the **only** configuration channel.

Two related changes ship in the same release:

1. **`ODataValidationSettings` is no longer a route-DI service.** `RestierController` and the OpenAPI generators (Swagger / NSwag) now resolve `RestierValidationOptions` from the route container and either build settings on demand (the controller) or read fields directly (the generators). Third-party code that previously resolved `ODataValidationSettings` from `HttpRequest.GetRouteServices()` must switch to `RestierValidationOptions`.
2. **DI registration of `ODataValidationSettings` is rejected.** Registering it inside the `AddRestierRoute` service callback throws `InvalidOperationException` at startup with a migration message pointing at the bag.

`ODataValidationSettings` is an upstream per-action class designed for `[EnableQuery]` controller methods. Restier has no per-action layer, so the per-action model never applied. Pre-2.0 versions accepted DI-registered instances as a workaround, which produced silent conflicts with the global `ODataOptions.SetMaxTop(...)` (see issues [#684](https://github.com/OData/RESTier/issues/684) and [#719](https://github.com/OData/RESTier/issues/719)). The 2.0 bag makes the route-level scope explicit, and the only place `MaxTop` can still appear twice — bag and global — emits a loud `Trace.TraceWarning` if the two values disagree.

See the new [Query Validation Options](/guides/server/validation-options) guide and issue [#751](https://github.com/OData/RESTier/issues/751) for the design rationale.

### Migration

Before:

```csharp
options.AddRestierRoute<NorthwindApi>(string.Empty, restierServices =>
{
    restierServices
        .AddEFCoreProviderServices<NorthwindContext>(...)
        .AddSingleton(new ODataValidationSettings
        {
            MaxTop = 5,
            MaxExpansionDepth = 3,
            MaxAnyAllExpressionDepth = 3,
        });
});
```

After:

```csharp
options.Select().Expand().Filter().OrderBy().SetMaxTop(5).Count();

options.AddRestierRoute<NorthwindApi>(
    string.Empty,
    restierServices => restierServices
        .AddEFCoreProviderServices<NorthwindContext>(...),
    bag =>
    {
        bag.Validation.MaxExpansionDepth = 3;
        bag.Validation.MaxAnyAllExpressionDepth = 3;
    });
```
```

If appending to an existing 2.0 notes file, place the section under an appropriate heading and skip the frontmatter.

- [ ] **Step 3: Register the file in the docsproj nav (only if newly created)**

Open `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` and find the `<Group Name="Release Notes">` block (around lines 63-79):

```xml
                        <Group Name="Release Notes" Icon="clipboard-list">
                            <Pages>
                                release-notes/index;
                                release-notes/1-2-0;
```

Insert the new release at the top of the list (most recent first):

```xml
                        <Group Name="Release Notes" Icon="clipboard-list">
                            <Pages>
                                release-notes/index;
                                release-notes/2-0-0-beta;
                                release-notes/1-2-0;
```

- [ ] **Step 4: Build docs**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: BUILD SUCCEEDED. Verify `docs.json` mentions `2-0-0-beta` in the Release Notes group.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/release-notes/2-0-0-beta.md src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj src/Microsoft.Restier.Docs/docs.json
git commit -m "docs(release-notes): announce RestierRouteOptions.Validation bag for 2.0.0-beta"
```

---

## Task 10: Final verification — whole-solution build + test

- [ ] **Step 1: Build the whole solution**

```bash
dotnet build RESTier.slnx
```

Expected: BUILD SUCCEEDED with 0 errors, 0 warnings related to this change. The whole solution build also rebuilds the docs project, which regenerates `docs.json` and the API-reference markdown.

- [ ] **Step 2: Run the whole test suite**

```bash
dotnet test RESTier.slnx
```

Expected: All tests PASS. Key regressions to watch for:
- `Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore.PagingTests` (depended on the helper's `MaxTop=5`)
- `Microsoft.Restier.Tests.AspNetCore.FeatureTests.EF6.PagingTests` (same)
- `Microsoft.Restier.Tests.AspNetCore.FeatureTests.{EFCore,EF6}.ValidationOptionsTests` (newly added)
- `Microsoft.Restier.Tests.Core.RestierValidationOptionsTests` (newly added)
- `Microsoft.Restier.Tests.AspNetCore.Options.RestierValidationOptionsResolverTests` (newly added)

If a test fails, do not silence the warning by adjusting the test — diagnose whether the bag is being read at the right layer, then fix the production code.

- [ ] **Step 3: Spot-check the rendered docs**

If a Mintlify preview is available locally, render the docs and visit `/guides/server/validation-options`, `/guides/server/conformance-options`, and the new release-notes entry. Confirm the new page renders, the table cross-reference resolves, and the warnings render in the styled `<Warning>` callout.

If no preview is available, this step is satisfied by the successful docs-project build in Step 1, which validates MDX syntax.

- [ ] **Step 4: Final commit (no-op if everything was committed in earlier tasks)**

If the previous tasks committed everything cleanly, this step is a no-op. If `dotnet build` regenerated artifacts in `src/Microsoft.Restier.Docs/api-reference/` (because XML doc comments changed in Tasks 1-3), commit them:

```bash
git add src/Microsoft.Restier.Docs/api-reference/
git commit -m "docs(api-reference): regenerate from validation-options bag changes"
```

---

## Self-Review Notes

**Spec coverage (against the bag-only architecture):**

1. *"`ODataValidationSettings` is not a Restier-shaped concept; remove it from the DI surface."* → Task 4 deletes `services.TryAddSingleton<ODataValidationSettings>()`, throws on direct registration attempts, and migrates `RestierController` + Swagger generator + NSwag generator to resolve `RestierValidationOptions` instead. The upstream class is built on demand by the controller from the bag.
2. *"Default `MaxTop` from the route's `ODataOptions`."* → Task 3's resolver reads `globalOptions.QuerySettings.MaxTop` when `bag.MaxTop` is null (both in `Resolve` and `Build`).
3. *"Drop the legacy registration pattern from samples and Breakdance."* → Task 4 migrates Breakdance + Northwind + NorthwindVersioned + Postgres samples in the same commit. Task 8 sweeps any remaining doc snippets.
4. *"Warn loudly when configuration disagrees."* → Task 3's `Resolve` emits a `Trace.TraceWarning` for the only remaining two-channel value, `MaxTop` (bag vs. global `SetMaxTop`). Task 4 step 3 calls `Resolve` once at route-add for this side effect; `Build` is the silent per-request variant used by `RestierController` so warnings aren't repeated per request.

**Type consistency check:**
- `RestierValidationOptions` (Task 1) properties match the names used by the resolver (Task 3): `MaxTop`, `MaxSkip`, `MaxExpansionDepth`, `MaxAnyAllExpressionDepth`, `MaxOrderByNodeCount`, `MaxNodeCount`. All `int?`.
- `RestierRouteOptions.Validation` (Task 2) returns `RestierValidationOptions` — matches what Task 4 step 3 registers via `services.AddSingleton(options.Validation)` and what Task 4 step 4 resolves in `RestierController`.
- Resolver method signatures (Task 3): `Resolve(bag, globalOptions, routePrefix)` (3 params, noisy) and `Build(bag, globalOptions)` (2 params, silent). Task 4 step 3 calls `Resolve`; Task 4 step 4 calls `Build`. No `userInstance` parameter anywhere.
- The resolver lives in `Microsoft.Restier.AspNetCore.Routing` (Task 3) — qualified as `Routing.RestierValidationOptionsResolver.Resolve(...)` / `.Build(...)` in `RestierController` and `RestierODataOptionsExtensions`.

**Known unknowns (executor must verify):**
- `ODataOptions.QuerySettings.MaxTop` — Task 3 step 4 calls this out. If the property path differs in `Microsoft.AspNetCore.OData` 9.x, adjust the resolver and the tests in lockstep (try `GetMaxTop()` or a property on `DefaultQuerySettings`).
- `IOptions<ODataOptions>` registration in app DI (used by Task 4 step 4 to fetch the global at request time) — `AddOData(...)` should register it as a standard `IConfigureOptions<ODataOptions>` pattern. If `HttpContext.RequestServices.GetService<IOptions<ODataOptions>>()` returns null in tests, fall back to `GetService<ODataOptions>()` directly.
- The exact signature of `AddVersion<T>` in `Microsoft.Restier.AspNetCore.Versioning` (Task 4 step 9) — the commit log says the third parameter is `Action<RestierRouteOptions>` as of `80316665`, but if there's a versioning-specific options type that wraps it, the bag access path may be `opts.RouteOptions.Validation.*` rather than `opts.Validation.*`.
- The Breakdance helper test from Task 4 step 1 (`GetTestableRestierServer<LibraryApi>`) — confirm the exact method name and parameter order in `src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs` before writing the test. The signature varies between overloads.
- The exact downstream line in the Swagger and NSwag generators that references `odataValidationSettings.MaxTop` (Task 4 steps 5 and 6) — read each file's full block before editing to make sure no other property of the upstream type is consumed; if so, read those off the bag too.
