# Multi-Tenancy with RESTier — Design Spec

**Date:** 2026-05-05
**Status:** Draft

## Goal

Document — and prove with an integration test — how to serve multiple tenants from a single Restier API by resolving the tenant from the URL in ASP.NET Core middleware and selecting a per-tenant connection string at `DbContext` resolution time. The pattern requires **no changes to the RESTier framework**: it is built entirely on top of Restier's existing per-route scoped DI and on `Microsoft.EntityFrameworkCore`'s standard runtime `DbContextOptions` configuration.

The deliverables are a guide page in `Microsoft.Restier.Docs` and a scenario integration test that demonstrates end-to-end isolation.

## Non-goals

- **Schema-per-tenant.** One `ApiBase` subclass means one EDM is built per route at startup; tenant-specific schema differences cannot be expressed in this pattern. Users who need that register a separate API per tenant (or use the per-tenant-deployment topology, documented as an alternative).
- **DbContext pooling.** `AddDbContextPool<T>` keys options at startup and won't pick up a per-request connection string. Out of scope; called out as a limitation.
- **Tenant onboarding / provisioning.** Creating a tenant's database and writing its connection string to the configuration source is a deployment concern, not a library concern.
- **Authorization by tenant.** Verifying that the authenticated principal is allowed to act on the resolved tenant is application-specific; the doc page mentions the boundary but does not prescribe an implementation.
- **Changes to `Microsoft.Restier.Core`, `Microsoft.Restier.AspNetCore`, or any other shipped package.** This is a recipe + test + docs, not a framework feature.

## Background

### What RESTier already provides

- **Per-route scoped DI.** `AddRestierRoute<TApi>(prefix, configureRouteServices)` registers an OData route at `prefix` and a DI container scoped to that route (`src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs`). For each request the controller resolves `ApiBase` and any registered `DbContext` from the per-route container in the request scope.
- **Runtime `DbContextOptions` configuration.** EF Core's `AddDbContext<TContext>((sp, opt) => ...)` runs the options-builder lambda on every resolution, with access to the request-scoped `IServiceProvider`. This is the seam used to pick a connection string per request without modifying RESTier.
- **`PathBase`-aware URL generation.** RESTier composes `@odata.context`, `@odata.id`, and entity-link URLs from `Request.PathBase` plus the registered route prefix, the same as ASP.NET Core's standard URL helpers. The path-segment tenant-resolution flavor relies on this — middleware moves the stripped tenant segment into `PathBase` so generated URLs remain valid for OData clients.

### Why this works

The whole pattern reduces to:

1. Middleware reads tenant id from the URL → writes it to a request-scoped `ITenantContext`.
2. `DbContext` options factory reads `ITenantContext` → picks a connection string via `IConnectionStringProvider` → configures the provider (SQL Server, Postgres, etc.).

Both seams are stock ASP.NET Core / EF Core extension points. RESTier sits in between but does not need to know that tenancy is happening.

## Architecture

```
HTTP request
    │
    ▼
ASP.NET Core pipeline
    UseMiddleware<TenantResolutionMiddleware>   ◀── runs BEFORE UseRouting
        ├── extracts tenant from URL
        ├── validates via IConnectionStringProvider.TryGetConnectionString
        ├── writes app-scoped ITenantContext.TenantId
        └── (path-segment flavor only) moves stripped segment to Request.PathBase
    UseRouting                                  ◀── now matches `odata/{**odataPath}`
    UseEndpoints
        │
        ▼
RestierController (per-route scoped DI, resolved via Request.GetRouteServices())
    │
    ▼
TenantDbContext  ◀── AddDbContext factory lambda
                       │
                       ├── sp.GetRequiredService<IHttpContextAccessor>().HttpContext        (route scope → bridge)
                       ├── http.RequestServices.GetRequiredService<ITenantContext>()         (app scope)
                       ├── http.RequestServices.GetRequiredService<IConnectionStringProvider>() (app scope)
                       └── configures DbContextOptions (UseSqlServer, ...)
```

