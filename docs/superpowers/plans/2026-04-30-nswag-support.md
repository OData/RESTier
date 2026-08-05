# NSwag + ReDoc Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `Microsoft.Restier.AspNetCore.NSwag` package that serves Restier OpenAPI documents via custom middleware (mirroring the existing Swagger package) and configures NSwag's UI hosts (`UseSwaggerUi`, `UseReDoc`) to render them. Make NSwag the recommended OpenAPI path in Restier's docs and samples.

**Architecture:** Hybrid integration. Restier OpenAPI JSON is served by `RestierOpenApiMiddleware` at `/openapi/{name}/openapi.json` — Restier docs are *not* registered in NSwag's `IOpenApiDocumentGenerator` registry. NSwag's UI middleware is configured with explicit URLs that point at our middleware. `AddRestierNSwag()` also registers an MVC `IApplicationModelConvention` that hides `RestierController` from ApiExplorer, so it cannot leak into the user's plain-controllers OpenAPI doc.

**Tech Stack:** .NET 8/9/10, `Microsoft.OpenApi.OData` (EDM → OpenAPI), `NSwag.AspNetCore` 14.x (UI hosts and the user's controllers doc), xUnit v3, AwesomeAssertions, `Microsoft.AspNetCore.Mvc.Testing` for `TestServer`-based integration tests, `Microsoft.Restier.Breakdance` for in-memory Restier hosting.

**Spec:** [`docs/superpowers/specs/2026-04-30-nswag-support-design.md`](../specs/2026-04-30-nswag-support-design.md). Refer to the spec's Decision table for any context the steps below assume.

**Branch:** Work directly on `feature/vnext`. Additive (new package, new tests, sample edits).

**NSwag API note:** This plan uses NSwag 14.x method names: `UseSwaggerUi(...)` with `SwaggerUiSettings`, `UseReDoc(...)` with `ReDocSettings`, `SwaggerUiRoute` for multi-doc dropdowns. If `dotnet build` reports an unknown method on `IApplicationBuilder`, NSwag may still expose the older `UseSwaggerUi3(...)` / `SwaggerUi3Settings` / `SwaggerUi3Route` names — try those before changing the package version.

**xUnit v3 + `TreatWarningsAsErrors` note:** xUnit v3's `xUnit1051` analyzer is enabled in this repo and warnings-as-errors is on. Every `client.GetAsync(...)`, `Content.ReadAsStringAsync()`, and `host.StartAsync()` call MUST receive a `CancellationToken` argument. The pattern used in the `IApplicationBuilderExtensionsTests` from Task 9 onward:

```csharp
var cancellationToken = TestContext.Current.CancellationToken;
using var host = await BuildHostAsync(routes: ..., cancellationToken);
var response = await client.GetAsync("/openapi/...", cancellationToken);
var body = await response.Content.ReadAsStringAsync(cancellationToken);
```

`BuildHostAsync` from Task 9 is `BuildHostAsync((string prefix, Type apiType)[] routes, CancellationToken cancellationToken)`. Tasks 10 and 11 extend the helper signature; preserve the cancellationToken parameter.

**`ApiBase` constructor signature note:** `Microsoft.Restier.Core.ApiBase` has the constructor `protected ApiBase(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)`. The `TestApi` test fixture in `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Infrastructure/TestApiBase.cs` uses this signature (Task 9 establishes it). When extending the test infrastructure in later tasks, mirror this signature — do not pass `IServiceProvider` directly.

**Project conventions you must follow** (from `Directory.Build.props` and `CLAUDE.md`):

- Allman braces; prefer `var`; curly braces even for single-line blocks.
- `ImplicitUsings` is **disabled** — every `using` directive must be explicit.
- `Nullable` is **disabled**.
- `TreatWarningsAsErrors` is **enabled** globally.
- `InternalsVisibleTo` is auto-configured by `Directory.Build.props` for `Microsoft.Restier.X` → `Microsoft.Restier.Tests.X`. The test project gets access to `internal` types automatically.
- Test project package references (`xunit.v3`, `AwesomeAssertions`, `NSubstitute`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`) come from `Directory.Build.props` automatically. Do not repeat them in the test csproj.
- Commit message style: lowercase prefix (`feat:`, `test:`, `docs:`, `chore:`); always include `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` trailer.

---

## Phase 1 — Project skeleton

### Task 1: Create the source project skeleton

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.NSwag/Microsoft.Restier.AspNetCore.NSwag.csproj`

- [ ] **Step 1: Verify the directory does not exist**

```bash
test ! -e src/Microsoft.Restier.AspNetCore.NSwag && echo "OK"
```

Expected: `OK`

- [ ] **Step 2: Create the directory and csproj**

```bash
mkdir -p src/Microsoft.Restier.AspNetCore.NSwag
```

Write `src/Microsoft.Restier.AspNetCore.NSwag/Microsoft.Restier.AspNetCore.NSwag.csproj`:

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
		<PackageReference Include="Microsoft.OpenApi.OData" Version="[3.*, 4.0.0)" />
		<PackageReference Include="NSwag.AspNetCore" Version="14.*" />
	</ItemGroup>

</Project>
```

- [ ] **Step 3: Verify the project restores**

```bash
dotnet restore src/Microsoft.Restier.AspNetCore.NSwag/Microsoft.Restier.AspNetCore.NSwag.csproj
```

Expected: `Restore complete` with no errors. (The project has no .cs files yet, but restore should succeed.)

- [ ] **Step 4: Verify the project builds (will be empty assembly)**

```bash
dotnet build src/Microsoft.Restier.AspNetCore.NSwag/Microsoft.Restier.AspNetCore.NSwag.csproj
```

Expected: `Build succeeded` with no errors.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.NSwag/Microsoft.Restier.AspNetCore.NSwag.csproj
git commit -m "$(cat <<'EOF'
chore: scaffold Microsoft.Restier.AspNetCore.NSwag csproj

Empty project skeleton; implementation follows.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Create the test project skeleton

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj`

- [ ] **Step 1: Create the directory and csproj**

```bash
mkdir -p test/Microsoft.Restier.Tests.AspNetCore.NSwag
```

Write `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0;</TargetFrameworks>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Microsoft.Restier.AspNetCore.NSwag\Microsoft.Restier.AspNetCore.NSwag.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="$(RestierNet9AspNetCoreTestHostVersion)" Condition="'$(TargetFramework)' == 'net9.0'" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="$(RestierNet10AspNetCoreTestHostVersion)" Condition="'$(TargetFramework)' == 'net10.0'" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="[8.*, 9.0.0)" Condition="'$(TargetFramework)' == 'net8.0'" />
  </ItemGroup>

</Project>
```

`xunit.v3`, `AwesomeAssertions`, `NSubstitute`, `Microsoft.NET.Test.Sdk`, and `coverlet.collector` come from `Directory.Build.props` automatically — do not list them.

- [ ] **Step 2: Restore the test project**

```bash
dotnet restore test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj
```

Expected: `Restore complete` with no errors.

- [ ] **Step 3: Build the test project**

```bash
dotnet build test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj
```

Expected: `Build succeeded` with no errors.

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj
git commit -m "$(cat <<'EOF'
chore: scaffold Microsoft.Restier.Tests.AspNetCore.NSwag csproj

Empty test project skeleton. xunit.v3 / AwesomeAssertions / NSubstitute
come from Directory.Build.props; only the test-host package is added
explicitly because it is TFM-specific.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Wire both projects into the solution

**Files:**
- Modify: `RESTier.slnx`

- [ ] **Step 1: Read the current slnx**

```bash
cat RESTier.slnx
```

Note the `/src/Web/` and `/test/Web/` folders that already contain the Swagger projects — that is where the new projects go.

- [ ] **Step 2: Add the source project to `/src/Web/`**

Edit `RESTier.slnx`. Inside the `<Folder Name="/src/Web/" Id="bf61c3f1-7c7e-4515-8b51-14b374a034f9">` element, add a third `<Project>` line so the folder reads:

```xml
  <Folder Name="/src/Web/" Id="bf61c3f1-7c7e-4515-8b51-14b374a034f9">
    <Project Path="src/Microsoft.Restier.AspNetCore.NSwag/Microsoft.Restier.AspNetCore.NSwag.csproj" />
    <Project Path="src/Microsoft.Restier.AspNetCore.Swagger/Microsoft.Restier.AspNetCore.Swagger.csproj" />
    <Project Path="src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj" />
  </Folder>
```

- [ ] **Step 3: Add the test project to `/test/Web/`**

Inside `<Folder Name="/test/Web/" Id="ae160b58-fb2d-4b9f-9357-8c7648381b95">`, add the new entry so the folder reads:

```xml
  <Folder Name="/test/Web/" Id="ae160b58-fb2d-4b9f-9357-8c7648381b95">
    <Project Path="test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj" />
    <Project Path="test/Microsoft.Restier.Tests.AspNetCore.Swagger/Microsoft.Restier.Tests.AspNetCore.Swagger.csproj" />
    <Project Path="test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj" />
  </Folder>
```

- [ ] **Step 4: Verify the full solution still restores and builds**

```bash
dotnet build RESTier.slnx
```

Expected: `Build succeeded` with no errors.

- [ ] **Step 5: Commit**

```bash
git add RESTier.slnx
git commit -m "$(cat <<'EOF'
chore: add NSwag source and test projects to RESTier.slnx

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 2 — Service registration

### Task 4: TDD `AddRestierNSwag` (no settings action)

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IServiceCollectionExtensionsTests.cs`
- Create: `src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IServiceCollectionExtensions.cs`

- [ ] **Step 1: Write the failing test**

Write `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IServiceCollectionExtensionsTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag.Extensions
{

    public class IServiceCollectionExtensionsTests
    {

        [Fact]
        public void AddRestierNSwag_NoSettingsAction_RegistersAtLeastOneService()
        {
            var collection = new ServiceCollection();
            collection.AddRestierNSwag();
            collection.Should().NotBeEmpty();
        }

    }

}
```

- [ ] **Step 2: Run the test, verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~AddRestierNSwag_NoSettingsAction_RegistersAtLeastOneService"
```

Expected: build error — `AddRestierNSwag` does not exist.

- [ ] **Step 3: Implement minimal `AddRestierNSwag`**

Write `src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IServiceCollectionExtensions.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OpenApi.OData;
using System;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Extension methods on <see cref="IServiceCollection"/> for Restier NSwag support.
    /// </summary>
    public static class Restier_AspNetCore_NSwag_IServiceCollectionExtensions
    {

        /// <summary>
        /// Adds the required services to use NSwag (with ReDoc) with Restier.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register NSwag services with.</param>
        /// <param name="openApiSettings">An <see cref="Action{OpenApiConvertSettings}"/> that allows you to configure the core OpenAPI output.</param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddRestierNSwag(this IServiceCollection services, Action<OpenApiConvertSettings> openApiSettings = null)
        {
            services.AddHttpContextAccessor();

            if (openApiSettings is not null)
            {
                services.AddSingleton(openApiSettings);
            }

            return services;
        }

    }

}
```

- [ ] **Step 4: Run the test, verify it passes**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~AddRestierNSwag_NoSettingsAction_RegistersAtLeastOneService"
```

Expected: 1 test passed.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IServiceCollectionExtensions.cs test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IServiceCollectionExtensionsTests.cs
git commit -m "$(cat <<'EOF'
feat: add AddRestierNSwag service registration

Mirrors AddRestierSwagger: registers IHttpContextAccessor and the
optional OpenApiConvertSettings configurator. RestierController
filtering and middleware wiring follow.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: TDD `AddRestierNSwag` (with settings action)

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write the failing test**

Add this `[Fact]` inside `IServiceCollectionExtensionsTests`:

```csharp
[Fact]
public void AddRestierNSwag_WithSettingsAction_RegistersConfiguratorAsSingleton()
{
    var collection = new ServiceCollection();
    collection.AddRestierNSwag(settings => settings.AddAlternateKeyPaths = true);

    var provider = collection.BuildServiceProvider();
    var configurator = provider.GetService<Action<Microsoft.OpenApi.OData.OpenApiConvertSettings>>();
    configurator.Should().NotBeNull("the settings action must be retrievable as a singleton service");
}
```

- [ ] **Step 2: Run, expect PASS** (the implementation already supports this)

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~AddRestierNSwag_WithSettingsAction_RegistersConfiguratorAsSingleton"
```

Expected: 1 test passed.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IServiceCollectionExtensionsTests.cs
git commit -m "$(cat <<'EOF'
test: cover AddRestierNSwag settings-action registration path

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 3 — `RestierController` ApiExplorer convention

### Task 6: TDD `RestierControllerApiExplorerConvention`

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/RestierControllerApiExplorerConventionTests.cs`
- Create: `src/Microsoft.Restier.AspNetCore.NSwag/RestierControllerApiExplorerConvention.cs`

- [ ] **Step 1: Write the failing test**

Write `test/Microsoft.Restier.Tests.AspNetCore.NSwag/RestierControllerApiExplorerConventionTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.NSwag;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag
{

    public class RestierControllerApiExplorerConventionTests
    {

        [Fact]
        public void Apply_HidesRestierControllerActions_FromApiExplorer()
        {
            var convention = new RestierControllerApiExplorerConvention();
            var application = BuildApplicationModel(typeof(RestierController), typeof(SamplePlainController));

            convention.Apply(application);

            var restierActions = application.Controllers
                .Single(c => c.ControllerType.AsType() == typeof(RestierController))
                .Actions;
            restierActions.Should().AllSatisfy(a => a.ApiExplorer.IsVisible.Should().Be(false));
        }

        [Fact]
        public void Apply_LeavesNonRestierControllers_Untouched()
        {
            var convention = new RestierControllerApiExplorerConvention();
            var application = BuildApplicationModel(typeof(RestierController), typeof(SamplePlainController));

            convention.Apply(application);

            var plainActions = application.Controllers
                .Single(c => c.ControllerType.AsType() == typeof(SamplePlainController))
                .Actions;
            plainActions.Should().AllSatisfy(a => a.ApiExplorer.IsVisible.Should().NotBe(false),
                "convention must not change visibility on non-Restier controllers");
        }

        private static ApplicationModel BuildApplicationModel(params System.Type[] controllerTypes)
        {
            var application = new ApplicationModel();
            foreach (var t in controllerTypes)
            {
                var controllerInfo = t.GetTypeInfo();
                var controller = new ControllerModel(controllerInfo, controllerInfo.GetCustomAttributes(inherit: true).Cast<object>().ToArray());
                foreach (var method in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (method.IsSpecialName) { continue; }
                    var action = new ActionModel(method, method.GetCustomAttributes(inherit: true).Cast<object>().ToArray());
                    controller.Actions.Add(action);
                }
                application.Controllers.Add(controller);
            }
            return application;
        }

        public class SamplePlainController : ControllerBase
        {
            public IActionResult Get() => new OkResult();
        }

    }

}
```

- [ ] **Step 2: Run, verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~RestierControllerApiExplorerConventionTests"
```

Expected: build error — `RestierControllerApiExplorerConvention` does not exist.

- [ ] **Step 3: Implement the convention**

Write `src/Microsoft.Restier.AspNetCore.NSwag/RestierControllerApiExplorerConvention.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Restier.AspNetCore;
using System;

namespace Microsoft.Restier.AspNetCore.NSwag
{

    /// <summary>
    /// MVC application-model convention that hides <see cref="RestierController"/> actions from
    /// ApiExplorer. Any OpenAPI generator that relies on ApiExplorer (NSwag, Swashbuckle, .NET 9
    /// OpenAPI) will then exclude Restier endpoints from MVC-derived documents, so they cannot
    /// leak into a user's plain-controllers OpenAPI doc.
    /// </summary>
    internal class RestierControllerApiExplorerConvention : IApplicationModelConvention
    {

        public void Apply(ApplicationModel application)
        {
            if (application is null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            foreach (var controller in application.Controllers)
            {
                if (!typeof(RestierController).IsAssignableFrom(controller.ControllerType))
                {
                    continue;
                }

                foreach (var action in controller.Actions)
                {
                    action.ApiExplorer.IsVisible = false;
                }
            }
        }

    }

}
```

- [ ] **Step 4: Run, verify both tests pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~RestierControllerApiExplorerConventionTests"
```

Expected: 2 tests passed.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.NSwag/RestierControllerApiExplorerConvention.cs test/Microsoft.Restier.Tests.AspNetCore.NSwag/RestierControllerApiExplorerConventionTests.cs
git commit -m "$(cat <<'EOF'
feat: add RestierControllerApiExplorerConvention

Hides RestierController actions from ApiExplorer so any OpenAPI
generator that scans MVC controllers (NSwag, Swashbuckle, .NET 9
OpenAPI) skips them.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 7: Wire the convention into `AddRestierNSwag`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IServiceCollectionExtensions.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IServiceCollectionExtensionsTests.cs`

- [ ] **Step 1: Write the failing test**

Add to `IServiceCollectionExtensionsTests`:

```csharp
[Fact]
public void AddRestierNSwag_RegistersApiExplorerConvention_OnMvcOptions()
{
    var collection = new ServiceCollection();
    collection.AddOptions();
    collection.AddRestierNSwag();

    var provider = collection.BuildServiceProvider();
    var mvcOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions>>().Value;

    mvcOptions.Conventions
        .OfType<Microsoft.Restier.AspNetCore.NSwag.RestierControllerApiExplorerConvention>()
        .Should().HaveCount(1, "AddRestierNSwag must register the convention exactly once");
}
```

- [ ] **Step 2: Run, verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~AddRestierNSwag_RegistersApiExplorerConvention_OnMvcOptions"
```

Expected: 1 test failed (no convention registered).

- [ ] **Step 3: Update `AddRestierNSwag` to register the convention**

Replace the entire file content of `src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IServiceCollectionExtensions.cs` with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.OData;
using Microsoft.Restier.AspNetCore.NSwag;
using System;

namespace Microsoft.Extensions.DependencyInjection
{

    /// <summary>
    /// Extension methods on <see cref="IServiceCollection"/> for Restier NSwag support.
    /// </summary>
    public static class Restier_AspNetCore_NSwag_IServiceCollectionExtensions
    {

        /// <summary>
        /// Adds the required services to use NSwag (with ReDoc) with Restier.
        /// Registers an MVC application-model convention that hides <see cref="Microsoft.Restier.AspNetCore.RestierController"/>
        /// from ApiExplorer so it does not leak into the user's plain-controllers OpenAPI document.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register NSwag services with.</param>
        /// <param name="openApiSettings">An <see cref="Action{OpenApiConvertSettings}"/> that allows you to configure the core OpenAPI output.</param>
        /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddRestierNSwag(this IServiceCollection services, Action<OpenApiConvertSettings> openApiSettings = null)
        {
            services.AddHttpContextAccessor();

            if (openApiSettings is not null)
            {
                services.AddSingleton(openApiSettings);
            }

            services.Configure<MvcOptions>(options =>
            {
                options.Conventions.Add(new RestierControllerApiExplorerConvention());
            });

            return services;
        }

    }

}
```

- [ ] **Step 4: Run, verify all three service-collection tests pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~IServiceCollectionExtensionsTests"
```

Expected: 3 tests passed.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IServiceCollectionExtensions.cs test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IServiceCollectionExtensionsTests.cs
git commit -m "$(cat <<'EOF'
feat: register RestierControllerApiExplorerConvention from AddRestierNSwag

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 4 — Restier OpenAPI document generation and middleware

### Task 8: Port `RestierOpenApiDocumentGenerator` from the Swagger project

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiDocumentGenerator.cs`

This file is a near-verbatim copy of `src/Microsoft.Restier.AspNetCore.Swagger/RestierOpenApiDocumentGenerator.cs`; only the namespace differs. We do not share the file because the Swagger and NSwag packages are independent NuGet outputs.

- [ ] **Step 1: Create the file**

Write `src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiDocumentGenerator.cs`:

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
        /// <param name="documentName">The document name.</param>
        /// <param name="odataOptions">The OData options.</param>
        /// <param name="request">The current HTTP request, or null.</param>
        /// <param name="openApiSettings">Optional settings configurator.</param>
        /// <returns>The generated document, or null if the route was not found.</returns>
        public static OpenApiDocument GenerateDocument(
            string documentName,
            ODataOptions odataOptions,
            HttpRequest request,
            Action<OpenApiConvertSettings> openApiSettings)
        {
            var routePrefix = string.Equals(documentName, DefaultDocumentName, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : documentName;

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

    }

}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build src/Microsoft.Restier.AspNetCore.NSwag/Microsoft.Restier.AspNetCore.NSwag.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiDocumentGenerator.cs
git commit -m "$(cat <<'EOF'
feat: port RestierOpenApiDocumentGenerator into NSwag package

Verbatim copy of the generator from the Swagger package; namespace is
the only difference. Tested indirectly through the middleware tests
(Task 9 and the integration tests).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 9: TDD `RestierOpenApiMiddleware` and `UseRestierOpenApi` (200 + 404 + multi-route)

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensionsTests.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Infrastructure/TestApiBase.cs`
- Create: `src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiMiddleware.cs`
- Create: `src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensions.cs`

- [ ] **Step 1: Create a tiny in-memory Restier API for tests**

Write `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Infrastructure/TestApiBase.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core;
using System.Linq;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag.Infrastructure
{

    public class TestApi : ApiBase
    {

        public TestApi(System.IServiceProvider services) : base(services)
        {
        }

        public IQueryable<TestEntity> Items => System.Linq.Enumerable.Empty<TestEntity>().AsQueryable();

    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public static class TestEdmModelBuilder
    {
        public static IEdmModel Build()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<TestEntity>(nameof(TestApi.Items));
            return builder.GetEdmModel();
        }
    }

}
```

- [ ] **Step 2: Write the failing test for the middleware behavior**

Write `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensionsTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Tests.AspNetCore.NSwag.Infrastructure;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag.Extensions
{

    public class IApplicationBuilderExtensionsTests
    {

        [Fact]
        public async Task UseRestierOpenApi_ServesEachRegisteredRouteUnderItsName()
        {
            using var host = await BuildHostAsync(routes: new[] { ("", typeof(TestApi)), ("v3", typeof(TestApi)) });
            var client = host.GetTestClient();

            var defaultResponse = await client.GetAsync("/openapi/default/openapi.json");
            defaultResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var defaultJson = await defaultResponse.Content.ReadAsStringAsync();
            JsonDocument.Parse(defaultJson).RootElement.GetProperty("openapi").GetString().Should().StartWith("3.");

            var v3Response = await client.GetAsync("/openapi/v3/openapi.json");
            v3Response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task UseRestierOpenApi_ReturnsNotFound_ForUnknownDocumentName()
        {
            using var host = await BuildHostAsync(routes: new[] { ("", typeof(TestApi)) });
            var client = host.GetTestClient();

            var response = await client.GetAsync("/openapi/nonexistent/openapi.json");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        private static async Task<IHost> BuildHostAsync((string prefix, System.Type apiType)[] routes)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services
                            .AddControllers()
                            .AddRestier(options =>
                            {
                                foreach (var (prefix, apiType) in routes)
                                {
                                    options.AddRestierRouteForApiType(prefix, apiType, restierServices =>
                                    {
                                        restierServices.AddSingleton(TestEdmModelBuilder.Build());
                                    });
                                }
                            });
                        services.AddRestierNSwag();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapRestier());
                        app.UseRestierOpenApi();
                    }));

            var host = await builder.StartAsync();
            return host;
        }

    }

}
```

`AddRestierRouteForApiType(prefix, apiType, ...)` is not a real method — the actual generic call is `options.AddRestierRoute<T>(prefix, ...)`. Adjust the helper to register one closed generic per item:

```csharp
foreach (var (prefix, apiType) in routes)
{
    if (apiType == typeof(TestApi))
    {
        options.AddRestierRoute<TestApi>(prefix, restierServices =>
        {
            restierServices.AddSingleton(TestEdmModelBuilder.Build());
        });
    }
}
```

Replace the generic-loop block in the test helper with the closed-generic version above.

- [ ] **Step 3: Run, verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~UseRestierOpenApi"
```

Expected: build error — `UseRestierOpenApi` does not exist.

- [ ] **Step 4: Implement the middleware**

Write `src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiMiddleware.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Microsoft.OpenApi.OData;
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

        public RestierOpenApiMiddleware(
            RequestDelegate next,
            IOptions<ODataOptions> odataOptions,
            Action<OpenApiConvertSettings> openApiSettings = null)
        {
            this.next = next;
            this.odataOptions = odataOptions;
            this.openApiSettings = openApiSettings;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            if (path is not null
                && path.StartsWith(PathPrefix, StringComparison.OrdinalIgnoreCase)
                && path.EndsWith(PathSuffix, StringComparison.OrdinalIgnoreCase))
            {
                var documentName = path.Substring(PathPrefix.Length, path.Length - PathPrefix.Length - PathSuffix.Length);
                if (!string.IsNullOrEmpty(documentName))
                {
                    var document = RestierOpenApiDocumentGenerator.GenerateDocument(
                        documentName,
                        odataOptions.Value,
                        context.Request,
                        openApiSettings);

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

- [ ] **Step 5: Implement `UseRestierOpenApi` (other methods stubbed for now)**

Write `src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensions.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.AspNetCore.NSwag;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Extension methods on <see cref="IApplicationBuilder"/> for Restier NSwag support.
    /// </summary>
    public static class Restier_AspNetCore_NSwag_IApplicationBuilderExtensions
    {

        /// <summary>
        /// Adds middleware that serves OpenAPI 3.0 JSON for every registered Restier route at
        /// <c>/openapi/{documentName}/openapi.json</c>.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add middleware to.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
        public static IApplicationBuilder UseRestierOpenApi(this IApplicationBuilder app)
        {
            app.UseMiddleware<RestierOpenApiMiddleware>();
            return app;
        }

    }

}
```

- [ ] **Step 6: Run, verify both middleware tests pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~UseRestierOpenApi"
```

Expected: 2 tests passed.

- [ ] **Step 7: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.NSwag/RestierOpenApiMiddleware.cs src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensions.cs test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensionsTests.cs test/Microsoft.Restier.Tests.AspNetCore.NSwag/Infrastructure/TestApiBase.cs
git commit -m "$(cat <<'EOF'
feat: add RestierOpenApiMiddleware and UseRestierOpenApi

Serves OpenAPI 3.0 JSON for every Restier route at
/openapi/{name}/openapi.json. Returns 404 for unknown document names.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 10: Test `ServiceRoot` reflection and that the `OpenApiConvertSettings` callback is invoked

**Files:**
- Modify: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensionsTests.cs`

- [ ] **Step 1: Extend `BuildHostAsync` to accept a service-collection configurator**

Update the helper signature to:

```csharp
private static async Task<IHost> BuildHostAsync(
    (string prefix, System.Type apiType)[] routes,
    System.Action<IServiceCollection> configureServices = null,
    System.Action<IApplicationBuilder> configurePipeline = null)
```

Inside the `.ConfigureServices(services => { ... })` block, after the existing `services.AddRestierNSwag();` line, change to:

```csharp
if (configureServices is not null)
{
    configureServices(services);
}
else
{
    services.AddRestierNSwag();
}
```

(The default branch still registers `AddRestierNSwag()` so existing callers keep working without changes.)

- [ ] **Step 2: Write the `ServiceRoot` test**

Add to `IApplicationBuilderExtensionsTests`:

```csharp
[Fact]
public async Task UseRestierOpenApi_ReflectsInboundHostAndPathBase_InServiceRoot()
{
    using var host = await BuildHostAsync(routes: new[] { ("v3", typeof(TestApi)) });
    var client = host.GetTestClient();
    client.DefaultRequestHeaders.Host = "example.com:8443";

    var response = await client.GetAsync("/openapi/v3/openapi.json");
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var json = await response.Content.ReadAsStringAsync();
    var root = JsonDocument.Parse(json).RootElement;
    var serverUrl = root.GetProperty("servers")[0].GetProperty("url").GetString();
    serverUrl.Should().Contain("example.com:8443");
    serverUrl.Should().EndWith("/v3");
}
```

- [ ] **Step 3: Write the configurator-callback test**

Add a second `[Fact]`:

```csharp
[Fact]
public async Task AddRestierNSwag_InvokesOpenApiConvertSettingsCallback_OnEachRequest()
{
    var callbackInvocations = 0;
    using var host = await BuildHostAsync(
        routes: new[] { ("", typeof(TestApi)) },
        configureServices: services =>
        {
            services.AddRestierNSwag(settings =>
            {
                settings.TopExample = 42;
                System.Threading.Interlocked.Increment(ref callbackInvocations);
            });
        });
    var client = host.GetTestClient();

    var response = await client.GetAsync("/openapi/default/openapi.json");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    callbackInvocations.Should().BeGreaterThan(0,
        "the OpenApiConvertSettings configurator must be invoked when generating the document");
}
```

- [ ] **Step 4: Run, verify both tests pass**

The middleware already builds `ServiceRoot` from `request.Host`/`PathBase`/route prefix and already invokes the configurator; no implementation change required.

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~UseRestierOpenApi_ReflectsInboundHostAndPathBase|FullyQualifiedName~AddRestierNSwag_InvokesOpenApiConvertSettingsCallback"
```

Expected: 2 tests passed.

- [ ] **Step 5: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensionsTests.cs
git commit -m "$(cat <<'EOF'
test: cover ServiceRoot from request and OpenApiConvertSettings callback

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 5 — NSwag UI hosts

### Task 11: TDD `UseRestierReDoc`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensions.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensionsTests.cs`

- [ ] **Step 1: Write the failing test**

Add:

```csharp
[Fact]
public async Task UseRestierReDoc_ServesOnePagePerRoutePrefix_PointingAtRestierMiddlewareUrl()
{
    using var host = await BuildHostAsync(
        routes: new[] { ("", typeof(TestApi)), ("v3", typeof(TestApi)) },
        configurePipeline: app =>
        {
            app.UseRestierOpenApi();
            app.UseRestierReDoc();
        });
    var client = host.GetTestClient();

    var defaultPage = await client.GetAsync("/redoc/default");
    defaultPage.StatusCode.Should().Be(HttpStatusCode.OK);
    var defaultBody = await defaultPage.Content.ReadAsStringAsync();
    defaultBody.Should().Contain("/openapi/default/openapi.json", "ReDoc must load Restier doc from the middleware URL");

    var v3Page = await client.GetAsync("/redoc/v3");
    v3Page.StatusCode.Should().Be(HttpStatusCode.OK);
    (await v3Page.Content.ReadAsStringAsync()).Should().Contain("/openapi/v3/openapi.json");
}
```

Update `BuildHostAsync` so it accepts an optional `configurePipeline` callback. The signature becomes:

```csharp
private static async Task<IHost> BuildHostAsync(
    (string prefix, System.Type apiType)[] routes,
    System.Action<IApplicationBuilder> configurePipeline = null)
```

Replace the existing `.Configure(app => { ... app.UseRestierOpenApi(); })` block with:

```csharp
.Configure(app =>
{
    app.UseRouting();
    app.UseEndpoints(endpoints => endpoints.MapRestier());
    if (configurePipeline is not null)
    {
        configurePipeline(app);
    }
    else
    {
        app.UseRestierOpenApi();
    }
})
```

- [ ] **Step 2: Run, verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~UseRestierReDoc"
```

Expected: build error — `UseRestierReDoc` does not exist.

- [ ] **Step 3: Implement `UseRestierReDoc`**

Replace the entire content of `src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensions.cs` with:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.NSwag;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Extension methods on <see cref="IApplicationBuilder"/> for Restier NSwag support.
    /// </summary>
    public static class Restier_AspNetCore_NSwag_IApplicationBuilderExtensions
    {

        /// <summary>
        /// Adds middleware that serves OpenAPI 3.0 JSON for every registered Restier route at
        /// <c>/openapi/{documentName}/openapi.json</c>.
        /// </summary>
        public static IApplicationBuilder UseRestierOpenApi(this IApplicationBuilder app)
        {
            app.UseMiddleware<RestierOpenApiMiddleware>();
            return app;
        }

        /// <summary>
        /// Adds NSwag's ReDoc middleware once per Restier route, configured with the matching
        /// <c>/openapi/{name}/openapi.json</c> document URL.
        /// </summary>
        public static IApplicationBuilder UseRestierReDoc(this IApplicationBuilder app)
        {
            var odataOptions = app.ApplicationServices.GetRequiredService<IOptions<ODataOptions>>().Value;
            foreach (var prefix in odataOptions.GetRestierRoutePrefixes())
            {
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

    }

}
```

- [ ] **Step 4: Run, verify the test passes**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~UseRestierReDoc"
```

Expected: 1 test passed.

If the build reports "UseReDoc does not exist," NSwag may expose it under a different namespace — add `using NSwag.AspNetCore;` at the top of the file.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensions.cs test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensionsTests.cs
git commit -m "$(cat <<'EOF'
feat: add UseRestierReDoc

Configures NSwag's ReDoc middleware once per Restier route, pointing
at the matching /openapi/{name}/openapi.json URL.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 12: TDD `UseRestierNSwagUI`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensions.cs`
- Modify: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensionsTests.cs`

- [ ] **Step 1: Write the failing test**

Add:

```csharp
[Fact]
public async Task UseRestierNSwagUI_ListsAllRestierRoutes_AsSwaggerUrls()
{
    using var host = await BuildHostAsync(
        routes: new[] { ("", typeof(TestApi)), ("v3", typeof(TestApi)) },
        configurePipeline: app =>
        {
            app.UseRestierOpenApi();
            app.UseRestierNSwagUI();
        });
    var client = host.GetTestClient();

    // NSwag's Swagger UI exposes its config at /swagger/index.html (HTML) and references the doc URLs in script.
    // Easiest stable assertion: the index page mentions both Restier doc URLs.
    var indexPage = await client.GetAsync("/swagger/index.html");
    indexPage.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await indexPage.Content.ReadAsStringAsync();
    body.Should().Contain("/openapi/default/openapi.json");
    body.Should().Contain("/openapi/v3/openapi.json");
}
```

- [ ] **Step 2: Run, verify it fails**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~UseRestierNSwagUI"
```

Expected: build error — `UseRestierNSwagUI` does not exist.

- [ ] **Step 3: Implement `UseRestierNSwagUI`**

Append a new method inside `Restier_AspNetCore_NSwag_IApplicationBuilderExtensions`:

```csharp
/// <summary>
/// Adds NSwag's Swagger UI 3 host at <c>/swagger</c> with a dropdown listing every Restier route.
/// </summary>
public static IApplicationBuilder UseRestierNSwagUI(this IApplicationBuilder app)
{
    var odataOptions = app.ApplicationServices.GetRequiredService<IOptions<ODataOptions>>().Value;
    app.UseSwaggerUi(settings =>
    {
        settings.Path = "/swagger";
        foreach (var prefix in odataOptions.GetRestierRoutePrefixes())
        {
            var documentName = string.IsNullOrEmpty(prefix)
                ? RestierOpenApiDocumentGenerator.DefaultDocumentName
                : prefix;
            settings.SwaggerRoutes.Add(new global::NSwag.AspNetCore.SwaggerUiRoute(documentName, $"/openapi/{documentName}/openapi.json"));
        }
    });
    return app;
}
```

If `UseSwaggerUi` / `SwaggerUiRoute` are not found, swap to NSwag's older names: `UseSwaggerUi3`, `SwaggerUi3Route`.

- [ ] **Step 4: Run, verify the test passes**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~UseRestierNSwagUI"
```

Expected: 1 test passed.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensions.cs test/Microsoft.Restier.Tests.AspNetCore.NSwag/Extensions/IApplicationBuilderExtensionsTests.cs
git commit -m "$(cat <<'EOF'
feat: add UseRestierNSwagUI

Mounts NSwag's Swagger UI 3 at /swagger with a dropdown of every
Restier route.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 6 — End-to-end integration tests

### Task 13: Combined Restier + plain MVC controller scenario

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/IntegrationTests/CombinedAppTests.cs`

- [ ] **Step 1: Write the integration test**

Write `test/Microsoft.Restier.Tests.AspNetCore.NSwag/IntegrationTests/CombinedAppTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Tests.AspNetCore.NSwag.Infrastructure;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag.IntegrationTests
{

    public class CombinedAppTests
    {

        [Fact]
        public async Task RestierDocAndControllersDoc_AreIsolated()
        {
            using var host = await BuildAsync();
            var client = host.GetTestClient();

            // Restier doc contains Restier paths, not the plain controller's path.
            var restierJson = await client.GetStringAsync("/openapi/default/openapi.json");
            var restierRoot = JsonDocument.Parse(restierJson).RootElement;
            restierRoot.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/Items"));
            restierJson.Should().NotContain("/health/live");

            // User's controllers doc contains the plain controller, not RestierController.
            var controllersJson = await client.GetStringAsync("/swagger/controllers/swagger.json");
            controllersJson.Should().Contain("/health/live");
            controllersJson.Should().NotContain("RestierController");
        }

        [Fact]
        public async Task RestierDocs_AreNotInNSwagRegistry()
        {
            using var host = await BuildAsync();
            var client = host.GetTestClient();

            // NSwag's default path for a doc named "default" would be /swagger/default/swagger.json.
            // Restier docs are not in NSwag's registry, so this must 404.
            var response = await client.GetAsync("/swagger/default/swagger.json");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        private static async Task<IHost> BuildAsync()
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services
                            .AddControllers()
                            .AddRestier(options =>
                            {
                                options.AddRestierRoute<TestApi>("", restierServices =>
                                    restierServices.AddSingleton(TestEdmModelBuilder.Build()));
                            })
                            .AddApplicationPart(typeof(HealthController).Assembly);

                        services.AddRestierNSwag();
                        services.AddOpenApiDocument(c => c.DocumentName = "controllers");
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
                        app.UseOpenApi();
                    }));

            return await builder.StartAsync();
        }

        [ApiController]
        [Route("health")]
        public class HealthController : ControllerBase
        {

            [HttpGet("live")]
            public IActionResult Live() => Ok(new { status = "ok" });

        }

    }

}
```

- [ ] **Step 2: Run, verify both tests pass**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj --filter "FullyQualifiedName~CombinedAppTests"
```

Expected: 2 tests passed.

The "controllers doc does not contain RestierController" assertion is the load-bearing proof that the `IApplicationModelConvention` hides `RestierController` from ApiExplorer even when it is reached via `MapDynamicControllerRoute` rather than attribute routing.

- [ ] **Step 3: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore.NSwag/IntegrationTests/CombinedAppTests.cs
git commit -m "$(cat <<'EOF'
test: end-to-end Restier + plain MVC controller doc isolation

Proves (1) the user's controllers doc contains plain controllers but
not RestierController (auto-filter end-to-end through dynamic routing);
(2) Restier docs are not in NSwag's registry — /swagger/default/swagger.json
returns 404, only /openapi/default/openapi.json serves them.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 14: Run the full test project across all TFMs

- [ ] **Step 1: Run every test on every TFM**

```bash
dotnet test test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj
```

Expected: all tests pass on `net8.0`, `net9.0`, `net10.0`. If any TFM fails to build because NSwag does not target it, follow the spec's risk note: drop that TFM from the source `csproj` and rerun.

- [ ] **Step 2: If any TFM was dropped, commit the change**

If you removed a TFM:

```bash
git add src/Microsoft.Restier.AspNetCore.NSwag/Microsoft.Restier.AspNetCore.NSwag.csproj test/Microsoft.Restier.Tests.AspNetCore.NSwag/Microsoft.Restier.Tests.AspNetCore.NSwag.csproj
git commit -m "$(cat <<'EOF'
chore: scope NSwag package TFMs to versions NSwag 14.x supports

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

If no change was needed, skip this step.

---

## Phase 7 — Sample updates

### Task 15: Switch the Northwind sample to NSwag and add a plain controller

**Files:**
- Modify: `src/Microsoft.Restier.Samples.Northwind.AspNetCore/Microsoft.Restier.Samples.Northwind.AspNetCore.csproj`
- Modify: `src/Microsoft.Restier.Samples.Northwind.AspNetCore/Startup.cs`
- Create: `src/Microsoft.Restier.Samples.Northwind.AspNetCore/Controllers/HealthController.cs`

- [ ] **Step 1: Read the current Northwind csproj**

```bash
cat src/Microsoft.Restier.Samples.Northwind.AspNetCore/Microsoft.Restier.Samples.Northwind.AspNetCore.csproj
```

Locate the `<ProjectReference Include="..\Microsoft.Restier.AspNetCore.Swagger\..." />` element.

- [ ] **Step 2: Replace the Swagger ProjectReference with the NSwag ProjectReference**

In the csproj, change:

```xml
<ProjectReference Include="..\Microsoft.Restier.AspNetCore.Swagger\Microsoft.Restier.AspNetCore.Swagger.csproj" />
```

to:

```xml
<ProjectReference Include="..\Microsoft.Restier.AspNetCore.NSwag\Microsoft.Restier.AspNetCore.NSwag.csproj" />
```

- [ ] **Step 3: Add a plain `HealthController`**

Write `src/Microsoft.Restier.Samples.Northwind.AspNetCore/Controllers/HealthController.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Restier.Samples.Northwind.AspNetCore.Controllers
{

    /// <summary>
    /// Plain ASP.NET Core controller used to demonstrate combining Restier with regular MVC
    /// endpoints in the same OpenAPI surface. This controller appears in the "controllers"
    /// OpenAPI document, separate from the Restier-derived Northwind document.
    /// </summary>
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {

        [HttpGet("live")]
        public IActionResult Live() => Ok(new { status = "ok" });

        [HttpGet("version")]
        public IActionResult Version() => Ok(new { version = typeof(HealthController).Assembly.GetName().Version?.ToString() });

    }

}
```

- [ ] **Step 4: Update `Startup.cs` to use NSwag**

Read the file:

```bash
cat src/Microsoft.Restier.Samples.Northwind.AspNetCore/Startup.cs
```

Apply these changes:

1. Replace `services.AddRestierSwagger();` with:

```csharp
services.AddRestierNSwag();
services.AddOpenApiDocument(c => c.DocumentName = "controllers");
```

2. Replace `app.UseRestierSwaggerUI();` with:

```csharp
app.UseRestierOpenApi();
app.UseRestierReDoc();
app.UseRestierNSwagUI();
app.UseOpenApi();
app.UseReDoc(c =>
{
    c.Path = "/redoc/controllers";
    c.DocumentPath = "/swagger/controllers/swagger.json";
});
```

3. Add the required `using` directives at the top if not already present:

```csharp
using NSwag.AspNetCore;
```

- [ ] **Step 5: Build the sample**

```bash
dotnet build src/Microsoft.Restier.Samples.Northwind.AspNetCore/Microsoft.Restier.Samples.Northwind.AspNetCore.csproj
```

Expected: `Build succeeded`. If you see a missing namespace, add `using` directives until it compiles.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.Samples.Northwind.AspNetCore/Microsoft.Restier.Samples.Northwind.AspNetCore.csproj src/Microsoft.Restier.Samples.Northwind.AspNetCore/Startup.cs src/Microsoft.Restier.Samples.Northwind.AspNetCore/Controllers/HealthController.cs
git commit -m "$(cat <<'EOF'
feat(samples): switch Northwind from Swagger to NSwag, add HealthController

Demonstrates the combined-app scenario: Northwind OData surface is
served as one OpenAPI doc at /openapi/default/openapi.json; the plain
HealthController is served as a separate "controllers" doc at
/swagger/controllers/swagger.json.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 16: Add NSwag to the Postgres sample

**Files:**
- Modify: `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Microsoft.Restier.Samples.Postgres.AspNetCore.csproj`
- Modify: `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Program.cs`

- [ ] **Step 1: Add the NSwag ProjectReference**

Edit `Microsoft.Restier.Samples.Postgres.AspNetCore.csproj`. Inside the existing `<ItemGroup>` that holds `<ProjectReference>`s, add:

```xml
<ProjectReference Include="..\Microsoft.Restier.AspNetCore.NSwag\Microsoft.Restier.AspNetCore.NSwag.csproj" />
```

- [ ] **Step 2: Wire NSwag into `Program.cs`**

Read the file:

```bash
cat src/Microsoft.Restier.Samples.Postgres.AspNetCore/Program.cs
```

After the `.AddApplicationPart(typeof(RestierController).Assembly);` call, add:

```csharp
            builder.Services.AddRestierNSwag();
```

After `app.UseEndpoints(...)` and before `app.Run();`, add:

```csharp
            app.UseRestierOpenApi();
            app.UseRestierReDoc();
```

(No `UseRestierNSwagUI()` — keep the minimal sample minimal; ReDoc only.)

- [ ] **Step 3: Build the sample**

```bash
dotnet build src/Microsoft.Restier.Samples.Postgres.AspNetCore/Microsoft.Restier.Samples.Postgres.AspNetCore.csproj
```

Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Samples.Postgres.AspNetCore/Microsoft.Restier.Samples.Postgres.AspNetCore.csproj src/Microsoft.Restier.Samples.Postgres.AspNetCore/Program.cs
git commit -m "$(cat <<'EOF'
feat(samples): add NSwag (ReDoc only) to Postgres sample

Minimal NSwag wire-up for users who want a plain Restier service with
ReDoc and nothing else.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 17: Manual browser verification of the samples

**Why:** Per `CLAUDE.md`, UI changes must be verified in a browser. Type checks and tests verify code correctness, not feature correctness.

- [ ] **Step 1: Run Northwind**

```bash
dotnet run --project src/Microsoft.Restier.Samples.Northwind.AspNetCore/Microsoft.Restier.Samples.Northwind.AspNetCore.csproj
```

Note the listening URL printed in the console (default: `http://localhost:5000` or similar).

- [ ] **Step 2: Verify Northwind in a browser**

Open each URL and confirm:

- `/redoc/default` — ReDoc renders the Northwind OData entity sets (Customers, Orders, Products, etc.). No `/health/...` endpoints appear.
- `/redoc/controllers` — ReDoc renders only `GET /health/live` and `GET /health/version`. No OData entity sets.
- `/swagger` — NSwag Swagger UI 3 displays a dropdown listing the Restier route(s). Default route is selected.
- `/openapi/default/openapi.json` — Returns valid OpenAPI 3.0 JSON.
- `/swagger/controllers/swagger.json` — Returns valid OpenAPI JSON for `HealthController` only.
- `/swagger/default/swagger.json` — Returns 404 (Restier docs are not in NSwag's registry).

If any page is broken, stop, fix the code, rebuild, retry.

- [ ] **Step 3: Stop Northwind, run Postgres**

Ctrl+C the Northwind process. Postgres requires a running PostgreSQL instance — verify connection details in `appsettings.Development.json` first; if unavailable, document and skip the runtime check.

```bash
dotnet run --project src/Microsoft.Restier.Samples.Postgres.AspNetCore/Microsoft.Restier.Samples.Postgres.AspNetCore.csproj
```

- [ ] **Step 4: Verify Postgres in a browser**

- `/redoc/v3` — ReDoc renders the Postgres OData API.
- `/openapi/v3/openapi.json` — Returns valid OpenAPI 3.0 JSON.
- `/swagger` — Returns 404 (UI was not enabled in the Postgres sample).

If Postgres is not available locally, skip the runtime check and explicitly note it: "Postgres sample build verified; runtime not verified locally because Postgres unavailable."

- [ ] **Step 5: No commit needed for verification — the samples were already committed.**

---

## Phase 8 — Documentation

### Task 18: Wire the NSwag project into the docs project

**Files:**
- Modify: `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`

- [ ] **Step 1: Read the docsproj**

```bash
cat src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Locate the two ItemGroups that list source projects: one with `<ProjectReference>` items, one with `<_DocsSourceProject>` items.

- [ ] **Step 2: Add the NSwag project to both ItemGroups**

Inside the `<ItemGroup>` that contains existing `<ProjectReference>` lines, add:

```xml
<ProjectReference Include="..\Microsoft.Restier.AspNetCore.NSwag\Microsoft.Restier.AspNetCore.NSwag.csproj" />
```

Inside the `<ItemGroup>` that contains existing `<_DocsSourceProject>` lines, add:

```xml
<_DocsSourceProject Include="..\Microsoft.Restier.AspNetCore.NSwag\Microsoft.Restier.AspNetCore.NSwag.csproj" />
```

- [ ] **Step 3: Build the docsproj to confirm it picks up the new assembly**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: `Build succeeded`. The DotNetDocs SDK will generate API-reference MDX for the new assembly (you should see new files appearing under `src/Microsoft.Restier.Docs/api-reference/Microsoft/Restier/AspNetCore/NSwag/...`). These files are gitignored.

- [ ] **Step 4: Commit (csproj only — api-reference is gitignored)**

```bash
git add src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
git commit -m "$(cat <<'EOF'
docs: include Microsoft.Restier.AspNetCore.NSwag in docsproj sources

Wires the new package into DotNetDocs SDK's source-project list so
api-reference MDX gets auto-generated.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 19: Write `guides/server/nswag.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/nswag.mdx`

- [ ] **Step 1: Read the existing `swagger.mdx` for style reference**

```bash
cat src/Microsoft.Restier.Docs/guides/server/swagger.mdx
```

Note the Mintlify components used (`<Note>`, code blocks with language hints, fenced examples).

- [ ] **Step 2: Write the new page**

Write `src/Microsoft.Restier.Docs/guides/server/nswag.mdx`:

````mdx
---
title: "OpenAPI / NSwag Support"
description: "Generate OpenAPI documents from your Restier API and render them with NSwag and ReDoc"
icon: "code"
sidebarTitle: "NSwag (recommended)"
---

RESTier can automatically generate an [OpenAPI](https://www.openapis.org/) document from your EDM
model and render it with [NSwag](https://github.com/RicoSuter/NSwag) — including [ReDoc](https://redocly.com/redoc),
[Swagger UI 3](https://swagger.io/tools/swagger-ui/), and the [NSwagStudio](https://github.com/RicoSuter/NSwag/wiki/NSwagStudio)
client-code-generation tooling. This is provided by the `Microsoft.Restier.AspNetCore.NSwag` package.

<Note>NSwag is the recommended OpenAPI integration for new Restier projects. The
[Swashbuckle-based Swagger package](swagger) remains supported for projects already invested in it.</Note>

## Setup

### Install the NuGet Package

```bash
dotnet add package Microsoft.Restier.AspNetCore.NSwag
```

### Register Services

In your `Program.cs`, call `AddRestierNSwag()` on the service collection:

```csharp
builder.Services.AddRestierNSwag();
```

### Add Middleware

Wire up the middleware in your application pipeline:

```csharp
app.UseRestierOpenApi();   // serves /openapi/{name}/openapi.json
app.UseRestierReDoc();     // serves /redoc/{name}
app.UseRestierNSwagUI();   // serves /swagger (Swagger UI 3 with a route dropdown)
```

Each `Use*` method is independent — call any combination.

### Complete Example

```csharp
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRestierNSwag();

builder.Services
    .AddControllers()
    .AddRestier(options =>
    {
        options.Select().Expand().Filter().OrderBy().SetMaxTop(100).Count();

        options.AddRestierRoute<MyApi>("api", routeServices =>
        {
            routeServices.AddEFCoreProviderServices<MyDbContext>(dbOptions =>
                dbOptions.UseSqlServer(connectionString));
        });
    });

var app = builder.Build();

app.UseRouting();
app.MapControllers();
app.MapRestier();

app.UseRestierOpenApi();
app.UseRestierReDoc();
app.UseRestierNSwagUI();

app.Run();
```

## Endpoints

Once the middleware is registered, these endpoints become available:

| Endpoint | Description |
|----------|-------------|
| `/openapi/{documentName}/openapi.json` | OpenAPI 3.0 JSON for one Restier route |
| `/redoc/{documentName}` | ReDoc page for one Restier route |
| `/swagger` | Swagger UI 3 with a dropdown of every Restier route |

The `{documentName}` corresponds to the OData route prefix you registered. If your route prefix is `"api"`,
the document URL is `/openapi/api/openapi.json`. If the prefix is empty, the document name defaults to
`"default"`, so the URL is `/openapi/default/openapi.json`.

## Configuration

You can customize the generated OpenAPI document by passing an `Action<OpenApiConvertSettings>` to
`AddRestierNSwag()`. The `OpenApiConvertSettings` class comes from the
[Microsoft.OpenApi.OData](https://github.com/microsoft/OpenAPI.NET.OData) package and controls how the
EDM model is converted to OpenAPI:

```csharp
builder.Services.AddRestierNSwag(settings =>
{
    settings.TopExample = 10;
    settings.PathPrefix = "v1";
    settings.EnableKeyAsSegment = true;
});
```

<Note>RESTier automatically sets `TopExample` to your configured `MaxTop` value from
`ODataValidationSettings` and populates `ServiceRoot` from the incoming HTTP request. Any values you
set in the configuration action will override these defaults.</Note>

## Multiple Restier APIs

If your application registers multiple Restier APIs with different route prefixes, all the `Use*`
methods automatically discover them and serve a separate document/UI per route. The Swagger UI shows
a dropdown that lets you switch between APIs:

```csharp
builder.Services
    .AddControllers()
    .AddRestier(options =>
    {
        options.Select().Expand().Filter().OrderBy().SetMaxTop(100).Count();

        options.AddRestierRoute<TripsApi>("trips", routeServices =>
        {
            routeServices.AddEFCoreProviderServices<TripsContext>(dbOptions => /* ... */);
        });

        options.AddRestierRoute<BookingsApi>("bookings", routeServices =>
        {
            routeServices.AddEFCoreProviderServices<BookingsContext>(dbOptions => /* ... */);
        });
    });
```

You will get four endpoints from `UseRestierOpenApi()` + `UseRestierReDoc()`:

- `/openapi/trips/openapi.json` and `/openapi/bookings/openapi.json`
- `/redoc/trips` and `/redoc/bookings`

Plus a single `/swagger` page (from `UseRestierNSwagUI()`) with a two-entry dropdown.

## Combining with plain ASP.NET Core controllers

NSwag can scan your plain MVC controllers and serve them as a separate OpenAPI document alongside the
Restier docs. Register an extra document through NSwag's standard API:

```csharp
builder.Services.AddOpenApiDocument(c => c.DocumentName = "controllers");

// in the pipeline:
app.UseOpenApi();    // /swagger/controllers/swagger.json
app.UseReDoc(c =>
{
    c.Path = "/redoc/controllers";
    c.DocumentPath = "/swagger/controllers/swagger.json";
});
```

`AddRestierNSwag()` automatically hides `RestierController` from ApiExplorer, so it will not appear
in your controllers document. You don't need to add any `[ApiExplorerSettings]` attributes or
operation filters to make this work.

For a working sample, see the [Northwind sample](https://github.com/OData/RESTier/tree/main/src/Microsoft.Restier.Samples.Northwind.AspNetCore),
which combines a Restier OData service with a plain `HealthController`.

## What `AddRestierNSwag()` does for you

<Note>
Calling `AddRestierNSwag()` is a one-liner, but it wires up three things behind the scenes:

1. An MVC `IApplicationModelConvention` that hides `RestierController` from ApiExplorer, so it
   cannot leak into any OpenAPI document built via NSwag, Swashbuckle, or .NET 9 OpenAPI.
2. `IHttpContextAccessor` registration (used by `RestierOpenApiMiddleware` to compute `ServiceRoot`).
3. The optional `Action<OpenApiConvertSettings>` configurator, registered as a singleton so the
   middleware picks it up.

The middleware itself is added to the request pipeline by `UseRestierOpenApi()`, and the NSwag UI
hosts are configured with the matching URLs by `UseRestierReDoc()` and `UseRestierNSwagUI()`.
</Note>

## NSwag vs. Swagger (Swashbuckle)

Pick **NSwag** if you want NSwagStudio, NSwag.MSBuild client codegen, ReDoc + Swagger UI 3 from a
single package, or you need to serve Restier alongside plain ASP.NET Core controllers in one
application's OpenAPI surface.

Pick **[Swagger (Swashbuckle)](swagger)** if you have an existing investment in Swashbuckle filters
or your team already uses the Swashbuckle ecosystem.

NSwag's in-process `IDocumentProcessor` / `IOperationProcessor` pipeline applies to your plain
controllers document (because that one is registered with NSwag), but **not** to Restier-generated
documents. Restier OpenAPI documents are served by RESTier's own middleware and customized via the
`Action<OpenApiConvertSettings>` callback on `AddRestierNSwag()`.
````

- [ ] **Step 3: Build the docsproj to verify the page compiles**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: `Build succeeded` with no MDX/Mintlify warnings.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/nswag.mdx
git commit -m "$(cat <<'EOF'
docs: write guides/server/nswag.mdx

Recommended OpenAPI page covering AddRestierNSwag, UseRestierOpenApi /
UseRestierReDoc / UseRestierNSwagUI, the OpenApiConvertSettings
configurator, multi-route discovery, and the combined-with-plain-MVC
scenario.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 20: Reframe `guides/server/swagger.mdx` as the alternative

**Files:**
- Modify: `src/Microsoft.Restier.Docs/guides/server/swagger.mdx`

- [ ] **Step 1: Read the existing page**

```bash
cat src/Microsoft.Restier.Docs/guides/server/swagger.mdx
```

- [ ] **Step 2: Insert a `<Note>` callout immediately after the lead paragraph**

Find the line `RESTier can automatically generate an [OpenAPI](https://www.openapis.org/) (formerly Swagger) document from`. After the closing line of that paragraph (the line that mentions Swashbuckle and the colon-period), insert this `<Note>`:

```mdx
<Note>For new projects we recommend the [NSwag integration](nswag). NSwag supports ReDoc, NSwagStudio
client-code generation, and combining Restier with plain ASP.NET Core controllers in the same
application's OpenAPI surface. Both packages remain supported.</Note>
```

Do not change the existing Contributors table, link references at the bottom, or the body of the page.

- [ ] **Step 3: Build the docsproj to verify**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/swagger.mdx
git commit -m "$(cat <<'EOF'
docs: link Swagger page to NSwag as the recommended alternative

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 21: Add the NSwag page to the nav, ahead of Swagger

**Files:**
- Modify: `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`

- [ ] **Step 1: Locate the Server group in the MintlifyTemplate**

```bash
grep -n "guides/server/swagger" src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

You should see one occurrence inside the `<Group Name="Server" ...>` `<Pages>` block.

- [ ] **Step 2: Insert `guides/server/nswag` immediately before `guides/server/swagger`**

The current Pages list ends like:

```
guides/server/swagger;
guides/server/testing;
```

Change it to:

```
guides/server/nswag;
guides/server/swagger;
guides/server/testing;
```

- [ ] **Step 3: Build the docsproj — `docs.json` will regenerate**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: `Build succeeded`. `src/Microsoft.Restier.Docs/docs.json` should now have changes reflecting the new nav entry.

- [ ] **Step 4: Commit both the docsproj and the regenerated `docs.json`**

```bash
git add src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj src/Microsoft.Restier.Docs/docs.json
git commit -m "$(cat <<'EOF'
docs: list NSwag ahead of Swagger in Server guide nav

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 22: Write the package README

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore.NSwag/README.md`

- [ ] **Step 1: Read the Swagger package README for style reference**

```bash
cat src/Microsoft.Restier.AspNetCore.Swagger/README.md
```

Note the structure (header, package badges, Supported Platforms, Getting Started, Reporting Security Issues, Contributors, link references) and the Contributors table — preserve the same shape and credit existing contributors who worked on this package.

- [ ] **Step 2: Write the README**

Write `src/Microsoft.Restier.AspNetCore.NSwag/README.md`:

```markdown
# Microsoft Restier - OData Made Simple

[Releases](https://github.com/OData/RESTier/releases)&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;Documentation&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;[OData v4.01 Documentation](https://www.odata.org/documentation/)

## NSwag for Restier ASP.NET Core

This package helps you quickly implement OpenAPI with [NSwag](https://github.com/RicoSuter/NSwag),
[ReDoc](https://redocly.com/redoc), and [Swagger UI 3](https://swagger.io/tools/swagger-ui/) in your
Restier service in just a few lines of code. It is the recommended OpenAPI integration for new
projects.

For the Swashbuckle-based alternative, see
[Microsoft.Restier.AspNetCore.Swagger](https://www.nuget.org/packages/Microsoft.Restier.AspNetCore.Swagger).

## Supported Platforms

ASP.NET Core 8.0, 9.0, and 10.0 via Endpoint Routing.

## Getting Started

### Step 1: Install [Microsoft.Restier.AspNetCore.NSwag](https://www.nuget.org/packages/Microsoft.Restier.AspNetCore.NSwag)

Add the package to your API project.

### Step 2: Register services

```csharp
builder.Services.AddRestierNSwag();
```

There is an overload that takes an `Action<OpenApiConvertSettings>` for customizing the generated
OpenAPI document.

### Step 3: Add middleware

```csharp
app.UseRestierOpenApi();   // /openapi/{name}/openapi.json
app.UseRestierReDoc();     // /redoc/{name}
app.UseRestierNSwagUI();   // /swagger (Swagger UI 3 with a route dropdown)
```

### Step 4: Combine with plain MVC controllers (optional)

```csharp
builder.Services.AddOpenApiDocument(c => c.DocumentName = "controllers");

// in the pipeline:
app.UseOpenApi();
app.UseReDoc(c =>
{
    c.Path = "/redoc/controllers";
    c.DocumentPath = "/swagger/controllers/swagger.json";
});
```

`AddRestierNSwag()` hides `RestierController` from ApiExplorer automatically, so it will not appear
in your controllers document.

### Step 5: Browse

- `/redoc/{routeName}` — ReDoc for one Restier route
- `/swagger` — Swagger UI 3 with all Restier routes
- `/openapi/{routeName}/openapi.json` — Raw OpenAPI 3.0 JSON

## Reporting Security Issues

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response
Center (MSRC) <secure@microsoft.com>. You should receive a response within 24 hours. If for some
reason you do not, please follow up via email to ensure we received your original message. Further
information, including the MSRC PGP key, can be found in the
[Security TechCenter](https://www.microsoft.com/msrc/faqs-report-an-issue). You can also find these
instructions in this repo's [SECURITY.md](https://github.com/OData/RESTier/blob/main/SECURITY.md).

## Contributors

Special thanks to everyone involved in making Restier the best API development platform for .NET.
The following people have made various contributions to this package:

| External         |
|------------------|
| Jan-Willem Spuij |
```

- [ ] **Step 3: Verify the package picks up the README via `IncludeReadmeFile`**

The repo-wide `Directory.Build.props` enables `<IncludeReadmeFile>` automatically when a `readme.md`
exists. Note that the property check is case-sensitive on Linux/macOS but not on Windows. The
existing Swagger package also uses `README.md` (uppercase), so that name is fine. Build the package
to confirm:

```bash
dotnet pack src/Microsoft.Restier.AspNetCore.NSwag/Microsoft.Restier.AspNetCore.NSwag.csproj
```

Expected: `Successfully created package`. If `dotnet pack` warns the README is not being picked up,
rename to `readme.md` (lowercase) to match `Directory.Build.props` exactly.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore.NSwag/README.md
git commit -m "$(cat <<'EOF'
docs: add README for Microsoft.Restier.AspNetCore.NSwag

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 9 — Final verification

### Task 23: Full solution build and full test pass

- [ ] **Step 1: Clean build**

```bash
dotnet build RESTier.slnx
```

Expected: `Build succeeded`. With `TreatWarningsAsErrors` enabled, any warning fails the build — fix at the source. Do not suppress warnings to silence them.

- [ ] **Step 2: Full solution test**

```bash
dotnet test RESTier.slnx
```

Expected: every test project passes on every TFM. The new NSwag tests are part of this.

- [ ] **Step 3: Verify the docs build is still green**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: `Build succeeded`. `docs.json` should already be committed from Task 21; if a build now produces additional changes (e.g., regenerated api-reference MDX is gitignored — that is expected), don't commit them.

- [ ] **Step 4: Sanity-check the commit history is clean**

```bash
git log --oneline feature/vnext ^origin/feature/vnext
```

Expected: roughly 18–22 commits (one per task; some tasks have no code change). All commits should follow the project's lowercase-prefix convention and each include the `Co-Authored-By` trailer.

- [ ] **Step 5: No commit needed — this is verification only.**

---

## Out of scope (do not do these unless asked)

- Removing the `Microsoft.Restier.AspNetCore.Swagger` package.
- Backfilling test coverage on `Microsoft.Restier.Tests.AspNetCore.Swagger`.
- Making the URL paths configurable (`/openapi/`, `/redoc/`, `/swagger`).
- NSwagStudio profile templates or NSwag.MSBuild integration.
- Release notes for the shipping version (handled at release time, not in this PR).
