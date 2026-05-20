---
title: "Magical Operations (2.0.0-beta)"
description: "Operation auto-registration, optional parameters, and breaking change to GetParameterValueFunc"
sidebarTitle: "2.0 — Magical Operations"
---

## Magical Operations

RESTier 2.0 makes `[BoundOperation]` / `[UnboundOperation]`-decorated methods
fully self-registering. See the [Operations guide](/guides/server/operations#auto-registration-optional-parameters-and-annotations)
for the full feature surface.

Highlights:

- **Complex types are auto-registered** (issue [#651](https://github.com/OData/RESTier/issues/651)).
  Operation parameter and return types that aren't already in the model are
  registered as `ComplexType`, `EntityType` (when keyed), or `EnumType` without
  any manual model-builder work.
- **Optional parameters** (issue [#656](https://github.com/OData/RESTier/issues/656)).
  Four signal sources — `Nullable<T>`, compiler defaults, `[DefaultValue]`, and
  a new `[Optional]` attribute — produce the correct `EdmOptionalParameter`
  shape with the right default literal. The runtime executor substitutes
  declared defaults on URL-omitted parameters; explicit `?p=null` on a nullable
  parameter passes null.
- **Duplicate-name detection** (issue [#652](https://github.com/OData/RESTier/issues/652)).
  Declaring the same operation both manually and via `[Operation]` no longer
  creates a duplicate in the EDM model; the manual registration wins and a
  `Trace.TraceWarning` surfaces the duplicate.
- **`[Obsolete]` annotation**. Method-level `[Obsolete]` now emits
  `Core.V1.Revisions` with `Kind = Deprecated`, round-tripping into OpenAPI's
  `deprecated` field.
- **Parameter-level `[Description]`**. Annotates `EdmOperationParameter` with
  `Core.V1.Description`.

## Breaking change: `OperationContext.GetParameterValueFunc` is now presence-aware

`Microsoft.Restier.Core.Operation.OperationContext.GetParameterValueFunc` changed
from `Func<string, object>` to `Func<string, (bool Present, object Value)>`.
The `Present` flag is `true` when the parameter name appears in the request,
even if the supplied value is `null`. This is required to distinguish
"URL omitted the parameter" from "URL supplied `p=null`" — necessary for
both default substitution and explicit-null semantics on the same parameter.

**Affected:** custom `RestierController` subclasses that construct their own
`getParaValueFunc`, and any code that constructs `OperationContext` directly.

**Migration:** replace `Func<string, object>` with
`Func<string, (bool Present, object Value)>`. For URL/segment parameters,
build the delegate as:

```csharp
Func<string, (bool Present, object Value)> getParaValueFunc = p =>
{
    var match = segment.Parameters.FirstOrDefault(c => c.Name == p);
    return (match is not null, match?.Value);
};
```

Closes the operation-related items of [#750](https://github.com/OData/RESTier/issues/750).