The bridge is `IHttpContextAccessor`. Both `ITenantContext` (scoped) and `IConnectionStringProvider` (singleton) are registered in the **app** service collection and reached from the route-scope lambda via `IHttpContextAccessor.HttpContext.RequestServices`. The route container itself only registers `IHttpContextAccessor` — the minimum needed to enter the bridge. OData's per-route container does not fall back to app DI for general service resolution, so without the `HttpContext.RequestServices` hop a service registered in app DI would be invisible to anything resolved through `Request.GetRouteServices()`.

### Components (all in user code; no RESTier changes)

| Component | Container | Lifetime | Responsibility |
|---|---|---|---|
| `IHttpContextAccessor` | app + route | singleton | Standard ASP.NET Core accessor. Registered in the app via `AddHttpContextAccessor()`; also referenced from the route-services `AddDbContext` lambda. |
| `ITenantContext` | **app** | scoped | Holds `string TenantId` for the current request. Populated by middleware (which runs in the app pipeline) and consumed indirectly by the route-scope `DbContext` factory through `IHttpContextAccessor`. |
| `TenantContext` | app | scoped (impl of above) | Trivial concrete implementation: settable `TenantId`. |
| `IConnectionStringProvider` | **app** | singleton | Two methods: `string GetConnectionString(string tenantId)` (throws on unknown) and `bool TryGetConnectionString(string tenantId, out string connectionString)` (non-throwing). Single app-DI registration: the middleware reads it via constructor injection, and the route-scope `AddDbContext` factory reaches into app DI through `IHttpContextAccessor.HttpContext.RequestServices`. The seam users replace for Key Vault, a tenant-registry table, dynamic provisioning, etc. |
| `ConfigurationConnectionStringProvider` | app | singleton (default impl) | Reads `IConfiguration["ConnectionStrings:Tenant_{tenantId}"]`. |
| `TenantResolutionMiddleware` (3 flavors) | app pipeline | n/a | Path-segment / subdomain / header. **Must run before `UseRouting`** so RESTier's `{prefix}/{**odataPath}` pattern matches the rewritten path (path-segment flavor) or the unchanged path (subdomain/header flavors). All three populate `ITenantContext.TenantId`. All three call `IConnectionStringProvider.TryGetConnectionString` up-front and return `400 Bad Request` if the tenant is unknown — that is the canonical recipe and what the integration test asserts. |
| `MultiTenantApi : EntityFrameworkApi<TenantDbContext>` | route | scoped | Single `ApiBase` subclass for all tenants; the EDM is shared. |
| `TenantDbContext` | route | scoped | Standard EF Core `DbContext`; constructor takes `DbContextOptions<TenantDbContext>`. |

### Data flow (path-segment example)

