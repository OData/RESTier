# Magical Operations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `[Operation]`/`[BoundOperation]`/`[UnboundOperation]`-decorated methods self-registering — auto-register referenced complex/entity/enum types, dedupe by name with a Trace warning, honor parameter optionality and nullability independently, and emit `[Obsolete]` as a `Core.V1.Revisions` vocabulary annotation.

**Architecture:** Add a new `OperationTypeRegistrationModelBuilder` pre-pass that drives `ODataConventionModelBuilder` to register missing CLR types referenced by `[Operation]` methods. Extract parameter classification (`ComputeNullable` / `ClassifyOptionality` / `ResolveDefault`) into a shared static helper used by both the model builder (at build time) and `RestierOperationExecutor` (at request time). Change `OperationContext.GetParameterValueFunc` from `Func<string, object>` to `Func<string, (bool present, object value)>` so the executor can distinguish parameter omission from explicit-null. The breaking-change is acceptable on the vNext branch.

**Tech Stack:** .NET 8/9 + .NET Framework 4.8, Microsoft.OData.Edm 8.x, Microsoft.OData.ModelBuilder 2.x, xUnit v3, FluentAssertions (AwesomeAssertions), NSubstitute, Breakdance.

**Spec:** `docs/superpowers/specs/2026-05-19-magical-operations-design.md`

---

## File Map

**Source (new):**
- `src/Microsoft.Restier.AspNetCore/Model/OptionalAttribute.cs` — parameter-level marker (distinct from `System.Runtime.InteropServices.OptionalAttribute`).
- `src/Microsoft.Restier.AspNetCore/Model/OperationParameterClassifier.cs` — static helpers `ComputeNullable`, `ClassifyOptionality`, `IsOmittedOptional`, `ResolveDefault`. Shared between the model builder (build time) and the executor (request time).
- `src/Microsoft.Restier.AspNetCore/Model/ApiExtension/OperationTypeRegistrationModelBuilder.cs` — pre-pass that scans for `[Operation]` methods and registers their unknown types via `ODataConventionModelBuilder`.

**Source (modify):**
- `src/Microsoft.Restier.Core/Operation/OperationContext.cs` — change `GetParameterValueFunc` delegate type (binary-breaking, Core).
- `src/Microsoft.Restier.AspNetCore/RestierController.cs` — update both function-segment delegate builders at lines 109 and 143.
- `src/Microsoft.Restier.AspNetCore/Operation/RestierOperationExecutor.cs` — consume presence-aware delegate; substitute resolved defaults on absence.
- `src/Microsoft.Restier.AspNetCore/Model/ApiExtension/RestierWebApiOperationModelBuilder.cs` — namespace+name dedup with Trace warning; use the new classifier; emit `[Obsolete]` and parameter `[Description]`.
- `src/Microsoft.Restier.AspNetCore/Model/EdmHelpers.cs` — add `GetTypeReference(this Type, IEdmModel, bool nullable)` overload.
- `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` — register the new builder stage in both `AddRestierRoute` paths (lines 121–124 and 171–175).

**Tests (new):**
- `test/Microsoft.Restier.Tests.AspNetCore/Model/OperationParameterClassifierTests.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/Model/OperationTypeRegistrationModelBuilderTests.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/Operation/RestierOperationExecutorOptionalTests.cs`
- `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/MagicalOperationsTests.cs` (HTTP integration via Breakdance)
- `test/Microsoft.Restier.Tests.Shared/Scenarios/MagicalOps/MagicalOpsApi.cs` — test scenario API surface

**Tests (modify):**
- `test/Microsoft.Restier.Tests.AspNetCore/Model/RestierWebApiOperationModelBuilderTests.cs` — extend with new behavior cases.
- `test/Microsoft.Restier.Tests.AspNetCore/Model/RestierModelBuilderTests.cs` — if needed for end-to-end model construction.

**Docs (modify):**
- `src/Microsoft.Restier.Docs/guides/server/operations.mdx` — auto-registration, optional parameters, `[Obsolete]`, dedup warning.
- `src/Microsoft.Restier.Docs/release-notes/v2-0.mdx` (or equivalent) — breaking-change callout for `OperationContext.GetParameterValueFunc`.

---

## Task 1: Add `OptionalAttribute` marker

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore/Model/OptionalAttribute.cs`

- [ ] **Step 1: Create the file**

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.AspNetCore.Model
{
    /// <summary>
    /// Marks a RESTier operation parameter as optional in the EDM model.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Apply this attribute to a parameter of a method decorated with
    /// <see cref="BoundOperationAttribute"/> or <see cref="UnboundOperationAttribute"/>
    /// to declare that the parameter may be omitted from the URL. The resulting
    /// EDM parameter is emitted as an <c>EdmOptionalParameter</c> with a <c>null</c>
    /// default literal, and the parameter type reference is emitted with <c>Nullable = true</c>.
    /// </para>
    /// <para>
    /// Use this attribute when neither <c>Nullable&lt;T&gt;</c> nor a compile-time
    /// default value can express the intent — typically for reference-type parameters
    /// under nullable-reference-types-disabled compilation, or when the absence of
    /// the parameter should produce a <c>null</c> CLR argument at invocation time.
    /// </para>
    /// <para>
    /// This attribute is intentionally distinct from
    /// <see cref="System.Runtime.InteropServices.OptionalAttribute"/>. Use the
    /// fully qualified name <c>Microsoft.Restier.AspNetCore.Model.OptionalAttribute</c>
    /// when both namespaces are in scope.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class OptionalAttribute : Attribute
    {
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj`
Expected: Build succeeds with 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Model/OptionalAttribute.cs
git commit -m "feat(operations): add [Optional] parameter marker

Parameter-level marker for explicit opt-in to omittable parameters in
operations decorated with [BoundOperation]/[UnboundOperation]. Distinct
from System.Runtime.InteropServices.OptionalAttribute to keep the
OData/EDM semantics RESTier-owned.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Add `OperationParameterClassifier` with unit tests

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore/Model/OperationParameterClassifier.cs`
- Test: `test/Microsoft.Restier.Tests.AspNetCore/Model/OperationParameterClassifierTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `test/Microsoft.Restier.Tests.AspNetCore/Model/OperationParameterClassifierTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Globalization;
using System.Reflection;
using FluentAssertions;
using Microsoft.Restier.AspNetCore.Model;
using Xunit;
using RestierOptional = Microsoft.Restier.AspNetCore.Model.OptionalAttribute;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

public class OperationParameterClassifierTests
{
    // Reflection helpers — each public method below has parameters that exercise a classifier case.
    private static ParameterInfo Param(string methodName, int index = 0)
        => typeof(SampleParameters).GetMethod(methodName)!.GetParameters()[index];

    [Fact]
    public void ComputeNullable_ReturnsTrue_ForNullableValueType()
    {
        OperationParameterClassifier.ComputeNullable(Param(nameof(SampleParameters.NullableInt)))
            .Should().BeTrue();
    }

    [Fact]
    public void ComputeNullable_ReturnsTrue_ForReferenceType()
    {
        OperationParameterClassifier.ComputeNullable(Param(nameof(SampleParameters.ReferenceString)))
            .Should().BeTrue();
    }

    [Fact]
    public void ComputeNullable_ReturnsTrue_ForOptionalAttribute()
    {
        OperationParameterClassifier.ComputeNullable(Param(nameof(SampleParameters.OptionalNullableInt)))
            .Should().BeTrue();
    }

    [Fact]
    public void ComputeNullable_ReturnsFalse_ForBareValueType()
    {
        OperationParameterClassifier.ComputeNullable(Param(nameof(SampleParameters.PlainInt)))
            .Should().BeFalse();
    }

    [Fact]
    public void ClassifyOptionality_ReturnsRequired_ForBareValueType()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.PlainInt)));
        isOptional.Should().BeFalse();
        literal.Should().BeNull();
    }

    [Fact]
    public void ClassifyOptionality_ReturnsRequired_ForBareNullable_NoDefault()
    {
        // Nullable<T> without a default and without [Optional] is required (but type-ref nullable).
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.NullableInt)));
        isOptional.Should().BeFalse();
        literal.Should().BeNull();
    }

    [Fact]
    public void ClassifyOptionality_ReturnsOptional_ForCompilerDefault()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.IntWithDefault)));
        isOptional.Should().BeTrue();
        literal.Should().Be("5");
    }

    [Fact]
    public void ClassifyOptionality_ReturnsOptional_ForDefaultValueAttribute()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.StringWithDefaultValueAttribute)));
        isOptional.Should().BeTrue();
        literal.Should().Be("hello");
    }

    [Fact]
    public void ClassifyOptionality_DefaultValueAttribute_OverridesCompilerDefault()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.AttributeOverridesCompilerDefault)));
        isOptional.Should().BeTrue();
        literal.Should().Be("attribute");
    }

    [Fact]
    public void ClassifyOptionality_ReturnsOptional_ForOptionalAttribute()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.OptionalNullableInt)));
        isOptional.Should().BeTrue();
        literal.Should().Be("null");
    }

    [Fact]
    public void ClassifyOptionality_NullCompilerDefault_EmitsNullLiteral()
    {
        var (isOptional, literal) = OperationParameterClassifier.ClassifyOptionality(Param(nameof(SampleParameters.NullableIntWithNullDefault)));
        isOptional.Should().BeTrue();
        literal.Should().Be("null");
    }

    [Fact]
    public void IsOmittedOptional_MirrorsClassifyOptionality()
    {
        OperationParameterClassifier.IsOmittedOptional(Param(nameof(SampleParameters.IntWithDefault))).Should().BeTrue();
        OperationParameterClassifier.IsOmittedOptional(Param(nameof(SampleParameters.PlainInt))).Should().BeFalse();
    }

    [Fact]
    public void ResolveDefault_ReturnsAttributeValue_WhenPresent()
    {
        OperationParameterClassifier.ResolveDefault(Param(nameof(SampleParameters.StringWithDefaultValueAttribute)))
            .Should().Be("hello");
    }

    [Fact]
    public void ResolveDefault_ReturnsCompilerDefault_WhenNoAttribute()
    {
        OperationParameterClassifier.ResolveDefault(Param(nameof(SampleParameters.IntWithDefault)))
            .Should().Be(5);
    }

    [Fact]
    public void ResolveDefault_ReturnsNull_ForOptionalAttribute_Only()
    {
        OperationParameterClassifier.ResolveDefault(Param(nameof(SampleParameters.OptionalNullableInt)))
            .Should().BeNull();
    }

    // Sample method surface — each method's parameters exercise one classifier case.
    public class SampleParameters
    {
        public void PlainInt(int p) { }
        public void NullableInt(int? p) { }
        public void NullableIntWithNullDefault(int? p = null) { }
        public void IntWithDefault(int p = 5) { }
        public void ReferenceString(string p) { }
        public void StringWithDefaultValueAttribute([System.ComponentModel.DefaultValue("hello")] string p) { }
        public void AttributeOverridesCompilerDefault([System.ComponentModel.DefaultValue("attribute")] string p = "compiler") { }
        public void OptionalNullableInt([RestierOptional] int? p) { }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~OperationParameterClassifierTests"`
