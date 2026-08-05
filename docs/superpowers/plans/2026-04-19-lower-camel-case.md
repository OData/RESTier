# Lower camelCase JSON Property Naming Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enable opt-in lower camelCase JSON property naming per Restier route, using `ODataConventionModelBuilder.EnableLowerCamelCase()` with EDM-to-CLR property name mapping.

**Architecture:** A new `RestierNamingConvention` enum is passed to `AddRestierRoute()`, registered in DI, consumed by `EFModelBuilder` to call `EnableLowerCamelCase()` on the model builder. An `EdmClrPropertyMapper` utility resolves EDM property names back to CLR names using `ClrPropertyInfoAnnotation`. All places that build LINQ expressions or property dictionaries from EDM names are updated to use CLR names instead.

**Tech Stack:** C# / .NET 8+9 / Microsoft.OData.ModelBuilder 2.x / Microsoft.AspNetCore.OData 9.x / xUnit v3 / FluentAssertions

**Spec:** `docs/superpowers/specs/2026-04-19-lower-camel-case-design.md`

---

## File Structure

| File | Responsibility |
|------|---------------|
| `src/Microsoft.Restier.Core/RestierNamingConvention.cs` | **New.** Enum: PascalCase, LowerCamelCase, LowerCamelCaseWithEnumMembers |
| `src/Microsoft.Restier.AspNetCore/EdmClrPropertyMapper.cs` | **New.** Maps EDM property names to CLR names via `ClrPropertyInfoAnnotation` |
| `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs` | **Modify.** Add naming convention parameter to all `AddRestierRoute` overloads; register in both DI containers |
| `src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs` | **Modify.** Inject naming convention; call `EnableLowerCamelCase()` on builder |
| `src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs` | **Modify.** Use `EdmClrPropertyMapper` in key, navigation, and property handlers |
| `src/Microsoft.Restier.AspNetCore/Extensions/Extensions.cs` | **Modify.** Normalize property dict keys to CLR names |
| `src/Microsoft.Restier.AspNetCore/RestierController.cs` | **Modify.** Pass model to `GetPathKeyValues`; normalize ETag OriginalValues |
| `src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs` | **Modify.** Add naming convention parameter to test helpers |
| `test/Microsoft.Restier.Tests.AspNetCore/EdmClrPropertyMapperTests.cs` | **New.** Unit tests for mapper |
| `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/BookCategory.cs` | **New.** Enum for testing LowerCamelCaseWithEnumMembers |
| `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Book.cs` | **Modify.** Add nullable `Category` property |
| `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/LibraryCard.cs` | **Modify.** Add `[ConcurrencyCheck]` to `DateRegistered` |
| `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs` | **Modify.** Seed category values and LibraryCard data |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/NamingConventionTests.cs` | **New.** Abstract integration tests (15 tests) |
| `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/NamingConventionTests.cs` | **New.** Concrete EFCore tests |

---

### Task 1: RestierNamingConvention Enum

**Files:**
- Create: `src/Microsoft.Restier.Core/RestierNamingConvention.cs`

- [ ] **Step 1: Create the enum file**

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Specifies the naming convention for OData JSON property names.
    /// </summary>
    public enum RestierNamingConvention
    {
        /// <summary>
        /// Use PascalCase property names (default). Property names match CLR type definitions.
        /// </summary>
        PascalCase = 0,

        /// <summary>
        /// Use lower camelCase property names. E.g. <c>FirstName</c> becomes <c>firstName</c>.
        /// </summary>
        LowerCamelCase = 1,

        /// <summary>
        /// Use lower camelCase for both property names and enum member names.
        /// </summary>
        LowerCamelCaseWithEnumMembers = 2,
    }
}
```

- [ ] **Step 2: Verify it builds**

Run: `dotnet build src/Microsoft.Restier.Core/Microsoft.Restier.Core.csproj`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.Core/RestierNamingConvention.cs
git commit -m "feat: add RestierNamingConvention enum (#549)"
```

---

### Task 2: EdmClrPropertyMapper Utility + Unit Tests

**Files:**
- Create: `src/Microsoft.Restier.AspNetCore/EdmClrPropertyMapper.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/EdmClrPropertyMapperTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `test/Microsoft.Restier.Tests.AspNetCore/EdmClrPropertyMapperTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore;

public class EdmClrPropertyMapperTests
{
    private class SampleEntity
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    [Fact]
    public void GetClrPropertyName_WithoutCamelCase_ReturnsEdmName()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<SampleEntity>("Samples");
        var model = builder.GetEdmModel();

        var entityType = model.FindDeclaredType(typeof(SampleEntity).FullName) as IEdmStructuredType;
        var firstNameProperty = entityType.FindProperty("FirstName");

        var result = EdmClrPropertyMapper.GetClrPropertyName(firstNameProperty, model);

        result.Should().Be("FirstName");
    }

    [Fact]
    public void GetClrPropertyName_WithCamelCase_ReturnsClrName()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<SampleEntity>("Samples");
        builder.EnableLowerCamelCase();
        var model = builder.GetEdmModel();

        var entityType = model.FindDeclaredType(typeof(SampleEntity).FullName) as IEdmStructuredType;
        var firstNameProperty = entityType.FindProperty("firstName");

        firstNameProperty.Should().NotBeNull("EnableLowerCamelCase should create camelCase EDM property names");

        var result = EdmClrPropertyMapper.GetClrPropertyName(firstNameProperty, model);

        result.Should().Be("FirstName");
    }

    [Fact]
    public void GetClrPropertyName_WithCamelCase_KeyProperty_ReturnsClrName()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<SampleEntity>("Samples");
        builder.EnableLowerCamelCase();
        var model = builder.GetEdmModel();

        var entityType = model.FindDeclaredType(typeof(SampleEntity).FullName) as IEdmStructuredType;
        var idProperty = entityType.FindProperty("id");

        idProperty.Should().NotBeNull();

        var result = EdmClrPropertyMapper.GetClrPropertyName(idProperty, model);

        result.Should().Be("Id");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~EdmClrPropertyMapperTests" --no-build 2>&1 || true`
