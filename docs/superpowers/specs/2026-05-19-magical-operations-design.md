# Magical Operations Design: Auto-Registration, De-duplication, Optional Parameters, Annotations

**Date**: 2026-05-19
**Issue**: [OData/RESTier#750](https://github.com/OData/RESTier/issues/750) — umbrella, rolling up #651 (complex-type auto-registration), #652 (duplicate operation entries), #656 (optional parameters)
**Status**: Draft

## Overview

The current `RestierWebApiOperationModelBuilder` discovers methods decorated with `[BoundOperation]` / `[UnboundOperation]`, but it leaves three usability gaps:

1. **#651** — Parameter and return types that are not already in the model (e.g. POCO request/response types) fall through `EdmHelpers.GetTypeReference` as `null`, so users must take over `IModelBuilder` to register them.
2. **#652** — If the same operation is declared both manually (via `ODataModelBuilder.Action`/`Function`) and via an `[Operation]` attribute, no idempotency check exists; the EDM model contains two identical entries.
3. **#656** — Method parameters that are nullable (`int?`), have a C# default value (`int p = 5`), or are marked `[Optional]` are emitted into the EDM model as non-nullable, required parameters, so `?p=null` fails at conversion time.

The umbrella issue also asks for "looping through all the supported annotations." `[Description]` already maps to `Core.V1.Description` on operations (`be817e21`). This spec extends that pattern to `[Obsolete]` → `Core.V1.Revisions`. The `[DefaultValue]` attribute is also consumed in this spec — not as a vocabulary annotation, but as a source for `EdmOptionalParameter.DefaultValue` literals (see Optional parameters below).

The goal is "tag a method, model builds itself" — the operation builder should auto-register referenced types, dedupe gracefully, honor parameter optionality, and emit the documentation annotations that come for free from `System.ComponentModel`.

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Complex-type registration approach | Dedicated pre-pass stage driving `ODataConventionModelBuilder` | Keeps the operation builder focused, defers convention work to OData ModelBuilder (enums, nested types, `[Required]`, `[MaxLength]`), single source of truth |
| Pre-pass placement | New `OperationTypeRegistrationModelBuilder` runs between `RestierWebApiModelBuilder` and `RestierWebApiOperationModelBuilder` | Standard `IChainedService<IModelBuilder>` pattern; types are registered before operations look them up |
| Type classification | `[Key]` or `Id`-property convention → `EntityType` (no `EntitySet`); `Enum` → `EnumType`; else `ComplexType` | Mirrors `ODataConventionModelBuilder` heuristics; entity-shaped returns work for keyed operation results |
| Duplicate detection | Match by **namespace + name** alone (not signature) | `RestierOperationExecutor` resolves operations by name via `GetMethod(name, BindingFlags...)` with no type array (see `RestierOperationExecutor.cs:78-80` and the comment at `:76`). Any same-name pair the model accepted would either be unreachable or trigger `AmbiguousMatchException` at dispatch. Overload-by-parameter-set support would require a separate spec that also updates the executor. |
| Duplicate behavior | First-registration wins; subsequent registration skipped with `Trace.TraceWarning` | Matches the existing warning style at `RestierWebApiOperationModelBuilder.cs:154`; loud enough to fix, doesn't break startup. Manual `ODataModelBuilder` registration in `Inner` runs first and therefore wins; attribute-driven adds are suppressed with the warning. |
| Type-ref nullability (accept `null` as a value) | `Nullable<T>` OR `[Optional]` | The literal #656 repro is `?p=null` on `int?` — that's nullability, not omittability. Emit the EDM type reference with `Nullable = true`. |
| EDM optional parameter (omittable from URL) | `ParameterInfo.HasDefaultValue` OR `[DefaultValue]` OR `[Optional]` | These are the only signals that imply "user may leave it out." A pure `Nullable<T>` with no default does NOT make the parameter omittable — `Foo(int? p)` is still a required positional CLR argument. |
| Optional default-value source | `[DefaultValue]` > `ParameterInfo.DefaultValue` (compiler-supplied) > `null` literal for `[Optional]` alone | Lets users override compiler defaults; `[DefaultValue]` also handles non-constant defaults; `null` fallback only applies when omittability is signalled by `[Optional]` and the param type accepts null |
| Runtime handling of omitted optional params | Extend `RestierOperationExecutor` to fill `parameters[i]` with the parameter's resolved default when `GetParameterValueFunc(name)` returns null | `MethodInfo.Invoke` does not honor C# compile-time defaults (those are a compiler call-site feature, not a reflection feature). Without this, omitting an optional param would call the method with `null` / `default(T)`, not the declared default. |
| Nullable reference types | Out of scope | Project compiles with NRT disabled (per `CLAUDE.md`); the `[Optional]` attribute covers the escape hatch |
| `[Obsolete]` mapping | Method-level → `Core.V1.Revisions` annotation on `EdmOperation` with `Kind = Deprecated` and `Description = Obsolete.Message` | Round-trips into OpenAPI's `deprecated` field for the existing Swagger/NSwag integration on this branch |
| `[DisplayName]` mapping | Out of scope for this spec | OpenAPI tooling rarely surfaces it; revisit if requested |

## Architecture

### Pipeline order

Model-build chain:

```
Inner (EF model builder)                  ← entity sets, entity types
    ↓
RestierWebApiModelBuilder                 ← convention-based entity sets / singletons
    ↓
OperationTypeRegistrationModelBuilder     ← NEW: scans [Operation] methods, registers
    ↓                                       missing complex/entity/enum types
RestierWebApiOperationModelBuilder        ← MODIFIED: dedup by namespace+name,
    ↓                                       nullability/optionality split, annotations
ConventionBasedAnnotationModelBuilder
```

`OperationTypeRegistrationModelBuilder` runs **after** the entity-set extender so that already-registered entity types are visible; it runs **before** the operation builder so types are present when `BuildOperationParameters` resolves type references.

Runtime-side, `RestierOperationExecutor` is extended to honor optional-parameter defaults during reflective invocation (see Runtime section below) — this is a separate concern from model building but conceptually paired with the optionality work.

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
            // Read ClrTypeAnnotation directly: EdmHelpers.GetClrType throws when annotation is missing,
            // so we must NOT call it here. Schema types that lack the annotation (e.g. types added by
            // a custom ODataModelBuilder without RESTier conventions) are simply skipped — the final
            // merge step guards collisions with `model.FindDeclaredType(...) is null`.
            var clr = model.GetAnnotationValue<ClrTypeAnnotation>(known)?.ClrType;
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
var alreadyDeclared = model.SchemaElements.OfType<IEdmOperation>()
    .Any(op => op.Namespace == namespaceName && op.Name == operationInfo.Name);
if (alreadyDeclared)
{
    Trace.TraceWarning($"Restier: An operation named '{namespaceName}.{operationInfo.Name}' is already declared on the model " +
                       $"(likely via a custom ODataModelBuilder registration). Skipping the duplicate registration from " +
                       $"[Operation] attribute. Remove either the manual registration or the [Operation] attribute to silence this warning. " +
                       $"Note: same-name overloads are not supported by RestierOperationExecutor (resolves by name only).");
    continue;
}
```

The match is on `namespace + name` alone — the runtime executor cannot dispatch overloads (see Design Decisions table), so accepting a same-name pair would create unreachable metadata. The unbound case additionally guards against duplicate `OperationImport` entries on the entity container by checking `entityContainer.Elements.OfType<IEdmOperationImport>().Any(...)` with the same name match.

#### Parameter classification (`BuildOperationParameters`)

Nullability and optionality are independent signals — see Design Decisions. They are computed separately:

```csharp
foreach (var parameter in method.GetParameters())
{
    var underlyingType = TypeHelper.GetUnderlyingTypeOrSelf(parameter.ParameterType);
    var isNullableInModel = ComputeNullable(parameter);    // Nullable<T> OR [Optional] OR class type
    var (isOptional, defaultValueLiteral) = ClassifyOptionality(parameter);

    var typeRef = underlyingType.GetTypeReference(model, nullable: isNullableInModel);

    EdmOperationParameter edmParam = isOptional
        ? new EdmOptionalParameter(operation, parameter.Name, typeRef, defaultValueLiteral)
        : new EdmOperationParameter(operation, parameter.Name, typeRef);

    EmitParameterAnnotations(model, edmParam, parameter);   // [Description], [Obsolete]
    operation.AddParameter(edmParam);
}
```

`ComputeNullable(parameter)` returns true when:
- `Nullable.GetUnderlyingType(parameter.ParameterType) is not null`, OR
- `parameter.GetCustomAttribute<OptionalAttribute>() is not null`, OR
- `parameter.ParameterType.IsClass` (matches existing OData ModelBuilder behavior for reference types).

`ClassifyOptionality(parameter)` returns `(isOptional, defaultLiteral)` by checking in order:

1. `parameter.GetCustomAttribute<DefaultValueAttribute>()?.Value` → `(true, value.ToInvariantString())`.
2. `parameter.HasDefaultValue` → `(true, parameter.DefaultValue.ToInvariantString())`.
3. `parameter.GetCustomAttribute<OptionalAttribute>() is not null` → `(true, "null")`. Requires the parameter type to be nullable; otherwise the builder emits a `Trace.TraceWarning` and treats it as required.
4. Otherwise → `(false, null)`.

A bare `Nullable<T>` with no default and no `[Optional]` produces `isOptional = false` but `isNullableInModel = true` — the model accepts `?p=null` (the #656 literal repro) but the URL must still mention `p`. This separation matches OData's two-axis representation and avoids changing the contract for parameters that the method signature still treats as required.

`EdmHelpers.GetTypeReference` gains a `bool nullable = true` overload so the builder can explicitly pass `false` for non-nullable value-type parameters.

#### Runtime: honoring optional defaults (`RestierOperationExecutor`)

`MethodInfo.Invoke` does not honor compile-time defaults — the runtime executor must fill them itself. The loop at `RestierOperationExecutor.cs:102-129` is extended:

```csharp
for (; paraIndex < parameterArray.Length; paraIndex++)
{
    var parameter = parameterArray[paraIndex];
    var currentParameterValue = restierOperationContext.GetParameterValueFunc(parameter.Name);

    object convertedValue;
    if (currentParameterValue is null && IsOmittedOptional(parameter))
    {
        convertedValue = ResolveDefault(parameter);   // [DefaultValue].Value, then parameter.DefaultValue, then null
    }
    else if (restierOperationContext.IsFunction)
    {
        // existing path — ConvertValue
    }
    else
    {
        // existing path — ConvertCollectionType
    }
    parameters[paraIndex] = convertedValue;
}
```

`IsOmittedOptional(parameter)` mirrors `ClassifyOptionality` from the model builder. Without this change, omitting an optional URL parameter would invoke the method with `null` / `default(T)` instead of the declared default, defeating the purpose of `EdmOptionalParameter.DefaultValue`. The classification helper is extracted to a small static class shared between the model builder and the executor to avoid duplication.

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
| `Model/OperationParameterClassifier.cs` | **NEW** — static helpers `IsOmittedOptional` / `ResolveDefault` / `ComputeNullable` shared between model builder and executor |
| `Model/ApiExtension/OperationTypeRegistrationModelBuilder.cs` | **NEW** — pre-pass that auto-registers types referenced by `[Operation]` methods |
| `Model/ApiExtension/RestierWebApiOperationModelBuilder.cs` | **MODIFY** — dedup by namespace+name with Trace warning; classify+emit optional parameters using the new classifier; emit `Core.V1.Revisions` from `[Obsolete]`; emit parameter-level `Core.V1.Description` |
| `Model/EdmHelpers.cs` | **MODIFY** — add `GetTypeReference(this Type, IEdmModel, bool nullable)` overload so value-type parameters can be emitted as non-nullable when neither nullable nor optional |
| `Operation/RestierOperationExecutor.cs` | **MODIFY** — when a URL-side parameter value is null and the parameter is omittable per `OperationParameterClassifier`, fill the positional slot with the resolved default (`[DefaultValue]` > `ParameterInfo.DefaultValue` > `null`) instead of the converted-from-null value |
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
  - Dedup: when `model` already contains an operation with the same namespace+name, skip and emit `Trace.TraceWarning` containing the operation name
  - Dedup: same name, different parameter types → second registration is also skipped (overload preservation is *not* a goal; comment ties this to the executor's name-only dispatch)
  - Nullable: bare `int? p` (no default) → `EdmOperationParameter` (required), type ref `Nullable = true`
  - Optional via `int p = 5` → `EdmOptionalParameter` with `DefaultValue = "5"`, type ref `Nullable = false`
  - Optional via `int? p = null` → `EdmOptionalParameter` with `DefaultValue = "null"`, type ref `Nullable = true`
  - Optional via `[Optional] int? p` → `EdmOptionalParameter` with `DefaultValue = "null"`, type ref `Nullable = true`
  - `[Optional] int p` (non-nullable, no default) → builder logs a `Trace.TraceWarning` and treats the parameter as required (cannot represent omission of a non-nullable value type without a default)
  - `[DefaultValue("foo")] string p` → `EdmOptionalParameter` with `DefaultValue = "foo"`
  - `[Obsolete("Use Bar instead.")]` → vocabulary annotation with `Core.V1.Revisions` term, `Description = "Use Bar instead."`
  - Plain `int p` (no defaults, no attribute) → `EdmOperationParameter` with type ref `Nullable = false`
- `RestierOperationExecutor` (new test file `Operation/RestierOperationExecutorOptionalTests.cs`):
  - When `?p` is absent on a function whose declared parameter is `int p = 5`, the executor invokes the method with the integer `5` (not `0`)
  - When `?p` is absent and the parameter has `[DefaultValue("hello")] string p`, the executor passes `"hello"`
  - When `?p=null` is present on `int? p` (nullable, not optional), the executor passes `null`
  - When `?p` is absent on a required `int p`, existing behavior is preserved (URL parsing surfaces the missing-parameter error path)

HTTP integration (Breakdance, following `FeatureTests/DeepInsertTests.cs` style):

- A `[UnboundOperation]` taking `int? parameter1` responds 200 to `Query(parameter1=null)` (the literal #656 repro — nullability, not omittability)
- A `[UnboundOperation]` taking `int parameter1 = 5` responds 200 to `Query()` and the returned body reflects that the method received `5` (not `0`)
- `$metadata` contains the expected `Annotation Term="Core.V1.Description"` and `Annotation Term="Core.V1.Revisions"` markup
- Registering the same `[UnboundOperation]` and also calling `ODataModelBuilder.Function(...)` for it produces exactly one `OperationImport` in `$metadata` and a single `Trace.TraceWarning`
- A `[UnboundOperation]` whose parameter and return are custom POCOs (no manual `ComplexType` registration) responds correctly and emits both types in `$metadata`

## Risks & Open Questions

- **Aux builder type re-emission**: pre-Ignoring every already-declared type on `ODataConventionModelBuilder` requires CLR-type resolution from the inner `EdmModel`. The pre-pass reads `ClrTypeAnnotation` directly via `model.GetAnnotationValue<ClrTypeAnnotation>(known)?.ClrType` rather than calling `EdmHelpers.GetClrType` (which throws when the annotation is missing — see `EdmHelpers.cs:52-67`). Schema types lacking that annotation are simply skipped; the merge step also guards with `model.FindDeclaredType(...) is null` so a collision is impossible.
- **`ODataConventionModelBuilder.Ignore` API shape**: the exact method/property used to suppress emission of a known type on the auxiliary builder will be confirmed during implementation against the version of `Microsoft.OData.ModelBuilder` pinned by the solution. If the literal `Ignore(...)` shape differs (e.g. requires per-property ignore), the implementation plan should call that out and adjust.
- **Bound operations referencing types not on any `EntitySet`**: registering a missing keyed type as `EntityType` (no `EntitySet`) is sufficient for the operation builder but means that type is not directly queryable. That's the existing OData behavior; documented in the operations guide.
- **`[Obsolete]` term Version string**: the OData `Core.V1.Revisions` schema requires a `Version` field; the literal `"obsolete"` is a convention placeholder. If a future iteration wants real semver tracking, that would be a separate spec.
- **Optional parameter type nullability**: Nullability and optionality are independently emitted (see `ComputeNullable` and `ClassifyOptionality`). An `EdmOptionalParameter` with `DefaultValue = "5"` and a non-nullable `Edm.Int32` type ref is the intended shape for `int p = 5`. Tests under "Parameter classification" pin this contract.
- **Action vs. function optional handling**: The executor extension applies to both function and action parameters, but action parameters typically come from a JSON body where omission means the property is missing. The `IsOmittedOptional` check fires regardless of `IsFunction`; the test matrix covers function URL parameters explicitly and treats action-body coverage as a follow-up if real-world action scenarios surface in code review.

## Out of Scope (Deferred)

- **Same-name operation overloads.** Preserving multiple operations with the same namespace+name would require also updating `RestierOperationExecutor` to dispatch by parameter type list (today it does `GetMethod(name, BindingFlags...)` — see `RestierOperationExecutor.cs:78-80` and the comment at `:76`). That change is its own design question (URL parsing must select the right overload from candidate signatures) and is left to a future spec.
- `[DisplayName]` → `Core.V1.LongDescription` mapping
- Nullable reference types (`string?`) as a nullable/optional signal — requires reading `NullableAttribute` byte arrays; the `[Optional]` escape hatch is sufficient under NRT-disabled compilation
- `[Action]` / `[Function]` attribute aliases replacing the `OperationType` enum (separate UX-only refactor)
- Auto-registration of types referenced only by entity sets (`RestierWebApiModelBuilder` already handles that path)
- Composable function inference from method signature (separate "operation composition" topic)