Expected: All 15 tests fail with build errors — type `OperationParameterClassifier` does not exist yet.

- [ ] **Step 3: Implement the classifier**

Create `src/Microsoft.Restier.AspNetCore/Model/OperationParameterClassifier.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using RestierOptional = Microsoft.Restier.AspNetCore.Model.OptionalAttribute;

namespace Microsoft.Restier.AspNetCore.Model
{
    /// <summary>
    /// Shared classification helpers for RESTier operation parameters.
    /// Used at build time by <see cref="ApiExtension.RestierWebApiOperationModelBuilder"/>
    /// and at request time by <see cref="Operation.RestierOperationExecutor"/>.
    /// </summary>
    /// <remarks>
    /// Nullability (the EDM type reference accepts <c>null</c> as a value) and
    /// optionality (the parameter may be omitted from the URL, in which case a
    /// declared default applies) are independent signals. See the magical-operations
    /// design spec for the full semantics table.
    /// </remarks>
    public static class OperationParameterClassifier
    {
        /// <summary>
        /// Returns <see langword="true"/> when the EDM type reference for this parameter
        /// should be emitted with <c>Nullable = true</c>.
        /// </summary>
        public static bool ComputeNullable(ParameterInfo parameter)
        {
            Ensure.NotNull(parameter, nameof(parameter));
            if (Nullable.GetUnderlyingType(parameter.ParameterType) is not null)
            {
                return true;
            }

            if (parameter.GetCustomAttribute<RestierOptional>(true) is not null)
            {
                return true;
            }

            return parameter.ParameterType.IsClass;
        }

        /// <summary>
        /// Classifies whether the parameter is omittable (an <c>EdmOptionalParameter</c>)
        /// and returns the literal string used as the EDM default-value attribute.
        /// </summary>
        public static (bool IsOptional, string DefaultLiteral) ClassifyOptionality(ParameterInfo parameter)
        {
            Ensure.NotNull(parameter, nameof(parameter));

            var attr = parameter.GetCustomAttribute<DefaultValueAttribute>(true);
            if (attr is not null)
            {
                return (true, FormatLiteral(attr.Value));
            }

            if (parameter.HasDefaultValue)
            {
                return (true, FormatLiteral(parameter.DefaultValue));
            }

            if (parameter.GetCustomAttribute<RestierOptional>(true) is not null)
            {
                return (true, "null");
            }

            return (false, null);
        }

        /// <summary>
        /// Returns <see langword="true"/> when this parameter, if absent from a request,
        /// should be substituted with its declared default rather than passed as null.
        /// </summary>
        public static bool IsOmittedOptional(ParameterInfo parameter)
            => ClassifyOptionality(parameter).IsOptional;

        /// <summary>
        /// Resolves the runtime CLR default value for an omitted optional parameter.
        /// </summary>
        /// <returns>
        /// The <c>[DefaultValue]</c> attribute value when present, then
        /// <see cref="ParameterInfo.DefaultValue"/> when supplied by the compiler,
        /// then <see langword="null"/>.
        /// </returns>
        public static object ResolveDefault(ParameterInfo parameter)
        {
            Ensure.NotNull(parameter, nameof(parameter));
            var attr = parameter.GetCustomAttribute<DefaultValueAttribute>(true);
            if (attr is not null)
            {
                return attr.Value;
            }

            if (parameter.HasDefaultValue)
            {
                return parameter.DefaultValue;
            }

            return null;
        }

        private static string FormatLiteral(object value)
        {
            if (value is null)
            {
                return "null";
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            return value.ToString();
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~OperationParameterClassifierTests"`
Expected: All 15 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Model/OperationParameterClassifier.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Model/OperationParameterClassifierTests.cs
git commit -m "feat(operations): add OperationParameterClassifier helper

Classifies parameter nullability and optionality independently. Shared
between RestierWebApiOperationModelBuilder (build time) and
RestierOperationExecutor (request time) so the model contract and the
runtime invocation agree on what counts as omitted-optional.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Add nullable-aware `EdmHelpers.GetTypeReference` overload

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Model/EdmHelpers.cs:75-105`
- Test: `test/Microsoft.Restier.Tests.AspNetCore/Model/EdmHelpersTests.cs` (extend if exists, else create)

- [ ] **Step 1: Check whether `EdmHelpersTests.cs` already exists**

Run: `ls test/Microsoft.Restier.Tests.AspNetCore/Model/EdmHelpersTests.cs 2>&1 || echo MISSING`
Expected: Either an absolute path to the file, or `MISSING`.

- [ ] **Step 2: Write the failing test**

If `EdmHelpersTests.cs` does not exist, create it:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

public class EdmHelpersTests
{
    private readonly EdmModel _model = new();

    [Fact]
    public void GetTypeReference_ValueType_DefaultsToNullableTrue()
    {
        // Existing two-arg overload preserves current behavior (nullable = true).
        var reference = typeof(int).GetTypeReference(_model);
        reference.Should().NotBeNull();
        reference.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void GetTypeReference_ValueType_NullableFalseOverload_EmitsNonNullable()
    {
        var reference = typeof(int).GetTypeReference(_model, nullable: false);
        reference.Should().NotBeNull();
        reference.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void GetTypeReference_ValueType_NullableTrueOverload_EmitsNullable()
    {
        var reference = typeof(int).GetTypeReference(_model, nullable: true);
        reference.Should().NotBeNull();
        reference.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void GetTypeReference_NullableValueType_AlwaysNullable()
    {
        // Nullable<T> wraps the underlying primitive; the type ref is always nullable.
        var reference = typeof(int?).GetTypeReference(_model, nullable: false);
        reference.Should().NotBeNull();
        reference.IsNullable.Should().BeTrue();
    }
}
```

If it already exists, append the four new tests inside the existing class without disturbing existing tests.

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~EdmHelpersTests.GetTypeReference"`
Expected: The two new "overload" tests fail with build error — overload doesn't exist. The existing-behavior test passes.

- [ ] **Step 4: Add the overload**

Edit `src/Microsoft.Restier.AspNetCore/Model/EdmHelpers.cs`. Replace the existing `GetTypeReference` method (lines 75–105) with two methods:

```csharp
/// <summary>
/// Get the edm type reference for a clr type. The reference is emitted as nullable.
/// </summary>
/// <param name="type">The clr type.</param>
/// <param name="model">The Edm model.</param>
/// <returns>The Edm type reference.</returns>
public static IEdmTypeReference GetTypeReference(this Type type, IEdmModel model)
    => GetTypeReference(type, model, nullable: true);

/// <summary>
/// Get the edm type reference for a clr type with explicit control over nullability.
/// </summary>
/// <param name="type">The clr type.</param>
/// <param name="model">The Edm model.</param>
/// <param name="nullable">
/// Whether the resulting type reference should be marked nullable. For <c>Nullable&lt;T&gt;</c>
/// inputs the reference is always nullable regardless of this argument.
/// </param>
/// <returns>The Edm type reference.</returns>
public static IEdmTypeReference GetTypeReference(this Type type, IEdmModel model, bool nullable)
{
    if (type is null || model is null)
    {
        return null;
    }

    if (type.TryGetElementType(out var elementType))
    {
        return EdmCoreModel.GetCollection(GetTypeReference(elementType, model, nullable));
    }

    // Nullable<T> implies a nullable reference no matter what the caller passed.
    var effectiveNullable = nullable || Nullable.GetUnderlyingType(type) is not null;

    var edmType = model.FindDeclaredType(type.FullName);

    if (edmType is IEdmEnumType enumType)
    {
        return new EdmEnumTypeReference(enumType, effectiveNullable);
    }

    if (edmType is IEdmComplexType complexType)
    {
        return new EdmComplexTypeReference(complexType, effectiveNullable);
    }

    if (edmType is IEdmEntityType entityType)
    {
        return new EdmEntityTypeReference(entityType, effectiveNullable);
    }

    return type.GetPrimitiveTypeReference(effectiveNullable);
}
```

Also update `GetPrimitiveTypeReference` to accept the nullable hint. Replace the existing definition (lines 26–44) with:

```csharp
/// <summary>
/// The type to get the primitive type reference.
/// </summary>
public static EdmTypeReference GetPrimitiveTypeReference(this Type type)
    => type.GetPrimitiveTypeReference(nullable: true);

/// <summary>
/// The type to get the primitive type reference with explicit nullability.
/// </summary>
public static EdmTypeReference GetPrimitiveTypeReference(this Type type, bool nullable)
{
    if (type is null)
    {
        throw new ArgumentNullException(nameof(type));
    }

    var primitiveTypeKind = EdmHelpers.GetPrimitiveTypeKind(type, out var isNullableValueType);

    if (!primitiveTypeKind.HasValue)
    {
        return null;
    }

    // Nullable<T> always emits nullable. Otherwise honor the caller's hint.
    return new EdmPrimitiveTypeReference(
        EdmCoreModel.Instance.GetPrimitiveType(primitiveTypeKind.Value),
        isNullableValueType || nullable);
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~EdmHelpersTests.GetTypeReference"`
Expected: All four tests pass.

