# DateOnly/TimeOnly Support in Restier

**Date:** 2026-04-15
**Status:** Design approved

## Goal

Add `DateOnly` and `TimeOnly` as first-class primitive types in Restier's type mapping pipeline, mapping them to the existing OData EDM types `Edm.Date` and `Edm.TimeOfDay`. This allows EF Core entities to use idiomatic .NET types instead of the obsolete OData `Date` and `TimeOfDay` types.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Provider scope | EFCore only | EF6 doesn't natively support DateOnly/TimeOnly |
| OData.NET dependency | None — use existing Edm.Date and Edm.TimeOfDay | OData.NET 8.x has no native support; Restier already bridges CLR↔EDM types |
| Test entity changes | Add DateOnly to Universe, convert TimeOfDay to TimeOnly | End-to-end coverage for both types |

## Background

OData.NET 8.x predates `DateOnly`/`TimeOnly` (.NET 6+) and uses its own `Microsoft.OData.Edm.Date` and `Microsoft.OData.Edm.TimeOfDay` types (both now marked obsolete). Restier already bridges between CLR types and OData EDM types — for example, `DateTime` maps to `Edm.Date` and `TimeSpan` maps to `Edm.Duration`. This enhancement adds `DateOnly` and `TimeOnly` to that same bridge.

EF Core natively supports `DateOnly` and `TimeOnly` (since EF Core 6.0), so no EF Core value converters are needed. EF6 does not support these types and continues using `DateTime`/`TimeSpan`/`TimeOfDay` as before.

## Architecture

### Type Mapping Pipeline

Restier's type mapping flows through four stages. Each gets a small addition:

**Stage 1: Model Building** (`EdmHelpers.GetPrimitiveTypeKind`)

Maps CLR types to EDM primitive types during OData model construction:

- `DateOnly` → `EdmPrimitiveTypeKind.Date`
- `TimeOnly` → `EdmPrimitiveTypeKind.TimeOfDay`

This resolves the long-standing TODO (GitHubIssue#49) — `TimeOnly` is the proper CLR type for `Edm.TimeOfDay`, unlike `TimeSpan` which maps to `Edm.Duration`.

**Stage 2: Type Checking Helpers** (`TypeExtensions`)

Add `IsDateOnly(Type)` and `IsTimeOnly(Type)` methods that handle nullable variants (`DateOnly?`, `TimeOnly?`) via `GetUnderlyingTypeOrSelf`.

**Stage 3: Outbound Serialization** (`RestierPayloadValueConverter.ConvertToPayloadValue`)

Converts CLR values to OData payload values for HTTP responses:

- `DateOnly` → `Date(year, month, day)`
- `TimeOnly` → `TimeOfDay(hour, minute, second, millisecond)`

Added alongside existing `DateTime → Date` and `TimeSpan → TimeOfDay` conversions.

**Stage 4: Inbound Deserialization** (`EFChangeSetInitializer.ConvertToEfValue`, EFCore only)

Converts incoming OData payload values back to CLR types on submit:

- `Date` → `DateOnly` (when target property type is `DateOnly`)
- `TimeOfDay` → `TimeOnly` (when target property type is `TimeOnly`)

The EF6 `EFChangeSetInitializer` is unchanged — it continues mapping `Date → DateTime` and `TimeOfDay → TimeSpan`.

### Test Entity Changes

The `Universe` complex type (used in the Library test scenario) gets conditional compilation:

- **EFCore:** `DateOnly DateProperty` (new) and `TimeOnly TimeOfDayProperty` (changed from `TimeOfDay`)
- **EF6:** Unchanged — keeps `TimeOfDay TimeOfDayProperty`, `DateProperty` stays commented out

The manual `TimeOfDay → TimeOnly` value converter in `LibraryContext.OnModelCreating` is removed since the property is now natively `TimeOnly`.

Seed data in `LibraryTestInitializer` uses conditional compilation to provide the correct types per provider.

EFCore metadata baselines are regenerated to reflect the new `DateProperty` in the `Universe` complex type.

## Scope

### Source files (Restier core)

- `src/Microsoft.Restier.AspNetCore/Model/EdmHelpers.cs` — add DateOnly/TimeOnly → EDM mappings
- `src/Microsoft.Restier.AspNetCore/RestierPayloadValueConverter.cs` — add outbound serialization
- `src/Microsoft.Restier.EntityFrameworkCore/Submit/EFChangeSetInitializer.cs` — add inbound deserialization
- `src/Microsoft.Restier.Core/Extensions/TypeExtensions.cs` — add IsDateOnly/IsTimeOnly helpers

### Test files

- `test/Microsoft.Restier.Tests.Shared/Scenarios/Library/Universe.cs` — conditional DateOnly/TimeOnly for EFCore
- `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryContext.cs` — remove value converter
- `test/Microsoft.Restier.Tests.Shared.EntityFramework/Scenarios/Library/LibraryTestInitializer.cs` — conditional seed data
- `test/Microsoft.Restier.Tests.AspNetCore/Baselines/LibraryApi-EFCore-ApiMetadata.txt` — regenerate

### Not changed

- EF6 `EFChangeSetInitializer` — existing Date/TimeOfDay → DateTime/TimeSpan mappings unchanged
- Existing DateTime/TimeSpan conversions in RestierPayloadValueConverter — unchanged
- EF6 test entities and seed data — unchanged
- MarvelApi baselines — Marvel scenario doesn't use Universe
