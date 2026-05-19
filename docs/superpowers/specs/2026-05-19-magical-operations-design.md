# Magical Operations Design: Auto-Registration, De-duplication, Optional Parameters, Annotations

**Date**: 2026-05-19
**Issue**: [OData/RESTier#750](https://github.com/OData/RESTier/issues/750) — umbrella, rolling up #651 (complex-type auto-registration), #652 (duplicate operation entries), #656 (optional parameters)
**Status**: Draft

## Overview

The current `RestierWebApiOperationModelBuilder` discovers methods decorated with `[BoundOperation]` / `[UnboundOperation]`, but it leaves three usability gaps:

1. **#651** — Parameter and return types that are not already in the model (e.g. POCO request/response types) fall through `EdmHelpers.GetTypeReference` as `null`, so users must take over `IModelBuilder` to register them.
2. **#652** — If the same operation is declared both manually (via `ODataModelBuilder.Action`/`Function`) and via an `[Operation]` attribute, no idempotency check exists; the EDM model contains two identical entries.
3. **#656** — Method parameters that are nullable (`int? p = null`), have a C# default value (`int p = 5`), or are marked `[Optional]` are emitted into the EDM model as non-nullable, required parameters, so `?p=null` fails at conversion time.

The umbrella issue also asks for "looping through all the supported annotations." `[Description]` already maps to `Core.V1.Description` on operations (`be817e21`). This spec extends that pattern to `[Obsolete]` → `Core.V1.Revisions` and parameter `[DefaultValue]`.

The goal is "tag a method, model builds itself" — the operation builder should auto-register referenced types, dedupe gracefully, honor parameter optionality, and emit the documentation annotations that come for free from `System.ComponentModel`.

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Complex-type registration approach | Dedicated pre-pass stage driving `ODataConventionModelBuilder` | Keeps the operation builder focused, defers convention work to OData ModelBuilder (enums, nested types, `[Required]`, `[MaxLength]`), single source of truth |
| Pre-pass placement | New `OperationTypeRegistrationModelBuilder` runs between `RestierWebApiModelBuilder` and `RestierWebApiOperationModelBuilder` | Standard `IChainedService<IModelBuilder>` pattern; types are registered before operations look them up |
| Type classification | `[Key]` or `Id`-property convention → `EntityType` (no `EntitySet`); `Enum` → `EnumType`; else `ComplexType` | Mirrors `ODataConventionModelBuilder` heuristics; entity-shaped returns work for keyed operation results |
| Duplicate detection | Match by full signature (namespace+name+ordered parameter types, including binding parameter for bound ops) | Preserves OData's legitimate overload-by-parameter-set; only exact duplicates are suppressed |
| Duplicate behavior | Skip with `Trace.TraceWarning` | Matches the existing warning style at `RestierWebApiOperationModelBuilder.cs:154`; loud enough to fix, doesn't break startup |
| Optional-parameter signals | `ParameterInfo.HasDefaultValue` OR `Nullable<T>` OR `[Optional]` attribute | Covers #656 reporter (B), plus explicit escape hatch for reference-type cases under NRT-disabled compilation |
| Optional default-value source | `[DefaultValue]` > `ParameterInfo.DefaultValue` (compiler-supplied) > `null` for `[Optional]` or `Nullable<T>` | Lets users override compiler defaults; `[DefaultValue]` also handles non-constant defaults; falls back to `null` literal when only optionality is signalled |
| Nullable reference types | Out of scope | Project compiles with NRT disabled (per `CLAUDE.md`); the `[Optional]` attribute covers the escape hatch |
| `[Obsolete]` mapping | Method-level → `Core.V1.Revisions` annotation on `EdmOperation` with `Kind = Deprecated` and `Description = Obsolete.Message` | Round-trips into OpenAPI's `deprecated` field for the existing Swagger/NSwag integration on this branch |
| `[DisplayName]` mapping | Out of scope for this spec | OpenAPI tooling rarely surfaces it; revisit if requested |

## Architecture

### Pipeline order

```
Inner (EF model builder)                  ← entity sets, entity types
    ↓
RestierWebApiModelBuilder                 ← convention-based entity sets / singletons
    ↓
OperationTypeRegistrationModelBuilder     ← NEW: scans [Operation] methods, registers
    ↓                                       missing complex/entity/enum types
RestierWebApiOperationModelBuilder        ← MODIFIED: dedup, optional params,
    ↓                                       [Obsolete] → Core.V1.Revisions
ConventionBasedAnnotationModelBuilder
```

`OperationTypeRegistrationModelBuilder` runs **after** the entity-set extender so that already-registered entity types are visible; it runs **before** the operation builder so types are present when `BuildOperationParameters` resolves type references.

### Pre-pass: `OperationTypeRegistrationModelBuilder`

```csharp
public class OperationTypeRegistrationModelBuilder : IModelBuilder
{
    public IModelBuilder Inner { get; set; }
    public IEdmModel GetEdmModel()
    {
        if (Inner?.GetEdmModel() is not EdmModel model) return Inner?.GetEdmModel();

        var operationMethods = ScanOperationMethods(targetApiType);
        var referencedTypes = CollectReferencedTypes(operationMethods);   // unwrap Nullable<>, IEnumerable<>, arrays
        var missingTypes = referencedTypes.Where(t => model.FindDeclaredType(t.FullName) is null && !t.IsBuiltInPrimitive()).ToList();
        if (missingTypes.Count == 0) return model;

        // Sketch — exact API surface confirmed during implementation.
        var auxBuilder = new ODataConventionModelBuilder();
        foreach (var known in model.SchemaElements.OfType<IEdmSchemaType>())
        {
            var clr = known.GetClrType(model);                            // via existing ClrTypeAnnotation lookup
            if (clr is null) continue;
            auxBuilder.Ignore(clr);                                       // suppress re-emission of types already in inner model
        }
        foreach (var t in missingTypes)
            RegisterByClassification(auxBuilder, t);                       // AddComplexType / AddEntityType / AddEnumType

        var auxModel = auxBuilder.GetEdmModel() as EdmModel;
        foreach (var element in auxModel.SchemaElements)
        {
            if (model.FindDeclaredType(element.FullName()) is null && element is IEdmSchemaElement)
                model.AddElement(element);
        }
        return model;
    }
}
```

Key behaviors:

- **Type discovery**: same predicate as `RestierWebApiOperationModelBuilder.ScanForOperations` (any `OperationAttribute`-decorated method, excluding `IsSpecialName` and `System.Object` methods). Type collection unwraps `Nullable<>`, `IEnumerable<>`, `IQueryable<>`, arrays, and recurses into the type's CLR properties (the convention builder will do the recursion once `AddComplexType` is called on a root type).
- **Classification**:
  - `type.IsEnum` → `AddEnumType`
  - Any public property named `Id` or decorated with `[Key]` → `AddEntityType` (registers the type but no `EntitySet`; an operation can return a keyed type without exposing it as a CRUD resource)
  - Otherwise → `AddComplexType`
- **Pre-Ignore of known types**: prevents the auxiliary `ODataConventionModelBuilder` from re-emitting types that the inner builder already produced (which would otherwise collide on `AddElement`).
- **Cycle safety**: delegated to `ODataConventionModelBuilder`'s own cycle detection.

### Operation builder changes (`RestierWebApiOperationModelBuilder`)

#### De-duplication (`BuildOperations`)

Before `model.AddElement(operation)`:

```csharp
var signature = BuildSignatureKey(namespaceName, operationInfo.Name, operationInfo.Method, isBound);
var existing = model.SchemaElements.OfType<IEdmOperation>()
    .FirstOrDefault(op => BuildSignatureKey(op) == signature);
if (existing is not null)
{
    Trace.TraceWarning($"Restier: Operation '{namespaceName}.{operationInfo.Name}' is already declared with the same signature. " +
                       $"Skipping the duplicate registration from [Operation] attribute. " +
                       $"Remove the manual ModelBuilder registration or the [Operation] attribute to silence this warning.");
    continue;
}
```

`BuildSignatureKey(IEdmOperation)` formats as `Namespace.Name(BindingTypeFullName,ParamTypeFullName,...)`. For unbound operations the binding-type slot is empty. Action/Function import duplicates are similarly checked against `entityContainer.Elements` (action imports and function imports use distinct EDM lookups).

#### Parameter optionality (`BuildOperationParameters`)

```csharp
foreach (var parameter in method.GetParameters())
{
    var (isOptional, defaultValueLiteral) = ClassifyParameter(parameter);
    var underlyingType = TypeHelper.GetUnderlyingTypeOrSelf(parameter.ParameterType);
    var typeRef = underlyingType.GetTypeReference(model, nullable: isOptional || IsNullable(parameter.ParameterType));

    EdmOperationParameter edmParam = isOptional
        ? new EdmOptionalParameter(operation, parameter.Name, typeRef, defaultValueLiteral)
        : new EdmOperationParameter(operation, parameter.Name, typeRef);

    EmitParameterAnnotations(model, edmParam, parameter);   // [Description], [Obsolete]
    operation.AddParameter(edmParam);
}
```

`ClassifyParameter` order of precedence:

1. `parameter.GetCustomAttribute<DefaultValueAttribute>()?.Value` → optional with that literal.
2. `parameter.HasDefaultValue` → optional with `parameter.DefaultValue.ToInvariantString()`.
3. `parameter.GetCustomAttribute<OptionalAttribute>() is not null` → optional with `null` literal.
4. `Nullable.GetUnderlyingType(parameter.ParameterType) is not null` → optional with `null` literal.
5. Otherwise → required.

`EdmHelpers.GetTypeReference` gains a `bool nullable = true` overload so the builder can pass `false` for non-optional value types.

#### Annotations (`BuildOperations`)

After `model.AddElement(operation)`:

```csharp
EmitOperationAnnotations(model, operation, operationInfo.Method);

static void EmitOperationAnnotations(EdmModel model, EdmOperation op, MethodInfo method)
{
    var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
    if (!string.IsNullOrWhiteSpace(description))
        model.AddVocabularyAnnotation(new EdmVocabularyAnnotation(op, CoreVocabularyModel.DescriptionTerm, new EdmStringConstant(description)));

    var obsolete = method.GetCustomAttribute<ObsoleteAttribute>();
    if (obsolete is not null)
        model.AddVocabularyAnnotation(BuildRevisionsAnnotation(op, obsolete));
}
```

`BuildRevisionsAnnotation` constructs a single-entry `Collection` of `Core.V1.RevisionType` records with `Version = "obsolete"`, `Kind = Deprecated`, `Description = obsolete.Message ?? "Deprecated."`. The literal `"obsolete"` version string is a convention placeholder when no semantic version is supplied (consistent with how the Microsoft.OData.ModelBuilder emits revisions from `[Obsolete]` in convention mode).

`EmitParameterAnnotations` does the analogous work for `EdmOperationParameter` with the same Description/Revisions terms.

### New file: `OptionalAttribute`

```csharp
namespace Microsoft.Restier.AspNetCore.Model;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class OptionalAttribute : Attribute { }
```

A no-property marker. Distinct from `System.Runtime.InteropServices.OptionalAttribute` (we want a RESTier-owned, OData-shaped concept and to avoid pulling COM semantics into the model builder).

### DI wiring

`src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs`, in both `AddRestierRoute` paths around lines 121–124 and 171–175, insert the new builder between `RestierWebApiModelBuilder` and `RestierWebApiOperationModelBuilder`:

```csharp
services.AddSingleton<IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
    .AddSingleton(new RestierWebApiModelExtender(type))
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new OperationTypeRegistrationModelBuilder(type))   // NEW
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new RestierWebApiOperationModelBuilder(type, sp.GetRequiredService<RestierWebApiModelExtender>()))
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new ConventionBasedAnnotationModelBuilder(type));
```

The chain factory composes these by registration order; the operation builder receives the augmented model from the new stage via its `Inner` property.

## Component Changes

### `src/Microsoft.Restier.AspNetCore`

| File | Change |
|------|--------|
| `Model/OptionalAttribute.cs` | **NEW** — parameter-level marker |
| `Model/ApiExtension/OperationTypeRegistrationModelBuilder.cs` | **NEW** — pre-pass that auto-registers types referenced by `[Operation]` methods |
| `Model/ApiExtension/RestierWebApiOperationModelBuilder.cs` | **MODIFY** — dedup-by-signature with Trace warning; classify+emit optional parameters; emit `Core.V1.Revisions` from `[Obsolete]`; emit parameter-level `Core.V1.Description` |
| `Model/EdmHelpers.cs` | **MODIFY** — add `GetTypeReference(this Type, IEdmModel, bool nullable)` overload so value-type parameters can be emitted as non-nullable when not optional |
| `Extensions/RestierODataOptionsExtensions.cs` | **MODIFY** — register `OperationTypeRegistrationModelBuilder` between web-api model builder and operation model builder (both `AddRestierRoute` paths) |

### `test/Microsoft.Restier.Tests.AspNetCore`

| File | Change |
|------|--------|
| `Model/RestierWebApiOperationModelBuilderTests.cs` | **MODIFY** — add: dedup-with-warning emits trace, optional-via-default-value, optional-via-nullable, optional-via-`[Optional]`, `[Obsolete]` → `Core.V1.Revisions`, `[DefaultValue]` overrides compiler default, value-type parameter is non-nullable when not optional |
| `Model/OperationTypeRegistrationModelBuilderTests.cs` | **NEW** — complex-type auto-register from operation parameter, complex-type auto-register from operation return, nested complex recursion, enum auto-register, entity-type detection by `[Key]`, no-op when no missing types, already-registered type not duplicated |
| `FeatureTests/MagicalOperationsTests.cs` | **NEW** — Breakdance HTTP scenarios: `GET /Api/Query(parameter1=null)` returns 200 on `int?` (literal #656 repro); `GET /Api/$metadata` contains `Core.V1.Description` and `Core.V1.Revisions` annotations; manual+attribute duplicate produces exactly one model entry and one trace warning; operation returning a custom POCO works without manual ComplexType registration (literal #651 repro) |

### Documentation

A short follow-up to `src/Microsoft.Restier.Docs/guides/server/operations.mdx` covering: complex types are auto-registered, optional parameters, `[Description]` / `[Obsolete]`, and the de-duplication warning. Out of scope for the code spec but called out here so the implementation plan can include it.

## Testing Strategy

Unit (xUnit + FluentAssertions + NSubstitute, following `RestierWebApiOperationModelBuilderTests.cs` style):

- `OperationTypeRegistrationModelBuilder`:
  - Returns `null` when `Inner.GetEdmModel()` is `null`
  - Returns the inner model unchanged when no `[Operation]` methods exist
  - Registers a missing class referenced by an operation parameter as `EdmComplexType`
  - Registers a missing class with `[Key]`/`Id` as `EdmEntityType` without an `EntitySet`
  - Registers an `enum` type as `EdmEnumType`
  - Does not re-register a type already declared on the inner model
  - Recurses: registers a nested complex type referenced only through another complex type's property
- `RestierWebApiOperationModelBuilder` (extending existing test class):
  - Dedup: when `model` already contains an operation with the matching signature, skip and emit `Trace.TraceWarning` containing the operation name
  - Dedup: same name, different parameter types → both kept (legitimate overload)
  - Optional via `int p = 5` → `EdmOptionalParameter` with `DefaultValue = "5"`
  - Optional via `int? p` → `EdmOptionalParameter` with `DefaultValue = "null"` and type ref `Nullable = true`
  - Optional via `[Optional] int p` → `EdmOptionalParameter` with `DefaultValue = "null"`
  - `[DefaultValue("foo")] string p` → `EdmOptionalParameter` with `DefaultValue = "foo"`
  - `[Obsolete("Use Bar instead.")]` → vocabulary annotation with `Core.V1.Revisions` term, `Description = "Use Bar instead."`
  - Plain `int p` (no defaults, no attribute) → `EdmOperationParameter` with type ref `Nullable = false`

HTTP integration (Breakdance, following `FeatureTests/DeepInsertTests.cs` style):

- A `[UnboundOperation]` taking `int? parameter1 = null` responds 200 to `Query(parameter1=null)` (the literal #656 repro)
- `$metadata` contains the expected `Annotation Term="Core.V1.Description"` and `Annotation Term="Core.V1.Revisions"` markup
- Registering the same `[UnboundOperation]` and also calling `ODataModelBuilder.Function(...)` for it produces exactly one `OperationImport` in `$metadata`
- A `[UnboundOperation]` whose parameter and return are custom POCOs (no manual `ComplexType` registration) responds correctly and emits both types in `$metadata`

## Risks & Open Questions

- **Aux builder type re-emission**: pre-Ignoring every already-declared type on `ODataConventionModelBuilder` requires CLR-type resolution from the inner `EdmModel`. RESTier already retrieves CLR types via `ClrTypeAnnotation` (see `EdmHelpers.GetClrType`). If a known schema element has no `ClrTypeAnnotation`, fall back to skipping its registration; the merge step also guards with `model.FindDeclaredType(...) is null` so a collision is impossible.
- **`ODataConventionModelBuilder.Ignore` API shape**: the exact method/property used to suppress emission of a known type on the auxiliary builder will be confirmed during implementation against the version of `Microsoft.OData.ModelBuilder` pinned by the solution. If the literal `Ignore(...)` shape differs (e.g. requires per-property ignore), the implementation plan should call that out and adjust.
- **Bound operations referencing types not on any `EntitySet`**: registering a missing keyed type as `EntityType` (no `EntitySet`) is sufficient for the operation builder but means that type is not directly queryable. That's the existing OData behavior; documented in the operations guide.
- **`[Obsolete]` term Version string**: the OData `Core.V1.Revisions` schema requires a `Version` field; the literal `"obsolete"` is a convention placeholder. If a future iteration wants real semver tracking, that would be a separate spec.
- **Optional parameter type nullability**: When a parameter is optional and its CLR type is a non-nullable value type (e.g. `int p = 5`), the EDM type ref must still be nullable per OData's representation of optional parameters with default values. Confirmed against `EdmOptionalParameter` semantics; the test for "Optional via `int p = 5`" verifies this.

## Out of Scope (Deferred)

- `[DisplayName]` → `Core.V1.LongDescription` mapping
- Nullable reference types (`string?`) as an optional signal — requires reading `NullableAttribute` byte arrays; the `[Optional]` escape hatch is sufficient under NRT-disabled compilation
- `[Action]` / `[Function]` attribute aliases replacing the `OperationType` enum (separate UX-only refactor)
- Auto-registration of types referenced only by entity sets (`RestierWebApiModelBuilder` already handles that path)
- Composable function inference from method signature (separate "operation composition" topic)