- [ ] **Step 6: Run the full Tests.AspNetCore suite for regressions**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj`
Expected: All existing tests still pass (the two-arg overload preserves the original behavior).

- [ ] **Step 7: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Model/EdmHelpers.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Model/EdmHelpersTests.cs
git commit -m "feat(edm): EdmHelpers.GetTypeReference accepts explicit nullable

Adds a (this Type, IEdmModel, bool nullable) overload so operation
parameter emission can request non-nullable value-type refs when neither
nullability nor optionality is signalled. The original two-argument
overload preserves the existing default-to-nullable behavior. Nullable<T>
inputs are always emitted nullable regardless of the hint.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Change `OperationContext.GetParameterValueFunc` to presence-aware delegate

**Files:**
- Modify: `src/Microsoft.Restier.Core/Operation/OperationContext.cs`

This is a binary-breaking change to a public API in `Microsoft.Restier.Core`. After this task, the solution will not compile until Task 5 updates the controller and Task 6 updates the executor. Tasks 4–6 must be implemented sequentially without a commit gap that leaves `main`/`feature/vnext` in a broken state.

- [ ] **Step 1: Update the contract**

Edit `src/Microsoft.Restier.Core/Operation/OperationContext.cs`. Replace lines 34–59 (constructor + `GetParameterValueFunc` property) with:

```csharp
/// <summary>
/// Initializes a new instance of the <see cref="OperationContext" /> class.
/// </summary>
/// <param name="api">An Api.</param>
/// <param name="getParameterValueFunc">
/// The function used to retrieve a parameter's URL value alongside a presence flag.
/// The flag is <see langword="true"/> when the parameter name appears in the request
/// URL or body, regardless of whether the value is <see langword="null"/>.
/// </param>
/// <param name="operationName">The operation name.</param>
/// <param name="isFunction">A flag indicating this is a function call or action call.</param>
/// <param name="bindingParameterValue">
/// A queryable for the binding-parameter value; <see langword="null"/> for function/action imports.
/// </param>
public OperationContext(
    ApiBase api,
    Func<string, (bool Present, object Value)> getParameterValueFunc,
    string operationName,
    bool isFunction,
    IEnumerable bindingParameterValue)
    : base(api)
{
    Ensure.NotNull(getParameterValueFunc, nameof(getParameterValueFunc));
    Ensure.NotNullOrWhiteSpace(operationName, nameof(operationName));

    GetParameterValueFunc = getParameterValueFunc;
    OperationName = operationName;
    IsFunction = isFunction;
    BindingParameterValue = bindingParameterValue;
}

/// <summary>
/// Gets the operation name.
/// </summary>
public string OperationName { get; }

/// <summary>
/// Gets the function used to retrieve a parameter's URL value along with a
/// presence flag distinguishing an omitted parameter (Present = false) from
/// an explicit null value (Present = true, Value = null).
/// </summary>
public Func<string, (bool Present, object Value)> GetParameterValueFunc { get; }
```

- [ ] **Step 2: Build to surface compile errors**

Run: `dotnet build src/Microsoft.Restier.Core/Microsoft.Restier.Core.csproj`
Expected: Build succeeds (no internal callers within Core).

Run: `dotnet build RESTier.slnx 2>&1 | grep -E "error|RestierController|RestierOperationExecutor" | head -20`
Expected: Compile errors in `RestierController.cs` and `RestierOperationExecutor.cs` referencing `getParaValueFunc` / `GetParameterValueFunc`.

- [ ] **Step 3: Do NOT commit yet**

The repository is now in a broken state. Tasks 5 and 6 restore compilation. Proceed directly to Task 5.

---

## Task 5: Update `RestierController` to construct the presence-aware delegate

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs:109`
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs:143`

- [ ] **Step 1: Update the unbound function delegate**

Edit `src/Microsoft.Restier.AspNetCore/RestierController.cs`. Replace line 109:

```csharp
Func<string, object> getParaValueFunc = p => unboundSegment.Parameters.FirstOrDefault(c => c.Name == p).Value;
```

with:

```csharp
Func<string, (bool Present, object Value)> getParaValueFunc = p =>
{
    var match = unboundSegment.Parameters.FirstOrDefault(c => c.Name == p);
    return (match is not null, match?.Value);
};
```

- [ ] **Step 2: Update the bound function delegate**

Replace line 143 the same way:

```csharp
Func<string, (bool Present, object Value)> getParaValueFunc = p =>
{
    var match = segment.Parameters.FirstOrDefault(c => c.Name == p);
    return (match is not null, match?.Value);
};
```

- [ ] **Step 3: Build to verify the controller compiles**

Run: `dotnet build src/Microsoft.Restier.AspNetCore/Microsoft.Restier.AspNetCore.csproj 2>&1 | tail -20`
Expected: Compile errors remain only in `RestierOperationExecutor.cs` (still expects the old signature).

- [ ] **Step 4: Do NOT commit yet**

Repository remains in a broken state. Proceed directly to Task 6.

---

## Task 6: Update `RestierOperationExecutor` for presence + default substitution, with unit tests

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Operation/RestierOperationExecutor.cs:102-130`
- Test: `test/Microsoft.Restier.Tests.AspNetCore/Operation/RestierOperationExecutorOptionalTests.cs` (new)

- [ ] **Step 1: Write the failing test file**

Create `test/Microsoft.Restier.Tests.AspNetCore/Operation/RestierOperationExecutorOptionalTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Restier.AspNetCore.Operation;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Operation;
using NSubstitute;
using Xunit;
using RestierOptional = Microsoft.Restier.AspNetCore.Model.OptionalAttribute;

namespace Microsoft.Restier.Tests.AspNetCore.Operation;

/// <summary>
/// Direct executor unit tests focused on omitted-optional-parameter handling.
/// These tests bypass HTTP routing and feed a fake presence-aware delegate
/// straight into the operation context so each (present, value) case can
/// be exercised in isolation.
/// </summary>
public class RestierOperationExecutorOptionalTests
{
    private readonly IChainOfResponsibilityFactory<IOperationAuthorizer> _authorizerFactory
        = Substitute.For<IChainOfResponsibilityFactory<IOperationAuthorizer>>();

    private readonly IChainOfResponsibilityFactory<IOperationFilter> _filterFactory
        = Substitute.For<IChainOfResponsibilityFactory<IOperationFilter>>();

    private readonly OptionalParamsApi _api = new();

    [Fact]
    public async Task OmittedCompilerDefault_PassesDeclaredDefault()
    {
        var executor = new RestierOperationExecutor(_authorizerFactory, _filterFactory);
        var ctx = BuildContext(
            api: _api,
            operationName: nameof(OptionalParamsApi.IntWithDefault),
            isFunction: true,
            delegateImpl: _ => (false, null));   // omitted

        await executor.ExecuteOperationAsync(ctx, CancellationToken.None);

        _api.LastReceived.Should().Be(5);
    }

    [Fact]
    public async Task OmittedDefaultValueAttribute_PassesAttributeValue()
    {
        var executor = new RestierOperationExecutor(_authorizerFactory, _filterFactory);
        var ctx = BuildContext(
            api: _api,
            operationName: nameof(OptionalParamsApi.StringWithDefaultAttr),
            isFunction: true,
            delegateImpl: _ => (false, null));

        await executor.ExecuteOperationAsync(ctx, CancellationToken.None);

        _api.LastReceived.Should().Be("hello");
    }

    [Fact]
    public async Task ExplicitNullOnNullable_PassesNull()
    {
        var executor = new RestierOperationExecutor(_authorizerFactory, _filterFactory);
        var ctx = BuildContext(
            api: _api,
            operationName: nameof(OptionalParamsApi.NullableInt),
            isFunction: true,
            delegateImpl: _ => (true, null));   // explicit null

        await executor.ExecuteOperationAsync(ctx, CancellationToken.None);

        _api.LastReceived.Should().BeNull();
    }

    [Fact]
    public async Task ExplicitNullOnNullableWithDefault_PrefersExplicitNullOverDefault()
    {
        // int? p = 5 is BOTH nullable and optional. Explicit ?p=null must pass null,
        // not substitute the default. Default substitution applies only on absence.
        var executor = new RestierOperationExecutor(_authorizerFactory, _filterFactory);
        var ctx = BuildContext(
            api: _api,
            operationName: nameof(OptionalParamsApi.NullableIntWithDefault),
            isFunction: true,
            delegateImpl: _ => (true, null));

        await executor.ExecuteOperationAsync(ctx, CancellationToken.None);

        _api.LastReceived.Should().BeNull();
    }

    [Fact]
    public async Task OmittedNullableWithDefault_SubstitutesDefault()
    {
        var executor = new RestierOperationExecutor(_authorizerFactory, _filterFactory);
        var ctx = BuildContext(
            api: _api,
            operationName: nameof(OptionalParamsApi.NullableIntWithDefault),
            isFunction: true,
            delegateImpl: _ => (false, null));

        await executor.ExecuteOperationAsync(ctx, CancellationToken.None);

        _api.LastReceived.Should().Be(5);
    }

    // RestierOperationContext requires both API and request context fields. Build a
    // minimally-populated instance backed by stub services and the fake delegate.
    private static RestierOperationContext BuildContext(
        ApiBase api,
        string operationName,
        bool isFunction,
        Func<string, (bool Present, object Value)> delegateImpl)
        => new(
            api,
            delegateImpl,
            operationName,
            isFunction,
            bindingParameterValue: null);

    // Test API surface. Each method captures the value it actually received so the
    // assertions can verify what the executor passed into the reflected invocation.
    public class OptionalParamsApi : ApiBase
    {
        public object LastReceived { get; private set; }

        public OptionalParamsApi() : base(serviceProvider: null) { }

        public int IntWithDefault(int p = 5)
        {
            LastReceived = p;
            return p;
        }

        public string StringWithDefaultAttr([System.ComponentModel.DefaultValue("hello")] string p)
        {
            LastReceived = p;
            return p;
        }

        public int? NullableInt(int? p)
        {
            LastReceived = p;
            return p;
        }

        public int? NullableIntWithDefault(int? p = 5)
        {
            LastReceived = p;
            return p;
        }
    }
}
```

