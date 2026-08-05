# Convert PostgreSQL Sample to vnext

## Goal

Convert `Microsoft.Restier.Samples.Postgres.AspNetCore` from the old RESTier API (main branch) to the vnext API surface on `feature/vnext`. The sample already uses EF Core and PostgreSQL — only the RESTier wiring needs updating.

## Reference Implementation

The Northwind sample (`Microsoft.Restier.Samples.Northwind.AspNetCore`) is the canonical vnext sample. All patterns below mirror it.

## Changes

### 1. Program.cs — Full Rewrite

Replace the old registration API with the vnext pattern:

**Service registration:**

```csharp
builder.Services
    .AddControllers()
    .AddRestier(options =>
    {
        options.Select().Expand().Filter().OrderBy().SetMaxTop(100).Count();
        options.TimeZone = TimeZoneInfo.Utc;

        options.AddRestierRoute<RestierTestContextApi>("v3", restierServices =>
        {
            restierServices
                .AddEFCoreProviderServices<RestierTestContext>((services, dbOptions) =>
                    dbOptions.UseNpgsql(builder.Configuration.GetConnectionString("RestierTestContext")))
                .AddSingleton(new ODataValidationSettings
                {
                    MaxTop = 5,
                    MaxAnyAllExpressionDepth = 3,
                    MaxExpansionDepth = 3,
                });
        });
    })
    .AddApplicationPart(typeof(RestierTestContextApi).Assembly)
    .AddApplicationPart(typeof(RestierController).Assembly);
```

**Middleware pipeline:**

```csharp
app.UseMiddleware<Restier.AspNetCore.Middleware.ODataBatchHttpContextFixerMiddleware>();
app.UseODataBatching();
app.UseODataRouteDebug();
app.UseRouting();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapRestier();
});
```

**Key namespace changes:**
- Remove: `Microsoft.AspNet.OData.Extensions`, `Microsoft.AspNet.OData.Query`
- Add: `Microsoft.AspNetCore.OData`, `Microsoft.AspNetCore.OData.Query.Validator`

### 2. RestierTestContextApi.cs — Constructor Update

Replace old `IServiceProvider`-based constructor with vnext DI signature:

```csharp
public RestierTestContextApi(
    RestierTestContext dbContext,
    IEdmModel model,
    IQueryHandler queryHandler,
    ISubmitHandler submitHandler)
    : base(dbContext, model, queryHandler, submitHandler)
{
}
```

New using directives needed: `Microsoft.OData.Edm`, `Microsoft.Restier.Core.Query`, `Microsoft.Restier.Core.Submit`.

Remove dead code: commented-out `IMessagePublisher`, `#region` blocks, wrong `<see cref="PartnerProfileContextApi">`.

### 3. .csproj — Property Alignment

Add properties matching the Northwind sample:

```xml
<PropertyGroup>
    <IsPackable>false</IsPackable>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>

<PropertyGroup>
    <NoWarn>;NU5125;NU5105;CA1812;CA1001;CA1062;CA1707;CA1716;CA1801;CA1819;CA1822;CA2007;CA2227</NoWarn>
</PropertyGroup>
```

Mark `Microsoft.EntityFrameworkCore.Tools` with `PrivateAssets`/`IncludeAssets` (design-time only).

### 4. Delete Template Boilerplate

Remove files that are ASP.NET Core template leftovers, not part of the RESTier sample:

- `Controllers/WeatherForecastController.cs`
- `WeatherForecast.cs`

### 5. No Changes Required

These files are already correct for vnext:

- `Models/RestierTestContext.cs` — EF Core `DbContext`, no RESTier dependency
- `Models/User.cs` — POCO entity
- `Models/UserType.cs` — POCO entity
- `appsettings.json` / `appsettings.Development.json` — connection string unchanged
- `efpt.config.json` — EF Core Power Tools config
- `Microsoft.Restier.Samples.Postgres.AspNetCore.http` — manual test file
