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