- [ ] **Step 2: Update the executor**

Edit `src/Microsoft.Restier.AspNetCore/Operation/RestierOperationExecutor.cs`. Replace lines 102–130 (the parameter loop):

```csharp
for (; paraIndex < parameterArray.Length; paraIndex++)
{
    var parameter = parameterArray[paraIndex];
    var (isPresent, urlValue) = restierOperationContext.GetParameterValueFunc(parameter.Name);

    object convertedValue;
    if (!isPresent && OperationParameterClassifier.IsOmittedOptional(parameter))
    {
        // Parameter omitted from URL/body and declared omittable → substitute
        // the declared default. MethodInfo.Invoke does not honor compile-time
        // defaults, so we resolve them here.
        convertedValue = OperationParameterClassifier.ResolveDefault(parameter);
    }
    else if (restierOperationContext.IsFunction)
    {
        var parameterTypeRef = parameter.ParameterType.GetTypeReference(model);

        // Change to right CLR class for collection/Enum/Complex/Entity
        // JWS: As long as OData requires the ServiceProvider, we have to provide it. DI abuse smell.
        convertedValue = DeserializationHelpers.ConvertValue(
            urlValue,
            parameter.Name,
            parameter.ParameterType,
            parameterTypeRef,
            model,
            restierOperationContext.Request,
            restierOperationContext.Request.GetRouteServices());
    }
    else
    {
        convertedValue = DeserializationHelpers.ConvertCollectionType(
            urlValue, parameter.ParameterType);
    }

    parameters[paraIndex] = convertedValue;
}
```

Also add `using Microsoft.Restier.AspNetCore.Model;` near the top of the file if it isn't there already.

- [ ] **Step 3: Build the full solution**

Run: `dotnet build RESTier.slnx`
Expected: Build succeeds across all projects. Solution compiles again after the Task 4/5/6 sequence.

- [ ] **Step 4: Run the new executor tests**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierOperationExecutorOptionalTests"`
Expected: All 5 tests pass.

- [ ] **Step 5: Run the existing executor and HTTP integration suites**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~Operation|FunctionTests|ActionTests"`
Expected: All existing operation/function/action tests still pass. If any fail, debug — the test scenarios likely call functions that never had `(Present, Value)` accounted for, but the new branch only diverges from prior behavior when a parameter is genuinely omittable.

- [ ] **Step 6: Commit Tasks 4–6 together**

```bash
git add src/Microsoft.Restier.Core/Operation/OperationContext.cs \
        src/Microsoft.Restier.AspNetCore/RestierController.cs \
        src/Microsoft.Restier.AspNetCore/Operation/RestierOperationExecutor.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Operation/RestierOperationExecutorOptionalTests.cs
git commit -m "feat(operations)!: presence-aware parameter delegate

BREAKING CHANGE: OperationContext.GetParameterValueFunc changes from
Func<string, object> to Func<string, (bool Present, object Value)>.
Downstream consumers that construct an OperationContext directly or
override the parameter delegate in a custom RestierController must
migrate.

Why: RestierController previously built the value delegate as
segment.Parameters.FirstOrDefault(...).Value, which returns null for
both 'URL omitted the parameter' and 'URL supplied p=null'. The
executor could not distinguish those cases, so omitted-optional
defaulting and explicit-null semantics had no clean contract.

With the presence flag, the executor branches:
- !present + omittable → substitute resolved default (via
  OperationParameterClassifier.ResolveDefault)
- !present + required → existing ConvertValue(null, ...) path
- present + null → existing ConvertValue path; succeeds when the
  EDM type ref is nullable
- present + value → existing ConvertValue path

Explicit ?p=null on a parameter that is both nullable and optional
(e.g. int? p = 5) now passes null, not the default.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 7: Add namespace+name dedup with Trace warning

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Model/ApiExtension/RestierWebApiOperationModelBuilder.cs:127-193`
- Test: `test/Microsoft.Restier.Tests.AspNetCore/Model/RestierWebApiOperationModelBuilderTests.cs` (extend)

- [ ] **Step 1: Write the failing dedup tests**

Append the following test methods to `RestierWebApiOperationModelBuilderTests.cs` (just before the closing `}` of the test class):

```csharp
[Fact]
public void GetEdmModel_DuplicateOperationByName_SkipsAttributeAdditionWithWarning()
{
    var testTraceListener = new TestTraceListener();
    Trace.Listeners.Add(testTraceListener);
    try
    {
        // Arrange — inner model already declares an EdmFunction named "SampleMethod".
        var edmModel = new EdmModel();
        var container = new EdmEntityContainer("TestNamespace", "DefaultContainer");
        edmModel.AddElement(container);
        var preexistingFunction = new EdmFunction(typeof(SampleApi).Namespace, "SampleMethod",
            EdmCoreModel.Instance.GetPrimitiveType(EdmPrimitiveTypeKind.Int32).ToTypeReference());
        edmModel.AddElement(preexistingFunction);
        container.AddFunctionImport("SampleMethod", preexistingFunction);
        _innerModelBuilder.GetEdmModel().Returns(edmModel);

        var extender = new RestierWebApiModelExtender(_targetApiType);
        var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender)
        {
            Inner = _innerModelBuilder
        };

        // Act
        var result = builder.GetEdmModel();

        // Assert — exactly one operation declared (the original), exactly one import.
        result.Should().NotBeNull();
        result.SchemaElements.OfType<IEdmOperation>().Count(o => o.Name == "SampleMethod").Should().Be(1);
        result.EntityContainer.Elements.OfType<IEdmOperationImport>().Count(i => i.Name == "SampleMethod").Should().Be(1);
        testTraceListener.Messages.Should().Contain(m => m.Contains("already declared") && m.Contains("SampleMethod"));
    }
    finally
    {
        Trace.Listeners.Remove(testTraceListener);
    }
}
```

Add a small extension helper near the top of the file (above the class) if `ToTypeReference` doesn't exist as a helper:

```csharp
internal static class TestEdmExtensions
{
    public static IEdmPrimitiveTypeReference ToTypeReference(this IEdmPrimitiveType type)
        => new EdmPrimitiveTypeReference(type, isNullable: false);
}
```

(Skip the helper if your local copy of the test project already exposes one.)

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierWebApiOperationModelBuilderTests.GetEdmModel_DuplicateOperationByName"`
Expected: FAIL — current builder adds a duplicate without warning.

- [ ] **Step 3: Implement the dedup in `BuildOperations`**

Edit `src/Microsoft.Restier.AspNetCore/Model/ApiExtension/RestierWebApiOperationModelBuilder.cs`. Add this guard at the top of the `foreach (var operationInfo in operationInfos)` loop in `BuildOperations` (after `namespaceName` is computed but before the `EdmFunction` / `EdmAction` construction at lines 161–169):

```csharp
// Dedup by namespace+name — RestierOperationExecutor dispatches by name only
// (see RestierOperationExecutor.cs:78-80), so a same-name pair would be either
// unreachable or trigger AmbiguousMatchException. Same-signature overloads are
// out of scope; see the magical-operations design.
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

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierWebApiOperationModelBuilderTests.GetEdmModel_DuplicateOperationByName"`
Expected: PASS.

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierWebApiOperationModelBuilderTests"`
Expected: All existing tests in the class still pass.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Model/ApiExtension/RestierWebApiOperationModelBuilder.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Model/RestierWebApiOperationModelBuilderTests.cs
git commit -m "feat(operations): dedupe [Operation] registrations by name

When the same operation is declared both manually via ODataModelBuilder
and via an [Operation]-family attribute, skip the attribute-driven add
with a Trace warning. Matches by namespace+name only — overloads are
unsupported by RestierOperationExecutor's name-based dispatch, and a
followup spec would be needed to enable them.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 8: Wire `OperationParameterClassifier` into `BuildOperationParameters`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Model/ApiExtension/RestierWebApiOperationModelBuilder.cs:117-125`
- Test: `test/Microsoft.Restier.Tests.AspNetCore/Model/RestierWebApiOperationModelBuilderTests.cs` (extend)

- [ ] **Step 1: Write the failing parameter-classification tests**

Append to `RestierWebApiOperationModelBuilderTests.cs`. First, extend the `SampleApi` test class at the bottom with parameter cases:

```csharp
public class SampleApi
{
    [UnboundOperation]
    public int SampleMethod() => 42;

    [BoundOperation]
    public int WrongBoundMethod() => 42;

    [UnboundOperation]
    public int IntWithDefault(int p = 5) => p;

    [UnboundOperation]
    public int? NullableInt(int? p) => p;

    [UnboundOperation]
    public int? NullableIntWithDefault(int? p = null) => p;

    [UnboundOperation]
    public string OptionalRef([Microsoft.Restier.AspNetCore.Model.OptionalAttribute] string p) => p;

    [UnboundOperation]
    public string DefaultValueAttr([System.ComponentModel.DefaultValue("hello")] string p) => p;

