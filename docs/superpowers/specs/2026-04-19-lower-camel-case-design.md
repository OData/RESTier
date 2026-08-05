# Lower camelCase JSON Property Naming Support in Restier

**Date:** 2026-04-19
**Status:** Design approved
**GitHub Issue:** https://github.com/OData/RESTier/issues/549

## Goal

Enable opt-in lower camelCase JSON property naming for Restier APIs, so that JSON payloads use `firstName` instead of `FirstName`. This is configured per-route via a new `RestierNamingConvention` enum, and applies consistently across `$metadata`, JSON serialization/deserialization, and OData query options (`$filter`, `$select`, `$expand`, `$orderby`).

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Scope | Per-route configuration | Restier supports multiple APIs per host; casing is a per-model decision |
| Mechanism | `ODataConventionModelBuilder.EnableLowerCamelCase()` | Standard OData approach; consistent across $metadata, JSON, and URLs |
| API surface | Enum parameter on `AddRestierRoute` | Simple, extensible, backward-compatible with default `PascalCase` |
| Granularity | Three levels: off / properties / properties+enums | Covers common needs without exposing raw `NameResolverOptions` flags |
| EDM-to-CLR mapping | Central utility using `ClrPropertyInfoAnnotation` | Reusable, safe to call unconditionally, works for both conventions |
| Property dictionary normalization | At creation boundary in `CreatePropertyDictionary` | Keeps submit pipeline (EFChangeSetInitializer) unchanged |

## Background

RESTier currently outputs JSON with PascalCase property names (e.g. `FirstName`, `Title`) because the EDM model is built directly from CLR type definitions via `ODataConventionModelBuilder` without any naming transformation. JSON APIs conventionally use lower camelCase (`firstName`, `title`).

The upstream `ODataConventionModelBuilder` (from `Microsoft.OData.ModelBuilder` 2.x) already supports `EnableLowerCamelCase()`, which:
1. Transforms EDM property names to lower camelCase during model building
2. Annotates each EDM property with `ClrPropertyInfoAnnotation` mapping back to the original CLR `PropertyInfo`
3. The OData query infrastructure (`$filter`, `$select`, etc.) already uses these annotations

However, Restier has several places that assume EDM property names match CLR property names. These must be fixed to support the mapping.

## Architecture

### Configuration Flow

```
AddRestierRoute<TApi>(routePrefix, configureServices, namingConvention: LowerCamelCase)
    |
    v
Register RestierNamingConvention in model-building DI container
    |
    v
EFModelBuilder resolves RestierNamingConvention from DI
    |
    v
ODataConventionModelBuilder.EnableLowerCamelCase() called before GetEdmModel()
    |
    v
EDM model has camelCase property names + ClrPropertyInfoAnnotation on each property
    |
    v
Register RestierNamingConvention in route DI container (for runtime use)
```

### New Types

**`RestierNamingConvention`** enum in `Microsoft.Restier.Core`:

```csharp
public enum RestierNamingConvention
{
    PascalCase = 0,
    LowerCamelCase = 1,
    LowerCamelCaseWithEnumMembers = 2,
}
```

**`EdmClrPropertyMapper`** internal static class in `Microsoft.Restier.AspNetCore`:

```csharp
internal static class EdmClrPropertyMapper
{
    public static string GetClrPropertyName(IEdmProperty edmProperty, IEdmModel model)
    {
        var annotation = model.GetAnnotationValue<ClrPropertyInfoAnnotation>(edmProperty);
        return annotation?.ClrPropertyInfo?.Name ?? edmProperty.Name;
    }
}
```

When `EnableLowerCamelCase()` has been called, the annotation maps e.g. `firstName` -> `PropertyInfo { Name = "FirstName" }`. When it hasn't, no annotation exists and the fallback is the EDM name (which already matches CLR). This is safe to call unconditionally.

### Modified API Surface

**All `AddRestierRoute` overloads** gain a new optional parameter. Both the prefixless overload (line 43) and the routePrefix overload (line 58) must be updated, as well as the private `AddRestierRoute` helper (line 86):