Expected: Compilation error — `EdmClrPropertyMapper` does not exist yet.

- [ ] **Step 3: Create the mapper**

Create `src/Microsoft.Restier.AspNetCore/EdmClrPropertyMapper.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Microsoft.Restier.AspNetCore
{
    /// <summary>
    /// Maps EDM property names back to CLR property names using model annotations.
    /// When <see cref="ODataConventionModelBuilderExtensions.EnableLowerCamelCase"/> has been called,
    /// EDM properties carry a <see cref="ClrPropertyInfoAnnotation"/> that maps to the original CLR PropertyInfo.
    /// Without camelCase, no annotation exists and the EDM name is returned as-is.
    /// </summary>
    internal static class EdmClrPropertyMapper
    {
        /// <summary>
        /// Gets the CLR property name for a given EDM property.
        /// </summary>
        /// <param name="edmProperty">The EDM property to look up.</param>
        /// <param name="model">The EDM model that may contain CLR annotations.</param>
        /// <returns>The CLR property name, or the EDM property name if no annotation exists.</returns>
        public static string GetClrPropertyName(IEdmProperty edmProperty, IEdmModel model)
        {
            var annotation = model.GetAnnotationValue<ClrPropertyInfoAnnotation>(edmProperty);
            return annotation?.ClrPropertyInfo?.Name ?? edmProperty.Name;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~EdmClrPropertyMapperTests"`
Expected: 3 passed

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/EdmClrPropertyMapper.cs test/Microsoft.Restier.Tests.AspNetCore/EdmClrPropertyMapperTests.cs
git commit -m "feat: add EdmClrPropertyMapper utility with unit tests (#549)"
```

---

### Task 3: AddRestierRoute Overloads + DI Registration

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs`

- [ ] **Step 1: Add the naming convention parameter to all three methods**

In `RestierODataOptionsExtensions.cs`, update the prefixless overload (around line 43):

```csharp
    public static ODataOptions AddRestierRoute<TApi>
    (this ODataOptions oDataOptions,
            Action<IServiceCollection> configureRouteServices, bool useRestierBatching = true,
            RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
    where TApi : ApiBase
        => oDataOptions.AddRestierRoute<TApi>(string.Empty, configureRouteServices, useRestierBatching, namingConvention);
```

Update the prefix overload (around line 58):

```csharp
    public static ODataOptions AddRestierRoute<TApi>(
        this ODataOptions oDataOptions,
        string routePrefix,
        Action<IServiceCollection> configureRouteServices,
        bool useRestierBatching = true,
        RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
    where TApi : ApiBase
    => AddRestierRoute(oDataOptions, typeof(TApi), routePrefix , configureRouteServices, useRestierBatching, namingConvention);
```

Update the private helper signature (around line 86):

```csharp
    private static ODataOptions AddRestierRoute(
        ODataOptions oDataOptions,
        Type type, string routePrefix,
        Action<IServiceCollection> configureRouteServices,
        bool useRestierBatching,
        RestierNamingConvention namingConvention)
```

- [ ] **Step 2: Register naming convention in both DI containers**

In the private `AddRestierRoute` method body, add after `configureRouteServices.Invoke(modelBuildingServices);` (around line 107):

```csharp
        modelBuildingServices.AddSingleton(namingConvention);
```

Inside the `oDataOptions.AddRouteComponents(routePrefix, model, services => { ... })` lambda, add after `services.RemoveAll<ODataQuerySettings>()` (around line 150):

```csharp
            services.AddSingleton(namingConvention);
```

- [ ] **Step 3: Add the using directive**

Add at the top of the file, among the existing usings:

```csharp
using Microsoft.Restier.Core;
```

Note: This using likely already exists. Verify and add only if missing.

- [ ] **Step 4: Verify the solution builds**

Run: `dotnet build RESTier.slnx`
Expected: Build succeeded

- [ ] **Step 5: Run existing tests to verify no regression**