    [UnboundOperation]
    public int PlainValueType(int p) => p;
}
```

Then add these test methods to the test class:

```csharp
[Theory]
[InlineData(nameof(SampleApi.IntWithDefault), "5", false)]
[InlineData(nameof(SampleApi.NullableIntWithDefault), "null", true)]
[InlineData(nameof(SampleApi.OptionalRef), "null", true)]
[InlineData(nameof(SampleApi.DefaultValueAttr), "hello", true)]
public void GetEdmModel_EmitsOptionalParameter_WithExpectedDefaultAndNullability(
    string operationName, string expectedDefault, bool expectedNullable)
{
    var edmModel = new EdmModel();
    edmModel.AddElement(new EdmEntityContainer("TestNamespace", "DefaultContainer"));
    _innerModelBuilder.GetEdmModel().Returns(edmModel);
    var extender = new RestierWebApiModelExtender(_targetApiType);
    var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender) { Inner = _innerModelBuilder };

    var result = builder.GetEdmModel() as EdmModel;

    var op = result.SchemaElements.OfType<IEdmOperation>().Single(o => o.Name == operationName);
    var param = op.Parameters.Single();
    param.Should().BeAssignableTo<IEdmOptionalParameter>();
    ((IEdmOptionalParameter)param).DefaultValueString.Should().Be(expectedDefault);
    param.Type.IsNullable.Should().Be(expectedNullable);
}

[Fact]
public void GetEdmModel_BareNullableParam_IsNullableButRequired()
{
    var edmModel = new EdmModel();
    edmModel.AddElement(new EdmEntityContainer("TestNamespace", "DefaultContainer"));
    _innerModelBuilder.GetEdmModel().Returns(edmModel);
    var extender = new RestierWebApiModelExtender(_targetApiType);
    var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender) { Inner = _innerModelBuilder };

    var result = builder.GetEdmModel() as EdmModel;

    var op = result.SchemaElements.OfType<IEdmOperation>().Single(o => o.Name == nameof(SampleApi.NullableInt));
    var param = op.Parameters.Single();
    param.Should().NotBeAssignableTo<IEdmOptionalParameter>();   // not optional
    param.Type.IsNullable.Should().BeTrue();                     // but nullable
}

[Fact]
public void GetEdmModel_PlainValueTypeParam_IsNonNullableAndRequired()
{
    var edmModel = new EdmModel();
    edmModel.AddElement(new EdmEntityContainer("TestNamespace", "DefaultContainer"));
    _innerModelBuilder.GetEdmModel().Returns(edmModel);
    var extender = new RestierWebApiModelExtender(_targetApiType);
    var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender) { Inner = _innerModelBuilder };

    var result = builder.GetEdmModel() as EdmModel;

    var op = result.SchemaElements.OfType<IEdmOperation>().Single(o => o.Name == nameof(SampleApi.PlainValueType));
    var param = op.Parameters.Single();
    param.Should().NotBeAssignableTo<IEdmOptionalParameter>();
    param.Type.IsNullable.Should().BeFalse();
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierWebApiOperationModelBuilderTests"`
Expected: The six new tests fail — current `BuildOperationParameters` emits required, nullable-by-default `EdmOperationParameter` for everything.

- [ ] **Step 3: Implement the parameter classification path**

Replace `BuildOperationParameters` (lines 117–125) in `RestierWebApiOperationModelBuilder.cs` with:

```csharp
private static void BuildOperationParameters(EdmOperation operation, MethodInfo method, IEdmModel model)
{
    foreach (var parameter in method.GetParameters())
    {
        var isNullable = OperationParameterClassifier.ComputeNullable(parameter);
        var (isOptional, defaultLiteral) = OperationParameterClassifier.ClassifyOptionality(parameter);

        var parameterTypeReference = parameter.ParameterType.GetTypeReference(model, isNullable);

        EdmOperationParameter operationParam = isOptional
            ? new EdmOptionalParameter(operation, parameter.Name, parameterTypeReference, defaultLiteral)
            : new EdmOperationParameter(operation, parameter.Name, parameterTypeReference);

        operation.AddParameter(operationParam);
    }
}
```

Add `using Microsoft.Restier.AspNetCore.Model;` to the file if not already imported (the namespace is the same as the builder, so this is implicit — but verify after the edit).

- [ ] **Step 4: Run the parameter tests**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierWebApiOperationModelBuilderTests"`
Expected: All tests pass.

- [ ] **Step 5: Run the full Tests.AspNetCore suite for regressions**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj`
Expected: All tests pass. The classifier should produce the same EDM shape that the prior code produced for plain (non-optional, non-nullable) parameters — verify by inspecting any failures.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Model/ApiExtension/RestierWebApiOperationModelBuilder.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Model/RestierWebApiOperationModelBuilderTests.cs
git commit -m "feat(operations): emit optional + nullable parameter shapes

Use OperationParameterClassifier in BuildOperationParameters so each
parameter is independently classified for nullability (type ref) and
optionality (EdmOptionalParameter with default literal). The four
signal sources — HasDefaultValue, [DefaultValue], Nullable<T>,
[Optional] — produce the EDM shape required to address issue #656
without conflating 'accept null' with 'omittable'.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 9: Emit `[Obsolete]` → `Core.V1.Revisions` and parameter `[Description]`

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Model/ApiExtension/RestierWebApiOperationModelBuilder.cs`
- Test: `test/Microsoft.Restier.Tests.AspNetCore/Model/RestierWebApiOperationModelBuilderTests.cs` (extend)

Note: `[Description]` on operation methods already emits `Core.V1.Description` per commit `be817e21`. This task adds the parameter-level analog and the `[Obsolete]` mapping.

- [ ] **Step 1: Write the failing annotation tests**

Add to `SampleApi`:

```csharp
[UnboundOperation]
[System.Obsolete("Use NewMethod instead.")]
public int DeprecatedMethod() => 42;

[UnboundOperation]
public int ParamWithDescription([System.ComponentModel.Description("Search string.")] string query) => 0;
```

Add to the test class:

```csharp
[Fact]
public void GetEdmModel_ObsoleteOperation_EmitsRevisionsAnnotation()
{
    var edmModel = new EdmModel();
    edmModel.AddElement(new EdmEntityContainer("TestNamespace", "DefaultContainer"));
    _innerModelBuilder.GetEdmModel().Returns(edmModel);
    var extender = new RestierWebApiModelExtender(_targetApiType);
    var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender) { Inner = _innerModelBuilder };

    var result = builder.GetEdmModel();

    var op = result.SchemaElements.OfType<IEdmOperation>().Single(o => o.Name == nameof(SampleApi.DeprecatedMethod));
    var annotations = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(op).ToList();
    annotations.Should().Contain(a => a.Term.FullName().Contains("Revisions"));
    var revisionAnnotation = annotations.First(a => a.Term.FullName().Contains("Revisions"));
    revisionAnnotation.Value.Should().NotBeNull();
    // Spot-check the obsolete message survives into the annotation; structural form
    // depends on the vocabulary-record construction.
    revisionAnnotation.ToString().Should().Contain("Use NewMethod instead.");
}

[Fact]
public void GetEdmModel_ParameterDescription_EmitsCoreDescription()
{
    var edmModel = new EdmModel();
    edmModel.AddElement(new EdmEntityContainer("TestNamespace", "DefaultContainer"));
    _innerModelBuilder.GetEdmModel().Returns(edmModel);
    var extender = new RestierWebApiModelExtender(_targetApiType);
    var builder = new RestierWebApiOperationModelBuilder(_targetApiType, extender) { Inner = _innerModelBuilder };

    var result = builder.GetEdmModel();

    var op = result.SchemaElements.OfType<IEdmOperation>().Single(o => o.Name == nameof(SampleApi.ParamWithDescription));
    var queryParam = op.Parameters.Single(p => p.Name == "query");
    var paramAnnotations = result.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(queryParam).ToList();
    paramAnnotations.Should().Contain(a => a.Term.FullName().EndsWith("Description"));
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierWebApiOperationModelBuilderTests.GetEdmModel_ObsoleteOperation|FullyQualifiedName~RestierWebApiOperationModelBuilderTests.GetEdmModel_ParameterDescription"`
Expected: Both tests fail — annotations not emitted.

- [ ] **Step 3: Implement the annotation emission**

Edit `RestierWebApiOperationModelBuilder.cs`. After `model.AddElement(operation)` inside `BuildOperations` (around line 172), add:

```csharp
EmitOperationAnnotations(model, operation, operationInfo.Method);
EmitParameterAnnotations(model, operation, operationInfo.Method);
```

Add these methods to the class (private static):

```csharp
private static void EmitOperationAnnotations(EdmModel model, EdmOperation operation, MethodInfo method)
{
    var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
    if (!string.IsNullOrWhiteSpace(description))
    {
        model.AddVocabularyAnnotation(new EdmVocabularyAnnotation(
            operation,
            CoreVocabularyModel.DescriptionTerm,
            new EdmStringConstant(description)));
    }

    var obsolete = method.GetCustomAttribute<ObsoleteAttribute>();
    if (obsolete is not null)
    {
        model.AddVocabularyAnnotation(BuildRevisionsAnnotation(operation, obsolete));
    }
}

private static void EmitParameterAnnotations(EdmModel model, EdmOperation operation, MethodInfo method)
{
    var clrParams = method.GetParameters();
    foreach (var edmParam in operation.Parameters)
    {
        var clrParam = clrParams.FirstOrDefault(p => p.Name == edmParam.Name);
        if (clrParam is null) continue;

        var description = clrParam.GetCustomAttribute<DescriptionAttribute>()?.Description;
        if (!string.IsNullOrWhiteSpace(description))
        {
            model.AddVocabularyAnnotation(new EdmVocabularyAnnotation(
                (IEdmVocabularyAnnotatable)edmParam,
                CoreVocabularyModel.DescriptionTerm,
                new EdmStringConstant(description)));
        }

        var obsolete = clrParam.GetCustomAttribute<ObsoleteAttribute>();
        if (obsolete is not null)
        {
            model.AddVocabularyAnnotation(BuildRevisionsAnnotation((IEdmVocabularyAnnotatable)edmParam, obsolete));
        }
    }
}

private static EdmVocabularyAnnotation BuildRevisionsAnnotation(IEdmVocabularyAnnotatable target, ObsoleteAttribute obsolete)
{
    // Core.V1.Revisions is a Collection(Core.V1.RevisionType). Each entry has
    // Version (string), Kind (enum: Added/Modified/Deprecated), Description (string).
    // Emit a single-entry collection with Kind = Deprecated.
    var revisionsTerm = CoreVocabularyModel.Instance.FindDeclaredTerm("Org.OData.Core.V1.Revisions");
    if (revisionsTerm is null)
    {
        // Fallback — different Core vocabulary version. Skip rather than crash.
        return null;
    }

    var revisionRecord = new EdmRecordExpression(
        new EdmPropertyConstructor("Version", new EdmStringConstant("obsolete")),
        new EdmPropertyConstructor("Kind", new EdmEnumMemberExpression(
            ((IEdmEnumType)revisionsTerm.Type.AsCollection().ElementType()
                .AsComplex().ComplexDefinition().FindProperty("Kind").Type.Definition).Members
                .First(m => m.Name == "Deprecated"))),
        new EdmPropertyConstructor("Description", new EdmStringConstant(obsolete.Message ?? "Deprecated.")));

    return new EdmVocabularyAnnotation(target, revisionsTerm,
        new EdmCollectionExpression(revisionRecord));
}
```

