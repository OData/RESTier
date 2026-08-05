# OpenAPI Annotation Attributes — Design Spec

**Issue:** [OData/RESTier#660](https://github.com/OData/RESTier/issues/660)
**Date:** 2026-05-01
**Status:** Approved

## Goal

Map standard .NET attributes on RESTier API entities, properties, and operations to OData vocabulary annotations in `$metadata`. The annotations serve two purposes:

1. **OpenAPI enrichment.** They flow through `Microsoft.OpenApi.OData` to Swagger/NSwag output as descriptions, validation hints, and `readOnly` flags.
2. **Server behavior.** RESTier's existing submit pipeline already consumes `Core.V1.Computed` and `Core.V1.Immutable` (`Extensions.cs:162-177`) to set `PropertyAttributes.IgnoreForCreation` / `IgnoreForUpdate`. So `[DatabaseGenerated]` and `[ReadOnly]` will, by side-effect, cause the server to ignore those properties on POST/PATCH/PUT request bodies — replacing the client-supplied value with the database-generated one (or rejecting the change for `[ReadOnly]`).

Both are intentional outcomes. (2) is what users actually want when they put `[DatabaseGenerated(Identity)]` on an `Id` property — they want the database to assign the value, not whatever the client posts. Today that requires writing a custom `IModelBuilder` to add the annotation manually; this feature makes it automatic.

Ship as a default-on convention with no opt-in step.

## Scope (v1)

| .NET attribute | Target | OData term | Term namespace |
|---|---|---|---|
| `[Description("…")]` | entity, complex, property, navigation, operation | `Description` | `Org.OData.Core.V1` |
| `[DatabaseGenerated(Identity)]` / `[DatabaseGenerated(Computed)]` | property | `Computed` | `Org.OData.Core.V1` |
| `[ReadOnly(true)]` | property | `Immutable` | `Org.OData.Core.V1` |
| `[Range(min, max)]` | numeric property | `Minimum`, `Maximum` | `Org.OData.Validation.V1` |
| `[RegularExpression(pattern)]` | string property | `Pattern` | `Org.OData.Validation.V1` |

`[MaxLength]` / `[StringLength]` are **not** mapped to a vocabulary annotation. `ODataConventionModelBuilder` already absorbs them as the structural `MaxLength` facet on `Edm.String` / `Edm.Binary` properties, which `Microsoft.OpenApi.OData` reads to emit JSON-Schema `maxLength`. Emitting `Org.OData.Validation.V1.MaxLength` would duplicate the constraint.

## Out of Scope (v1)

- Operation-parameter annotations (descriptions on individual parameters).
- XML doc comments (`<summary>`) as a description source.
- `OnAnnotating{X}()` convention-based interceptor methods. (The existing custom `IModelBuilder` extension point — documented in `model-building.mdx:232-292` — covers these dynamic cases.)
- Capabilities-vocabulary annotations (`UpdateRestrictions`, `InsertRestrictions`, etc.) and other non-listed terms.

These are deliberate deferrals. The chained-builder mechanism makes any of them a small, focused follow-up.

## Server behavior implications

`Microsoft.Restier.AspNetCore/Extensions/Extensions.cs:142-198` already reads `Core.V1.Computed` and `Core.V1.Immutable` annotations from the EDM model and translates them into write-pipeline behavior:

| Annotation | Effect (`PropertyAttributes` flag) | Observable behavior |
|---|---|---|
| `Core.V1.Computed = true` | `IgnoreForCreation \| IgnoreForUpdate` | property dropped from POST and PATCH/PUT request bodies before the change set is applied |
| `Core.V1.Immutable = true` | `IgnoreForUpdate` | property dropped from PATCH/PUT request bodies; accepted on POST |

Because of this, emitting these annotations is **not metadata-only** — it changes how the server processes write requests. After this feature ships:

- `[DatabaseGenerated(Identity)]` on an `Id` property: the server will silently discard a client-supplied `Id` in a POST body and let the database assign one. This is almost certainly the user's intent — manually specifying `Id` in a POST to an identity column is a bug today; from now on it's a no-op.
- `[ReadOnly(true)]` on, e.g., a `CreatedOn` property: the server accepts the value on initial POST but silently drops it from PATCH/PUT bodies. Clients that previously could "fix up" the value via PATCH will see their change ignored.

These are intentional outcomes — they're the reason `Core.V1.Computed` / `Core.V1.Immutable` exist as terms — but they constitute observable behavior changes for any RESTier API that already uses these attributes for any other reason (display formatting, EF migrations, etc.). Three guardrails:

1. **Integration test asserts the behavior.** A test in `AnnotationMetadataTests` POSTs to `AnnotatedApi` with a value for a `[DatabaseGenerated(Identity)]` property and asserts the persisted entity uses the database value, not the posted value. Same pattern for `[ReadOnly(true)]` on PATCH. Without this assertion, a future refactor could regress the link between annotation and pipeline behavior.
2. **MDX page calls it out.** A `<Warning>` block on the new `openapi-annotations.mdx` page lists each attribute that has server-side effects, what those effects are, and how to opt out (use a custom `IModelBuilder` to remove the annotation).
3. **Release notes flag it.** The next release-notes entry mentions both the new feature and the behavior change side-effect, naming `[DatabaseGenerated]` and `[ReadOnly]` explicitly.

`[Description]`, `[Range]`, and `[RegularExpression]` are pure metadata — they have no current effect on the submit pipeline, so no behavior risk.

## Architecture

### New service

`src/Microsoft.Restier.AspNetCore/Model/ConventionBasedAnnotationModelBuilder.cs`

```csharp
public class ConventionBasedAnnotationModelBuilder : IModelBuilder
{
    public ConventionBasedAnnotationModelBuilder(Type apiType);
    public IModelBuilder Inner { get; set; }
    public IEdmModel GetEdmModel();
}
```

- Lives in `src/Microsoft.Restier.AspNetCore/Model/` next to its peer `RestierWebApiOperationModelBuilder`. The builder needs to recognize `BoundOperationAttribute` / `UnboundOperationAttribute` (which currently live in the AspNetCore assembly), so this is the layer that owns them. Trying to host the builder in `Microsoft.Restier.Core` would require either moving those attributes (breaking the public API namespace) or matching attribute types by string name (a hack that obscures the dependency).
- Constructor takes `Type apiType`.
- Builds an internal `Dictionary<string, MethodInfo>` operation index from `apiType` once at construction time.
- `GetEdmModel()` calls `Inner?.GetEdmModel()` (returning `null` if the inner returns `null`), walks `model.SchemaElements`, scans CLR types via `model.GetAnnotationValue<ClrTypeAnnotation>(type)?.ClrType`, scans operations via the precomputed index, and emits annotations.

### Pipeline placement

`EFModelBuilder<T>` → `RestierWebApiModelBuilder` → `RestierWebApiOperationModelBuilder` → **`ConventionBasedAnnotationModelBuilder`** (new, last).

Last so it can annotate every entity, complex type, property, and operation contributed by inner builders.

### Registration

`src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` — append the new builder to both chains:

- Model-building service container (after line 117):
  ```csharp
  modelBuildingServices.AddSingleton<IChainedService<IModelBuilder>>(
      sp => new ConventionBasedAnnotationModelBuilder(type));
  ```
- Route service container (after line 168):
  ```csharp
  services.AddSingleton<IChainedService<IModelBuilder>>(
      sp => new ConventionBasedAnnotationModelBuilder(type));
  ```

No new public extension methods. Always-on per the question-4 decision.

### No attribute moves

Earlier drafts of this spec proposed moving `BoundOperationAttribute` / `UnboundOperationAttribute` / `OperationAttribute` / `OperationType` from `Microsoft.Restier.AspNetCore.Model` to `Microsoft.Restier.Core.Model`, with `[TypeForwardedTo]` shims for compat. **Rejected:** `[TypeForwardedTo]` only preserves type identity when the fully-qualified name (including namespace) is unchanged. Renaming the namespace would be a source-and-binary breaking change for every existing consumer regardless of forwarders. The simpler resolution is to keep the attributes where they are and host the builder alongside them in AspNetCore.

## Annotation emission

```csharp
var annotation = new EdmVocabularyAnnotation(target, term, expression);
annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
model.AddVocabularyAnnotation(annotation);
```

Where:
- `target` is the `IEdmVocabularyAnnotatable` (entity type, property, operation).
- `term` comes from `CoreVocabularyModel` or `ValidationVocabularyModel`.
- `expression` is `EdmStringConstant`, `EdmBooleanConstant`, or `EdmIntegerConstant` matching the term's type.
- Inline serialization is required so the annotation appears on its target element in `$metadata`, not detached at the bottom — `Microsoft.OpenApi.OData` reads inline annotations.

### Idempotence

Before emitting, check `model.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(target, "<term-name>")`. If an annotation with the same term already exists, skip — preserves user-supplied annotations from custom `IModelBuilder` extensions earlier in the chain.

### Range value typing

`RangeAttribute` exposes its `Minimum` and `Maximum` as `object` because the .NET API supports three constructor shapes — `(int, int)`, `(double, double)`, and `(Type, string, string)` (used for `decimal`, `DateTime`, and similar). The OData `Validation.Minimum` / `Validation.Maximum` terms must be expressed as a constant expression whose primitive type matches the target property's EDM type — otherwise `Microsoft.OData.Edm` will refuse the annotation as invalid, or downstream tooling will silently discard it.

Resolution: dispatch on the property's `IEdmPrimitiveType.PrimitiveKind`:

| Property primitive kind | Constant expression | Conversion |
|---|---|---|
| `Byte`, `SByte`, `Int16`, `Int32`, `Int64` | `EdmIntegerConstant` | `Convert.ToInt64(rangeValue, InvariantCulture)` |
| `Single`, `Double` | `EdmFloatingConstant` | `Convert.ToDouble(rangeValue, InvariantCulture)` |
| `Decimal` | `EdmDecimalConstant` | `Convert.ToDecimal(rangeValue, InvariantCulture)` |
| anything else | skip + `Trace.TraceWarning` | — |

If `Convert.To*` throws (e.g., `[Range(typeof(string), "a", "z")]` on a numeric property — user error), wrap in try/catch, trace-warn with the property name and offending value, and skip the annotation. Silent failure on a malformed user attribute is the right call here: we don't want a misconfigured attribute to fail the whole model build.

`Minimum` and `Maximum` are emitted independently — if only one was set in the attribute, only one annotation is emitted.

### Operation lookup

Built at construction time, exactly mirroring `RestierWebApiOperationModelBuilder.ScanForOperations` (`src/Microsoft.Restier.AspNetCore/Model/ApiExtension/RestierWebApiOperationModelBuilder.cs:214-229`):

```csharp
private static Dictionary<string, MethodInfo> BuildOperationIndex(Type apiType)
{
    var index = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
    var methods = apiType
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Public
                  | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
        .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object));

    foreach (var method in methods)
    {
        if (method.GetCustomAttribute<OperationAttribute>(inherit: true) is null)
        {
            continue;
        }

        // EDM operation name is the C# method name. The [BoundOperation]/[UnboundOperation]
        // attributes do not currently expose a Name override (verified
        // src/Microsoft.Restier.AspNetCore/Model/{Bound,Unbound,}OperationAttribute.cs).
        index.TryAdd(method.Name, method);
    }

    return index;
}
```

This match-the-real-scanner approach is required because:
- `RestierWebApiOperationModelBuilder` includes inherited methods (`FlattenHierarchy`) and non-public methods (`NonPublic`). A narrower scan in our builder would miss operations the rest of RESTier already considers operations.
- `IsSpecialName` exclusion skips property getter/setter methods that happen to be public.
- `GetCustomAttribute<OperationAttribute>(inherit: true)` matches `BoundOperationAttribute`, `UnboundOperationAttribute`, and any future `OperationAttribute` subclass without enumerating them.

At `GetEdmModel()` time, for each `IEdmOperation op`, do `operationMethods.TryGetValue(op.Name, out var methodInfo)` and apply `[Description]` if present.

## XML doc comments

### On the new code

All public types and members get standard XML doc comments (`<summary>`, `<param>`, `<returns>`, `<exception>`). Required because:
- `Directory.Build.props` enables `GenerateDocumentationFile` and `TreatWarningsAsErrors` for every project, including `Microsoft.Restier.AspNetCore` — CS1591 will fail the build.
- `DotNetDocs.Sdk` regenerates `api-reference/` from these comments at build time.

`GetEdmModel()` uses `<inheritdoc />` since the contract is on `IModelBuilder.GetEdmModel()`.

### As a description source

Out of scope for v1 (deferred per Section 4 design discussion). The new MDX page calls this out in a `<Note>`: "RESTier does not currently read XML doc summaries as a description source. Use `[Description]` for now."

## Testing

### Unit tests

`test/Microsoft.Restier.Tests.AspNetCore/Model/ConventionBasedAnnotationModelBuilderTests.cs`

(Test path mirrors source path. The `Model/` subfolder under `Microsoft.Restier.Tests.AspNetCore` is new — first test file there.)

Each attribute family gets a focused test using a small fixture entity built via `ODataConventionModelBuilder` (which sets `ClrTypeAnnotation`).

| Test | Asserts |
|---|---|
| Description on entity type | `Core.V1.Description` on type |
| Description on property | `Core.V1.Description` on property |
| Description on complex type | `Core.V1.Description` on complex |
| Description on operation | `Core.V1.Description` on operation |
| Computed from `DatabaseGenerated.Identity` | `Core.V1.Computed = true` |
| Computed from `DatabaseGenerated.Computed` | `Core.V1.Computed = true` |
| Computed skipped for `DatabaseGenerated.None` | no annotation |
| Immutable from `ReadOnly(true)` | `Core.V1.Immutable = true` |
| Immutable skipped for `ReadOnly(false)` | no annotation |
| Range on `int` emits `EdmIntegerConstant` Min/Max | both annotations, integer-typed |
| Range on `double` emits `EdmFloatingConstant` Min/Max | both annotations, floating-typed |
| Range on `decimal` emits `EdmDecimalConstant` Min/Max | both annotations, decimal-typed |
| Range on `string` property logs and skips | no annotation, no exception |
| RegularExpression emits Pattern | `Validation.Pattern = "regex"` |
| MaxLength does not emit vocabulary annotation | structural facet only |
| Idempotent — pre-existing annotation preserved | same single annotation, value unchanged |
| Operation lookup matches `MethodInfo` by C# name | annotation found on EDM op |
| Operation scan includes inherited methods | annotation found on op declared on base class |
| Operation scan includes non-public methods | annotation found on op declared as `protected` or `internal` |
| Operation scan excludes `IsSpecialName` methods | property accessor not treated as op |
| Null inner returns null | returns `null` |
| Constructor null `apiType` throws | `ArgumentNullException` |

22 tests, fast (no Breakdance startup).

### Integration tests

`test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/AnnotationMetadataTests.cs`

End-to-end tests through the full RESTier pipeline using a focused new test scenario:

- `test/Microsoft.Restier.Tests.Shared/Scenarios/Annotated/AnnotatedEntity.cs` — one entity with one of each attribute family. Includes an `Id` property with `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]` and a `CreatedOn` property with `[ReadOnly(true)]`.
- `test/Microsoft.Restier.Tests.Shared/Scenarios/Annotated/AnnotatedApi.cs` — `ApiBase` subclass with one entity set, one bound operation carrying `[Description]`. Mirrors the `StoreApi` test scenario shape (in-memory, no EF dependency).
- `test/Microsoft.Restier.Tests.AspNetCore/Baselines/AnnotatedApi-ApiMetadata.txt` — captured XML containing `<Annotation Term="…" />` elements for every attribute family.

Three `[Fact]` tests:

1. **`AnnotatedApi_MetadataMatchesBaseline`** — mirrors `MetadataTests.StoreApi_CompareCurrentApiMetadataToPriorRun`. Asserts the rendered `$metadata` equals the baseline file. This is the primary regression guard for the annotation-emission logic end-to-end.
2. **`PostingComputedProperty_IgnoresClientValue`** — POSTs `{ "Id": 9999, "Name": "Test" }` to `/AnnotatedEntities`. Asserts the persisted entity has whatever ID the in-memory store assigned, *not* `9999`. Proves the `Core.V1.Computed` annotation correctly drives `IgnoreForCreation` end-to-end.
3. **`PatchingImmutableProperty_IgnoresClientValue`** — first POSTs to create an entity (capturing `CreatedOn`), then PATCHes `{ "CreatedOn": "1900-01-01T00:00:00Z" }`. Asserts the persisted `CreatedOn` is unchanged. Proves `Core.V1.Immutable` correctly drives `IgnoreForUpdate`.

The two behavior tests are the guardrail called out in the "Server behavior implications" section. Without them, a future change could decouple our annotation emission from RESTier's existing submit-pipeline reading code, and the metadata baseline would still pass.

### Existing baselines

Spot-check showed `[MaxLength(13)]` on `Library/Book.cs` and `Marvel/Comic.cs`, and no `[Description]`/`[DatabaseGenerated]`/`[ReadOnly]`/`[Range]`/`[RegularExpression]`. Per the section 2 decision (`MaxLength` skipped), existing `LibraryApi`, `MarvelApi`, `StoreApi` baselines should be unchanged. The plan includes a verification step asserting they still pass after the builder is wired up.

## Documentation

### New page

`src/Microsoft.Restier.Docs/guides/server/openapi-annotations.mdx`

Frontmatter:

```mdx
---
title: "OpenAPI Annotation Attributes"
description: "Enrich your OData $metadata and OpenAPI/Swagger output with .NET attributes"
icon: "tags"
sidebarTitle: "OpenAPI Annotations"
---
```

Sections:

1. **Overview** — convention-based attribute scanning + `Microsoft.OpenApi.OData` flow.
2. **`<Info>` callout** — on by default, no registration step.
3. **`<Warning>` callout — server behavior change.** "`[DatabaseGenerated]` and `[ReadOnly]` are not metadata-only — RESTier's submit pipeline already reads `Core.V1.Computed` and `Core.V1.Immutable` to drop properties from POST/PATCH/PUT request bodies. After enabling this feature, a client that POSTs an `Id` value to a `[DatabaseGenerated(Identity)]` property will see that value silently replaced by the database-assigned one. This is the intended behavior — it's why the OData terms exist — but it's a meaningful change for any API already using these attributes." Show before/after of a POST request body.
4. **Supported attributes table** — with "→ OpenAPI effect" column AND a "→ Server effect" column flagging the two attributes that change submit-pipeline behavior.
5. **`<Steps>` walkthrough** — POCO → `$metadata` → OpenAPI JSON.
6. **Per-attribute reference** — one subsection per attribute family with C# example, emitted term, OpenAPI effect, server effect (where applicable).
7. **`## What about [MaxLength] and [StringLength]?`** — explicit `<Note>` explaining the structural-facet path.
8. **`## Range value typing`** — short note explaining that `[Range]` values are typed to match the EDM property kind (int / double / decimal), and that `[Range]` on non-numeric properties is logged and skipped.
9. **`## Operations`** — `[Description]` on `[UnboundOperation]` end-to-end.
10. **`## Overriding or extending`** — `<Tip>` cross-link to `model-building.mdx#custom-model-extension`. Includes an "opt out of a single annotation" recipe — a custom `IModelBuilder` that runs after the convention builder and removes a specific `Core.V1.Computed` annotation. This is the documented escape hatch for users who want the OpenAPI metadata but not the submit-pipeline side-effect.
11. **`## XML doc comments`** — `<Note>` explaining v1 deferral.
12. **`## Limitations`** — bullet list of v1 boundaries.

Mintlify components: `<Info>`, `<Note>`, `<Tip>`, `<Warning>`, `<Steps>`, `<CodeGroup>`. Same conventions as `nswag.mdx` and `swagger.mdx`.

### Modified files

- `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` — add `guides/server/openapi-annotations;` to the `<Pages>` list inside the `Server` group, between `swagger;` and `testing;`.
- `src/Microsoft.Restier.Docs/guides/server/nswag.mdx` — add `<Tip>` cross-link.
- `src/Microsoft.Restier.Docs/guides/server/swagger.mdx` — add `<Tip>` cross-link.
- `src/Microsoft.Restier.Docs/guides/server/model-building.mdx` — append `<Note>` cross-link at the end of "Custom model extension".

### `docs.json`

Regenerated by the SDK from `<MintlifyTemplate>`. The plan includes a `dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` step after the docsproj edit and a `git add docs.json` step in the same commit.

### API reference

No changes needed. `Microsoft.Restier.AspNetCore.csproj` is already in the `<_DocsSourceProject>` list (`Microsoft.Restier.Docs.docsproj`), so `ConventionBasedAnnotationModelBuilder` auto-appears under `api-reference/microsoft-restier-aspnetcore/model/` on the next build.

### Release notes

Post-merge step. Add a line to the next release-notes page when the PR is cut. Not part of the implementation tasks.

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Existing baselines shift unexpectedly | Verification step in plan; investigate any diff before continuing |
| Annotation-emission API churn between `Microsoft.OData.Edm` versions | Pin to existing 8.x usage in RESTier; tests assert the `$metadata` output, not the API surface |
| Server behavior change from `[DatabaseGenerated]` / `[ReadOnly]` surprises an existing user | Documented prominently in the new MDX page (`<Warning>` callout); two integration tests assert the behavior; release notes flag both attributes by name; documented opt-out via custom `IModelBuilder` |
| `Microsoft.OpenApi.OData` doesn't honor every term we emit | Each attribute mapping is justified by an observable OpenAPI effect; documented per-attribute in the new MDX page |
| `[Range]` on non-numeric property fails to build the model | Try/catch around the conversion; log and skip rather than throw |

## Definition of Done

- `ConventionBasedAnnotationModelBuilder` exists in `src/Microsoft.Restier.AspNetCore/Model/`, registered in both model-building and route chains, all unit and integration tests passing.
- The two behavior-asserting integration tests (`PostingComputedProperty_IgnoresClientValue`, `PatchingImmutableProperty_IgnoresClientValue`) pass.
- New MDX page committed with the `<Warning>` callout for server-behavior changes; existing pages cross-linked; nav updated; `docs.json` regenerated.
- Existing `LibraryApi` / `MarvelApi` / `StoreApi` metadata baselines unchanged.
- `dotnet build RESTier.slnx` clean (no new CS1591 warnings).