Run: `dotnet test RESTier.slnx`
Expected: All existing tests pass (the new parameter defaults to `PascalCase`)

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Extensions/RestierODataOptionsExtensions.cs
git commit -m "feat: add RestierNamingConvention parameter to AddRestierRoute overloads (#549)"
```

---

### Task 4: EFModelBuilder — Call EnableLowerCamelCase

**Files:**
- Modify: `src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs`

- [ ] **Step 1: Add the using directive**

Add at the top of the file, among the existing usings:

```csharp
using Microsoft.Restier.Core;
```

- [ ] **Step 2: Add naming convention field and update constructor**

Replace the existing constructor and fields (lines 31-46):

```csharp
    public partial class EFModelBuilder<TDbContext> : IModelBuilder
        where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;
        private readonly ModelMerger _modelMerger;
        private readonly RestierNamingConvention _namingConvention;

        /// <summary>
        /// Initializes a new instance of the <see cref="EFModelBuilder{TDbContext}"/> class.
        /// </summary>
        /// <param name="dbContext">The DbContext to use for model building.</param>
        /// <param name="modelMerger">The model merger to use.</param>
        /// <param name="namingConvention">The naming convention to use for the EDM model. Defaults to PascalCase.</param>
        public EFModelBuilder(TDbContext dbContext, ModelMerger modelMerger, RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
        {
            Ensure.NotNull(dbContext, nameof(dbContext));
            Ensure.NotNull(modelMerger, nameof(modelMerger));
            this._dbContext = dbContext;
            this._modelMerger = modelMerger;
            this._namingConvention = namingConvention;
        }
```

- [ ] **Step 3: Pass naming convention to BuildEdmModelFromEntitySetMaps**

In `GetEdmModel()` (around line 68), change:

```csharp
            var result = BuildEdmModelFromEntitySetMaps(entitySetMap, entitySetKeyMap);
```

to:

```csharp
            var result = BuildEdmModelFromEntitySetMaps(entitySetMap, entitySetKeyMap, _namingConvention);
```

- [ ] **Step 4: Update BuildEdmModelFromEntitySetMaps signature and add EnableLowerCamelCase call**

Change the method signature (line 79):

```csharp
        private static EdmModel BuildEdmModelFromEntitySetMaps(Dictionary<string, Type> entitySetMap, Dictionary<Type, ICollection<PropertyInfo>> entitySetKeyMap, RestierNamingConvention namingConvention)
```

Add the `EnableLowerCamelCase` call just before `return (EdmModel)builder.GetEdmModel();` (before line 129):

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

            return (EdmModel)builder.GetEdmModel();
```

Note: `EnableLowerCamelCase()` is an extension method from `Microsoft.OData.ModelBuilder`. The `using Microsoft.OData.ModelBuilder;` import is already present on line 9.

- [ ] **Step 5: Verify it builds**

Run: `dotnet build RESTier.slnx`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs
git commit -m "feat: call EnableLowerCamelCase in EFModelBuilder when configured (#549)"
```

---

### Task 5: RestierQueryBuilder + RestierController Call Sites — Use CLR Property Names

This task merges query builder changes with their controller call sites so the build stays green.

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs`
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

- [ ] **Step 1: Fix HandleNavigationPathSegment**

In `RestierQueryBuilder.cs` `HandleNavigationPathSegment` (around line 211), change:

```csharp
            var navigationPropertyExpression =
                Expression.Property(entityParameterExpression, navigationSegment.NavigationProperty.Name);
```

to:

```csharp
            var navigationClrName = EdmClrPropertyMapper.GetClrPropertyName(navigationSegment.NavigationProperty, edmModel);
            var navigationPropertyExpression =
                Expression.Property(entityParameterExpression, navigationClrName);
```

- [ ] **Step 2: Fix HandlePropertyAccessPathSegment**

In `HandlePropertyAccessPathSegment` (around line 247), change:

```csharp
            var structuralPropertyExpression =
                Expression.Property(entityParameterExpression, propertySegment.Property.Name);
```

to:

```csharp
            var propertyClrName = EdmClrPropertyMapper.GetClrPropertyName(propertySegment.Property, edmModel);
            var structuralPropertyExpression =
                Expression.Property(entityParameterExpression, propertyClrName);
```

- [ ] **Step 3: Fix HandleKeyValuePathSegment**

In `HandleKeyValuePathSegment` (around line 187), change the method to resolve key names:

```csharp
        private void HandleKeyValuePathSegment(ODataPathSegment segment)
        {
            var keySegment = (KeySegment)segment;

            var parameterExpression = Expression.Parameter(currentType, DefaultNameOfParameterExpression);
            var keyValues = GetPathKeyValues(keySegment, edmModel);

            BinaryExpression keyFilter = null;
            foreach (var keyValuePair in keyValues)
            {
                var equalsExpression =
                    CreateEqualsExpression(parameterExpression, keyValuePair.Key, keyValuePair.Value);
                keyFilter = keyFilter is null ? equalsExpression : Expression.And(keyFilter, equalsExpression);
            }

            var whereExpression = Expression.Lambda(keyFilter, parameterExpression);
            queryable = ExpressionHelpers.Where(queryable, whereExpression, currentType);
        }
```

- [ ] **Step 4: Update GetPathKeyValues to resolve CLR property names**

Change the public `GetPathKeyValues(ODataPath)` method to accept an `IEdmModel`:

```csharp
        internal static IReadOnlyDictionary<string, object> GetPathKeyValues(ODataPath path, IEdmModel model)
        {
            var segments = path.ToList();

            if (segments.Count == 2 && segments[0] is EntitySetSegment && segments[1] is KeySegment keySegment)
            {
                return GetPathKeyValues(keySegment, model);
            }
            else if (segments.Count == 3 && segments[0] is EntitySetSegment && segments[1] is KeySegment keySegment2 && segments[2] is TypeSegment)
            {
                return GetPathKeyValues(keySegment2, model);
            }
            else if (segments.Count == 3 && segments[0] is EntitySetSegment && segments[1] is TypeSegment && segments[2] is KeySegment keySegment3)
            {
                return GetPathKeyValues(keySegment3, model);
            }
            else
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    AspNetResources.InvalidPathTemplateInRequest,
                    "~/entityset/key"));
            }
        }
```

Change the private `GetPathKeyValues(KeySegment)` to accept `IEdmModel` and resolve CLR names:

```csharp
        private static IReadOnlyDictionary<string, object> GetPathKeyValues(
            KeySegment keySegment, IEdmModel model)
        {
            var result = new Dictionary<string, object>();
            var entityType = keySegment.EdmType as IEdmEntityType;
            var keyValuePairs = keySegment.Keys;

            foreach (var keyValuePair in keyValuePairs)
            {
                var edmProperty = entityType?.FindProperty(keyValuePair.Key);
                var clrName = edmProperty is not null
                    ? EdmClrPropertyMapper.GetClrPropertyName(edmProperty, model)
                    : keyValuePair.Key;
                result.Add(clrName, keyValuePair.Value);
            }

            return result;
        }
```

- [ ] **Step 5: Update RestierController GetPathKeyValues call sites**

In `RestierController.cs`, in the `Update` method (around line 433), change:

```csharp
                RestierQueryBuilder.GetPathKeyValues(path),
```

to:

```csharp
                RestierQueryBuilder.GetPathKeyValues(path, api.Model),
```

In the `Delete` method (around line 287), change:

```csharp
                RestierQueryBuilder.GetPathKeyValues(path),
```

to:

```csharp
                RestierQueryBuilder.GetPathKeyValues(path, api.Model),
```

- [ ] **Step 6: Verify the solution builds**

Run: `dotnet build RESTier.slnx`
Expected: Build succeeded

- [ ] **Step 7: Run existing tests to verify no regression**

Run: `dotnet test RESTier.slnx`
Expected: All existing tests pass

- [ ] **Step 8: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Query/RestierQueryBuilder.cs src/Microsoft.Restier.AspNetCore/RestierController.cs
git commit -m "feat: use EdmClrPropertyMapper in RestierQueryBuilder and update call sites (#549)"
```

---

### Task 6: Extensions.cs — Normalize Property Dictionary Keys

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/Extensions/Extensions.cs`

- [ ] **Step 1: Update CreatePropertyDictionary to resolve CLR names**

Replace the `CreatePropertyDictionary` method (lines 92-129) with:

```csharp
        public static IReadOnlyDictionary<string, object> CreatePropertyDictionary(
            this Delta entity, IEdmStructuredType edmType, ApiBase api, bool isCreation)
        {
            var propertiesAttributes = RetrievePropertiesAttributes(edmType, api);

            var propertyValues = new Dictionary<string, object>();
            foreach (var propertyName in entity.GetChangedPropertyNames())
            {
                var edmProperty = edmType.FindProperty(propertyName);
                var clrPropertyName = edmProperty is not null
                    ? EdmClrPropertyMapper.GetClrPropertyName(edmProperty, api.Model)
                    : propertyName;

                if (propertiesAttributes is not null && propertiesAttributes.TryGetValue(clrPropertyName, out var attributes))
                {
                    if ((isCreation && (attributes & PropertyAttributes.IgnoreForCreation) != PropertyAttributes.None)
                      || (!isCreation && (attributes & PropertyAttributes.IgnoreForUpdate) != PropertyAttributes.None))
                    {
                        // Will not get the properties for update or creation
                        continue;
                    }
                }

                if (entity.TryGetPropertyValue(propertyName, out var value))
                {
                    if (value is EdmComplexObject complexObj)
                    {
                        value = CreatePropertyDictionary(complexObj, complexObj.ActualEdmType, api, isCreation);
                    }

                    // RWM: Navigation properties (e.g. from @odata.bind links) are not supported in
                    //      the property dictionary until we support Delta payloads. Skip them.
                    if (value is EdmEntityObject)
                    {
                        continue;
                    }

                    propertyValues.Add(clrPropertyName, value);
                }
            }

            return propertyValues;
        }
```

- [ ] **Step 2: Update RetrievePropertiesAttributes to use CLR names**

In `RetrievePropertiesAttributes` (line 137-192), change the line that adds to the dictionary (around line 188):

```csharp
                    propertiesAttributes.Add(property.Name, attributes);
```

to:

```csharp
                    var clrName = EdmClrPropertyMapper.GetClrPropertyName(property, model);
                    propertiesAttributes.Add(clrName, attributes);
```

- [ ] **Step 3: Verify it builds and tests pass**

Run: `dotnet build RESTier.slnx && dotnet test RESTier.slnx`
Expected: Build succeeded, all tests pass

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/Extensions/Extensions.cs
git commit -m "feat: normalize property dictionary keys to CLR names (#549)"
```

---

### Task 7: RestierController — ETag / OriginalValues Normalization

**Files:**
- Modify: `src/Microsoft.Restier.AspNetCore/RestierController.cs`

- [ ] **Step 1: Add NormalizePropertyNames helper**

Add a new private method after `GetOriginalValues` in `RestierController.cs`:

```csharp
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

- [ ] **Step 2: Update GetOriginalValues to normalize ETag property names**

Replace the `GetOriginalValues` method (lines 657-689) with:

```csharp
        private IReadOnlyDictionary<string, object> GetOriginalValues(IEdmEntitySet entitySet)
        {
            var originalValues = new Dictionary<string, object>();

            if (Request.Headers.TryGetValue("IfMatch", out var ifMatchValues))
            {
                var etagHeaderValue = EntityTagHeaderValue.Parse(ifMatchValues.SingleOrDefault());
                var etag = Request.GetETag(etagHeaderValue);
                etag.ApplyTo(originalValues);

                originalValues.Add(IfMatchKey, etagHeaderValue.Tag);
                return NormalizePropertyNames(originalValues, entitySet.EntityType, api.Model);
            }

            if (Request.Headers.TryGetValue("IfNoneMatch", out var ifNoneMatchValues))
            {
                var etagHeaderValue = EntityTagHeaderValue.Parse(ifNoneMatchValues.SingleOrDefault());
                var etag = Request.GetETag(etagHeaderValue);
                etag.ApplyTo(originalValues);

                originalValues.Add(IfNoneMatchKey, etagHeaderValue.Tag);
                return NormalizePropertyNames(originalValues, entitySet.EntityType, api.Model);
            }

            // return 428(Precondition Required) if entity requires concurrency check.
            var model = api.Model;
            if (model.IsConcurrencyCheckEnabled(entitySet))
            {
                return null;
            }

            return originalValues;
        }
```

- [ ] **Step 3: Verify everything builds and tests pass**

Run: `dotnet build RESTier.slnx && dotnet test RESTier.slnx`
Expected: Build succeeded, all tests pass

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.AspNetCore/RestierController.cs
git commit -m "feat: normalize ETag OriginalValues to CLR property names (#549)"
```

---

### Task 8: Test Infrastructure — Add Naming Convention to Test Helpers

**Files:**
- Modify: `src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs`

- [ ] **Step 1: Add using directive**

Add at the top of the file, among the existing usings:

```csharp
using Microsoft.Restier.Core;
```

- [ ] **Step 2: Update ExecuteTestRequest signature**

Change the `ExecuteTestRequest` method signature (around line 88). Replace:

```csharp
        public static async Task<HttpResponseMessage> ExecuteTestRequest<TApi>(HttpMethod httpMethod, string host = WebApiConstants.Localhost, string routeName = WebApiConstants.RouteName,
            string routePrefix = WebApiConstants.RoutePrefix, string resource = null, Action<IServiceCollection> serviceCollection = default, string acceptHeader = ODataConstants.MinimalAcceptHeader,
            DefaultQuerySettings defaultQuerySettings = null, TimeZoneInfo timeZoneInfo = null, object payload = null,
#if NET6_0_OR_GREATER
            JsonSerializerOptions jsonSerializerSettings = null)
#else
            JsonSerializerSettings jsonSerializerSettings = null)
