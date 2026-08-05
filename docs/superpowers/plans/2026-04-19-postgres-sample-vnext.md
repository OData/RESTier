# PostgreSQL Sample vnext Conversion — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert `Microsoft.Restier.Samples.Postgres.AspNetCore` from the old RESTier main-branch API to the vnext API surface.

**Architecture:** The sample already uses EF Core + PostgreSQL — only the RESTier service registration, middleware pipeline, and API class constructor need updating to match the vnext patterns used by the Northwind sample. Template boilerplate (WeatherForecast) gets removed.

**Tech Stack:** ASP.NET Core (.NET 10), EF Core 10 + Npgsql, RESTier vnext (EntityFrameworkCore, AspNetCore)

---

### Task 1: Delete template boilerplate

**Files:**
- Delete: `src/Microsoft.Restier.Samples.Postgres.AspNetCore/WeatherForecast.cs`
- Delete: `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Controllers/WeatherForecastController.cs`

- [ ] **Step 1: Delete the files**

```bash
git rm src/Microsoft.Restier.Samples.Postgres.AspNetCore/WeatherForecast.cs
git rm src/Microsoft.Restier.Samples.Postgres.AspNetCore/Controllers/WeatherForecastController.cs
```

- [ ] **Step 2: Commit**

```bash
git commit -m "chore: remove WeatherForecast template boilerplate from Postgres sample"
```

---

### Task 2: Update .csproj

**Files:**
- Modify: `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Microsoft.Restier.Samples.Postgres.AspNetCore.csproj`

- [ ] **Step 1: Replace .csproj contents**

The current file is:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net10.0</TargetFramework>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.*" />
		<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.*" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\Microsoft.Restier.AspNetCore\Microsoft.Restier.AspNetCore.csproj" />
		<ProjectReference Include="..\Microsoft.Restier.EntityFrameworkCore\Microsoft.Restier.EntityFrameworkCore.csproj" />
	</ItemGroup>

</Project>
```

Replace with (mirrors Northwind sample pattern):

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<IsPackable>false</IsPackable>
		<GeneratePackageOnBuild>false</GeneratePackageOnBuild>
		<GenerateDocumentationFile>false</GenerateDocumentationFile>
		<TargetFramework>net10.0</TargetFramework>
	</PropertyGroup>

	<PropertyGroup>
		<NoWarn>;NU5125;NU5105;CA1812;CA1001;CA1062;CA1707;CA1716;CA1801;CA1819;CA1822;CA2007;CA2227</NoWarn>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.*" />
		<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.*">
			<PrivateAssets>all</PrivateAssets>
			<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
		</PackageReference>
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\Microsoft.Restier.AspNetCore\Microsoft.Restier.AspNetCore.csproj" />
		<ProjectReference Include="..\Microsoft.Restier.EntityFrameworkCore\Microsoft.Restier.EntityFrameworkCore.csproj" />
	</ItemGroup>

</Project>
```

- [ ] **Step 2: Verify it builds**

Run: `dotnet build src/Microsoft.Restier.Samples.Postgres.AspNetCore/Microsoft.Restier.Samples.Postgres.AspNetCore.csproj`
Expected: Build succeeds (with possible warnings from not-yet-updated .cs files — that's OK, they get fixed in Tasks 3-4).

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.Samples.Postgres.AspNetCore/Microsoft.Restier.Samples.Postgres.AspNetCore.csproj
git commit -m "chore: align Postgres sample .csproj with Northwind vnext pattern"
```

---

### Task 3: Rewrite RestierTestContextApi to vnext constructor

**Files:**
- Modify: `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Controllers/RestierTestContextApi.cs`

**Reference:** The Northwind vnext API class at `src/Microsoft.Restier.Samples.Northwind.AspNetCore/Controllers/NorthwindApi.cs` — constructor takes `(TDbContext dbContext, IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)`.

- [ ] **Step 1: Replace RestierTestContextApi.cs contents**

The current file uses old API: `EntityFrameworkApi<T>(IServiceProvider serviceProvider)`.

Replace with:

```csharp
using System;
using System.Diagnostics;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Samples.Postgres.AspNetCore.Models;

namespace Microsoft.Restier.Samples.Postgres.AspNetCore.Controllers
{
    public class RestierTestContextApi : EntityFrameworkApi<RestierTestContext>
    {
        public RestierTestContextApi(
            RestierTestContext dbContext,
            IEdmModel model,
            IQueryHandler queryHandler,
            ISubmitHandler submitHandler)
            : base(dbContext, model, queryHandler, submitHandler)
        {
        }

        /// <summary>
        /// Checks if the database is online.
        /// </summary>
        /// <returns>True if the database can connect; otherwise, false.</returns>
        [UnboundOperation]
        public bool IsOnline()
        {
            try
            {
                return DbContext.Database.CanConnect();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Debug.WriteLine(ex);
                return false;
            }
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/Microsoft.Restier.Samples.Postgres.AspNetCore/Controllers/RestierTestContextApi.cs
git commit -m "refactor: update RestierTestContextApi to vnext constructor signature"
```

---

### Task 4: Rewrite Program.cs to vnext registration

**Files:**
- Modify: `src/Microsoft.Restier.Samples.Postgres.AspNetCore/Program.cs`

**Reference:** The Northwind vnext `Startup.cs` at `src/Microsoft.Restier.Samples.Northwind.AspNetCore/Startup.cs` — uses `AddControllers().AddRestier(options => { ... })` on `IMvcBuilder`, and `endpoints.MapRestier()` with no arguments.

- [ ] **Step 1: Replace Program.cs contents**

The current file uses old API: `builder.Services.AddRestier(...)`, `endpoints.MapRestier(builder => builder.MapApiRoute<T>(...))`, `app.UseRestierBatching()`, and old `Microsoft.AspNet.OData.*` namespaces.

Replace with:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Samples.Postgres.AspNetCore.Controllers;
using Microsoft.Restier.Samples.Postgres.AspNetCore.Models;
using System;

namespace Microsoft.Restier.Samples.Postgres.AspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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

            app.Run();
        }
    }
}
```

- [ ] **Step 2: Build the project**

Run: `dotnet build src/Microsoft.Restier.Samples.Postgres.AspNetCore/Microsoft.Restier.Samples.Postgres.AspNetCore.csproj`
Expected: Build succeeds with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/Microsoft.Restier.Samples.Postgres.AspNetCore/Program.cs
git commit -m "refactor: rewrite Program.cs to vnext RESTier registration API"
```

---

### Task 5: Build the full solution and verify

- [ ] **Step 1: Build entire solution**

Run: `dotnet build RESTier.slnx`
Expected: Build succeeds with 0 errors.

- [ ] **Step 2: Run all tests**

Run: `dotnet test RESTier.slnx`
Expected: All tests pass. The Postgres sample has no tests of its own — this verifies nothing else broke.