Add usings at the top of the file:

```csharp
using System.ComponentModel;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.Edm.Vocabularies.V1;
using Microsoft.OData.Edm.Csdl;
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~RestierWebApiOperationModelBuilderTests"`
Expected: All tests pass.

If the `BuildRevisionsAnnotation` Kind-enum construction throws or the term lookup returns null, debug by inspecting the actual `Core.V1` term structure for the pinned `Microsoft.OData.Edm` version (8.4.3). The fallback path returns `null` and the caller (Step 3 added) needs to skip null returns — adjust the call sites:

```csharp
var revisions = BuildRevisionsAnnotation(operation, obsolete);
if (revisions is not null)
{
    model.AddVocabularyAnnotation(revisions);
}
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Model/ApiExtension/RestierWebApiOperationModelBuilder.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Model/RestierWebApiOperationModelBuilderTests.cs
git commit -m "feat(operations): emit [Obsolete] and parameter [Description]

Method-level [Obsolete] becomes a Core.V1.Revisions annotation with
Kind=Deprecated; parameter-level [Description] becomes a Core.V1.Description
annotation. Mirrors the existing operation-level [Description] support
introduced in be817e21.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 10: Create `OperationTypeRegistrationModelBuilder` with unit tests

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore/Model/ApiExtension/OperationTypeRegistrationModelBuilder.cs`
- Test: `test/Microsoft.Restier.Tests.AspNetCore/Model/OperationTypeRegistrationModelBuilderTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `test/Microsoft.Restier.Tests.AspNetCore/Model/OperationTypeRegistrationModelBuilderTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Model;
using NSubstitute;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Model;

public class OperationTypeRegistrationModelBuilderTests
{
    private readonly IModelBuilder _innerModelBuilder = Substitute.For<IModelBuilder>();

    [Fact]
    public void GetEdmModel_NullInnerModel_ReturnsNull()
    {
        _innerModelBuilder.GetEdmModel().Returns((IEdmModel)null);
        var builder = new OperationTypeRegistrationModelBuilder(typeof(EmptyApi))
        {
            Inner = _innerModelBuilder,
        };

        builder.GetEdmModel().Should().BeNull();
    }