#endif
            where TApi : ApiBase
```

with:

```csharp
        public static async Task<HttpResponseMessage> ExecuteTestRequest<TApi>(HttpMethod httpMethod, string host = WebApiConstants.Localhost, string routeName = WebApiConstants.RouteName,
            string routePrefix = WebApiConstants.RoutePrefix, string resource = null, Action<IServiceCollection> serviceCollection = default, string acceptHeader = ODataConstants.MinimalAcceptHeader,
            DefaultQuerySettings defaultQuerySettings = null, TimeZoneInfo timeZoneInfo = null, object payload = null,
#if NET6_0_OR_GREATER
            JsonSerializerOptions jsonSerializerSettings = null,
#else
            JsonSerializerSettings jsonSerializerSettings = null,
#endif
            RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
            where TApi : ApiBase
```

In the method body, update the `NET6_0_OR_GREATER` branch (around line 100):

```csharp
            var server = GetTestableRestierServer<TApi>(routeName, routePrefix, serviceCollection, namingConvention);
```

- [ ] **Step 3: Update GetTestableRestierServer signature**

Change (around line 379):

```csharp
        public static TestServer GetTestableRestierServer<TApi>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix,
            Action<IServiceCollection> apiServiceCollection = default)
            where TApi : ApiBase
            => GetTestBaseInstance<TApi>(routeName, routePrefix, apiServiceCollection).TestServer;