1. `GET /acme/odata/Books` arrives.
2. `TenantResolutionMiddleware` (path-segment flavor) runs **before** `UseRouting`:
   - splits the path; first segment is `acme`.
   - calls `IConnectionStringProvider.TryGetConnectionString("acme", out _)` (resolved from the app DI container via the middleware's constructor injection); if it returns `false`, short-circuits with `400 Bad Request` and the pipeline stops here.
   - resolves `ITenantContext` from `httpContext.RequestServices` (app scope) and sets `TenantId = "acme"`.
   - rewrites the request: `httpContext.Request.PathBase = httpContext.Request.PathBase.Add("/acme");` and `httpContext.Request.Path = "/odata/Books";`.
3. `UseRouting` runs against the rewritten path. The endpoint registered by `MapRestier` for prefix `odata` (pattern `odata/{**odataPath}`) matches.
4. `UseEndpoints` invokes the matched endpoint. The Restier dynamic route transformer parses `Books` against the `odata` prefix and dispatches to `RestierController`.
5. `RestierController.EnsureInitialized` resolves `MultiTenantApi` from `HttpContext.Request.GetRouteServices()` (the per-route scoped container).
6. EF resolves `TenantDbContext`. The `AddDbContext` factory lambda runs in the route scope:
   - resolves `IHttpContextAccessor` from the route `sp` (the only app-DI service we expose to the route container).
   - via `HttpContext.RequestServices` (the app scope), resolves `ITenantContext` (reads `"acme"`) and `IConnectionStringProvider` (calls `GetConnectionString("acme")`). The provider lookup already succeeded in step 2; in production the provider is expected to cache.
   - configures `DbContextOptions` (e.g. `opt.UseSqlServer(connStr)` or, in the test, `opt.UseInMemoryDatabase("tenant-acme-db")`).
7. Query executes against tenant `acme`'s database.
8. Response body's `@odata.context` reads `…/acme/odata/$metadata` because `PathBase` was preserved.

### Subdomain and header flavors

Mechanically identical to the path-segment flavor except:

- **Subdomain:** read `httpContext.Request.Host.Host`, take the first label (or any user-defined extraction). No path mutation.
- **Header:** read a configurable header (default `X-Tenant-Id`). No path mutation.

In neither flavor is `PathBase` touched, because the URL path itself is already what RESTier expects.

## Failure modes

The canonical recipe validates up-front in middleware, so most failure cases short-circuit before reaching RESTier.

| Failure | Where it surfaces | Canonical behavior |
|---|---|---|
| Tenant segment missing from path (path-segment mode) | middleware | `400 Bad Request` with body explaining the tenant segment is required. |
| Unknown tenant (`TryGetConnectionString` returns false) | middleware, after URL extraction | `400 Bad Request`. Test 5 asserts this. |
| Middleware not registered | the endpoint runs with `ITenantContext.TenantId == null` | `IConnectionStringProvider.GetConnectionString(null)` throws → ASP.NET Core surfaces as `500`. The docs include a startup-time sanity check recipe (e.g., a smoke-test endpoint or a hosted service that asserts the middleware is wired) but the spec does not mandate one. |
| `AddDbContextPool` used instead of `AddDbContext` | first request after pool warm-up | wrong tenant's connection string is silently reused. **Incompatible**; called out explicitly in the limitations section of the docs. |

**Alternatives the docs page describes (not the canonical default):**

- **Skip middleware-side validation, let the provider throw → 500.** Slightly less code but worse UX (500 implies a server bug, not a bad client request). Shown for completeness.
- **Map unknown tenant to 404 instead of 400.** Reasonable if tenants are an addressable resource and you want consistency with a "tenant not found" REST semantic. One-line change to the middleware.

## Testing

### Test project and location

`test/Microsoft.Restier.Tests.AspNetCore/ScenarioTests/EFCore/MultiTenancyScenarioTests.cs`.

Placed under `ScenarioTests/EFCore/` (not `RegressionTests/`) because this is a documented capability, not bug-tracking. EF Core only — EF6 isn't a target audience for new SaaS guidance.

### Test fixture

Subclass `RestierTestBase<MultiTenantApi>` directly (no abstract intermediate, since we're not parameterizing across multiple `TApi`/`TContext` pairs the way Issue671 does). The fixture wires three sets of services:

- **App-level services** (configured via the host builder): `AddHttpContextAccessor()`, `AddScoped<ITenantContext, TenantContext>()`, `AddSingleton<IConnectionStringProvider>(new InMemoryTenantConnectionStringProvider(...))`. The middleware reads `IConnectionStringProvider` from app DI via constructor injection and writes `ITenantContext.TenantId` from app DI via `httpContext.RequestServices.GetRequiredService<ITenantContext>()`.
- **Pipeline middleware** (also app-level): `app.UseMiddleware<PathSegmentTenantResolutionMiddleware>()` registered **before** `UseRouting`. The test invokes the `ApplicationBuilderAction` hook on `RestierBreakdanceTestBase`, which is invoked at the very top of the pipeline.
- **Route-level services** (via `AddRestierAction` → `AddRestierRoute<MultiTenantApi>("odata", services => ...)`):
  - `services.AddHttpContextAccessor()` — required so the per-route container can resolve `IHttpContextAccessor` inside the `AddDbContext` factory; this is the only entry point into the bridge.
  - `services.AddDbContext<TenantDbContext>((sp, opt) => { ... })` — factory bridges into app DI via `IHttpContextAccessor.HttpContext.RequestServices` to read both `ITenantContext` and `IConnectionStringProvider` (which live there, not in the route container).
  - The standard RESTier EF Core service registrations.

### Test impl of `IConnectionStringProvider`

```csharp
internal sealed class InMemoryTenantConnectionStringProvider : IConnectionStringProvider
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["acme"]   = "tenant-acme-db",
        ["globex"] = "tenant-globex-db",
    };

    public string GetConnectionString(string tenantId)
        => TryGetConnectionString(tenantId, out var name)
            ? name
            : throw new InvalidOperationException($"Unknown tenant '{tenantId}'.");

    public bool TryGetConnectionString(string tenantId, out string connectionString)
        => Map.TryGetValue(tenantId ?? "", out connectionString);
}
```

The "connection string" is the EF Core in-memory database name; `UseInMemoryDatabase(name)` keys isolated databases by that string.

### Test seed data

A small in-line seed runs once per test (or in the constructor): each tenant's `TenantDbContext` is created by hand (using the same factory) and a single distinguishable `Book` is added.

- Tenant `acme` → `Book { Title = "AcmeBook" }`
- Tenant `globex` → `Book { Title = "GlobexBook" }`

### Test cases

| # | Test | Asserts |
|---|---|---|
| 1 | `Acme_GetsAcmeData` | `GET /acme/odata/Books` returns exactly `AcmeBook` |
| 2 | `Globex_GetsGlobexData` | `GET /globex/odata/Books` returns exactly `GlobexBook` |
| 3 | `CrossTenantIsolation_PostToAcme_DoesNotLeakToGlobex` | `POST /acme/odata/Books` then `GET /globex/odata/Books` does not contain the new book |
| 4 | `OdataContextUrlPreservesTenantPrefix` | `@odata.context` in the response body equals `…/acme/odata/$metadata`, proving `PathBase` rewrite worked |
| 5 | `UnknownTenant_Returns400` | `GET /unknown/odata/Books` returns `400 Bad Request` (the in-test middleware rejects unknown tenants up-front rather than letting the provider throw) |

Test 5 chooses the "middleware rejects with 400" semantics for the integration test, while the doc page additionally describes "let the provider throw / map to 404" as a viable alternative. This matches what the doc page says about failure modes.

### What the integration test does NOT cover

By design — these are documented patterns but not part of this test's scope:

- Subdomain and header resolution flavors. (Possible future addition; their middlewares are simple enough that adding them later is mechanical.)
- The shared-DB-with-`tenant_id`-column hardening pattern. That is a different multi-tenancy strategy and gets a code sketch in the docs but no test here.
- Concurrent multi-tenant requests racing in the same process (the InMemory provider already isolates by name; the request scope already isolates `ITenantContext` per request — no shared mutable state to race).

## Documentation

### Page

`src/Microsoft.Restier.Docs/guides/server/multi-tenancy.mdx`.

### Navigation

Add to the `<MintlifyTemplate>` block in `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`, in the `guides/server/` group, immediately after `api-versioning`. The DotNetDocs SDK regenerates `docs.json` on build; the regenerated `docs.json` is committed.

### Page outline

```
title: Multi-Tenancy
description: Serve multiple tenants from one Restier API by resolving the tenant in
             middleware and selecting a per-tenant connection string at DbContext
             resolution time.
imports: Steps, Tabs, Tab, CodeGroup, Note, Tip, Warning

intro paragraph (1–2 sentences): one ApiBase, one EDM, per-tenant DB. No framework
changes required; works on Restier's per-route scoped DI plus EF Core's runtime
options configuration.

<Note>
Scope: shared schema, DB-per-tenant. For shared-DB-with-tenant-column see
"Hardening: shared-database alternative" below.
</Note>

## How it works
  4–5 line ASCII data-flow block (request → middleware → ITenantContext →
  DbContextOptions factory → tenant DB).

## Setup
  <Steps>
    Step 1: Define ITenantContext + scoped impl. Register at the APP level
            (builder.Services.AddScoped<ITenantContext, TenantContext>()).
            Also call builder.Services.AddHttpContextAccessor().
    Step 2: Define IConnectionStringProvider + the IConfiguration-backed default impl.
            Registered later as a singleton in the ROUTE services (Step 4).
    Step 3: Define TenantResolutionMiddleware (path-segment flavor; subdomain/header
            shown in the next section). Up-front validation via TryGetConnectionString;
            return 400 on unknown tenant. The path-segment flavor moves the stripped
            segment to Request.PathBase before mutating Request.Path.
    Step 4: Register MultiTenantApi via AddRestierRoute("odata", services => ...).
            Inside that lambda: register IConnectionStringProvider, register
            IHttpContextAccessor (for the route container), and call
            AddDbContext<TenantDbContext>((sp, opt) => ...) with the bridge:
                var http = sp.GetRequiredService<IHttpContextAccessor>().HttpContext!;
                var tenant = http.RequestServices.GetRequiredService<ITenantContext>();
                opt.UseSqlServer(connStringProvider.GetConnectionString(tenant.TenantId));
    Step 5: Wire middleware in the pipeline — CRITICAL: BEFORE UseRouting, not after.
                app.UseMiddleware<TenantResolutionMiddleware>();   // first
                app.UseRouting();
                app.UseEndpoints(e => { e.MapControllers(); e.MapRestier(); });
            With the path-segment flavor, RESTier's odata/{**odataPath} pattern
            relies on the rewritten path that the middleware produces, so ordering
            is not optional.
  </Steps>

  <Warning>
    Middleware ordering: the tenant-resolution middleware must run BEFORE
    UseRouting. RESTier registers its endpoint at pattern {prefix}/{**odataPath};
    in path-segment mode the request URL doesn't match that pattern until after
    the tenant segment is stripped, so endpoint matching has to happen on the
    rewritten path, not the original.
  </Warning>

## Tenant resolution strategies
  <Tabs>
    <Tab title="Path segment">  full middleware with PathBase-preservation
    <Tab title="Subdomain">     full middleware, no path mutation
    <Tab title="Header">        full middleware (default header X-Tenant-Id)
  </Tabs>

  <Warning>
    PathBase preservation: in path-segment mode the stripped tenant segment MUST be
    moved to Request.PathBase, otherwise @odata.context and entity-link URLs in
    response bodies will point at /odata/... instead of /{tenant}/odata/... and
    OData clients will follow broken links.
  </Warning>

## Connection string sources
  IConfiguration-backed default impl (~5 lines).
  Production swap-ins: Key Vault, tenant-registry table, dynamic provisioning.
  <Tip>caching the resolved connection string per tenant if the provider is
  expensive (the provider is a singleton so caching there is straightforward).</Tip>

## Hardening: shared-database alternative
  When DB-per-tenant isn't the right fit (lots of small tenants, shared compute, no
  per-tenant compliance boundary), the alternative is shared-DB with a tenant_id
  column on every entity, plus per-entity-set RESTier filter methods named per the
  convention OnFilter{EntitySetName} (e.g., OnFilterBooks) that AND in
  e => e.TenantId == currentTenant. The current tenant is read from the
  ITenantContext injected into the ApiBase subclass via constructor DI.
  Code sketch (~15 lines).
  Explicit framing: this is a DIFFERENT strategy, not an addition to the one above.
  Pick one.

## Limitations
  - Schema-per-tenant: not supported; one EDM per route is built at startup.
  - AddDbContextPool: incompatible (pool keys options at startup).
  - First request per tenant pays connection-open / migration cost.
  - Tenant authorization (is the principal allowed to act on this tenant?) is
    application-specific and out of scope here.

## Alternative: per-tenant deployment behind a reverse proxy
  Same shape as the api-versioning guide's reverse-proxy section.
  - When to choose it: large enterprise customers, strict blast-radius isolation,
    independent rollouts per tenant, tenant-specific runtime/dependency divergence.
  - Backend: plain AddRestierRoute<TApi>("odata", ...) with a fixed connection string;
    no ITenantContext, no IConnectionStringProvider.
  - Proxy maps {tenant}.example.com or /{tenant}/... to the right backend.
  - X-Forwarded-Prefix advice (cross-link to the versioning guide's section) if the
    backend should see the public URL.
  - "When to choose which" comparison table:
    | Concern | In-process per-tenant DB | Per-tenant deployment |
    |---|---|---|
    | Blast radius                 | one process, all tenants | hard isolation |
    | Per-tenant rollout           | coupled                  | independent |
    | New-tenant onboarding        | config + DB provision    | new deployment |
    | Mixed runtime versions       | not possible             | natural |
    | Cross-tenant code reuse      | direct                   | shared NuGet |
    | Operational footprint        | one process              | N processes |

## See also
  - API Versioning — same per-prefix-route mechanic.
  - The reverse-proxy / X-Forwarded-Prefix details in the Versioning guide.
```

### Length budget

Approximately the same as `api-versioning.mdx` (~220 lines including code blocks and tables). Substantial enough to be a real reference, not a stub.

## File-by-file deliverables

| Path | Action |
|---|---|
| `src/Microsoft.Restier.Docs/guides/server/multi-tenancy.mdx` | new |
| `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` | edit `<MintlifyTemplate>` to add the new page |
| `src/Microsoft.Restier.Docs/docs.json` | regenerated on build, committed |
| `test/Microsoft.Restier.Tests.AspNetCore/ScenarioTests/EFCore/MultiTenancyScenarioTests.cs` | new — fixture + 5 test cases |
| `test/Microsoft.Restier.Tests.AspNetCore/ScenarioTests/EFCore/MultiTenancy/ITenantContext.cs` | new |
| `test/Microsoft.Restier.Tests.AspNetCore/ScenarioTests/EFCore/MultiTenancy/TenantContext.cs` | new |
| `test/Microsoft.Restier.Tests.AspNetCore/ScenarioTests/EFCore/MultiTenancy/IConnectionStringProvider.cs` | new |
| `test/Microsoft.Restier.Tests.AspNetCore/ScenarioTests/EFCore/MultiTenancy/InMemoryTenantConnectionStringProvider.cs` | new (test impl) |
| `test/Microsoft.Restier.Tests.AspNetCore/ScenarioTests/EFCore/MultiTenancy/PathSegmentTenantResolutionMiddleware.cs` | new |
| `test/Microsoft.Restier.Tests.AspNetCore/ScenarioTests/EFCore/MultiTenancy/MultiTenantApi.cs` | new |
| `test/Microsoft.Restier.Tests.AspNetCore/ScenarioTests/EFCore/MultiTenancy/TenantDbContext.cs` | new (small DbContext) |
| `test/Microsoft.Restier.Tests.AspNetCore/ScenarioTests/EFCore/MultiTenancy/Book.cs` | new (single POCO entity used by the test; intentionally not reusing `Library.Book` to keep this scenario self-contained) |

No changes to `src/Microsoft.Restier.Core`, `src/Microsoft.Restier.AspNetCore`, `src/Microsoft.Restier.EntityFrameworkCore`, or any other shipped package.

## Notes for the plan writer

1. **Pipeline-middleware hook in `RestierTestBase` — needs verification.** Inspect `RestierBreakdanceTestBase` (in `Microsoft.Restier.Breakdance`) to find a hook that accepts `IApplicationBuilder` configuration so the test can install `PathSegmentTenantResolutionMiddleware` **before** `UseRouting` (an `IStartupFilter` registered in the **app** services, not the route services, is the safe fallback).
2. **`IHttpContextAccessor` visibility across containers — verify at plan-writing time.** Inspect how `Microsoft.AspNetCore.OData`'s `AddRouteComponents` builds its per-route `IServiceProvider`. If process-singletons registered at the app level (like `IHttpContextAccessor` from `AddHttpContextAccessor()`) are visible to the route container, the explicit re-registration in route services is belt-and-braces and harmless. If not, the explicit registration is required. Either way the spec is correct as written; this note confirms which of the two cases applies.
3. **Failure-mode semantics — settled.** Canonical recipe: middleware up-front validates via `TryGetConnectionString` and returns 400 on unknown tenant. Test 5 asserts 400. The docs describe 500 (let provider throw) and 404 (map manually) as variants.
4. **File layout — settled.** Support types live in `ScenarioTests/EFCore/MultiTenancy/`, mirroring how `Library` and `Marvel` scenarios are organized in `Microsoft.Restier.Tests.Shared`.
5. **CLAUDE.md inaccuracy worth fixing separately.** `CLAUDE.md` claims convention names like `OnFiltering{EntitySet}()`. The actual factory (`src/Microsoft.Restier.Core/Conventions/ConventionBasedMethodNameFactory.cs:78,159`) produces `OnFilter{EntitySetName}` (no `-ing`/`-ed` suffix on Filter, entity-set name not entity-type name). Out of scope for this spec but worth flagging in a follow-up doc fix.