    [Fact]
    public void GetEdmModel_NoOperations_PassesModelThrough()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(EmptyApi)) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel();

        result.Should().BeSameAs(inner);
        result.SchemaElements.OfType<IEdmSchemaType>().Should().BeEmpty();
    }

    [Fact]
    public void GetEdmModel_OperationWithMissingComplexType_RegistersIt()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(ApiWithUnknownComplexInput)) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel();

        result.FindDeclaredType(typeof(SearchCriteria).FullName).Should().NotBeNull();
        result.FindDeclaredType(typeof(SearchCriteria).FullName).Should().BeAssignableTo<IEdmComplexType>();
    }

    [Fact]
    public void GetEdmModel_OperationWithMissingEnumReturn_RegistersIt()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(ApiWithUnknownEnumReturn)) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel();

        result.FindDeclaredType(typeof(Color).FullName).Should().BeAssignableTo<IEdmEnumType>();
    }

    [Fact]
    public void GetEdmModel_OperationWithMissingEntityReturn_RegistersAsEntityType()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(ApiWithUnknownEntityReturn)) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel();

        var declared = result.FindDeclaredType(typeof(Author).FullName);
        declared.Should().BeAssignableTo<IEdmEntityType>();
        // No entity set is created for operation-only types.
        result.EntityContainer.EntitySets().Should().NotContain(s => s.EntityType.FullName() == typeof(Author).FullName);
    }

    [Fact]
    public void GetEdmModel_NestedComplexType_RegistersBoth()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(ApiWithNestedComplex)) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel();

        result.FindDeclaredType(typeof(Outer).FullName).Should().NotBeNull();
        result.FindDeclaredType(typeof(Inner_).FullName).Should().NotBeNull();
    }

    [Fact]
    public void GetEdmModel_TypeAlreadyDeclared_DoesNotDuplicate()
    {
        var inner = new EdmModel();
        inner.AddElement(new EdmEntityContainer("Test", "DefaultContainer"));
        // Pre-declare SearchCriteria.
        var existing = new EdmComplexType(typeof(SearchCriteria).Namespace, nameof(SearchCriteria));
        inner.AddElement(existing);
        inner.SetAnnotationValue(existing, new ClrTypeAnnotation(typeof(SearchCriteria)));
        _innerModelBuilder.GetEdmModel().Returns(inner);

        var builder = new OperationTypeRegistrationModelBuilder(typeof(ApiWithUnknownComplexInput)) { Inner = _innerModelBuilder };

        var result = builder.GetEdmModel();

        result.SchemaElements.OfType<IEdmComplexType>()
            .Count(t => t.FullName() == typeof(SearchCriteria).FullName).Should().Be(1);
    }

    // ---- Test scenario surface ----

    public class EmptyApi { }

    public class ApiWithUnknownComplexInput
    {
        [UnboundOperation]
        public int Search(SearchCriteria criteria) => 0;
    }

    public class ApiWithUnknownEnumReturn
    {
        [UnboundOperation]
        public Color GetColor() => Color.Red;
    }

    public class ApiWithUnknownEntityReturn
    {
        [UnboundOperation]
        public Author GetAuthor() => null;
    }

    public class ApiWithNestedComplex
    {
        [UnboundOperation]
        public Outer Wrap() => null;
    }

    public class SearchCriteria
    {
        public string Query { get; set; }
        public int Limit { get; set; }
    }

    public enum Color { Red, Green, Blue }

    public class Author
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Outer
    {
        public Inner_ Detail { get; set; }
    }

    public class Inner_
    {
        public string Note { get; set; }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~OperationTypeRegistrationModelBuilderTests"`
Expected: All tests fail with build errors — `OperationTypeRegistrationModelBuilder` does not exist.

- [ ] **Step 3: Implement the pre-pass model builder**

Create `src/Microsoft.Restier.AspNetCore/Model/ApiExtension/OperationTypeRegistrationModelBuilder.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.AspNetCore.Model.ApiExtension;

/// <summary>
/// Pre-pass model builder that scans the target API for methods decorated with
/// <see cref="OperationAttribute"/> family attributes and registers any
/// CLR types referenced by their parameters or return types that the inner
/// model has not already declared. Runs between <see cref="RestierWebApiModelBuilder"/>
/// and <see cref="RestierWebApiOperationModelBuilder"/> in the chain.
/// </summary>
public class OperationTypeRegistrationModelBuilder : IModelBuilder
{
    private readonly Type _targetApiType;

    /// <summary>
    /// Gets or sets the inner model builder.
    /// </summary>
    public IModelBuilder Inner { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationTypeRegistrationModelBuilder"/> class.
    /// </summary>
    public OperationTypeRegistrationModelBuilder(Type targetApiType)
    {
        Ensure.NotNull(targetApiType, nameof(targetApiType));
        _targetApiType = targetApiType;
    }

    /// <inheritdoc />
    public IEdmModel GetEdmModel()
    {
        var inner = Inner?.GetEdmModel();
        if (inner is not EdmModel model)
        {
            return inner;
        }

        var referencedTypes = CollectReferencedTypes();
        var missingTypes = referencedTypes
            .Where(t => model.FindDeclaredType(t.FullName) is null && !IsBuiltInPrimitive(t))
            .Distinct()
            .ToList();
        if (missingTypes.Count == 0)
        {
            return model;
        }

        try
        {
            MergeIntoInnerModel(model, missingTypes);
        }
        catch (Exception ex)
        {
            // The auxiliary builder is best-effort; never crash model-build on a
            // convention failure. Log and let the downstream operation builder
            // report a clearer error if the type is still unresolved.
            Trace.TraceWarning(
                $"Restier: OperationTypeRegistrationModelBuilder failed to register one or more types referenced " +
                $"by [Operation]-decorated methods. Error: {ex.Message}");
        }

        return model;
    }

    private HashSet<Type> CollectReferencedTypes()
    {
        var methods = _targetApiType
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object))
            .Where(m => m.GetCustomAttribute<OperationAttribute>(inherit: true) is not null);

        var seen = new HashSet<Type>();
        foreach (var method in methods)
        {
            foreach (var parameter in method.GetParameters())
            {
                AddType(parameter.ParameterType, seen);
            }

            AddType(method.ReturnType, seen);
        }

        return seen;
    }

    private static void AddType(Type type, HashSet<Type> seen)
    {
        if (type is null)
        {
            return;
        }

        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        // Unwrap collection wrappers (arrays, IEnumerable<T>, IQueryable<T>).
        if (underlying.IsArray && underlying.GetElementType() is not null)
        {
            AddType(underlying.GetElementType(), seen);
            return;
        }

        if (underlying.IsGenericType)
        {
            var generic = underlying.GetGenericTypeDefinition();
            if (generic == typeof(System.Collections.Generic.IEnumerable<>)
                || generic == typeof(System.Collections.Generic.IList<>)
                || generic == typeof(System.Collections.Generic.ICollection<>)
                || generic == typeof(System.Linq.IQueryable<>)
                || generic == typeof(System.Collections.Generic.List<>))
            {
                AddType(underlying.GetGenericArguments()[0], seen);
                return;
            }
        }

        if (underlying.IsValueType && !underlying.IsEnum)
        {
            return;   // primitives handled by EdmHelpers
        }

        if (underlying == typeof(string) || underlying == typeof(void) || underlying == typeof(object))
        {
            return;
        }

        seen.Add(underlying);
    }

    private static bool IsBuiltInPrimitive(Type type)
        => type.IsPrimitive || type == typeof(string) || type == typeof(decimal)
           || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid)
           || type == typeof(TimeSpan) || type == typeof(byte[]);

    private static void MergeIntoInnerModel(EdmModel model, List<Type> missingTypes)
    {
        var auxBuilder = new ODataConventionModelBuilder();

        // Pre-ignore every type the inner model already declares so the auxiliary
        // builder does not re-emit it. ClrTypeAnnotation is read directly because
        // EdmHelpers.GetClrType throws when the annotation is absent.
        var alreadyKnown = model.SchemaElements.OfType<IEdmSchemaType>()
            .Select(s => model.GetAnnotationValue<ClrTypeAnnotation>(s)?.ClrType)
            .Where(t => t is not null)
            .Distinct()
            .ToArray();
        if (alreadyKnown.Length > 0)
        {
            auxBuilder.Ignore(alreadyKnown);
        }

        foreach (var type in missingTypes)
        {
            if (type.IsEnum)
            {
                auxBuilder.AddEnumType(type);
                continue;
            }

            if (HasKey(type))
            {
                auxBuilder.AddEntityType(type);
                continue;
            }

            auxBuilder.AddComplexType(type);
        }

        var auxModel = auxBuilder.GetEdmModel() as EdmModel;
        if (auxModel is null)
        {
            return;
        }

        foreach (var element in auxModel.SchemaElements.OfType<IEdmSchemaElement>())
        {
            if (element is IEdmSchemaType schemaType
                && model.FindDeclaredType(schemaType.FullName()) is null)
            {
                model.AddElement(schemaType);
            }
        }
    }

    private static bool HasKey(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttributes(true).Any(a => a.GetType().Name == "KeyAttribute"))
            {
                return true;
            }

            if (string.Equals(property.Name, "Id", StringComparison.Ordinal)
                || string.Equals(property.Name, type.Name + "Id", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
```

Note: the `_` suffix on `Inner_` in the test scenario avoids a name collision with the builder's `Inner` property — unrelated to production code.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~OperationTypeRegistrationModelBuilderTests"`
Expected: All 7 tests pass.

If `ODataConventionModelBuilder.Ignore(System.Type[])` signature differs from `params Type[]` (per the 2.0.0 NuGet metadata it's a fixed array), wrap with `new[] { ... }` as needed:

```csharp
auxBuilder.Ignore(new[] { clr });
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Model/ApiExtension/OperationTypeRegistrationModelBuilder.cs \
        test/Microsoft.Restier.Tests.AspNetCore/Model/OperationTypeRegistrationModelBuilderTests.cs
git commit -m "feat(operations): auto-register types referenced by [Operation] methods

New OperationTypeRegistrationModelBuilder runs before the existing
RestierWebApiOperationModelBuilder. It scans the target API for
[Operation]-decorated methods, collects every CLR type referenced by
their parameters or return values, and uses ODataConventionModelBuilder
to register any type the inner model has not already declared. Enums
become EdmEnumType; classes with a [Key]/Id convention become EdmEntityType
(without an EntitySet); other classes become EdmComplexType. Nested
complex types are picked up by the convention builder automatically.

Closes the #651 leg of issue #750.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 11: Wire the new builder into DI

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs:121-124`
- Modify: `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs:171-175`

- [ ] **Step 1: Add the registration in the first path**

Edit `RestierODataOptionsExtensions.cs`. Replace lines 121–124:

```csharp
modelBuildingServices.AddSingleton<IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
    .AddSingleton(new RestierWebApiModelExtender(type))
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new RestierWebApiOperationModelBuilder(type, sp.GetRequiredService<RestierWebApiModelExtender>()))
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new ConventionBasedAnnotationModelBuilder(type));
```

with:

```csharp
modelBuildingServices.AddSingleton<IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
    .AddSingleton(new RestierWebApiModelExtender(type))
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new OperationTypeRegistrationModelBuilder(type))
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new RestierWebApiOperationModelBuilder(type, sp.GetRequiredService<RestierWebApiModelExtender>()))
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new ConventionBasedAnnotationModelBuilder(type));
```

- [ ] **Step 2: Add the registration in the second path**

Replace lines 171–175 with the same insertion pattern:

```csharp
services.AddSingleton<IChainedService<IModelBuilder>, RestierWebApiModelBuilder>()
    .AddSingleton(modelExtender)
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new OperationTypeRegistrationModelBuilder(type))
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new RestierWebApiOperationModelBuilder(type, sp.GetRequiredService<RestierWebApiModelExtender>()))
    .AddSingleton<IChainedService<IModelBuilder>>(sp => new ConventionBasedAnnotationModelBuilder(type))
    .AddSingleton<IChainedService<IModelMapper>, RestierWebApiModelMapper>()
    .AddSingleton<IChainedService<IQueryExpressionExpander>, RestierQueryExpressionExpander>();
```

Add `using Microsoft.Restier.AspNetCore.Model.ApiExtension;` at the top of the file if not already imported.

- [ ] **Step 3: Run the full Tests.AspNetCore suite for regressions**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs
git commit -m "feat(di): register OperationTypeRegistrationModelBuilder in chain

Insert the new pre-pass between RestierWebApiModelBuilder and
RestierWebApiOperationModelBuilder in both AddRestierRoute call sites
so missing operation-referenced types are auto-registered before the
operation builder resolves type references.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 12: HTTP integration tests via Breakdance

**Files:**
- Create: `test/Microsoft.Restier.Tests.Shared/Scenarios/MagicalOps/MagicalOpsApi.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/MagicalOperationsTests.cs`

- [ ] **Step 1: Create the scenario API**

Create `test/Microsoft.Restier.Tests.Shared/Scenarios/MagicalOps/MagicalOpsApi.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using RestierOptional = Microsoft.Restier.AspNetCore.Model.OptionalAttribute;

namespace Microsoft.Restier.Tests.Shared.Scenarios.MagicalOps;

public class MagicalOpsApi : ApiBase
{
    public MagicalOpsApi(IServiceProvider serviceProvider) : base(serviceProvider) { }

    // Nullable parameter — the #656 literal repro.
    [UnboundOperation]
    public int? Echo(int? parameter1) => parameter1;

    // Compiler-default optional parameter.
    [UnboundOperation]
    public int WithDefault(int parameter1 = 5) => parameter1;

    // Nullable + optional. Explicit null must beat default substitution.
    [UnboundOperation]
    public int? NullableWithDefault(int? parameter1 = 5) => parameter1;

    // Unknown complex input / unknown complex output — the #651 literal repro.
    [UnboundOperation]
    public SearchResult Search(SearchCriteria criteria)
        => new SearchResult { Found = (criteria?.Limit ?? 0) > 0 };

    [UnboundOperation]
    [System.ComponentModel.Description("Returns nothing.")]
    [Obsolete("Use NewMethod instead.")]
    public int DeprecatedMethod() => 0;
}

public class SearchCriteria
{
    public string Query { get; set; }
    public int Limit { get; set; }
}

public class SearchResult
{
    public bool Found { get; set; }
}
```

- [ ] **Step 2: Create the HTTP integration tests**

Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/MagicalOperationsTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#pragma warning disable xUnit1051 // CancellationToken not passed to async methods — acceptable in integration tests

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.MagicalOps;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public class MagicalOperationsTests
{
    private static System.Action<IServiceCollection> ConfigureServices => _ => { };

    [Fact]
    public async Task Echo_WithNullParameter_Returns200()
    {
        // Literal #656 repro: ?parameter1=null on a Nullable<int> parameter must succeed.
        var response = await RestierTestHelpers.ExecuteTestRequest<MagicalOpsApi>(
            HttpMethod.Get,
            resource: "/Echo(parameter1=null)",
            serviceCollection: ConfigureServices);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WithDefault_OmittedParameter_PassesDeclaredDefault()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<MagicalOpsApi>(
            HttpMethod.Get,
            resource: "/WithDefault()",
            serviceCollection: ConfigureServices);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"value\":5");
    }

    [Fact]
    public async Task NullableWithDefault_ExplicitNull_PassesNull()
    {
        // int? p = 5 is both nullable and optional. Explicit null must beat default.
        var response = await RestierTestHelpers.ExecuteTestRequest<MagicalOpsApi>(
            HttpMethod.Get,
            resource: "/NullableWithDefault(parameter1=null)",
            serviceCollection: ConfigureServices);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"value\":null");
    }

    [Fact]
    public async Task NullableWithDefault_Omitted_PassesDefault()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<MagicalOpsApi>(
            HttpMethod.Get,
            resource: "/NullableWithDefault()",
            serviceCollection: ConfigureServices);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"value\":5");
    }

    [Fact]
    public async Task Search_WithUnknownComplexInput_RoundTrips()
    {
        // Literal #651 repro: parameter and return are POCOs the inner model never saw.
        var response = await RestierTestHelpers.ExecuteTestRequest<MagicalOpsApi>(
            HttpMethod.Post,
            resource: "/Search",
            payload: "{\"criteria\":{\"Query\":\"book\",\"Limit\":10}}",
            acceptHeader: "application/json",
            serviceCollection: ConfigureServices);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"Found\":true");
    }

    [Fact]
    public async Task Metadata_DeprecatedMethod_EmitsRevisionsAnnotation()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<MagicalOpsApi>(
            serviceCollection: ConfigureServices);
        metadata.ToString().Should().Contain("Core.V1.Revisions");
        metadata.ToString().Should().Contain("Use NewMethod instead.");
    }

    [Fact]
    public async Task Metadata_DescribedMethod_EmitsDescriptionAnnotation()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<MagicalOpsApi>(
            serviceCollection: ConfigureServices);
        metadata.ToString().Should().Contain("Core.V1.Description");
        metadata.ToString().Should().Contain("Returns nothing.");
    }

    [Fact]
    public async Task Metadata_UnknownComplexType_IsRegistered()
    {
        var metadata = await RestierTestHelpers.GetApiMetadataAsync<MagicalOpsApi>(
            serviceCollection: ConfigureServices);
        // The SearchCriteria and SearchResult types should appear as ComplexTypes.
        metadata.ToString().Should().Contain("SearchCriteria");
        metadata.ToString().Should().Contain("SearchResult");
    }
}
```

Note: the exact `RestierTestHelpers.ExecuteTestRequest` signature for POST with a payload may vary; if `payload` is not a recognized parameter name in this repo's version of Breakdance, replace with the actual helper signature (see `Microsoft.Restier.Breakdance.RestierTestHelpers` source / `FeatureTests/DeepInsertTests.cs` for POST patterns).

- [ ] **Step 3: Run tests**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~MagicalOperationsTests"`
Expected: All 8 tests pass.