```

to:

```csharp
        public static TestServer GetTestableRestierServer<TApi>(string routeName = WebApiConstants.RouteName, string routePrefix = WebApiConstants.RoutePrefix,
            Action<IServiceCollection> apiServiceCollection = default, RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
            where TApi : ApiBase
            => GetTestBaseInstance<TApi>(routeName, routePrefix, apiServiceCollection, namingConvention).TestServer;
```

- [ ] **Step 4: Update GetTestBaseInstance to use naming convention**

Change (around line 392):

```csharp
        public static RestierBreakdanceTestBase<TApi> GetTestBaseInstance<TApi>(string routeName = WebApiConstants.RouteName, 
            string routePrefix = WebApiConstants.RoutePrefix, Action<IServiceCollection> apiServiceCollection = default)
            where TApi : ApiBase
```

to:

```csharp
        public static RestierBreakdanceTestBase<TApi> GetTestBaseInstance<TApi>(string routeName = WebApiConstants.RouteName, 
            string routePrefix = WebApiConstants.RoutePrefix, Action<IServiceCollection> apiServiceCollection = default,
            RestierNamingConvention namingConvention = RestierNamingConvention.PascalCase)
            where TApi : ApiBase
```

Inside the method, change the `AddRestierRoute` call from:

```csharp
                odataOptions.AddRestierRoute<TApi>(routeName, restierServices =>
```

to include the naming convention:

```csharp
                odataOptions.AddRestierRoute<TApi>(routeName, restierServices =>
                {
                    restierServices
                        .AddSingleton(new ODataValidationSettings
                        {
                            MaxTop = 5,
                            MaxAnyAllExpressionDepth = 3,
                            MaxExpansionDepth = 3,
                        });
                    apiServiceCollection?.Invoke(restierServices);
                }, namingConvention: namingConvention);
```

(Replace the entire lambda + closing `);` to add the `namingConvention:` named argument.)

- [ ] **Step 5: Verify everything builds and existing tests pass**

Run: `dotnet build RESTier.slnx && dotnet test RESTier.slnx`
Expected: Build succeeded, all existing tests pass (default is `PascalCase`, so behavior unchanged)

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.Breakdance/RestierTestHelpers.cs
git commit -m "feat: add RestierNamingConvention parameter to test helpers (#549)"
```

---

### Task 9: Test Model Additions — BookCategory Enum + Concurrency Token

The existing Library test models lack enums and concurrency tokens. We need both to test `LowerCamelCaseWithEnumMembers` and ETag normalization.

**Files:**
- Create: `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/BookCategory.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Book.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/LibraryCard.cs`
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryContext.cs` (EFCore section)
- Modify: `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs`

- [ ] **Step 1: Create BookCategory enum**

Create `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/BookCategory.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    /// <summary>
    /// Category of a book.
    /// </summary>
    public enum BookCategory
    {
        Fiction = 0,
        NonFiction = 1,
        Science = 2,
    }
}
```

- [ ] **Step 2: Add Category property to Book**

In `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Book.cs`, add inside the class:

```csharp
        /// <summary>
        /// The category of the book.
        /// </summary>
        public BookCategory? Category { get; set; }