```csharp
// Prefixless overload
public static ODataOptions AddRestierRoute<TApi>(
    this ODataOptions oDataOptions,
    Action<IServiceCollection> configureRouteServices,
    bool useRestierBatching = true,
    RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
    where TApi : ApiBase

// Prefix overload
public static ODataOptions AddRestierRoute<TApi>(
    this ODataOptions oDataOptions,
    string routePrefix,
    Action<IServiceCollection> configureRouteServices,
    bool useRestierBatching = true,
    RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
    where TApi : ApiBase

// Private helper (receives the value from both public overloads)
private static ODataOptions AddRestierRoute(
    ODataOptions oDataOptions,
    Type type, string routePrefix,
    Action<IServiceCollection> configureRouteServices,
    bool useRestierBatching,
    RestierNamingConvention namingConvention)
```

The naming convention is registered in both DI containers:
- Model-building container (used by `EFModelBuilder` during startup)
- Route container (available at runtime for property name resolution)

### Model Building Changes

**`EFModelBuilder<TDbContext>`** (`Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs`):

Constructor gains an optional `RestierNamingConvention` parameter (defaults to `PascalCase` if not registered in DI). `GetEdmModel()` passes it to `BuildEdmModelFromEntitySetMaps()`.

In `BuildEdmModelFromEntitySetMaps()`, after registering entity sets and keys but before `builder.GetEdmModel()`:

```csharp
switch (namingConvention)
{
    case RestierNamingConvention.LowerCamelCase:
        builder.EnableLowerCamelCase();
        break;
    case RestierNamingConvention.LowerCamelCaseWithEnumMembers:
        builder.EnableLowerCamelCaseForPropertiesAndEnums();
        break;
}
```

### Query Builder Fixes

**`RestierQueryBuilder`** (`Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs`) has four places that use `Expression.Property(parameterExpression, edmPropertyName)`. Each must resolve the CLR property name via `EdmClrPropertyMapper`:

1. **`HandleNavigationPathSegment`** (line 211):
   ```csharp
   // Before:
   Expression.Property(entityParameterExpression, navigationSegment.NavigationProperty.Name)
   // After:
   Expression.Property(entityParameterExpression,
       EdmClrPropertyMapper.GetClrPropertyName(navigationSegment.NavigationProperty, edmModel))
   ```

2. **`HandlePropertyAccessPathSegment`** (line 247):
   ```csharp
   // Before:
   Expression.Property(entityParameterExpression, propertySegment.Property.Name)
   // After:
   Expression.Property(entityParameterExpression,
       EdmClrPropertyMapper.GetClrPropertyName(propertySegment.Property, edmModel))
   ```

3. **`HandleKeyValuePathSegment`** (line 192-199): Key property names from `KeySegment.Keys` are EDM names. Resolve each to CLR name before passing to `CreateEqualsExpression`. The entity type is available from the key segment's `EdmType`.

4. **`GetPathKeyValues`** (static, line 122-138): Returns key names from `KeySegment.Keys` that flow into `DataModificationItem.ResourceKey`. This method needs access to the `IEdmModel` to resolve CLR names. Change signature to accept the model, and resolve key names. Callers (`RestierController`) already have access to the model.

### Property Dictionary Normalization

**`Extensions.CreatePropertyDictionary()`** (`Microsoft.Restier.AspNetCore/Extensions/Extensions.cs`, line 92-129):

When iterating `entity.GetChangedPropertyNames()`, resolve each EDM property name to CLR before adding to the dictionary:

```csharp
foreach (var propertyName in entity.GetChangedPropertyNames())
{
    // Resolve EDM property name to CLR property name
    var edmProperty = edmType.FindProperty(propertyName);
    var clrPropertyName = edmProperty is not null
        ? EdmClrPropertyMapper.GetClrPropertyName(edmProperty, api.Model)
        : propertyName;

    // ... existing attribute checking uses clrPropertyName ...

    if (entity.TryGetPropertyValue(propertyName, out var value))
    {
        // ... existing value processing ...
        propertyValues.Add(clrPropertyName, value);
    }
}
```

**`Extensions.RetrievePropertiesAttributes()`** (line 137-192): Uses `property.Name` as dictionary keys. These must also use CLR names so they match the normalized property dictionary keys. Apply the same `EdmClrPropertyMapper.GetClrPropertyName()` call.

This normalization means `DataModificationItem.LocalValues` and `DataModificationItem.ResourceKey` always contain CLR property names, so `EFChangeSetInitializer.SetValues()` works unchanged.