If any test fails due to OData URL parsing rejecting `WithDefault()` (an empty parameter list when the function's metadata says the param is optional), inspect the actual error — likely the metadata isn't being treated as fully optional. The fix is in Task 8's classifier output. If the test fails because of payload-helper signature mismatch, adapt to the local helper.

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.Shared/Scenarios/MagicalOps/MagicalOpsApi.cs \
        test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/MagicalOperationsTests.cs
git commit -m "test(operations): HTTP integration coverage for magical operations

Covers the literal issue #656 (?p=null on int?), the literal issue #651
(unknown complex POCO parameter/return), explicit-null-beats-default,
omitted-with-default substitution, and $metadata annotation emission
for [Description] and [Obsolete]. Uses Breakdance and an in-memory
scenario API in the existing test fixture style.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Task 13: Documentation + release notes

**Files:**
- Modify: `src/Microsoft.Restier.Docs/guides/server/operations.mdx`
- Modify: `src/Microsoft.Restier.Docs/release-notes/v2-0.mdx` (or whichever release-notes file is current; check git log for recent changes)

- [ ] **Step 1: Locate the operations guide**

Run: `ls src/Microsoft.Restier.Docs/guides/server/operations*`
Expected: An `operations.mdx` file. If absent, search for similar:

Run: `grep -lR "BoundOperation" src/Microsoft.Restier.Docs/`

- [ ] **Step 2: Add "Magical Operations" section to the operations guide**

Append a new section to `src/Microsoft.Restier.Docs/guides/server/operations.mdx`:

```mdx
## Auto-registration, optional parameters, and annotations

In RESTier 2.0, methods decorated with `[BoundOperation]` or `[UnboundOperation]`
are fully self-registering. You no longer need to take over the model builder
just to declare a complex-typed parameter or return.

### Complex / entity / enum types

Any POCO, enum, or keyed entity type referenced by an operation parameter or
return value is automatically registered when the model is built. Classes with
an `Id` property or a `[Key]`-decorated property are registered as entity types
(without an entity set); enums are registered as enum types; other classes are
registered as complex types. Nested complex properties are picked up
recursively by the convention builder.

```csharp
public class SearchCriteria { public string Query { get; set; } public int Limit { get; set; } }
public class SearchResult { public bool Found { get; set; } }

public class MyApi : EntityFrameworkApi<MyContext>
{
    // SearchCriteria and SearchResult are auto-registered as ComplexType.
    [UnboundOperation]
    public SearchResult Search(SearchCriteria criteria) => ...;
}
```

### Optional parameters and nullability

Nullability and optionality are independent.

| Signal | EDM type ref `Nullable` | EDM parameter shape |
|---|---|---|
| `int p` | `false` | `EdmOperationParameter` (required) |
| `int? p` | `true` | `EdmOperationParameter` (required-but-nullable) |
| `int p = 5` | `false` | `EdmOptionalParameter` (default `5`) |
| `int? p = null` | `true` | `EdmOptionalParameter` (default `null`) |
| `[DefaultValue("x")] string p` | `true` | `EdmOptionalParameter` (default `x`) |
| `[Optional] int? p` | `true` | `EdmOptionalParameter` (default `null`) |

`Foo(int? p)` *accepts* `?p=null` from the URL, but the URL must still mention
`p`. To make a parameter omittable, give it a default value or use the
`[Optional]` attribute (from `Microsoft.Restier.AspNetCore.Model`, distinct
from `System.Runtime.InteropServices.OptionalAttribute`).

When both signals apply — for example `int? p = 5` — `Foo()` substitutes the
default (`5`) and `Foo(p=null)` passes `null`. Explicit null beats default
substitution.

### Vocabulary annotations

| .NET attribute | EDM annotation |
|---|---|
| `[Description("…")]` on method | `Core.V1.Description` on `EdmOperation` |
| `[Description("…")]` on parameter | `Core.V1.Description` on `EdmOperationParameter` |
| `[Obsolete("…")]` on method | `Core.V1.Revisions` with `Kind = Deprecated` |

These annotations round-trip through OpenAPI/Swagger generation as `description`
and `deprecated` fields on the matching paths.

### Duplicate-name handling

If the same operation is declared both manually (via `ODataModelBuilder.Action`/`Function`)
and via `[Operation]`, the manual registration wins and the attribute-driven
add is skipped with a `Trace.TraceWarning`. Same-name overloads are *not*
supported — `RestierOperationExecutor` dispatches by name only.
```

- [ ] **Step 3: Find the active release-notes file**

Run: `ls src/Microsoft.Restier.Docs/release-notes/`
Pick the file matching the vNext (2.0) release series.

- [ ] **Step 4: Add breaking-change entry**

Add a new entry under "Breaking changes" in the appropriate release-notes file:

```mdx
### `OperationContext.GetParameterValueFunc` is now presence-aware

`Microsoft.Restier.Core.Operation.OperationContext.GetParameterValueFunc` changed
from `Func<string, object>` to `Func<string, (bool Present, object Value)>`.
The `Present` flag is `true` when the parameter name appears in the request,
even if the supplied value is `null`. Custom `RestierController` subclasses
that construct their own `getParaValueFunc` need to migrate.

**Why:** without a presence flag, the runtime could not distinguish "URL omitted
the parameter" from "URL supplied `p=null`" — required to implement optional
parameter default-substitution alongside nullability semantics. See issue #750.
```

- [ ] **Step 5: Build the docs project to confirm no MDX errors**

Run: `dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`
Expected: Build succeeds.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/operations.mdx \
        src/Microsoft.Restier.Docs/release-notes/
git commit -m "docs(operations): document magical operations and breaking change

Operations guide gets a new section covering type auto-registration,
the four optional-parameter signal sources, the explicit-null-vs-default
matrix, vocabulary annotations from .NET attributes, and the duplicate-name
warning.

Release notes call out the breaking GetParameterValueFunc signature
change on Microsoft.Restier.Core.OperationContext.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

## Final Verification

After all tasks complete:

- [ ] **Step 1: Full build**

Run: `dotnet build RESTier.slnx`
Expected: 0 warnings, 0 errors. (Warnings-as-errors is enabled globally per CLAUDE.md.)

- [ ] **Step 2: Full test run**

Run: `dotnet test RESTier.slnx`
Expected: All tests pass across all projects.

- [ ] **Step 3: Inspect $metadata against the scenario API**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~MagicalOperationsTests" --logger "console;verbosity=detailed"`
Expected: The metadata-emitting tests show that `Core.V1.Revisions` and `Core.V1.Description` markup is present and `SearchCriteria`/`SearchResult` are declared in the schema.

- [ ] **Step 4: Confirm the git log tells a clean story**

Run: `git log --oneline feature/vnext..HEAD`
Expected: A linear sequence of feat/test/docs commits for tasks 1–13, with the `feat(operations)!:` commit at task 6 being the only breaking change.

---

## Self-Review Notes

Spec coverage:
- Auto-registration of complex / entity / enum types → Task 10 + Task 11 wiring.
- Dedup by namespace + name with Trace warning → Task 7.
- Nullability vs. optionality split → Task 2 classifier + Task 8 model builder + Task 3 EdmHelpers overload.
- `[DefaultValue]` as default literal source → Task 2.
- `[Optional]` attribute marker → Task 1.
- `[Obsolete]` → `Core.V1.Revisions` and parameter-level `[Description]` → Task 9.
- Presence-aware delegate contract → Task 4 (Core), Task 5 (controller), Task 6 (executor).
- HTTP integration coverage → Task 12.
- Documentation + breaking-change release note → Task 13.

No spec requirements left untouched.

Type-consistency check:
- `OperationParameterClassifier` static method names match across Tasks 2, 6, 8 (`ComputeNullable`, `ClassifyOptionality`, `IsOmittedOptional`, `ResolveDefault`).
- `Func<string, (bool Present, object Value)>` shape matches across Tasks 4, 5, 6.
- `OperationTypeRegistrationModelBuilder` namespace `Microsoft.Restier.AspNetCore.Model.ApiExtension` matches the existing `RestierWebApiOperationModelBuilder` namespace.