```

- [ ] **Step 3: Add ConcurrencyCheck to LibraryCard**

In `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/LibraryCard.cs`, add `using System.ComponentModel.DataAnnotations;` to the usings and add `[ConcurrencyCheck]` to `DateRegistered`:

```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    /// <summary>
    /// An object in the model that is supposed to remain empty for unit tests.
    /// </summary>
    public class LibraryCard
    {
        public Guid Id { get; set; }

        [ConcurrencyCheck]
        public DateTimeOffset DateRegistered { get; set; }
    }
}
```

- [ ] **Step 4: Seed LibraryCard and Book Category data**

In `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs`, add `BookCategory` values to some existing Book seeds. In the first Publisher's books, update:

```csharp
                    new Book
                    {
                         Id = new Guid("19d68c75-1313-4369-b2bf-521f2b260a59"),
                         Isbn = "9476324472648",
                         Title = "A Clockwork Orange",
                         IsActive = true,
                         Category = BookCategory.Fiction,
                    },
```

And for the second book:

```csharp
                    new Book
                    {
                        Id = new Guid("c2081e58-21a5-4a15-b0bd-fff03ebadd30"),
                        Isbn = "7273389962644",
                        Title = "Jungle Book, The",
                        IsActive = true,
                        Category = BookCategory.Fiction,
                    },
```

Before `libraryContext.SaveChanges();`, add a seeded LibraryCard:

```csharp
            libraryContext.LibraryCards.Add(new LibraryCard
            {
                Id = new Guid("A1111111-1111-1111-1111-111111111111"),
                DateRegistered = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero),
            });