### ETag / OriginalValues Normalization

**`RestierController.GetOriginalValues()`** (`RestierController.cs`, line 657-689) copies ETag concurrency properties via `etag.ApplyTo(originalValues)`. Under camelCase EDM, the ETag property names are EDM names (camelCase), but `DataModificationItem.ValidateEtag()` (`ChangeSetItem.cs`, line 258-293) calls `ApplyPredicate()` which uses `Expression.Property(param, item.Key)` at line 304 - requiring CLR property names.

Without normalization, concurrency-enabled PATCH/PUT/DELETE will fail because ETag keys like `rowVersion` won't match CLR property `RowVersion`.

**Fix:** Normalize the OriginalValues dictionary in the controller after `etag.ApplyTo()` returns, before constructing the `DataModificationItem`. The controller already has access to the model and entity type:

```csharp
private IReadOnlyDictionary<string, object> GetOriginalValues(IEdmEntitySet entitySet)
{
    var originalValues = new Dictionary<string, object>();
    // ... existing ETag extraction ...

    // Normalize EDM property names to CLR property names
    return NormalizePropertyNames(originalValues, entitySet.EntityType, api.Model);
}

private static IReadOnlyDictionary<string, object> NormalizePropertyNames(
    Dictionary<string, object> values, IEdmStructuredType edmType, IEdmModel model)
{
    var normalized = new Dictionary<string, object>(values.Count);
    foreach (var kvp in values)
    {
        if (kvp.Key.StartsWith("@", StringComparison.Ordinal))
        {
            // Preserve internal keys like @IfMatchKey, @IfNoneMatchKey
            normalized.Add(kvp.Key, kvp.Value);
            continue;
        }
        var edmProperty = edmType.FindProperty(kvp.Key);
        var clrName = edmProperty is not null
            ? EdmClrPropertyMapper.GetClrPropertyName(edmProperty, model)
            : kvp.Key;
        normalized.Add(clrName, kvp.Value);
    }
    return normalized;
}
```

This ensures `DataModificationItem.OriginalValues` always uses CLR property names, so `ValidateEtag()` -> `ApplyPredicate()` -> `Expression.Property()` works correctly.

### What Doesn't Need Changes

| Component | Reason |
|-----------|--------|
| Custom serializers (`RestierResourceSerializer`, etc.) | Delegate to OData base classes which use EDM model correctly |
| Custom deserializers (`RestierEnumDeserializer`, etc.) | Same - OData handles EDM-to-CLR mapping |
| `ConventionBasedMethodNameFactory` | Uses entity set/type names, not property names; `EnableLowerCamelCase()` doesn't change these |
| `RestierWebApiModelExtender` | Works with entity set/singleton names from API class CLR properties |
| `RestierWebApiOperationModelBuilder` | Operation names come from CLR method names |
| OData query processing (`$filter`, `$select`, etc.) | `Microsoft.AspNetCore.OData` already uses `ClrPropertyInfoAnnotation` |
| `EFChangeSetInitializer.SetValues()` | Property dictionary keys are normalized to CLR names at creation |
| `EFSubmitExecutor` | Just calls `DbContext.SaveChangesAsync()` |
| `RestierPayloadValueConverter` | Converts value types, not property names |
| `DeserializationHelpers` | Converts OData values to CLR types, not property names |

## Testing Strategy

### Unit Tests

**`EdmClrPropertyMapperTests`** in `Microsoft.Restier.Tests.AspNetCore` (the mapper is internal to `Microsoft.Restier.AspNetCore`, which exposes internals to this test project):
- Returns EDM property name when no `ClrPropertyInfoAnnotation` exists (PascalCase model)
- Returns CLR property name when annotation exists (camelCase model)
- Handles null/missing annotation gracefully

### Integration Tests

**`NamingConventionTests<TApi, TContext>`** abstract class in `Microsoft.Restier.Tests.AspNetCore/FeatureTests/` with concrete EFCore implementation using the existing `LibraryApi`/`LibraryContext` infrastructure configured with `RestierNamingConvention.LowerCamelCase`.

**Serialization (GET):**
- GET entity set returns camelCase property names in JSON response body
- GET single entity returns camelCase property names
- GET `$metadata` shows camelCase property names in EDM
- GET with `$select=title` works (camelCase in query option)
- GET with `$filter=title eq 'value'` works
- GET with `$expand=publisher` works (camelCase navigation property)
- GET with `$orderby=title` works

