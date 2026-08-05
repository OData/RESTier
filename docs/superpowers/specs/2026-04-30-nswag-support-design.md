# Design: Add NSwag + ReDoc support to Restier

**Date:** 2026-04-30
**Status:** Revised after code review (pending user review)
**Branch:** `feature/vnext`

## Goal

Add a new `Microsoft.Restier.AspNetCore.NSwag` package that integrates Restier with [NSwag](https://github.com/RicoSuter/NSwag) and [ReDoc](https://redocly.com/redoc), making NSwag the recommended OpenAPI surface for Restier. The existing `Microsoft.Restier.AspNetCore.Swagger` package continues to ship unchanged as a Swashbuckle-based alternative.

The design also introduces a "combined Restier + plain ASP.NET Core controller" scenario in the Northwind sample, demonstrated as two separate OpenAPI documents: one for the Restier route, one for plain MVC controllers.

## Context

Today Restier ships `Microsoft.Restier.AspNetCore.Swagger`, which:

- Generates OpenAPI from the EDM model via `Microsoft.OpenApi.OData`.
- Serves JSON at `/swagger/{name}/swagger.json` via custom middleware (`RestierOpenApiMiddleware`).
- Hosts Swashbuckle's Swagger UI at `/swagger`.
- Works because no MVC controllers are scanned at all — `RestierController` therefore never leaks into any document.

NSwag is a widely-used alternative with stronger tooling: code generation (NSwag.MSBuild, NSwagStudio), ReDoc and Swagger UI 3 hosts, and a richer extensibility model (`IDocumentProcessor`, `IOperationProcessor`). NSwag also discovers ASP.NET Core MVC controllers via ApiExplorer — which means combining a Restier route with plain controllers in one application is supported by NSwag in a way Swashbuckle is not (in our integration).

NSwag uses its own `NSwag.OpenApiDocument` type, separate from `Microsoft.OpenApi.OpenApiDocument`. The two are not interchangeable in-process — but NSwag's UI hosts (`UseSwaggerUi3`, `UseReDoc`) consume OpenAPI by URL, not by in-process object reference. This integration takes that as the integration point: Restier serves OpenAPI JSON over HTTP from its own middleware, and NSwag UIs render it.

## Scope decisions

| Decision | Choice | Reason |
|---|---|---|
| Q1: Integration depth | **Hybrid** — Restier JSON via custom middleware; NSwag for UI hosts and the plain controllers doc | Avoids dependency on an unverified NSwag named-document hook; preserves a clean URL contract; NSwag's tooling (NSwagStudio, NSwag.MSBuild) consumes Restier docs by URL regardless of registry membership. |
| Q2: Default UI | **ReDoc** | User preference; cleaner reading experience. Swagger UI 3 also wired up via NSwag for try-it-out. |
| Q3: Existing Swagger package | **Keep, position as alternative** | Backwards-compatible; users with Swashbuckle investment unaffected. |
| Q4: Document topology | **One doc per Restier route + a separate "controllers" doc** | Cleanest separation, no duplicated controllers across docs, matches today's per-route Swagger behaviour. |
| Q5: API naming | **NSwag-branded with separate `Use*` methods** | Explicit control over which UIs are enabled. JSON path is `/openapi/...` because Restier docs are served by our middleware, not NSwag's `UseOpenApi`, so there is a real path-level separation (not just an alias). |
| Q6: EDM → NSwag bridge | **None — JSON over HTTP is the integration point** | NSwag's UI hosts (`UseSwaggerUi3`, `UseReDoc`) load OpenAPI by URL, so we never need an `NSwag.OpenApiDocument` instance. `RestierOpenApiMiddleware` serializes `Microsoft.OpenApi.OpenApiDocument` to JSON and writes it; the UIs fetch it. No in-process type bridge required. |
| Q7: `RestierController` filtering | **Automatic via MVC `IApplicationModelConvention`** | Sets `ActionModel.ApiExplorer.IsVisible = false` (equivalent to `[ApiExplorerSettings(IgnoreApi = true)]`) globally so NSwag, Swashbuckle, and .NET 9 OpenAPI all skip it without per-document config. End-to-end test required because `RestierController` reaches the request via `MapDynamicControllerRoute`, not attribute routing — see Testing. |
| Q8: Sample changes | **Northwind = combined scenario; Postgres = minimal NSwag** | Northwind already had Swagger and is the more fleshed-out sample; Postgres stays lean. |
| Q9: Doc nav order | **NSwag first, Swagger second** | Reflects new "recommended path" positioning. |
| Q10: TFMs | `net8.0;net9.0;net10.0` | Same as Swagger package and rest of suite. |

### What "full NSwag integration" means here (and doesn't)

Restier docs are *not* registered in NSwag's `IOpenApiDocumentGenerator` registry. Two consequences:

- **Works:** NSwag UI 3 + ReDoc rendering of Restier docs, NSwagStudio "load from URL", NSwag.MSBuild client codegen against the Restier doc URL. All of these consume OpenAPI by URL and don't care about registry membership.
- **Does not work:** NSwag's in-process `IDocumentProcessor` / `IOperationProcessor` pipeline applied to Restier docs. Users mutate Restier docs through the existing `Action<OpenApiConvertSettings>` callback on `AddRestierNSwag()`. NSwag's processor pipeline still applies to the user's plain controllers doc, which is registered with NSwag normally.

This trade-off exists because putting Restier docs in NSwag's registry would expose them at NSwag's default `/swagger/{name}/swagger.json` path (in addition to our `/openapi/...` path) any time a user calls `app.UseOpenApi()` for their controllers doc, undermining the URL contract. We pick the cleaner URL contract over the in-process processor pipeline.

## Solution layout

### New project — `src/Microsoft.Restier.AspNetCore.NSwag/`

Sibling of `src/Microsoft.Restier.AspNetCore.Swagger/`, same csproj shape.

```
Microsoft.Restier.AspNetCore.NSwag.csproj         # net8.0;net9.0;net10.0
RestierOpenApiDocumentGenerator.cs                # internal — calls Microsoft.OpenApi.OData
RestierOpenApiMiddleware.cs                       # internal — serves Restier OpenAPI JSON
RestierControllerApiExplorerConvention.cs         # internal — IApplicationModelConvention
Extensions/
  IServiceCollectionExtensions.cs                 # AddRestierNSwag
  IApplicationBuilderExtensions.cs                # UseRestierOpenApi / UseRestierReDoc / UseRestierNSwagUI
README.md
```

**Dependencies:**
- `Microsoft.OpenApi.OData` (`[3.*, 4.0.0)`) — same constraint as the Swagger package.
- `NSwag.AspNetCore` 14.x.
- ProjectReference to `Microsoft.Restier.AspNetCore`.

**Key csproj properties** (mirroring Swagger): `<TargetFrameworks>net8.0;net9.0;net10.0;</TargetFrameworks>`, `<StrongNamePublicKey>$(StrongNamePublicKey)</StrongNamePublicKey>`, `<DocumentationFile>$(DocumentationFile)\$(AssemblyName).xml</DocumentationFile>`.

### New test project — `test/Microsoft.Restier.Tests.AspNetCore.NSwag/`

Mirrors `Microsoft.Restier.Tests.AspNetCore.Swagger` structurally; broader coverage (see Testing).

### Solution wiring — `RESTier.slnx`

Source project under `/src/Web/`, test project under `/test/Web/`, alongside their Swagger counterparts.

### Docs project — `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`

- Add `<ProjectReference>` and matching `<_DocsSourceProject>` for the new package so the SDK builds it for `net8.0` and emits API-reference MDX.
- Update `<MintlifyTemplate>` Server group page list:
  ```
  guides/server/swagger;
  ```
  becomes:
  ```
  guides/server/nswag;
  guides/server/swagger;
  ```
- `docs.json` is regenerated by the SDK on build and committed.

## Document generation pipeline

### Per-route Restier docs (custom middleware, like the existing Swagger package)

Restier docs are served by our own middleware. They are **not** registered in NSwag's `IOpenApiDocumentGenerator` registry. NSwag's UI hosts consume them by URL.

1. **`RestierOpenApiDocumentGenerator` (internal, shared)** — same shape as the existing one in the Swagger project.
   - Looks up `ODataOptions.RouteComponents[routePrefix]` to get the `IEdmModel`.
   - Sets `OpenApiConvertSettings.TopExample` from `ODataValidationSettings.MaxTop` (default 5).
   - Sets `OpenApiConvertSettings.ServiceRoot` from `request.Scheme/Host/PathBase/prefix`.
   - Invokes user's `Action<OpenApiConvertSettings>` configurator (overrides defaults).
   - Returns `model.ConvertToOpenApi(settings)` — a `Microsoft.OpenApi.OpenApiDocument`.

2. **`RestierOpenApiMiddleware` (internal)** — matches `/openapi/{documentName}/openapi.json`.
   - Maps `documentName` to a Restier route prefix (`"default"` → `""`, otherwise the prefix verbatim).
   - Calls `RestierOpenApiDocumentGenerator.GenerateDocument(...)`.
   - Returns 200 with `application/json; charset=utf-8` and the JSON serialized via `SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0)`.
   - Returns 404 if the document name does not map to a registered Restier route.
   - Per-request regeneration — `ServiceRoot` depends on the inbound request.

This mirrors `RestierOpenApiMiddleware` in `Microsoft.Restier.AspNetCore.Swagger` almost line for line; the only differences are the URL prefix (`/openapi/...` vs `/swagger/...`) and the file extension on the document name (`openapi.json` vs `swagger.json`).

### NSwag UI hosts pointed at Restier middleware URLs

The `Use*` extensions configure NSwag's UI middleware (`UseSwaggerUi3`, `UseReDoc`) with explicit URLs that point at the Restier middleware:

- `UseRestierReDoc()` enumerates `ODataOptions.GetRestierRoutePrefixes()`. For each prefix, it calls `app.UseReDoc(c => { c.Path = $"/redoc/{name}"; c.DocumentPath = $"/openapi/{name}/openapi.json"; })`. ReDoc renders one page per Restier doc, loading the JSON from our middleware.
- `UseRestierNSwagUI()` registers a single `UseSwaggerUi3` instance at `/swagger` with `SwaggerRoutes` populated from the same enumeration — one entry per Restier route, each pointing at the corresponding `/openapi/{name}/openapi.json`. Users see a dropdown listing every Restier route.

Because Restier docs aren't in NSwag's registry, the user's `app.UseOpenApi()` (for their controllers doc) cannot accidentally serve them.

### Plain MVC controllers doc

Standard NSwag setup — **the user registers it themselves** in `Program.cs`:

```csharp
services.AddOpenApiDocument(c => c.DocumentName = "controllers");
// in pipeline:
app.UseOpenApi();
app.UseReDoc(c => { c.DocumentPath = "/swagger/controllers/swagger.json"; c.Path = "/redoc/controllers"; });
```

Our package does not register or modify this document. The user retains full control over MVC scanning settings, processors, and the URL paths of the controllers doc. NSwag's full processor pipeline (`IDocumentProcessor`, `IOperationProcessor`) applies to this doc.

### Auto-filtering `RestierController`

`AddRestierNSwag()` registers `RestierControllerApiExplorerConvention : IApplicationModelConvention` via `services.Configure<MvcOptions>(o => o.Conventions.Add(...))`. The convention sets `ActionModel.ApiExplorer.IsVisible = false` (equivalent to `[ApiExplorerSettings(IgnoreApi = true)]`) on every action whose controller type is `RestierController` or a subclass.

This is global, ApiExplorer-level — NSwag, Swashbuckle, and .NET 9 OpenAPI all use ApiExplorer for MVC scanning, so the filter applies regardless of which generator the user picks for their plain-controllers doc. No per-document config required.

**Caveat for the dynamic-routing case:** `RestierController` is reached at runtime via `MapDynamicControllerRoute`, not attribute routing. The unit test on the convention proves the `ApplicationModel` shape is correct, but the load-bearing assertion lives in the integration tests — see Testing.

## Public API surface

### Service registration (`Microsoft.Extensions.DependencyInjection`)

```csharp
public static class Restier_AspNetCore_NSwag_IServiceCollectionExtensions
{
    public static IServiceCollection AddRestierNSwag(
        this IServiceCollection services,
        Action<OpenApiConvertSettings> openApiSettings = null);
}
```

Registers:
- `IHttpContextAccessor`.
- The `OpenApiConvertSettings` configurator (if non-null), as a singleton.
- `RestierControllerApiExplorerConvention` via `services.Configure<MvcOptions>(o => o.Conventions.Add(...))`.

(No NSwag named-document registrations; Restier docs live in our middleware, not NSwag's registry.)

### Pipeline (`Microsoft.AspNetCore.Builder`)

```csharp
public static class Restier_AspNetCore_NSwag_IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRestierOpenApi(this IApplicationBuilder app);   // /openapi/{name}/openapi.json
    public static IApplicationBuilder UseRestierReDoc(this IApplicationBuilder app);     // /redoc/{name}
    public static IApplicationBuilder UseRestierNSwagUI(this IApplicationBuilder app);   // /swagger (NSwag UI 3)
}
```

Each method is independent — users call any combination.

- `UseRestierOpenApi` registers `RestierOpenApiMiddleware` once. The middleware dispatches `/openapi/{documentName}/openapi.json` requests by looking the `documentName` up against `ODataOptions.GetRestierRoutePrefixes()`.
- `UseRestierReDoc` enumerates `ODataOptions.GetRestierRoutePrefixes()` and calls NSwag's `app.UseReDoc(...)` once per Restier route, configuring `Path = "/redoc/{name}"` and `DocumentPath = "/openapi/{name}/openapi.json"`.
- `UseRestierNSwagUI` calls NSwag's `app.UseSwaggerUi3(...)` once with `Path = "/swagger"` and a `SwaggerRoutes` collection — one route per Restier doc, each pointing at `/openapi/{name}/openapi.json`.

### URL contract

| Path | Source | Content |
|---|---|---|
| `/openapi/default/openapi.json` | `RestierOpenApiMiddleware`, Restier route `""` | EDM-derived OpenAPI 3.0 |
| `/openapi/{prefix}/openapi.json` | `RestierOpenApiMiddleware`, Restier route `{prefix}` | EDM-derived OpenAPI 3.0 |
| `/redoc/default` | NSwag `UseReDoc`, points at `/openapi/default/openapi.json` | ReDoc page |
| `/redoc/{prefix}` | NSwag `UseReDoc`, points at `/openapi/{prefix}/openapi.json` | ReDoc page |
| `/swagger` | NSwag `UseSwaggerUi3` with `SwaggerRoutes` | Dropdown of all Restier docs |
| `/swagger/controllers/swagger.json` | User's `AddOpenApiDocument("controllers")` + `UseOpenApi()` | Plain MVC controllers (NSwag default path) |
| `/redoc/controllers` | User's `UseReDoc(...)` | ReDoc for the controllers doc |

Restier docs are not in NSwag's registry, so the user's `app.UseOpenApi()` only serves their controllers doc — Restier docs are not exposed at NSwag's default `/swagger/{name}/swagger.json` path.

### Path-collision handling

Referencing both `Microsoft.Restier.AspNetCore.Swagger` and `Microsoft.Restier.AspNetCore.NSwag` in the same app is **not supported** — the docs page states this explicitly. The Swagger package's `/swagger/{name}/swagger.json` middleware and NSwag's `/swagger` UI host both want `/swagger/...`; users pick one package.

### Configuration scope (intentionally narrow)

URL paths are not configurable in v1. Users who need different paths fall back to NSwag's lower-level APIs and re-register documents themselves. Keeps the package surface small.

## Sample changes

### `Microsoft.Restier.Samples.Northwind.AspNetCore` — combined Restier + MVC

- Replace package reference: `Microsoft.Restier.AspNetCore.Swagger` → `Microsoft.Restier.AspNetCore.NSwag`.
- `Startup.cs`:
  - `services.AddRestierSwagger()` → `services.AddRestierNSwag()`.
  - Add `services.AddOpenApiDocument(c => c.DocumentName = "controllers")`.
  - Replace `app.UseRestierSwaggerUI()` with `app.UseRestierOpenApi()` + `app.UseRestierReDoc()` + `app.UseRestierNSwagUI()`.
  - Add `app.UseOpenApi()` and `app.UseReDoc(...)` for the controllers doc.
- New `Controllers/HealthController.cs` — `[ApiController]` with `GET /health/live` and `GET /health/version`. Trivial; no DB access.
- Manual browser verification (per CLAUDE.md UI-changes gate):
  - `/redoc/default` shows Northwind OData entity sets, no `HealthController` entries.
  - `/redoc/controllers` shows only `HealthController`.
  - `/swagger` shows NSwag UI 3 with the Restier route(s) in the dropdown.

### `Microsoft.Restier.Samples.Postgres.AspNetCore` — minimal NSwag

- Add package reference: `Microsoft.Restier.AspNetCore.NSwag`.
- `Program.cs`: add `services.AddRestierNSwag()` after `AddRestier(...)`; add `app.UseRestierOpenApi()` and `app.UseRestierReDoc()` after `UseEndpoints`. Skip `UseRestierNSwagUI()` to keep the minimal sample minimal.
- No MVC controllers added.
- Manual browser verification: `/redoc/v3` shows Postgres OData API.

## Documentation changes

### New page — `src/Microsoft.Restier.Docs/guides/server/nswag.mdx`

Modeled on `swagger.mdx`. Sections:

- **Intro** — recommended OpenAPI path; cross-link to Swagger page as the alternative.
- **Setup** — install + four lines (`AddRestierNSwag` + the three `Use*`); complete `Program.cs` example.
- **Usage / endpoints table** — JSON, ReDoc, Swagger UI 3 paths.
- **Configuration** — `Action<OpenApiConvertSettings>` (same `OpenApiConvertSettings` from `swagger.mdx`).
- **Multiple Restier APIs** — analogous to swagger.mdx's section; one Restier doc per route prefix served by `RestierOpenApiMiddleware`.
- **Combining with plain MVC controllers** — unique to NSwag. Walkthrough of `AddOpenApiDocument("controllers")` + `UseOpenApi()` + `UseReDoc(...)`. Notes the auto-filter on `RestierController`. Cross-references the Northwind sample.
- **What `AddRestierNSwag()` does for you** — `<Note>` listing: (1) the MVC `IApplicationModelConvention` that hides `RestierController` from ApiExplorer; (2) `RestierOpenApiMiddleware` registration via `UseRestierOpenApi`; (3) NSwag UI host wiring with explicit Restier URLs via `UseRestierReDoc` / `UseRestierNSwagUI`.
- **Picking between NSwag and Swagger** — short closing section. Honest framing: NSwag is recommended for users who want NSwagStudio / NSwag.MSBuild / ReDoc / Swagger UI 3 from one package, or who need to combine Restier with plain MVC controllers in a single application. NSwag's in-process processor pipeline (`IDocumentProcessor`, `IOperationProcessor`) applies to the user's controllers doc but **not** to Restier docs — Restier docs are mutated through the `Action<OpenApiConvertSettings>` callback on `AddRestierNSwag()` instead.

### Updated page — `src/Microsoft.Restier.Docs/guides/server/swagger.mdx`

- Title unchanged ("OpenAPI / Swagger Support").
- Description and lead paragraph reframe as "Swashbuckle-based alternative to NSwag."
- New `<Note>` at top: "For new projects we recommend [the NSwag integration](nswag) — it supports ReDoc, NSwagStudio, and combining Restier routes with plain ASP.NET Core controllers in the same OpenAPI document. Both packages remain supported."
- Existing Contributors table, link references, attribution **preserved exactly** (per the credits-keeping convention).

### Package README — `src/Microsoft.Restier.AspNetCore.NSwag/README.md`

Modeled on the Swagger package README. Same Contributors / link-refs sections. NSwag-specific install / wire-up steps.

### API reference

Auto-generated by the DotNetDocs SDK. No hand-editing under `api-reference/`.

### Release notes

A new entry in `release-notes/` for the version that ships this — handled at release time, not in this design.

## Testing strategy

Test project: `test/Microsoft.Restier.Tests.AspNetCore.NSwag/` (xUnit v3, FluentAssertions / AwesomeAssertions, NSubstitute per project conventions).

### Unit tests

**`Extensions/IServiceCollectionExtensionsTests.cs`** (mirrors Swagger version, expanded):
- `AddRestierNSwag_NoSettingsAction` — services registered as expected.
- `AddRestierNSwag_SettingsAction` — settings configurator captured.
- `AddRestierNSwag_RegistersApiExplorerConvention` — convention added to `MvcOptions.Conventions`.

**`Extensions/IApplicationBuilderExtensionsTests.cs`** (mirrors Swagger version, replaces the existing empty file):
- `UseRestierOpenApi_RegistersOnePathPerRoutePrefix` — `TestServer` with two Restier routes (`""` and `"v3"`); assert `GET /openapi/default/openapi.json` and `GET /openapi/v3/openapi.json` return 200 with valid OpenAPI 3.0 JSON; `GET /openapi/nonexistent/openapi.json` returns 404 (not 500).
- `UseRestierOpenApi_HonorsServiceRootFromRequest` — call with `Host: example.com:8443` and a non-empty `PathBase`; assert the document's `servers[0].url` reflects scheme/host/pathBase/prefix.
- `UseRestierReDoc_PointsAtRestierDocument` — `GET /redoc/default` returns HTML that references `/openapi/default/openapi.json` as the document URL (via the embedded ReDoc config).
- `UseRestierNSwagUI_ListsAllRestierRoutes` — `GET /swagger` returns Swagger UI 3 HTML/config containing `urls` entries for each Restier doc URL (e.g., `/openapi/default/openapi.json`).

**`RestierOpenApiDocumentGeneratorTests.cs`** (new):
- Generates from a small EDM model via `Microsoft.Restier.Breakdance`.
- Route prefix → document name mapping (`""` → `"default"`, `"v3"` → `"v3"`).
- `TopExample` defaults to `ODataValidationSettings.MaxTop`, overridable via the `Action<OpenApiConvertSettings>` callback.
- `ServiceRoot` built from request scheme/host/pathBase/prefix.

**`ApiExplorerConventionTests.cs`** (new — fast smoke test):
- Build an `ApplicationModel` containing `RestierController` plus a sibling plain controller; run the convention; assert `ApiExplorer.IsVisible = false` only on `RestierController` actions and stays unchanged on the plain controller.

### Integration tests — `IntegrationTests/EndToEndTests.cs`

The load-bearing tests. `TestServer` with `MapRestier()` + `MapControllers()` (one plain MVC controller) + `services.AddOpenApiDocument("controllers")`:

- `GET /openapi/default/openapi.json` → 200, valid OpenAPI 3.0 JSON; contains Restier entity-set paths; does **not** contain the plain controller's path.
- `GET /swagger/controllers/swagger.json` (user-registered NSwag plain doc) → 200; **contains the plain controller's path**; **contains zero operations referencing `RestierController` actions**. This is the live proof that the `IApplicationModelConvention` filters dynamic-routed `RestierController` actions out of NSwag's MVC scan, not just the in-memory `ApplicationModel`.
- `GET /openapi/nonexistent/openapi.json` → 404.
- `GET /redoc/default` → 200; HTML references `/openapi/default/openapi.json`.
- `GET /swagger` → 200; NSwag UI 3 config lists the Restier doc URLs.
- `GET /swagger/default/swagger.json` → 404 (proves Restier docs are not in NSwag's registry, only at our `/openapi/...` paths).

### Out of scope

- **Snapshot tests of generated OpenAPI JSON.** Too brittle — drifts with `Microsoft.OpenApi.OData` and NSwag versions. Asserting structural invariants instead.
- **NSwag UI rendering tests.** NSwag's responsibility.
- **Backfilling test coverage on `Microsoft.Restier.Tests.AspNetCore.Swagger`.** That project has only two registration tests today and does not cover the document generator or middleware. Out of scope for this design — touching it expands the work beyond "add NSwag." Future task.

### `Microsoft.Restier.Tests.AspNetCore.NSwag.csproj`

- TFMs `net8.0;net9.0;net10.0`.
- ProjectReference to `Microsoft.Restier.AspNetCore.NSwag`, `Microsoft.Restier.Breakdance`, `Microsoft.Restier.Tests.Shared`.

## Verification gates

Per the project's CLAUDE.md:

- **Build:** `dotnet build RESTier.slnx` succeeds (warnings-as-errors enabled).
- **Tests:** `dotnet test RESTier.slnx` passes, including the new NSwag test project on all three TFMs.
- **Docs build:** `dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` succeeds; `docs.json` regenerates and is committed alongside nav changes.
- **UI verification (manual):** Northwind and Postgres samples each launched in the browser; the URLs listed in the sample sections render the expected content. Required because these are UI-affecting changes.

## Risks & open questions for implementation

- **NSwag UI 3 + user controllers doc dropdown.** `UseRestierNSwagUI()` registers Swagger UI 3 at `/swagger` with explicit `SwaggerRoutes` for Restier docs only. If the user *also* calls `app.UseSwaggerUi3()` for their controllers doc, both `UseSwaggerUi3` calls compete for the `/swagger` path. Implementation chooses one of: (a) `UseRestierNSwagUI` accepts an optional list of additional `SwaggerRoute` entries so users can include their controllers doc in the same dropdown; (b) the docs page tells users to mount their controllers UI at a different path (e.g., `/swagger-controllers`). Decision deferred to implementation, but the docs page must show whichever pattern is shipped.
- **`net10.0` NSwag support.** NSwag 14.x's TFM coverage for `net10.0` to be verified before merging; if a gap exists, document the constraint and target only `net8.0;net9.0` for the v1 ship (Swagger package and rest of suite stay on `net8.0;net9.0;net10.0`).

## Out of scope

- Removing the `Microsoft.Restier.AspNetCore.Swagger` package.
- Backfilling the existing Swagger test project's coverage.
- Configuring URL paths for the NSwag JSON / ReDoc / UI endpoints.
- Code-generation tooling (NSwagStudio profile templates, NSwag.MSBuild integration). Users can configure those themselves once the OpenAPI document is served.
- Release notes content for the shipping version.