```

- [ ] **Step 5: Verify build and tests**

Run: `dotnet build RESTier.slnx && dotnet test RESTier.slnx`
Expected: Build succeeded, all existing tests pass (nullable `Category` defaults to null; LibraryCard tests only check for empty set)

- [ ] **Step 6: Commit**

```bash
git add test/Microsoft.Restier.Tests.Shared/Scenarios/Library/BookCategory.cs test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Book.cs test/Microsoft.Restier.Tests.Shared/Scenarios/Library/LibraryCard.cs test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs
git commit -m "test: add BookCategory enum and ConcurrencyCheck to LibraryCard for naming tests (#549)"
```

---

### Task 10: Integration Tests — GET / Query / Key Handling

**Files:**
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/NamingConventionTests.cs`
- Create: `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/NamingConventionTests.cs`

- [ ] **Step 1: Create the abstract test class**

Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/NamingConventionTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.Breakdance.AspNetCore;
using CloudNimble.EasyAF.Http.OData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Extensions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

public abstract class NamingConventionTests<TApi, TContext> : RestierTestBase<TApi> where TApi : ApiBase where TContext : class
{
    protected abstract Action<IServiceCollection> ConfigureServices { get; }

    private static readonly JsonSerializerOptions CamelCaseSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly JsonSerializerOptions CamelCaseDeserializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    #region GET / Query

    [Fact]
    public async Task GetEntitySet_ReturnsCamelCasePropertyNames()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books",
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"title\"");
        content.Should().Contain("\"isbn\"");
        content.Should().Contain("\"id\"");
        content.Should().Contain("\"isActive\"");
        content.Should().NotContain("\"Title\"");
        content.Should().NotContain("\"Isbn\"");
        content.Should().NotContain("\"IsActive\"");
    }

    [Fact]
    public async Task GetMetadata_ShowsCamelCasePropertyNames()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/$metadata",
            acceptHeader: "application/xml",
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("Name=\"title\"");
        content.Should().Contain("Name=\"isbn\"");
        content.Should().Contain("Name=\"isActive\"");
    }

    [Fact]
    public async Task GetWithSelect_WorksWithCamelCase()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$select=title,isbn",
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"title\"");
        content.Should().Contain("\"isbn\"");
    }

    [Fact]
    public async Task GetWithFilter_WorksWithCamelCase()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$filter=title eq 'Nonexistent Book'",
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetWithExpand_WorksWithCamelCase()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Publishers?$expand=books",
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"books\"");
    }

    [Fact]
    public async Task GetWithOrderBy_WorksWithCamelCase()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$orderby=title",
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        _ = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    #endregion

    #region Key Handling

    [Fact]
    public async Task GetByKey_WorksWithCamelCase()
    {
        // First get a book to get its ID
        var listResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        listResponse.IsSuccessStatusCode.Should().BeTrue();
        var (bookList, _) = await listResponse.DeserializeResponseAsync<ODataV4List<Book>>(CamelCaseDeserializerOptions);
        var bookId = bookList.Items[0].Id;

        // GET by key
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({bookId})",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        content.Should().Contain("\"title\"");
        content.Should().Contain("\"id\"");
    }

    [Fact]
    public async Task DeleteByKey_WorksWithCamelCase()
    {
        // Insert a book we can safely delete
        var book = new Book
        {
            Title = "Book To Delete",
            Isbn = "9999999999999",
        };

        var insertResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers('Publisher1')/Books",
            payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            jsonSerializerSettings: CamelCaseSerializerOptions,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        insertResponse.IsSuccessStatusCode.Should().BeTrue();

        var (createdBook, _) = await insertResponse.DeserializeResponseAsync<Book>(CamelCaseDeserializerOptions);

        // DELETE by key
        var deleteResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Delete,
            resource: $"/Books({createdBook.Id})",
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion

    #region POST / PATCH / PUT with camelCase payloads

    [Fact]
    public async Task PostBook_WithCamelCasePayload_CreatesEntity()
    {
        var book = new Book
        {
            Title = "CamelCase Insert Test",
            Isbn = "0118006345789",
        };

        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers('Publisher1')/Books",
            payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            jsonSerializerSettings: CamelCaseSerializerOptions,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);

        response.IsSuccessStatusCode.Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"title\"");
        content.Should().Contain("CamelCase Insert Test");
    }

    [Fact]
    public async Task PatchBook_WithCamelCasePayload_UpdatesEntity()
    {
        // Get a book
        var listResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        listResponse.IsSuccessStatusCode.Should().BeTrue();
        var (bookList, _) = await listResponse.DeserializeResponseAsync<ODataV4List<Book>>(CamelCaseDeserializerOptions);
        var book = bookList.Items[0];
        var originalTitle = book.Title;

        // PATCH with camelCase anonymous payload (lowercase property names)
        var payload = new { title = $"{originalTitle} | CamelCase Patch" };

        var patchResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            new HttpMethod("PATCH"),
            resource: $"/Books({book.Id})",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        patchResponse.IsSuccessStatusCode.Should().BeTrue();

        // Verify the change persisted
        var checkResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({book.Id})",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        checkResponse.IsSuccessStatusCode.Should().BeTrue();

        var (updatedBook, _) = await checkResponse.DeserializeResponseAsync<Book>(CamelCaseDeserializerOptions);
        updatedBook.Title.Should().Be($"{originalTitle} | CamelCase Patch");
    }

    [Fact]
    public async Task PutBook_WithCamelCasePayload_ReplacesEntity()
    {
        // Get a book
        var listResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$top=1",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        listResponse.IsSuccessStatusCode.Should().BeTrue();
        var (bookList, _) = await listResponse.DeserializeResponseAsync<ODataV4List<Book>>(CamelCaseDeserializerOptions);
        var book = bookList.Items[0];
        var originalTitle = book.Title;
        book.Title = $"{originalTitle} | CamelCase Put";

        // PUT with camelCase payload
        var putResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Put,
            resource: $"/Books({book.Id})",
            payload: book,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            jsonSerializerSettings: CamelCaseSerializerOptions,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        putResponse.IsSuccessStatusCode.Should().BeTrue();

        // Verify the change persisted
        var checkResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: $"/Books({book.Id})",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        checkResponse.IsSuccessStatusCode.Should().BeTrue();

        var (updatedBook, _) = await checkResponse.DeserializeResponseAsync<Book>(CamelCaseDeserializerOptions);
        updatedBook.Title.Should().Be($"{originalTitle} | CamelCase Put");
    }

    #endregion

    #region Concurrency (ETag)

    [Fact]
    public async Task PatchLibraryCard_WithETag_WorksWithCamelCase()
    {
        // GET the seeded LibraryCard (has [ConcurrencyCheck] on DateRegistered)
        var getResponse = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/LibraryCards(a1111111-1111-1111-1111-111111111111)",
            acceptHeader: ODataConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCase);
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var content = await getResponse.Content.ReadAsStringAsync();
        content.Should().Contain("\"dateRegistered\"");

        // The response should include an ETag header for the concurrency-enabled entity
        var etag = getResponse.Headers.ETag;
        etag.Should().NotBeNull("LibraryCard has [ConcurrencyCheck] so responses should include ETag");
    }

    #endregion

    #region Enum Members (LowerCamelCaseWithEnumMembers)

    [Fact]
    public async Task GetBooks_WithEnumMembers_ReturnsCamelCaseEnumValues()
    {
        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Get,
            resource: "/Books?$filter=category ne null",
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCaseWithEnumMembers);
        var content = await TraceListener.LogAndReturnMessageContentAsync(response);

        response.IsSuccessStatusCode.Should().BeTrue();
        // With LowerCamelCaseWithEnumMembers, enum values should be camelCase
        content.Should().Contain("\"category\"");
        // The enum value "Fiction" should appear as "fiction" in the response
        content.Should().Contain("fiction");
    }

    [Fact]
    public async Task PostBook_WithCamelCaseEnumValue_CreatesEntity()
    {
        // POST with camelCase enum value
        var payload = new { title = "Enum Test Book", isbn = "5555555555555", category = "fiction" };

        var response = await RestierTestHelpers.ExecuteTestRequest<TApi>(
            HttpMethod.Post,
            resource: "/Publishers('Publisher1')/Books",
            payload: payload,
            acceptHeader: WebApiConstants.DefaultAcceptHeader,
            serviceCollection: ConfigureServices,
            namingConvention: RestierNamingConvention.LowerCamelCaseWithEnumMembers);

        response.IsSuccessStatusCode.Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("fiction");
    }

    #endregion
}
```

- [ ] **Step 2: Create the concrete EFCore test class**

Create `test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/NamingConventionTests.cs`:

```csharp
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore;

