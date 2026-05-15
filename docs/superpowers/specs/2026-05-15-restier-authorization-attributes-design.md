# `[AllowAnonymous]` / `[Authorize]` on RESTier API Surfaces

**Date:** 2026-05-15
**Status:** Design draft — awaiting confirmation
**Issue:** [OData/RESTier#717](https://github.com/OData/RESTier/issues/717) — `[AllowAnonymous]` does not allow anonymous requests

## Goal

Make ASP.NET Core's standard authorization attributes — `[AllowAnonymous]`, `[Authorize]`, `[Authorize(Policy="…")]`, `[Authorize(Roles="…")]`, `[Authorize(AuthenticationSchemes="…")]` — work on RESTier API surfaces the way developers expect them to work on any other ASP.NET Core controller or action. Specifically:

- `[AllowAnonymous]` on the `ApiBase` subclass overrides a globally-registered `[Authorize]` filter for every route served by `RestierController`.
- `[AllowAnonymous]` / `[Authorize]` on a `[Resource]`-decorated property scopes the attribute to that resource / singleton.
- `[AllowAnonymous]` / `[Authorize]` on a `[BoundOperation]` / `[UnboundOperation]` method scopes the attribute to that operation.

Implementation must be transparent: no new `app.Use…` call required. `AddRestier` wires everything via DI.

DbSet-backed entity sets are explicitly out of scope (they have no anchor on `ApiBase` — see Decisions). The class-level attribute still applies to them since it applies to every route.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Surfaces that carry attributes | Class (`ApiBase` subclass) + `[Resource]` properties + `[BoundOperation]` / `[UnboundOperation]` methods | These are the surfaces that actually live on `ApiBase`. DbSet-backed entity sets have no anchor there; users wanting per-DbSet-entity-set granularity continue to use RESTier's existing `Can*` / `IChangeSetItemAuthorizer` story. |
| Attribute set | Anything implementing `IAuthorizeData` or `IAllowAnonymous` (the same interfaces `AuthorizationMiddleware` consumes) | Pass-through: we copy these attributes onto endpoint metadata and let `AuthorizationMiddleware` apply its standard precedence rules. Free policy / roles / schemes support without re-implementing anything. |
| Mechanism | `IEndpointSelectorPolicy` (a `MatcherPolicy`) registered as singleton via DI | Runs during routing, between dynamic endpoint selection and `AuthorizationMiddleware`. Auto-wired by `AddRestier`; no `app.UseRestierAuthorization()` required, matching the user's "out of the box" requirement. |
| Pipeline ordering | None — `MatcherPolicy` does not require changes to the user's `Configure` method | The user's existing `UseRouting → UseAuthentication → UseAuthorization → UseEndpoints` ordering is sufficient. |
| Per-segment lookup | First "significant" segment of `ODataFeature.Path` (EntitySet / Singleton / OperationImport / Operation) | Matches what an OData consumer thinks of as "the action surface." Metadata / service document paths fall back to class-level only. |
| Precedence between class and member | Delegated to `AuthorizationMiddleware` | We add all collected attributes to endpoint metadata. ASP.NET Core's standard rule — `IAllowAnonymous` overrides any `IAuthorizeData` regardless of order — already does the right thing. |
| Caching | `(apiType, targetKey) → wrapped Endpoint` cached in a `ConcurrentDictionary<,>` on the policy | Reflection lookup happens once per (API type, target) tuple; subsequent requests hit the cache. Endpoints are immutable, so caching is safe. |
| Inheritance | `GetCustomAttributes(inherit: true)` | A custom API class inheriting from a base that declares `[Authorize]` picks it up. Standard CLR convention. |
| `$batch` requests | Each child request goes through routing → policy fires per child | No special-casing in the policy. Attributes apply per child operation. Covered by a dedicated test. |
| Bound operations on entity sets (`/Books({id})/Restier.DiscontinueBooks`) | Use the operation method's attributes (last `OperationSegment` wins) | Matches ASP.NET Core's "the action's attributes apply" convention. |

## Background

`RestierController` is a single, shared controller in `Microsoft.Restier.AspNetCore` that handles every HTTP verb (`Get`, `Post`, `Put`, `Patch`, `Delete`, `PostAction`, `GetMetadata`, `GetServiceDocument`). Routes are wired via `RestierRouteValueTransformer` (a `DynamicRouteValueTransformer`), which parses the OData path and dispatches to the appropriate action.

The user's `ApiBase` subclass (e.g. `TrippinApi`) is not a controller. It is resolved from per-route DI inside `RestierController.EnsureInitialized()`. ASP.NET Core's `AuthorizationMiddleware` reads `IAuthorizeData` and `IAllowAnonymous` from `HttpContext.GetEndpoint().Metadata` — but the only endpoint in play is `RestierController`'s action, which has no user attributes on it. So attributes placed on `TrippinApi`, `[Resource]` properties, or operation methods are invisible to authorization.

That's #717. The reporter has a global `services.AddControllers(opts => opts.Filters.Add(new AuthorizeFilter()))` registration, decorates `TrippinApi` with `[AllowAnonymous]`, and expects the filter to be overridden for RESTier routes. It isn't, because the global filter materializes as `IAuthorizeData` on every controller's endpoint metadata, and `RestierController` doesn't carry an `IAllowAnonymous` to override it.

## Architecture

### Components

| # | Component | Assembly | Notes |
|---|-----------|----------|-------|
| 1 | `RestierAuthorizationMetadataPolicy : MatcherPolicy, IEndpointSelectorPolicy` | `Microsoft.Restier.AspNetCore` | Stateless instance state; holds a process-scoped attribute lookup cache. Examines candidate endpoints, identifies Restier endpoints, resolves the API type + target, replaces the endpoint with one carrying augmented metadata. |
| 2 | `RestierRouteMarker` enriched with the API type | `Microsoft.Restier.AspNetCore` | Today the marker is a sentinel (`class RestierRouteMarker {}`). We add `Type ApiType { get; }` so the matcher policy can look it up from route services in O(1) without re-scanning `ODataOptions.RouteComponents`. |
| 3a | `RestierIMvcBuilderExtensions.AddRestier` registers the policy in the host service collection | `Microsoft.Restier.AspNetCore` | One added line in each of the four `AddRestier` overloads (factor a private helper to avoid duplication): `services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, RestierAuthorizationMetadataPolicy>());`. Registered unconditionally so existing `AddRestier` callers get it for free. |
| 3b | `RestierEndpointRouteBuilderExtensions.MapRestier` attaches the `RestierRouteMarker` to endpoint metadata | `Microsoft.Restier.AspNetCore` | Existing code already iterates registered Restier route prefixes. We call `MapDynamicControllerRoute<...>(...).WithMetadata(new RestierRouteMarker(apiType))` so `AppliesToEndpoints` can filter cheaply. |
| 3c | `RestierODataOptionsExtensions.AddRestierRoute<TApi>` passes the API type into the marker registered in route services | `Microsoft.Restier.AspNetCore` | Today the marker is registered as `services.AddSingleton(new RestierRouteMarker())`. We change it to pass `typeof(TApi)`. Existing consumers of the marker only check `is not null`, so this is backward-compatible. |
| 4 | Per-target lookup helper (private to the policy) | `Microsoft.Restier.AspNetCore` | Maps `ODataPath` → "target key" (one of: `class`, `resource:Foo`, `operation:Bar`). Reflectively finds the corresponding `MemberInfo` on the API type. |

### Why a `MatcherPolicy` rather than a middleware

Two reasons:

1. **No `app.Use…` call required.** A `MatcherPolicy` is registered via DI and runs inside `EndpointRoutingMiddleware`. The user's existing pipeline order works as-is.
2. **Right timing.** The policy runs *after* dynamic endpoint selection has matched `RestierController.Get` and `RestierRouteValueTransformer` has populated `ODataFeature.Path`, and *before* `AuthorizationMiddleware` reads endpoint metadata. We have everything we need and nothing else has acted yet.

### `RestierAuthorizationMetadataPolicy` shape

```csharp
internal sealed class RestierAuthorizationMetadataPolicy : MatcherPolicy, IEndpointSelectorPolicy
{
    private readonly IOptions<ODataOptions> odataOptions;
    private readonly ConcurrentDictionary<(Type apiType, string targetKey), Endpoint> cache = new();

    // DynamicControllerEndpointMatcherPolicy.Order == int.MinValue + 100. We run after it so the
    // OData path is already parsed and the candidate endpoint is the RestierController action.
    public override int Order => int.MinValue + 110;

    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        // Cheap filter: only engage if at least one endpoint has the RestierRouteMarker
        // in its metadata. Attached by MapRestier via the dynamic route's route services.
        for (var i = 0; i < endpoints.Count; i++)
        {
            if (endpoints[i].Metadata.GetMetadata<RestierRouteMarker>() is not null) return true;
        }
        return false;
    }

    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i)) continue;

            ref readonly var candidate = ref candidates[i];
            var marker = candidate.Endpoint.Metadata.GetMetadata<RestierRouteMarker>();
            if (marker is null) continue;

            var path = httpContext.ODataFeature().Path;
            var targetKey = ComputeTargetKey(path);
            var cacheKey = (marker.ApiType, targetKey);

            if (!cache.TryGetValue(cacheKey, out var wrapped))
            {
                wrapped = BuildWrappedEndpoint(candidate.Endpoint, marker.ApiType, path, targetKey);
                cache.TryAdd(cacheKey, wrapped);
            }

            candidates.ReplaceEndpoint(i, wrapped, candidate.Values);
        }

        return Task.CompletedTask;
    }
}
```

`BuildWrappedEndpoint` collects `IAuthorizeData` and `IAllowAnonymous` attributes from (in order) the API class and the target member, then returns either the original endpoint (no attributes found — saves an allocation) or a new `RouteEndpoint` whose metadata is the original's metadata plus the collected attributes.

### Target key resolution

Given `ODataPath`, `ComputeTargetKey` returns one of:

| Path shape | Target key | Member resolved |
|------------|-----------|-----------------|
| Empty path / service document | `"class"` | `apiType` itself |
| `$metadata` | `"class"` | `apiType` itself |
| `/{EntitySet}` (or any path starting with one) | `"resource:{name}"` if `[Resource] {name}` exists on `apiType`, else `"class"` | The property, or `apiType` |
| `/{Singleton}` | `"resource:{name}"` (same lookup) | The property, or `apiType` |
| `/{OperationImport}` | `"operation:{name}"` | The method on `apiType` |
| Path ending in `/Restier.{Operation}` (bound) | `"operation:{name}"` | The method on `apiType` |

`ComputeTargetKey` deals only in string keys; the reflection happens once when `BuildWrappedEndpoint` populates the cache miss.

### `RestierRouteMarker` enrichment

```csharp
internal sealed class RestierRouteMarker
{
    public RestierRouteMarker(Type apiType) => ApiType = apiType;

    public Type ApiType { get; }
}
```

`AddRestierRoute<TApi>` already knows `typeof(TApi)`. We pass it into the marker's constructor.

We also need the marker to land in *endpoint* metadata (not just route services) so `AppliesToEndpoints` can filter cheaply. Two options:

1. Add it via `MapDynamicControllerRoute(...).WithMetadata(new RestierRouteMarker(apiType))` in `MapRestier`. Cleanest — the marker is part of the endpoint's static metadata.
2. Resolve from route services inside `ApplyAsync`. Requires `httpContext.Request.GetRouteServices()` to be populated, which it is by the time matcher policies run.

Going with (1): static metadata makes `AppliesToEndpoints` a tight loop with no DI lookups, and there's only one marker per route. The route-services registration stays (it's used elsewhere for the dynamic transformer's filtering), so the marker just gets registered in both places.

## Data Flow

### Golden path — `[AllowAnonymous]` on `ApiBase`

Setup:

```csharp
[AllowAnonymous]
public class TrippinApi : EntityFrameworkApi<TrippinContext> { /* ... */ }

services.AddControllers(opts => opts.Filters.Add(new AuthorizeFilter()));
services.AddRestier(o => o.AddRestierRoute<TrippinApi>("api", svc => svc.AddEFCoreProviderServices<TrippinContext>(...)));
```

Request: `GET /api/Books`.

1. `EndpointRoutingMiddleware` runs.
   1. `DynamicControllerEndpointMatcherPolicy` invokes `RestierRouteValueTransformer.TransformAsync`. The transformer parses the OData path, populates `ODataFeature.Path = [EntitySetSegment("Books")]`, returns `{ controller = "Restier", action = "Get" }`. The system matches `RestierController.Get` as the candidate endpoint. Its static metadata includes `[AuthorizeFilter]` (from the global filter) and the `RestierRouteMarker(typeof(TrippinApi))` we added in `MapRestier`.
   2. `RestierAuthorizationMetadataPolicy.AppliesToEndpoints` returns true (marker present).
   3. `ApplyAsync` runs:
      - Reads `marker.ApiType = typeof(TrippinApi)`.
      - `ComputeTargetKey(path) = "class"` (no `[Resource] Books` on `TrippinApi`; DbSet-backed entity set; falls back to class).
      - Cache miss. `BuildWrappedEndpoint` reads `typeof(TrippinApi).GetCustomAttributes(inherit: true)` → finds `[AllowAnonymous]`. Builds wrapped `RouteEndpoint` with augmented metadata.
      - `candidates.ReplaceEndpoint(0, wrapped, candidate.Values)`.
2. `EndpointMiddleware` stores the wrapped endpoint on the request.
3. `AuthenticationMiddleware` runs.
4. `AuthorizationMiddleware` reads endpoint metadata, sees `IAllowAnonymous`, bypasses the global `[Authorize]` requirement.
5. `RestierController.Get` executes normally.

### Per-operation `[Authorize(Policy="…")]`

Setup:

```csharp
public class TrippinApi : EntityFrameworkApi<TrippinContext>
{
    [UnboundOperation]
    [Authorize(Policy = "Admin")]
    public void ResetDataSource() { /* ... */ }
}
```

Request: `POST /api/ResetDataSource`.

1. Routing matches `RestierController.PostAction`.
2. Matcher policy:
   - `ComputeTargetKey(path) = "operation:ResetDataSource"`.
   - `BuildWrappedEndpoint` finds the `ResetDataSource` method on `TrippinApi`, reads `[Authorize(Policy="Admin")]`. Adds to metadata.
3. `AuthorizationMiddleware` evaluates the `"Admin"` policy via the user's `IAuthorizationPolicyProvider`. Allows or denies as configured.

### `[AllowAnonymous]` on a `[Resource]` property

Setup:

```csharp
public class LibraryApi : EntityFrameworkApi<LibraryContext>
{
    [AllowAnonymous]
    [Resource]
    public IQueryable<Book> BooksWithPublisher => DbContext.Books.Include(b => b.Publisher);
}
```

Request: `GET /api/BooksWithPublisher`.

1. Matcher policy: `ComputeTargetKey(path) = "resource:BooksWithPublisher"`.
2. `BuildWrappedEndpoint` finds the property, reads `[AllowAnonymous]`.
3. Auth bypassed for this resource only. Plain `/Books` (DbSet-backed) still hits class-level (or, if no class-level attribute, gets the global `[Authorize]`).

## Error Handling & Edge Cases

- **No attribute on either class or target.** Policy is a no-op for that request: cache stores the original endpoint, `ReplaceEndpoint` is still called but with the same endpoint (no allocation beyond the cache entry, which is one-shot).
- **Conflicting class + member attributes.** `[Authorize]` on the class + `[AllowAnonymous]` on a member → both end up in metadata; `AuthorizationMiddleware` enforces "AllowAnonymous wins." No special handling required.
- **`$batch` requests.** Each child request runs through routing. `ODataBatchHttpContextFixerMiddleware` and the batch handler set up per-child `HttpContext` state; the matcher policy fires for each child operation. **Covered by a dedicated test.**
- **Operations bound to a resource path** (`/Books({id})/Restier.DiscontinueBooks`). `ComputeTargetKey` walks to the last `OperationSegment` — the operation's attributes win, not the entity set's.
- **`OperationSegment` for a function vs action.** Both are looked up by method name on the API type; no behavior difference.
- **Inheritance.** `GetCustomAttributes(inherit: true)` picks up attributes on a base API class. If a user has `class TrippinApi : RestrictedApi` and `[Authorize]` sits on `RestrictedApi`, subclasses inherit it unless they declare `[AllowAnonymous]` themselves.
- **Schemes / roles.** `[Authorize(AuthenticationSchemes="X", Roles="Y")]` is `IAuthorizeData`; passes through unchanged. The matcher policy doesn't introspect the attribute contents.
- **Cache scope.** The cache lives on the singleton policy instance, which is registered into the application's root service provider. API types are static (loaded assemblies); attribute decoration cannot change at runtime. No invalidation needed. In test harnesses each test builds its own host with its own service provider and thus its own policy instance — no cross-test contamination.

## Testing Strategy

### Unit tests

`test/Microsoft.Restier.Tests.AspNetCore/Routing/RestierAuthorizationMetadataPolicyTests.cs`

- `AppliesToEndpoints_NonRestierEndpoint_ReturnsFalse`
- `AppliesToEndpoints_RestierEndpoint_ReturnsTrue`
- `ApplyAsync_ClassWithAllowAnonymous_AugmentsMetadataWithAllowAnonymous`
- `ApplyAsync_ClassWithAuthorize_AugmentsMetadataWithAuthorizeData`
- `ApplyAsync_ResourcePropertyWithAllowAnonymous_AugmentsForThatResourceOnly`
- `ApplyAsync_OperationMethodWithAuthorize_AugmentsForThatOperation`
- `ApplyAsync_NoAttributes_LeavesEndpointUnchanged`
- `ApplyAsync_CacheHit_DoesNotReflect` (sentinel that asserts `GetCustomAttributes` isn't called twice for the same key)
- `ComputeTargetKey_MetadataPath_ReturnsClass`
- `ComputeTargetKey_EntitySetWithMatchingResource_ReturnsResource`
- `ComputeTargetKey_EntitySetWithoutMatchingResource_ReturnsClass`
- `ComputeTargetKey_OperationImport_ReturnsOperation`
- `ComputeTargetKey_BoundOperation_ReturnsOperation`

### Integration tests

`test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnonymousAccessTests.cs` (new file alongside the existing `AuthorizationTests.cs`)

Fixture API types declared in the test project:

- `AnonymousAtClassApi` (entire class `[AllowAnonymous]`)
- `AnonymousAtResourceApi` (one `[Resource]` property `[AllowAnonymous]`, rest auth-required)
- `AnonymousAtOperationApi` (one operation method `[AllowAnonymous]`, rest auth-required)
- `PolicyOnOperationApi` (operation method with `[Authorize(Policy = "Admin")]`)

Test scenarios:

| # | Scenario | Expected |
|---|----------|----------|
| 1 | Global `[Authorize]` filter + `[AllowAnonymous]` on API class + anonymous `GET /Books` | 200 OK |
| 2 | Global `[Authorize]` filter, no class attribute, anonymous `GET /Books` | 401/403 (control case — verifies the global filter actually fires) |
| 3 | `[AllowAnonymous]` on `BooksWithPublisher` `[Resource]`, anonymous `GET /BooksWithPublisher` | 200 OK |
| 4 | Same fixture as 3, anonymous `GET /Books` (no resource attribute, no class attribute) | 401/403 |
| 5 | `[AllowAnonymous]` on operation `Hello`, anonymous `GET /Hello()` | 200 OK |
| 6 | `[Authorize(Policy = "Admin")]` on operation `ResetDataSource`, authenticated non-admin user `POST /ResetDataSource` | 403 |
| 7 | Same as 6, authenticated admin | 200/204 |
| 8 | `$metadata` + global `[Authorize]` + class `[AllowAnonymous]` | 200 OK (validates the class-level path for metadata segment) |
| 9 | Service document (`GET /api/`) + class `[AllowAnonymous]` | 200 OK |
| 10 | `$batch` containing two child operations, one anonymous-allowed and one not, anonymous request | First child 200, second child 401/403 |
| 11 | `[Authorize]` on class, `[AllowAnonymous]` on a `[Resource]` property → that resource bypasses auth | 200 OK on the resource, 401/403 elsewhere |
| 12 | Inheritance: `[Authorize]` on base class, no override on subclass → subclass requires auth | 401 anonymous, 200 authenticated |

`RestierTestHelpers.ExecuteTestRequest` is the established pattern (see `AuthorizationTests.cs` for the harness shape). Tests register a global `AuthorizeFilter` and a fake authentication scheme via `services` to exercise the full middleware pipeline.

## Documentation

`src/Microsoft.Restier.Docs/guides/server/method-authorization.mdx` gains a new top section, placed *before* "Convention-Based Authorization":

> ## Using `[AllowAnonymous]` and `[Authorize]`
>
> RESTier honors the standard ASP.NET Core authorization attributes (`[AllowAnonymous]`, `[Authorize]`, `[Authorize(Policy = "…")]`, `[Authorize(Roles = "…")]`) on three surfaces of your API class. They behave exactly like they do on any other ASP.NET Core controller or action — they participate in `AuthorizationMiddleware` via endpoint metadata.
>
> ### Where attributes can go
>
> ```csharp
> // 1. On the API class itself — applies to every route served by this API.
> [AllowAnonymous]
> public class TrippinApi : EntityFrameworkApi<TrippinContext> { ... }
>
> public class LibraryApi : EntityFrameworkApi<LibraryContext>
> {
>     // 2. On a [Resource] property — applies to that resource only.
>     [AllowAnonymous]
>     [Resource]
>     public IQueryable<Book> BooksWithPublisher => DbContext.Books.Include(b => b.Publisher);
>
>     // 3. On a [BoundOperation] or [UnboundOperation] method — applies to that operation only.
>     [UnboundOperation]
>     [Authorize(Policy = "Admin")]
>     public void ResetDataSource() { ... }
> }
> ```
>
> ### How RESTier authorization relates to ASP.NET Core authorization
>
> Think of them as two complementary layers:
>
> | Layer | What it controls | How you opt in |
> |-------|------------------|----------------|
> | **ASP.NET Core authentication / authorization** | Whether the request reaches RESTier at all (authentication scheme, policy, role, anonymous override) | `[AllowAnonymous]` / `[Authorize]` attributes, evaluated by `AuthorizationMiddleware` |
> | **RESTier authorization** | Whether an authenticated request is allowed to perform a specific entity-set or operation action (`Can{Op}{EntitySet}`, custom `IChangeSetItemAuthorizer`) | Convention methods or chained services on your API class |
>
> `[AllowAnonymous]` *only* tells `AuthorizationMiddleware` to skip the standard auth check. It does not bypass RESTier's `Can*` methods. Use the convention methods (`CanDelete{EntitySet}`, etc.) when you need RESTier-level authorization to behave differently for anonymous vs authenticated users.
>
> ### Precedence
>
> RESTier delegates to the standard ASP.NET Core precedence rules:
>
> - `[AllowAnonymous]` always wins over `[Authorize]`, regardless of which is on the class vs the member.
> - `[Authorize]` attributes are combined (all roles, schemes, policies must be satisfied).
>
> ### Limitation: DbSet-backed entity sets
>
> Entity sets that come from a `DbContext`'s `DbSet<T>` properties (the canonical Entity Framework case) have no anchor on your `ApiBase` subclass — so you can't attach `[AllowAnonymous]` to just `Books`. The class-level attribute always covers them. For per-DbSet-entity-set granularity, use RESTier's existing `Can{Op}{EntitySet}` convention methods, which can inspect `ClaimsPrincipal.Current` directly.

The existing "Convention-Based Authorization" and "Centralized Authorization" sections remain unchanged, with a one-line cross-reference added at the top of "Convention-Based Authorization" pointing readers up to the new section for `[AllowAnonymous]` / `[Authorize]` use cases.

## Out of Scope

- Per-DbSet-entity-set `[AllowAnonymous]` / `[Authorize]`. (D5 in the design discussion — no anchor on `ApiBase` for these entity sets. Tracked separately if/when a clean syntax is proposed.)
- Custom RESTier-specific authorization attributes. We use the existing ASP.NET Core attributes; we do not invent new ones.
- Changing `Can*` / `IChangeSetItemAuthorizer` semantics. Those remain as-is.
- Authentication scheme registration. The user wires authentication via standard ASP.NET Core APIs (`AddAuthentication().AddJwtBearer(...)` etc.) just like for any other controller.
- Endpoint filters or post-execution authorization. Authorization happens at the middleware level (matching ASP.NET Core conventions); no controller-side filter is added or modified.