**Deserialization (POST/PATCH/PUT):**
- POST with camelCase JSON payload creates entity successfully
- PATCH with camelCase JSON payload updates entity successfully
- PUT with camelCase JSON payload replaces entity successfully

**Key handling:**
- GET by key (`/Books(1)`) works
- DELETE by key works

**Concurrency (ETag):**
- PATCH with If-Match ETag header on concurrency-enabled entity works with camelCase
- PUT with If-Match ETag header works with camelCase
- DELETE with If-Match ETag header works with camelCase

**Enum members (with `LowerCamelCaseWithEnumMembers`):**
- Enum values in response are camelCase
- POST/PATCH with camelCase enum values in payload deserializes correctly

**Backward compatibility:**
- Default configuration (no naming convention specified) uses PascalCase (existing tests cover this implicitly)

### Test Infrastructure

`RestierTestHelpers.GetTestBaseInstance<TApi>()` and `ExecuteTestRequest<TApi>()` gain an optional `RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase` parameter. This is passed through to the `AddRestierRoute` call inside `GetTestBaseInstance` (line 400). This ensures tests exercise the public route-level API rather than a DI backdoor:

```csharp
public static async Task<HttpResponseMessage> ExecuteTestRequest<TApi>(
    HttpMethod httpMethod,
    // ... existing parameters ...
    RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase,
    // ... existing parameters ...
    ) where TApi : ApiBase

public static RestierBreakdanceTestBase<TApi> GetTestBaseInstance<TApi>(
    string routeName = WebApiConstants.RouteName,
    string routePrefix = WebApiConstants.RoutePrefix,
    Action<IServiceCollection> apiServiceCollection = default,
    RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
    where TApi : ApiBase
```

Inside `GetTestBaseInstance`, the call becomes:
```csharp
odataOptions.AddRestierRoute<TApi>(routeName, restierServices => { ... },
    namingConvention: namingConvention);
```

## Scope Clarifications

**Non-EF model builders:** The `RestierNamingConvention` enum is registered in DI and accessible to any `IModelBuilder` implementation. However, the automatic `EnableLowerCamelCase()` call only happens in `EFModelBuilder`, which is the only built-in model builder that uses `ODataConventionModelBuilder`. Custom `IModelBuilder` implementations that build EDM models directly (without `ODataConventionModelBuilder`) would need to handle naming conventions themselves. This is acceptable since custom model builders are an advanced scenario where the developer already controls property naming.

**Enum member deserialization:** When `LowerCamelCaseWithEnumMembers` is used, both serialization and deserialization handle camelCase enum values. The `ODataConventionModelBuilder.EnableLowerCamelCaseForPropertiesAndEnums()` transforms enum member names in the EDM model itself, and OData's deserialization matches incoming values against EDM enum member names. This is bidirectional by design.

## File Change Summary

| File | Change Type | Description |
|------|-------------|-------------|
| `src/Microsoft.Restier.Core/RestierNamingConvention.cs` | **New** | Enum definition |
| `src/Microsoft.Restier.AspNetCore/EdmClrPropertyMapper.cs` | **New** | EDM-to-CLR property name mapping utility |
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` | Modified | New parameter on all `AddRestierRoute` overloads + private helper, register in DI |
| `src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs` | Modified | Inject naming convention, call `EnableLowerCamelCase()` |
| `src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs` | Modified | Use `EdmClrPropertyMapper` for LINQ expression property access |
| `src/Microsoft.Restier.AspNetCore/Extensions/Extensions.cs` | Modified | Normalize property dict keys to CLR names |
| `src/Microsoft.Restier.AspNetCore/RestierController.cs` | Modified | Pass model to `GetPathKeyValues`, normalize OriginalValues from ETag |
| `src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs` | Modified | Optional naming convention parameter on `ExecuteTestRequest` and `GetTestBaseInstance` |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/NamingConventionTests.cs` | **New** | Abstract integration tests |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/NamingConventionTests.cs` | **New** | Concrete EFCore integration tests |
| `test/Microsoft.Restier.Tests.AspNetCore/EdmClrPropertyMapperTests.cs` | **New** | Unit tests for mapper |