[Collection("LibraryApiEFCore")]
public class NamingConventionTests : NamingConventionTests<LibraryApi, LibraryContext>
{
    protected override Action<IServiceCollection> ConfigureServices
        => services => services.AddEntityFrameworkServices<LibraryContext>();
}
```

- [ ] **Step 3: Run the tests**

Run: `dotnet test test/Microsoft.Restier.Tests.AspNetCore/Microsoft.Restier.Tests.AspNetCore.csproj --filter "FullyQualifiedName~NamingConventionTests"`
Expected: All tests pass (7 GET/query + 2 key handling + 3 write + 1 concurrency + 2 enum = 15 tests)

- [ ] **Step 4: Commit**

```bash
git add test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/NamingConventionTests.cs test/Microsoft.Restier.Tests.AspNetCore/FeatureTests/EFCore/NamingConventionTests.cs
git commit -m "test: add comprehensive integration tests for camelCase naming convention (#549)"
```

---

### Task 11: Full Regression + Cleanup

**Files:** None new — validation only

- [ ] **Step 1: Run the full test suite**

Run: `dotnet test RESTier.slnx`
Expected: All tests pass — both new naming convention tests and all existing tests

- [ ] **Step 2: Verify build for all target frameworks**

Run: `dotnet build RESTier.slnx -c Release`
Expected: Build succeeded for all TFMs (net8.0, net9.0, net48)

- [ ] **Step 3: Final commit if any cleanup was needed**

If any adjustments were made during validation, commit them:

```bash
git add -A
git commit -m "fix: address issues found during final validation (#549)"
```
