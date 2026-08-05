# Spatial Types Round-Trip in Restier (Spec A)

**Date:** 2026-05-06
**Status:** Design approved (revised after review)
**Issue:** [OData/RESTier#673](https://github.com/OData/RESTier/issues/673)

## Goal

Add round-trip support for Microsoft.Spatial geographic and geometric types in Restier across **both** Entity Framework 6 and Entity Framework Core. Users declare a single property typed in the storage library (`DbGeography`/`DbGeometry` for EF6, `NetTopologySuite.Geometries.Geometry` and subclasses for EF Core); Restier exposes it to OData clients as the corresponding `Edm.Geography*` / `Edm.Geometry*` primitive and converts in both directions transparently — preserving SRID and Z/M coordinates.

This is the first of three planned specs. Server-side spatial filtering (`geo.distance`, `geo.length`, etc.) and entity-property sugar (e.g. a `[SpatialProperty]` source generator) are deferred to follow-up specs B and C respectively.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Entity shape | Single property typed in the storage library | Avoids the dual-property pattern (`Location` + `EdmLocation`) the bytefish reference uses; keeps user code free of OData concerns and positions spec B's filter binder cleanly because the LINQ expression `e.Location` is already the EF-native type. |
| EF6 + EF Core symmetry | Both supported with the same `ISpatialTypeConverter` interface | Issue #673 explicitly calls for EF Core support and "a nice interface for abstraction". |
| Type families | Geography and Geometry both | Plumbing is genus-agnostic; covering both now avoids a future migration for users with `DbGeometry` / projected NTS columns. |
| EF6 type disambiguation | Default to `Edm.Geography` / `Edm.Geometry` (abstract base); optional `[Spatial(typeof(GeographyPoint))]` attribute for precision | Zero-config story works out of the box; opt-in precision when schema strictness matters. |
| EF Core type disambiguation | `[Spatial]` attribute → relational column type lookup → **fail-fast model-build error** if neither is conclusive | Default-to-Geography would silently mismap PostGIS columns (Npgsql defaults to `geometry`). Strict validation matches Restier's existing model-build checks (e.g. owned-type-on-DbSet). |
| Package layout | Two new optional packages: `Microsoft.Restier.EntityFramework.Spatial` and `Microsoft.Restier.EntityFrameworkCore.Spatial` | Mirrors the `Microsoft.Restier.AspNetCore.Swagger` precedent. NetTopologySuite (~3 MB plus transitive deps) is opt-in instead of forced on every Restier-EFCore consumer. |
| WKT bridge | Microsoft.Spatial's `WellKnownTextSqlFormatter` (SQL Server WKT variant) paired with `DbSpatialServices.Default.AsTextIncludingElevationAndMeasure` + `DbGeography.FromText(wkt, srid)` for EF6, and NTS's `WKTReader`/`WKTWriter` configured for `Ordinates.XYZM` for EF Core | Plain WKT loses SRID and Z/M. The amended bridge round-trips SRID and Z/M explicitly through both directions. Replaces the buggy hand-built WKT in the current `GeographyConverter`. |
| `ISpatialTypeConverter` access | Constructor-injected into the model convention, payload value converter, and `EFChangeSetInitializer` | Avoids any static facade or ambient-state lookup. Converters are stateless and registered as singletons in the route service container, so the initializer's existing singleton lifetime is unchanged (singleton-into-singleton is fine because both share the same route-container scope). |
| Filter-binder behavior | Spec A explicitly does **not** ship `geo.*` translation. Tests assert the limitation. | Round-trip and filtering are separable concerns. Spec B picks up filtering on top of A's foundation. |

## Background

OData V4 declares `Edm.Geography*` and `Edm.Geometry*` as primitive types and Microsoft.Spatial provides their CLR representations. Neither EF6 nor EF Core natively maps Microsoft.Spatial types to database columns:

- **EF6** maps spatial columns to `System.Data.Entity.Spatial.DbGeography` / `DbGeometry`. There is no value-converter mechanism in EF6 to substitute a different CLR type.
- **EF Core** with the SQL Server NTS plugin (or PostGIS via Npgsql) maps spatial columns to `NetTopologySuite.Geometries.Geometry` and concrete subclasses (`Point`, `Polygon`, etc.).

The current `feature/vnext` branch ships a partial EF6-only implementation in `src/Microsoft.Restier.EntityFramework/Spatial/GeographyConverter.cs` that hand-builds WKT strings for `Point` and `LineString` only. The implementation has known bugs (WKT axis order is `lat lon` instead of `lon lat`; `ToGeographyLineString` reads `point.Latitude` for both arguments of `GeographyPosition`) and only the inbound (write) path is wired into `EFChangeSetInitializer.ConvertToEfValue`. The outbound (read) path is not wired anywhere, so reads of `DbGeography` columns currently fail to serialize. There is no EF Core support whatsoever.

The bytefish reference implementation linked from issue #673 ([www.bytefish.de](https://www.bytefish.de/blog/aspnet_core_odata_example.html#extending-partial-classes-with-microsoftspatial-properties)) demonstrates a working EF Core approach using a dual-property pattern (one NTS-typed property for EF, a partial-class shadow Microsoft.Spatial-typed property for OData) plus a model-builder rename hook. Spec A takes a different path — single property, pipeline conversions — for the reasons captured in the Decisions table.

## Architecture

### Components

Five components, distributed across the existing assembly layout plus two new optional packages:

| # | Component | Assembly | Notes |
|---|-----------|----------|-------|
| 1 | `[SpatialAttribute]`, `ISpatialTypeConverter`, `ISpatialModelMetadataProvider` interfaces | `Microsoft.Restier.Core` | No new dependencies. Microsoft.Spatial is already transitive via Microsoft.OData.Edm. |
| 2 | EDM model-builder convention | `Microsoft.Restier.EntityFramework.Shared` (the project that hosts `EFModelBuilder<TDbContext>` for both EF flavors) | Plugs into `EFModelBuilder` directly — see "Model-builder integration" below. |
| 3 | Read-path hook in `RestierPayloadValueConverter` | `Microsoft.Restier.AspNetCore` | Same pattern as the DateOnly outbound conversion. Resolves converters via constructor injection. |
| 4 | Write-path hook in each flavor's `EFChangeSetInitializer.ConvertToEfValue` | `Microsoft.Restier.EntityFramework`, `Microsoft.Restier.EntityFrameworkCore` | Constructor-injected `IEnumerable<ISpatialTypeConverter>`. Lifetime stays singleton — singleton `DefaultSubmitHandler` continues capturing it cleanly. |
| 5 | Per-flavor converter implementations and `ISpatialModelMetadataProvider` | new packages `Microsoft.Restier.EntityFramework.Spatial`, `Microsoft.Restier.EntityFrameworkCore.Spatial` | Registered into the route service container via `services.AddRestierSpatial()`. |

### `[Spatial]` attribute and core interfaces

In `Microsoft.Restier.Core/Spatial/`:

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SpatialAttribute : Attribute
{
    public SpatialAttribute(Type edmType) { EdmType = edmType; }
    public Type EdmType { get; }      // a Microsoft.Spatial.Geography* or Geometry* CLR type
}

public interface ISpatialTypeConverter
{
    bool CanConvert(Type storageType);
    object ToEdm(object storageValue, Type targetEdmType);
    object ToStorage(Type targetStorageType, object edmValue);
}

// Lets the EF-flavor-specific package contribute model-time knowledge that the
// shared EFModelBuilder needs but cannot directly access (e.g. EFCore's relational
// column-type lookup).
public interface ISpatialModelMetadataProvider
{
    bool IsSpatialStorageType(Type clrType);

    // providerContext carries flavor-specific lookup state:
    //   EF6   -> null (genus is fully determined by the CLR type)
    //   EFCore -> the active DbContext instance, cast inside the provider to read .Model
    SpatialGenus? InferGenus(Type entityClrType, PropertyInfo property, object providerContext);

    IReadOnlyList<Type> IgnoredStorageTypes { get; } // passed to ODataConventionModelBuilder.Ignore(...)
}
```

`CanConvert` lets multiple converters coexist in DI (one per EF flavor); the resolver picks the first registered converter whose `CanConvert` returns true for the value's storage type. Both `ToEdm` and `ToStorage` take an explicit target type because:
- `ToEdm` from `DbGeography` cannot infer `GeographyPoint` vs `GeographyPolygon` without external context — the EDM model-build step or the runtime EDM type reference supplies it.
- `ToStorage` needs the property's declared CLR type to round-trip into `DbGeography` vs `DbGeometry` (or the appropriate NTS subclass).

### Model-builder integration

The convention plugs into `EFModelBuilder<TDbContext>` (the shared partial class in `src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs`). It runs in two phases around the existing `ODataConventionModelBuilder.GetEdmModel()` call:

**Phase 1 — pre-model-build.** Before `builder.GetEdmModel()` is called:
1. Walk every entity type's reflection-discovered properties. For each property whose CLR type is a spatial storage type (per `ISpatialModelMetadataProvider.IsSpatialStorageType`), capture the `(entityType, propertyInfo, resolvedEdmType)` triple for phase 2. Resolution rules for `resolvedEdmType`:
   - `[Spatial]` attribute present → use its `EdmType`, then validate (see below).
   - Else, ask `ISpatialModelMetadataProvider.InferGenus(entityType, propertyInfo, providerContext)`. The convention passes its `_dbContext` for `providerContext` (EF6 partial passes `null`, EFCore partial passes the `DbContext` instance):
     - **EF6 provider**: returns Geography for `DbGeography`, Geometry for `DbGeometry`. The CLR type alone determines genus, so `providerContext` is unused.
     - **EFCore provider**: casts `providerContext` to `DbContext` and looks up `dbContext.Model.FindEntityType(entityClrType).FindProperty(propertyInfo.Name).GetColumnType()`. SQL Server NTS plugin returns `"geography"` / `"geometry"`; Npgsql returns prefixes like `geography(...)` / `geometry(...)`. Returns the matching `SpatialGenus` or `null` if the column type is unset/unrecognized.
   - Genus + concrete CLR subclass → specific `Edm.GeographyPoint` / `Edm.GeometryPolygon` / etc. For EF6 (no concrete CLR subclass) the abstract base `Edm.Geography` / `Edm.Geometry` is used unless `[Spatial]` overrode.
   - **If the genus cannot be determined** (EFCore property with no `[Spatial]` and no recognizable column type) → throw `EdmModelValidationException` with the entity and property name, suggesting `[Spatial(typeof(GeographyPoint))]` (or equivalent). Mirrors the existing owned-type-on-DbSet validation in `EFModelBuilder.EntityFrameworkCoreGetEntities`.

   **`[Spatial]` validation.** Whenever the attribute is the source of `resolvedEdmType`, run two checks before phase 2:
   - The supplied `Type` must be a Microsoft.Spatial primitive — a subclass of `Microsoft.Spatial.Geography` or `Microsoft.Spatial.Geometry`. Anything else → `EdmModelValidationException` with "X is not a Microsoft.Spatial primitive type" plus the entity/property name.
   - The attribute's genus must match the storage property's genus. EF6: `DbGeography` requires Geography family, `DbGeometry` requires Geometry family. EFCore: if `InferGenus` returns a non-null genus from the column-type lookup, the attribute must match that genus; if it returns null (column type unset/unrecognized), the attribute is the sole authority. Mismatch → `EdmModelValidationException` naming the entity, property, attribute genus, and storage genus. Without this check, a `[Spatial(typeof(GeometryPoint))]` on a `DbGeography` would publish `Edm.GeometryPoint` to clients but write back through the Geography path on submit, producing a runtime mismatch.
2. Call `builder.Ignore(...)` once with the union of every storage type the metadata provider reports — `DbGeography`, `DbGeometry` for EF6; the abstract `Geometry` plus every concrete NTS subclass (`Point`, `LineString`, `Polygon`, `MultiPoint`, `MultiLineString`, `MultiPolygon`, `GeometryCollection`) for EFCore. `ISpatialModelMetadataProvider.IgnoredStorageTypes` returns the list per flavor. Type-level `Ignore` causes the convention builder to skip any property of that type during structural-property discovery — exactly the side door we need so it doesn't try to treat them as unmapped complex types. The list is exhaustive for the storage CLR types Restier knows about; in practice none of these types have legitimate non-spatial uses, so the lack of a per-property opt-out is fine. (`EFModelBuilder` owns the underlying `ODataConventionModelBuilder` internally, so a `Property<T>`-style escape hatch is not exposed today; if a future caller needs one, the convention can grow an opt-out point — out of scope for spec A.)

**Phase 2 — post-model-build.** After `builder.GetEdmModel()` returns the `EdmModel`:
1. For each captured `(entityType, propertyInfo, resolvedEdmType)`:
   - Compute the EDM property name. If `EFModelBuilder`'s `RestierNamingConvention` is `LowerCamelCase` or `LowerCamelCaseWithEnumMembers`, lower-camelCase the original CLR name; otherwise use it verbatim. (Mirrors what `ODataConventionModelBuilder` does internally when `EnableLowerCamelCase` is set.)
   - Locate the matching `EdmEntityType` in the model.
   - Call `entityType.AddStructuralProperty(edmName, primitiveTypeKind, isNullable: true)`, capturing the new `EdmStructuralProperty`.
   - Call `model.SetAnnotationValue(edmStructuralProperty, new ClrPropertyInfoAnnotation(propertyInfo))`. This is what every other Restier path keys on (`EdmClrPropertyMapper.GetClrPropertyName`, used by `RestierResourceDeserializer`, `RestierQueryBuilder`, `RestierController`, `DeepUpdateClassifier`, `DeepOperationExtractor`); without it the spatial properties would lose CLR-name mapping under camelCase and bypass Restier's deserialization path.
2. The reflection-based property accessor used by AspNetCoreOData's serializer at runtime keys on the *original* CLR property name (resolved from the annotation), so it finds the storage-typed CLR property and returns its raw value. The read-path hook (next section) handles the type substitution before serialization.

`EdmHelpers.GetPrimitiveTypeKind` is also extended in this spec to recognize Microsoft.Spatial CLR types, but that extension is for Restier's own type-reference helpers (operations, function returns) — it is **not** the substitution mechanism for entity properties. The substitution happens only in the model-builder convention above.

**`EFModelBuilder` ctor change.** The current ctor is `EFModelBuilder(TDbContext, ModelMerger, RestierNamingConvention = PascalCase)`. Spec A appends an optional `IEnumerable<ISpatialModelMetadataProvider> spatialMetadataProviders = null` parameter. Behavior when null or empty: the convention is a no-op — no properties are captured, no `Ignore` call is made, no annotations are attached. This keeps the existing chained `IModelBuilder` contract intact for consumers who haven't opted into spatial.

DI resolves the parameter from the route service container automatically: `services.AddRestierSpatial()` registers the per-flavor provider as a singleton, and the existing `AddSingleton<IChainedService<IModelBuilder>, EFModelBuilder<TDbContext>>` registration in `AddEFProviderServices` doesn't need to change because the DI container fills `IEnumerable<T>` parameters with whatever's registered.

Direct-construction fixtures (`new EFModelBuilder<TDbContext>(dbContext, modelMerger)`) keep compiling unchanged thanks to the default-null. A repo grep at design time shows no current direct-construction sites in tests — but any that surface during implementation just need the new parameter omitted, since the default is the no-op path.

### Read-path hook

Extend `RestierPayloadValueConverter` to hold a constructor-injected `IEnumerable<ISpatialTypeConverter>`:

```csharp
public class RestierPayloadValueConverter : ODataPayloadValueConverter
{
    private readonly ISpatialTypeConverter[] spatialConverters;

    public RestierPayloadValueConverter(IEnumerable<ISpatialTypeConverter> spatialConverters = null)
    {
        this.spatialConverters = spatialConverters?.ToArray() ?? Array.Empty<ISpatialTypeConverter>();
    }

    public override object ConvertToPayloadValue(object value, IEdmTypeReference edmTypeReference)
    {
        // ... existing DateOnly / TimeOnly / DateTime branches ...

        if (edmTypeReference is not null && IsSpatialEdmType(edmTypeReference) && value is not null)
        {
            var storageType = value.GetType();
            for (var i = 0; i < spatialConverters.Length; i++)
            {
                if (spatialConverters[i].CanConvert(storageType))
                {
                    return spatialConverters[i].ToEdm(value, MapEdmSpatialToClr(edmTypeReference));
                }
            }
        }

        return base.ConvertToPayloadValue(value, edmTypeReference);
    }
}
```

Microsoft.Spatial values that arrive at the converter (because the user declared a Microsoft.Spatial-typed property directly) flow through unchanged via `base`.

The default-null parameter keeps the existing parameterless construction working — `DefaultRestierSerializerProvider` (`src/Microsoft.Restier.AspNetCore/Formatter/Serialization/DefaultRestierSerializerProvider.cs:49`) and the `RestierPayloadValueConverterTests` fixture both keep compiling without modification. The DI path uses the `IEnumerable<ISpatialTypeConverter>` resolution so `services.AddRestierSpatial()` flows the converters through.

### Write-path hook

`EFChangeSetInitializer` (both flavors) gains a constructor and stores the converter list as a field. Its DI registration stays as `AddSingleton` — converters are stateless and live in the route service container alongside the initializer at the same singleton lifetime, so the existing `DefaultSubmitHandler` (singleton, captures `IChangeSetInitializer`) is unaffected. The `ConvertToEfValue(Type, object)` signature is unchanged — callers in `SetValues` keep working without touching `SubmitContext`:

```csharp
public class EFChangeSetInitializer : DefaultChangeSetInitializer
{
    private readonly ISpatialTypeConverter[] spatialConverters;

    public EFChangeSetInitializer(IEnumerable<ISpatialTypeConverter> spatialConverters = null)
    {
        this.spatialConverters = spatialConverters?.ToArray() ?? Array.Empty<ISpatialTypeConverter>();
    }

    public virtual object ConvertToEfValue(Type type, object value)
    {
        // ... existing branches ...

        if (value is not null && IsSpatialStorageType(type))
        {
            for (var i = 0; i < spatialConverters.Length; i++)
            {
                if (spatialConverters[i].CanConvert(type))
                {
                    return spatialConverters[i].ToStorage(type, value);
                }
            }
        }

        return value;
    }
}
```

`IsSpatialStorageType` is a small flavor-specific helper (the EF6 `EFChangeSetInitializer` checks for `DbGeography`/`DbGeometry`; the EFCore one checks for `NetTopologySuite.Geometries.Geometry` and subclasses).

The optional ctor parameter keeps the existing test fixtures (`new EFChangeSetInitializer()`) working without modification — only the spatial-specific tests need to pass converters.

### Per-flavor converter packages

Two new `csproj`s ship the converter implementations and the `ISpatialModelMetadataProvider` for the flavor:

```
src/Microsoft.Restier.EntityFramework.Spatial/
    DbSpatialConverter.cs
    DbSpatialModelMetadataProvider.cs
    Extensions/ServiceCollectionExtensions.cs                (AddRestierSpatial)

src/Microsoft.Restier.EntityFrameworkCore.Spatial/
    NtsSpatialConverter.cs
    NtsSpatialModelMetadataProvider.cs                       (column-type-based genus inference)
    Extensions/ServiceCollectionExtensions.cs                (AddRestierSpatial)
```

Both converters use the same SRID- and Z/M-preserving round-trip. The Microsoft.Spatial side speaks **SQL Server extended WKT** (with `SRID=…;` prefix); the storage side speaks **bare WKT plus a separate SRID parameter**. The converter mediates the two dialects explicitly via two small helpers in the `.Spatial` package:

- `string FormatWithSridPrefix(int srid, string bareWkt)` → `"SRID={srid};{bareWkt}"`
- `(int srid, string body) ParseSridPrefix(string sridPrefixedWkt)` → splits at the first `;`

`Microsoft.Spatial.WellKnownTextSqlFormatter` parses the `SRID=…;` prefix during `Read<TGeo>(...)` and assigns the resulting value's `CoordinateSystem` from it (no post-parse mutation needed — `Geography.CoordinateSystem` is read-only). On the `ToStorage` direction the SRID is read directly from `value.CoordinateSystem.EpsgId` rather than relying on the formatter's emitted prefix (which the formatter may omit when the CRS equals the default), and `ParseSridPrefix` tolerates input both with and without a prefix — when absent, it returns the body unchanged and the converter uses the directly-read SRID.

**Non-EPSG coordinate systems.** `Microsoft.Spatial.CoordinateSystem.EpsgId` is `int?`; non-null only when the CRS came from the EPSG registry. `DbGeography.FromText(..., int srid)`, `DbGeometry.FromText(..., int srid)`, and `NTS.Geometry.SRID` (an `int`) all require a non-nullable integer SRID. If `EpsgId` is null on a `ToStorage` call, the converter throws `InvalidOperationException` naming the property and the non-EPSG `CoordinateSystem.Id` — silently picking a default would corrupt data. The opposite direction is safe: SRID always arrives from storage as a non-nullable int, and `CoordinateSystem.Geography(srid)` / `CoordinateSystem.Geometry(srid)` factories return a valid CRS for any int. Default-SRID configuration (per-API or per-property) remains in the deferred list; a future spec can layer a configurable default on top of this fail-fast behavior.

- **`ToEdm`** (EF6): `DbSpatialServices.Default.AsTextIncludingElevationAndMeasure(value)` returns bare SQL Server-variant WKT with Z and M ordinates (the `.AsText()` instance method is 2D only). `value.CoordinateSystemId` is the SRID. Build SRID-prefixed text with `FormatWithSridPrefix(srid, bareWkt)`; feed to `WellKnownTextSqlFormatter.Create(allowOnlyTwoDimensions: false).Read<TGeo>(reader)`. The parsed value already carries the right `CoordinateSystem`. `DbSpatialServices.Default` resolves to the configured provider's spatial services via `DbConfiguration.DependencyResolver`.
- **`ToStorage`** (EF6): `WellKnownTextSqlFormatter.Write(value, writer)` produces WKT (with or without `SRID=…;` prefix). Take the SRID directly from `value.CoordinateSystem.EpsgId`; pass through `ParseSridPrefix` to strip a prefix if present; pass the bare body and SRID to `DbGeography.FromText(body, srid)` / `DbGeometry.FromText(body, srid)`. Both APIs accept SQL Server-style ZM-augmented WKT (`POINT(lon lat z m)`).
- **`ToEdm`** (EFCore): NTS `WKTWriter` configured with `Ordinates.XYZM` produces bare WKT. SRID comes from `geometry.SRID`. Build SRID-prefixed text with `FormatWithSridPrefix`; feed to the formatter; parsed value carries the right `CoordinateSystem`.
- **`ToStorage`** (EFCore): same dialect handling as EF6 — SRID from `value.CoordinateSystem.EpsgId`, body via `ParseSridPrefix`; `WKTReader.Read(body)` parses the bare WKT; `result.SRID = srid` re-stamps the NTS instance (NTS `Geometry.SRID` is mutable, unlike Microsoft.Spatial's `CoordinateSystem`).

The full Microsoft.Spatial type tree is covered uniformly (Point, LineString, Polygon, MultiPoint, MultiLineString, MultiPolygon, Collection — Geography and Geometry families).

User-facing wiring is a single line per project:

```csharp
services.AddRestierSpatial();
```

This registers the flavor's converter and `ISpatialModelMetadataProvider` so that both the model-builder convention and the read/write hooks find them via DI.

### Data flow summary

**Read** (DB → client):
1. EF materializes `DbGeography`/NTS subclass from the column.
2. AspNetCoreOData serializer reads the property by reflection, gets the storage value.
3. `RestierPayloadValueConverter.ConvertToPayloadValue(value, edmType)` dispatches to the registered `ISpatialTypeConverter`.
4. Converter produces a Microsoft.Spatial value preserving SRID + Z/M.
5. OData writes the Microsoft.Spatial value to the payload.

**Write** (client → DB):
1. OData deserializes the `Edm.Geography*` payload to a Microsoft.Spatial value.
2. `EFChangeSetInitializer` builds a `DataModificationItem`; the `LocalValues` dictionary contains the Microsoft.Spatial value.
3. `ConvertToEfValue(propertyType, value)` dispatches to the registered `ISpatialTypeConverter`.
4. Converter produces a storage value preserving SRID + Z/M.
5. The storage value is assigned to the entity property; EF persists.

## Test plan

### Test scenario integration

Add spatial properties to the **Library** scenario (used by the DateOnly/TimeOnly spec — same pattern, same baselines already maintained), conditional per flavor:

```csharp
// test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Publisher.cs
public class Publisher
{
    public string Id { get; set; }
    public string Name { get; set; }

#if EF6
    public DbGeography HeadquartersLocation { get; set; }       // -> Edm.Geography (abstract base, no attribute)

    [Spatial(typeof(GeographyPolygon))]
    public DbGeography ServiceArea { get; set; }                 // -> Edm.GeographyPolygon

    public DbGeometry FloorPlan { get; set; }                    // -> Edm.Geometry (abstract base)
#endif

#if EFCore
    public NetTopologySuite.Geometries.Point HeadquartersLocation { get; set; }     // -> Edm.GeographyPoint (column-type-derived genus)
    public NetTopologySuite.Geometries.Polygon ServiceArea { get; set; }            // -> Edm.GeographyPolygon

    [Spatial(typeof(GeometryPoint))]
    public NetTopologySuite.Geometries.Point IndoorOrigin { get; set; }             // -> Edm.GeometryPoint (attribute override)
#endif
}
```

`LibraryTestInitializer` seeds well-known points/polygons (with non-default SRID and a Z-coordinate on at least one value) so SRID and dimensionality round-trip can be asserted end-to-end.

### Unit tests

Per `.Spatial` package:
- WKT round-trip for every Microsoft.Spatial type (Point, LineString, Polygon, MultiPoint, MultiLineString, MultiPolygon, Collection — Geography and Geometry families).
- **SRID preservation**: round-trip a Point with SRID 4326, a Point with SRID 4269, and a Geometry Point with SRID 3857. Assert the SRID survives both directions.
- **Z/M preservation**: round-trip a `POINT Z`, a `POINT M`, and a `POINT ZM`. Assert Z and M survive.
- **SRID-prefix parsing**: feed `"SRID=4269;POINT(...)"` directly to `WellKnownTextSqlFormatter.Read<GeographyPoint>` and assert the result's `CoordinateSystem.EpsgId == 4269` — proves the parse-time CRS path works without re-stamping.
- **`FormatWithSridPrefix` / `ParseSridPrefix` helpers**: round-trip happy path, plus parse failures (no `;`, empty body, malformed prefix).
- **Non-EPSG `CoordinateSystem` rejection**: `ToStorage` with a value whose `CoordinateSystem.EpsgId` is null → `InvalidOperationException` whose message includes the property name and the non-EPSG `CoordinateSystem.Id`.
- Null handling (storage `null` → Edm `null` and back).
- Type mismatch error path (e.g. asking `ToStorage` for `DbGeometry` with a `GeographyPoint` value).
- Axis-order regression: a fix-locked test that fails if anyone reintroduces `lat lon` ordering in WKT output.

### Model-builder unit tests

In `Microsoft.Restier.Tests.EntityFrameworkCore` (and EF6 equivalent):
- EFCore property typed as `NetTopologySuite.Geometries.Point` with `HasColumnType("geography")` configured → EDM declares `Edm.GeographyPoint`.
- EFCore property typed as `Point` with `HasColumnType("geometry(Point,4326)")` (Npgsql-style) configured → EDM declares `Edm.GeometryPoint`.
- EFCore property typed as `Point` with no column-type configuration and no `[Spatial]` → `EdmModelValidationException` with the entity and property name in the message.
- `[Spatial(typeof(GeometryPoint))]` overrides any column-type inference.
- **`[Spatial]` genus mismatch (EF6)**: `[Spatial(typeof(GeometryPoint))]` on a `DbGeography` property → `EdmModelValidationException` naming the entity, property, attribute genus, and storage genus.
- **`[Spatial]` genus mismatch (EFCore with column type)**: `[Spatial(typeof(GeographyPoint))]` on an NTS property whose column type lookup returned `Geometry` → throws.
- **`[Spatial]` non-Microsoft.Spatial type**: `[Spatial(typeof(string))]` (or any non-Spatial type) → `EdmModelValidationException` with "X is not a Microsoft.Spatial primitive type".
- **CamelCase + CLR annotation**: spatial property in a model built with `RestierNamingConvention.LowerCamelCase` → EDM property name is `headquartersLocation`; `EdmClrPropertyMapper.GetClrPropertyName(edmProperty, model)` returns `HeadquartersLocation` (proves `ClrPropertyInfoAnnotation` is attached).
- **PascalCase (default)**: spatial property in a default-naming model → EDM property name is `HeadquartersLocation` verbatim and the annotation still resolves the original CLR name.

### Integration tests

In `Microsoft.Restier.Tests.AspNetCore`, both flavors. **Each test asserts the EDM-metadata declaration and the payload-level type independently:**

- **EF6 `HeadquartersLocation` (unannotated `DbGeography`)**:
  - EDM `$metadata` declares `Property Name="HeadquartersLocation" Type="Edm.Geography"` (abstract base).
  - `GET /Publishers(1)` returns a payload whose `HeadquartersLocation` value carries `@odata.type: "#GeographyPoint"` and Microsoft.Spatial-shaped coordinates.
- **EF6 `ServiceArea` (`[Spatial(typeof(GeographyPolygon))]`)**:
  - EDM declares `Type="Edm.GeographyPolygon"` directly.
  - Payload omits `@odata.type` (redundant with declared type).
- **EFCore `HeadquartersLocation` (NTS `Point`, column type `geography`)**:
  - EDM declares `Type="Edm.GeographyPoint"`.
  - Payload omits `@odata.type`.
- **EFCore `IndoorOrigin` (`[Spatial(typeof(GeometryPoint))]`)**:
  - EDM declares `Type="Edm.GeometryPoint"`.
  - Payload omits `@odata.type`.
- **POST/PATCH round-trip**: `POST /Publishers` with a Microsoft.Spatial Polygon payload persists; subsequent `GET` round-trips with SRID and (where supplied) Z preserved.
- **`$select=HeadquartersLocation,ServiceArea`** returns just the spatial properties.
- **`$filter=geo.distance(HeadquartersLocation, ...) lt 10000`** — **negative test** asserting the documented spec-A limitation; flips to a positive test in spec B.

### Baseline regenerations

- `LibraryApi-EF6-ApiMetadata.txt` — shows `Edm.Geography` (abstract) for unannotated `DbGeography` properties, `Edm.GeographyPolygon` / `Edm.Geometry` for the others.
- `LibraryApi-EFCore-ApiMetadata.txt` — shows specific `Edm.GeographyPoint` / `Edm.GeographyPolygon` / `Edm.GeometryPoint` per the column-type-derived genus and attribute overrides.

## Documentation

- New hand-written guide page `src/Microsoft.Restier.Docs/guides/spatial-types.mdx` covering: which packages to install, what an entity property looks like for each EF flavor, when `[Spatial]` is required (always for EFCore unless the column type is unambiguous), and a "what's not yet supported" note pointing forward to spec B.
- The docsproj `<MintlifyTemplate>` block gets a new entry under the Guides group; `docs.json` regenerates on build.

## Sample app

The Postgres sample (`src/Microsoft.Restier.Samples.Postgres.AspNetCore`) is PostGIS-capable. Add a single spatial property on the `User` entity (`Point HomeLocation`) plus a migration. Because Npgsql's default column type is `geometry`, the sample either configures `HasColumnType("geography(Point,4326)")` in `OnModelCreating` or uses `[Spatial(typeof(GeographyPoint))]` — the guide page documents both options.

EF6 sample is unchanged in spec A.

## Scope

### Source files added

- `src/Microsoft.Restier.Core/Spatial/SpatialAttribute.cs`
- `src/Microsoft.Restier.Core/Spatial/SpatialGenus.cs`
- `src/Microsoft.Restier.Core/Spatial/ISpatialTypeConverter.cs`
- `src/Microsoft.Restier.Core/Spatial/ISpatialModelMetadataProvider.cs`
- `src/Microsoft.Restier.EntityFramework.Shared/Model/SpatialModelConvention.cs` (the two-phase EFModelBuilder integration)
- `src/Microsoft.Restier.EntityFramework.Spatial/` — new `csproj`, `DbSpatialConverter.cs`, `DbSpatialModelMetadataProvider.cs`, `ServiceCollectionExtensions.cs`
- `src/Microsoft.Restier.EntityFrameworkCore.Spatial/` — new `csproj`, `NtsSpatialConverter.cs`, `NtsSpatialModelMetadataProvider.cs`, `ServiceCollectionExtensions.cs`

### Source files modified

- `src/Microsoft.Restier.EntityFramework.Shared/Model/EFModelBuilder.cs` — append optional `IEnumerable<ISpatialModelMetadataProvider> spatialMetadataProviders = null` to the ctor; invoke `SpatialModelConvention` at phase 1 and phase 2 around `GetEdmModel()`; pass `_dbContext` through as the `providerContext` for `InferGenus`.
- `src/Microsoft.Restier.AspNetCore/Model/EdmHelpers.cs` — extend `GetPrimitiveTypeKind` for Microsoft.Spatial types (Restier's own type-reference helper, not the model substitution path).
- `src/Microsoft.Restier.AspNetCore/RestierPayloadValueConverter.cs` — accept `IEnumerable<ISpatialTypeConverter>` via constructor injection; add the spatial branch in `ConvertToPayloadValue`.
- `src/Microsoft.Restier.EntityFramework/Submit/EFChangeSetInitializer.cs` — replace the existing `DbGeography` block with constructor-injected converter dispatch.
- `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs` — add constructor-injected converter dispatch branch.

### Source files deleted

- `src/Microsoft.Restier.EntityFramework/Spatial/GeographyConverter.cs` (and the matching resource strings in `Resources.resx` / `Resources.Designer.cs`)
- Any tests that pin the deleted converter's hand-built WKT behavior

### Test files

- `test/Microsoft.Restier.Tests.EntityFramework.Spatial/` — new project, mirrors the new `.Spatial` package
- `test/Microsoft.Restier.Tests.EntityFrameworkCore.Spatial/` — new project, mirrors the new `.Spatial` package
- `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Publisher.cs` — conditional spatial properties
- `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs` — seed spatial values with non-default SRID + Z
- `test/Microsoft.Restier.Tests.Shared.EntityFrameworkCore/Scenarios/Library/LibraryTestInitializer.cs` — seed spatial values with non-default SRID + Z
- `test/Microsoft.Restier.Tests.AspNetCore/IntegrationTests/SpatialTypeIntegrationTests.cs` — new integration coverage (EDM + payload assertions per the test plan)
- `test/Microsoft.Restier.Tests.EntityFrameworkCore/Model/SpatialModelConventionTests.cs` — column-type-derived genus, attribute override, fail-fast on ambiguity
- `test/Microsoft.Restier.Tests.EntityFramework/EFChangeSetInitializerTests.cs` — update fixture ctor signature (no behaviour change)
- `test/Microsoft.Restier.Tests.AspNetCore/EFChangeSetInitializerTests.cs` — update fixture ctor signature
- `test/Microsoft.Restier.Tests.AspNetCore/Baselines/LibraryApi-EF6-ApiMetadata.txt` — regenerated
- `test/Microsoft.Restier.Tests.AspNetCore/Baselines/LibraryApi-EFCore-ApiMetadata.txt` — regenerated

### Documentation files

- `src/Microsoft.Restier.Docs/guides/spatial-types.mdx` (new)
- `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` — `<MintlifyTemplate>` updated

### Sample app changes

- `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Models/User.cs` — add `Point HomeLocation`
- `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Models/RestierTestContext.cs` — `OnModelCreating` configures `HasColumnType("geography(Point,4326)")` for the new property (or the sample uses `[Spatial(typeof(GeographyPoint))]`)
- `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Migrations/` — new migration
- `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Models/RestierTestContext.SeedData.cs` — seed coordinates

### Solution

- `RESTier.slnx` — add the four new projects (two `.Spatial` source projects, two test projects)

### Not changed in spec A

- No custom `IFilterBinder`. `geo.distance` and similar operators remain non-translatable in `$filter`. Spec A documents this and asserts it via a negative test.
- No source generator or `[SpatialProperty]`-style sugar. Users wanting to expose Microsoft.Spatial types directly on entities (the inverse of the current spec) wait for spec C.
- EF6 sample app — unchanged.

## Out of scope (deferred)

| Deferred to | Item |
|-------------|------|
| Spec B | Custom `IFilterBinder` so `geo.distance` / `geo.length` / `geo.intersects` translate to `DbGeography.Distance` / `Geometry.Distance` server-side. |
| Spec C | Source-generator or convention-driven sugar for users who prefer Microsoft.Spatial-typed entity properties (inverse of spec A's storage-typed model). |
| Later | `geo.*` `$orderby` support, default SRID configuration (per-API or per-property), very-large-geometry streaming via `Edm.Stream`. |
