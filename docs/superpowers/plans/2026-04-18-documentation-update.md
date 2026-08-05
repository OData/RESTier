# Documentation Update Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the `docs/msdocs` documentation up to date with the current RESTier vNext codebase, fixing all outdated code examples, removing dead content, and adding documentation for missing features.

**Architecture:** Each documentation file is updated independently. All code examples are rewritten to use the current ASP.NET Core DI patterns (constructor injection, `AddRestier()`, `AddRestierRoute<TApi>()`). Dead placeholder files are removed. New pages are added for undocumented features (Swagger, Breakdance, EF Core setup, Getting Started).

**Tech Stack:** Markdown (DocFx), C# code examples targeting .NET 8+/ASP.NET Core with Entity Framework Core.

---

## File Structure

| File | Action | Purpose |
|------|--------|---------|
| `docs/msdocs/docfx.json` | Modify | Fix metadata (remove PowerApps references) |
| `docs/msdocs/index.md` | Modify | Update platform info, component list, remove outdated sections |
| `docs/msdocs/getting-started.md` | Rewrite | Write complete getting started guide |
| `docs/msdocs/server/filters.md` | Modify | Update code examples to current API patterns |
| `docs/msdocs/server/method-authorization.md` | Modify | Update code examples, fix centralized auth section |
| `docs/msdocs/server/interceptors.md` | Modify | Fix incorrect descriptions, update code examples |
| `docs/msdocs/server/model-building.md` | Modify | Update code examples, DI patterns |
| `docs/msdocs/server/operations.md` | Create | Replace `extending-restier/additional-operations.md` with current patterns |
| `docs/msdocs/server/swagger.md` | Create | Document OpenAPI/Swagger support |
| `docs/msdocs/server/testing.md` | Create | Document Breakdance test framework |
| `docs/msdocs/extending-restier/in-memory-provider.md` | Modify | Update to ASP.NET Core patterns |
| `docs/msdocs/extending-restier/temporal-types.md` | Modify | Update namespace references |
| `docs/msdocs/extending-restier/additional-operations.md` | Delete | Replaced by `server/operations.md` |
| `docs/msdocs/contribution-guidelines.md` | Modify | Update tools, test framework references |
| `docs/msdocs/clients/dot-net.md` | Delete | Empty placeholder, no client SDK exists |
| `docs/msdocs/clients/dot-net-standard.md` | Delete | Empty placeholder |
| `docs/msdocs/clients/typescript.md` | Delete | Empty placeholder |
| `docs/msdocs/license.md` | Delete | Empty placeholder |

---

### Task 1: Fix `docfx.json` metadata

**Files:**
- Modify: `docs/msdocs/docfx.json`

- [ ] **Step 1: Update `docfx.json` to remove PowerApps references**

Replace the `globalMetadata` and `fileMetadata` sections. Remove all PowerApps-specific metadata, update the destination, and clean up the file metadata section which references PowerApps maker paths:

```json
{
  "build": {
    "content": [
      {
        "files": [
          "**/*.md",
          "**/*.yml"
        ],
        "exclude": [
          "**/obj/**",
          "**/includes/**",
          "_site/**",
          "README.md",
          "LICENSE",
          "LICENSE-CODE",
          "ThirdPartyNotices"
        ]
      }
    ],
    "resource": [
      {
        "files": [
          "**/*.png",
          "**/*.jpg",
          "**/*.gif",
          "**/*.svg"
        ],
        "exclude": [
          "**/obj/**"
        ]
      }
    ],
    "overwrite": [],
    "externalReference": [],
    "globalMetadata": {
      "titleSuffix": "Microsoft RESTier",
      "feedback_system": "GitHub",
      "feedback_github_repo": "OData/RESTier"
    },
    "template": [],
    "dest": "restier-docs",
    "markdownEngineName": "markdig"
  }
}
```

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/docfx.json
git commit -m "docs: fix docfx.json metadata — remove PowerApps references"
```

---

### Task 2: Delete dead placeholder files

**Files:**
- Delete: `docs/msdocs/clients/dot-net.md`
- Delete: `docs/msdocs/clients/dot-net-standard.md`
- Delete: `docs/msdocs/clients/typescript.md`
- Delete: `docs/msdocs/license.md`
- Delete: `docs/msdocs/extending-restier/additional-operations.md`

- [ ] **Step 1: Remove empty placeholder files and the outdated operations doc**

```bash
rm docs/msdocs/clients/dot-net.md
rm docs/msdocs/clients/dot-net-standard.md
rm docs/msdocs/clients/typescript.md
rm docs/msdocs/license.md
rm docs/msdocs/extending-restier/additional-operations.md
rmdir docs/msdocs/clients
```

- [ ] **Step 2: Commit**

```bash
git add -A docs/msdocs/clients docs/msdocs/license.md docs/msdocs/extending-restier/additional-operations.md
git commit -m "docs: remove empty placeholder files and outdated operations doc"
```

---

### Task 3: Update `index.md` — landing page

**Files:**
- Modify: `docs/msdocs/index.md`

- [ ] **Step 1: Rewrite `index.md` with current information**

Replace the entire file. Key changes:
- Update supported platforms from "Classic ASP.NET 5.2.3" to .NET 8, .NET 9, .NET 10
- Remove the Classic ASP.NET component list
- Update the component list to reflect current packages
- Remove "Coming Soon!" and "H1 2019" references
- Update ecosystem section
- Remove weekly standups reference
- Keep contributors section but note it may need updating

```markdown
<div align="center">
<h1>Microsoft RESTier - OData Made Simple</h1>

[Releases](https://github.com/OData/RESTier/releases)&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;Documentation&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;[OData v4.01 Documentation](https://www.odata.org/documentation/)

</div>

## What is RESTier?

RESTier is an API development framework for building standardized, OData V4 based RESTful services on .NET.

RESTier is the spiritual successor to [WCF Data Services](https://en.wikipedia.org/wiki/WCF_Data_Services). Instead of
generating endless boilerplate code with the current Web API + OData toolchain, RESTier helps you bootstrap a standardized,
queryable HTTP-based REST interface in literally minutes. And that's just the beginning.

Like WCF Data Services before it, RESTier provides simple and straightforward ways to shape queries and intercept submissions
_before_ and _after_ they hit the database. And like Web API + OData, you still have the flexibility to add your own
custom queries and actions with techniques you're already familiar with.

## What is OData?

OData stands for the Open Data Protocol. OData enables the creation and consumption of RESTful APIs, which allow
resources, defined in a data model and identified by using URLs, to be published and edited by Web clients using
simple HTTP requests.

The current version of the protocol (V4) was ratified by OASIS as an industry standard in February 2014.

## Getting Started

See the [Getting Started](getting-started.md) guide to create your first RESTier API.

## Supported Platforms

RESTier vNext supports:
- **.NET 8.0**
- **.NET 9.0**
- **.NET 10.0**

## RESTier Components

RESTier is made up of the following NuGet packages:

| Package | Purpose |
|---------|---------|
| **Microsoft.Restier.AspNetCore** | ASP.NET Core integration, routing, and OData controller |
| **Microsoft.Restier.Core** | Core convention-based interception framework and pipeline |
| **Microsoft.Restier.EntityFrameworkCore** | Entity Framework Core data provider |
| **Microsoft.Restier.EntityFramework** | Entity Framework 6.x data provider (.NET Framework) |
| **Microsoft.Restier.AspNetCore.Swagger** | OpenAPI/Swagger document generation |
| **Microsoft.Restier.Breakdance** | In-memory integration testing framework |

## Ecosystem

There is a growing set of tools to support RESTier-based development:

- [Breakdance](https://github.com/cloudnimble/breakdance): Convention-based name troubleshooting and integration test support.

## Community

### Contributing

If you'd like to help out with the project, please see the [Contribution Guidelines](contribution-guidelines.md).
```

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/index.md
git commit -m "docs: update index.md with current platform and component info"
```

---

### Task 4: Write the Getting Started guide

**Files:**
- Rewrite: `docs/msdocs/getting-started.md`

- [ ] **Step 1: Write the complete Getting Started guide**

This is the most critical missing doc. It should walk users through creating a RESTier API from scratch using the current ASP.NET Core patterns.

```markdown
# Getting Started

This guide walks you through creating a RESTier OData API from scratch using ASP.NET Core and Entity Framework Core.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later
- A code editor (Visual Studio 2022, VS Code, or JetBrains Rider)

## 1. Create a new ASP.NET Core project

```bash
dotnet new web -n MyRestierApi
cd MyRestierApi
```

## 2. Install NuGet packages

```bash
dotnet add package Microsoft.Restier.AspNetCore
dotnet add package Microsoft.Restier.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

For in-memory development/testing, you can use the in-memory database provider instead:

```bash
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

## 3. Define your Entity model

Create a `Models` folder and add your entity classes:

```csharp
// Models/Book.cs
using System;

namespace MyRestierApi.Models;

public class Book
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    public string Author { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }
}
```

## 4. Create a DbContext

```csharp
// Data/BookstoreContext.cs
using Microsoft.EntityFrameworkCore;
using MyRestierApi.Models;

namespace MyRestierApi.Data;

public class BookstoreContext : DbContext
{
    public BookstoreContext(DbContextOptions<BookstoreContext> options) : base(options)
    {
    }

    public DbSet<Book> Books { get; set; }
}
```

## 5. Create your RESTier API class

The API class is where you define your OData surface and add convention-based interceptors.

```csharp
// Api/BookstoreApi.cs
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;
using MyRestierApi.Data;

namespace MyRestierApi.Api;

public class BookstoreApi : EntityFrameworkApi<BookstoreContext>
{
    public BookstoreApi(
        BookstoreContext dbContext,
        IEdmModel model,
        IQueryHandler queryHandler,
        ISubmitHandler submitHandler)
        : base(dbContext, model, queryHandler, submitHandler)
    {
    }
}
```

## 6. Configure services in Program.cs

```csharp
// Program.cs
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.EntityFrameworkCore;
using MyRestierApi.Api;
using MyRestierApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddRestier(options =>
    {
        options.Select().Expand().Filter().OrderBy().SetMaxTop(100).Count();

        options.AddRestierRoute<BookstoreApi>("api", routeServices =>
        {
            routeServices.AddEFCoreProviderServices<BookstoreContext>(dbOptions =>
                dbOptions.UseInMemoryDatabase("Bookstore"));
        });
    });

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
```

## 7. Run and test

```bash
dotnet run
```

Your OData API is now available. Try these URLs:

- **Service document:** `http://localhost:5000/api`
- **Metadata:** `http://localhost:5000/api/$metadata`
- **Query books:** `http://localhost:5000/api/Books`
- **Filter:** `http://localhost:5000/api/Books?$filter=IsActive eq true`
- **Select:** `http://localhost:5000/api/Books?$select=Title,Author`

## Next Steps

- [EntitySet Filters](server/filters.md) — Control query results with convention-based filtering
- [Method Authorization](server/method-authorization.md) — Add fine-grained access control
- [Interceptors](server/interceptors.md) — Add validation and business logic before/after database operations
- [Customizing the Entity Model](server/model-building.md) — Customize the OData EDM model
- [Operations](server/operations.md) — Add custom OData actions and functions
- [OpenAPI/Swagger](server/swagger.md) — Generate OpenAPI documentation
- [Testing with Breakdance](server/testing.md) — Write integration tests
```

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/getting-started.md
git commit -m "docs: write Getting Started guide with ASP.NET Core and EF Core"
```

---

### Task 5: Update `server/filters.md`

**Files:**
- Modify: `docs/msdocs/server/filters.md`

- [ ] **Step 1: Rewrite `filters.md` with current API patterns**

Key changes:
- Replace `EntityFrameworkApi<TrippinModel>` with constructor-DI pattern
- Replace `Microsoft.Restier.Provider.EntityFramework` with current namespaces
- Remove `WebApiConfig.cs` reference in example comment
- Replace `System.Data.Entity` with `Microsoft.EntityFrameworkCore`
- Fix method name: convention is `OnFilter{EntitySetName}` (singular entity name for the method, plural for the set)
- Remove the incomplete "TODO: Pull content from Section 2.8" at the end

```markdown
# EntitySet Filters

Have you ever wanted to limit the results of a particular query based on the current user, or maybe you only want
to return results that are marked "active"?

EntitySet Filters allow you to consistently control the shape of the results returned from particular EntitySets,
even across navigation properties.

## Convention-Based Filtering

Like the rest of RESTier, this is accomplished through a simple convention that
meets the following criteria:

 1. The filter method name must be `OnFilter{EntitySetName}`, where `{EntitySetName}` is the name of the target EntitySet.
 2. It must be a `protected internal` method on your API class.
 3. It should accept an `IQueryable<T>` parameter and return an `IQueryable<T>` result where `T` is the Entity type.

### Example

```cs
using System.Linq;
using System.Security.Claims;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;

namespace MyApp.Api
{

    public class TrippinApi : EntityFrameworkApi<TrippinContext>
    {
        public TrippinApi(TrippinContext dbContext, IEdmModel model,
            IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(dbContext, model, queryHandler, submitHandler)
        {
        }

        /// <summary>
        /// Filters queries to the People EntitySet to only return People that have Trips.
        /// </summary>
        protected internal IQueryable<Person> OnFilterPeople(IQueryable<Person> entitySet)
        {
            return entitySet.Where(c => c.Trips.Any());
        }

        /// <summary>
        /// Filters queries to the Trips EntitySet to only return the current user's Trips.
        /// </summary>
        protected internal IQueryable<Trip> OnFilterTrips(IQueryable<Trip> entitySet)
        {
            var userId = ClaimsPrincipal.Current?.FindFirst("currentUserId")?.Value;
            return entitySet.Where(c => c.PersonId == userId);
        }
    }

}
```

> **Note:** To use `ClaimsPrincipal.Current` in ASP.NET Core, you must add the claims principal middleware
> in your `Program.cs`:
>
> ```cs
> app.UseClaimsPrincipals();
> ```
```

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/server/filters.md
git commit -m "docs: update filters.md with current ASP.NET Core API patterns"
```

---

### Task 6: Update `server/method-authorization.md`

**Files:**
- Modify: `docs/msdocs/server/method-authorization.md`

- [ ] **Step 1: Rewrite `method-authorization.md` with current API patterns**

Key changes:
- Replace `EntityFrameworkApi<TrippinModel>` parameterless class with constructor-DI pattern
- Remove `WebApiConfig.cs` references
- Replace `ConfigureApi()` override with `AddChainedService<>()` in route service configuration
- Update centralized authorization to use `AddChainedService<>()` pattern
- Replace MSTest unit test examples with xUnit
- Remove `AssemblyInfo.cs` / `InternalsVisibleTo` instructions (auto-configured)
- Fix the "Leveraging Both Techniques" section to use the chained service `Inner` property correctly
- Fix incomplete TODO placeholders in centralized authorization examples

```markdown
# Method Authorization

Method Authorization allows you to have fine-grained control over how different types of API requests can be executed.
Since RESTier uses a built-in convention over repetitive boilerplate controllers, you can't just add security attributes
to the controller methods.

However, there are two different methods for defining per-request security. One, like the rest of RESTier, is
convention-based, and the other executes before every request, allowing you to centralize your authorization logic.

No matter what approach you choose, the concept is simple. Either technique uses a function that returns boolean.
Return `true`, and processing continues normally. Return `false`, and RESTier returns a `403 Forbidden` to the client.

## Convention-Based Authorization

Users can control if one of the four submit operations is allowed on some EntitySet or Action by putting some
`protected internal` methods into the API class. The method name must conform to the convention
`Can{Operation}{TargetName}`.

<table style="width: 100%;">
    <tr>
        <td>The possible values for <code>{Operation}</code> are:</td>
        <td>The possible values for <code>{TargetName}</code> are:</td>
    </tr>
    <tr>
        <td>
            <ul style="margin-bottom: 0;">
                <li>Insert</li>
                <li>Update</li>
                <li>Delete</li>
                <li>Execute</li>
            </ul>
        </td>
        <td style="vertical-align: text-top;">
            <ul style="margin-bottom: 0;">
                <li><i>EntitySetName</i></li>
                <li><i>ActionName</i></li>
            </ul>
        </td>
    </tr>
</table>

### Example

The example below demonstrates how both types of `{TargetName}` can be used.

- The first method shows a simple way to prevent *any* user from deleting a particular EntitySet.
- The second method shows how you can integrate role-based security using claims.
- The third method shows how to prevent execution of a custom Action.

```cs
using System.Security.Claims;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;

namespace MyApp.Api
{

    public class TrippinApi : EntityFrameworkApi<TrippinContext>
    {
        public TrippinApi(TrippinContext dbContext, IEdmModel model,
            IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(dbContext, model, queryHandler, submitHandler)
        {
        }

        /// <summary>
        /// Prevents any user from deleting Trips.
        /// </summary>
        protected internal bool CanDeleteTrips()
        {
            return false;
        }

        /// <summary>
        /// Only allows users with the "admin" role to update Trips.
        /// </summary>
        protected internal bool CanUpdateTrips()
        {
            return ClaimsPrincipal.Current.IsInRole("admin");
        }

        /// <summary>
        /// Prevents execution of the ResetDataSource action.
        /// </summary>
        protected internal bool CanExecuteResetDataSource()
        {
            return false;
        }
    }

}
```

## Centralized Authorization

In addition to the more granular convention-based approach, you can also centralize processing into one location. This is
useful if you have cross-cutting authorization logic that applies to all entity sets.

Implement the `IChangeSetItemAuthorizer` interface and register it as a chained service. If the `AuthorizeAsync`
method returns `false`, RESTier returns a `403 Forbidden` response.

There are two steps to plug in centralized authorization logic:

1. Create a class that implements `IChangeSetItemAuthorizer`.
2. Register that class as a chained service in your route configuration.

### Example

```cs
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;

namespace MyApp.Api
{

    public class CustomAuthorizer : IChangeSetItemAuthorizer
    {
        public IChangeSetItemAuthorizer Inner { get; set; }

        public async Task<bool> AuthorizeAsync(
            SubmitContext context,
            ChangeSetItem item,
            CancellationToken cancellationToken)
        {
            // Add your global authorization logic here.
            // For example, check a bearer token or global permission.

            // Delegate to the inner (convention-based) authorizer.
            if (Inner is not null)
            {
                return await Inner.AuthorizeAsync(context, item, cancellationToken);
            }

            return true;
        }
    }

}
```

Register the custom authorizer in your route configuration in `Program.cs`:

```cs
options.AddRestierRoute<TrippinApi>("api", routeServices =>
{
    routeServices.AddEFCoreProviderServices<TrippinContext>(dbOptions =>
        dbOptions.UseSqlServer(connectionString));

    routeServices.AddChainedService<IChangeSetItemAuthorizer>((sp, inner) =>
        new CustomAuthorizer { Inner = inner });
});
```

## Leveraging Both Techniques

You can combine centralized and convention-based authorization. The centralized authorizer runs first and can
delegate to the convention-based methods via the `Inner` property. This is useful when you need a global check
(e.g., validate a bearer token) before falling through to per-entity authorization.

The example above in the Centralized Authorization section demonstrates this pattern — the `CustomAuthorizer`
performs its check and then calls `Inner.AuthorizeAsync()` to delegate to the convention-based `Can{Operation}`
methods.

## Unit Testing Considerations

Because both of these methods are decoupled from the code that interacts with the database, the authorization
logic is easily testable without having to fire up the entire ASP.NET Core pipeline.

> **Note:** RESTier auto-configures `InternalsVisibleTo` from each source project to its matching test project,
> so the `protected internal` convention methods are accessible from your tests without additional setup.

### Example

Given the [Convention-Based Authorization](#convention-based-authorization) example, the tests below should
provide full coverage.

```cs
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace MyApp.Tests.Api
{

    public class TrippinApiAuthorizationTests
    {
        [Fact]
        public void CanDeleteTrips_ShouldReturnFalse()
        {
            var api = GetApiInstance();
            api.CanDeleteTrips().Should().BeFalse();
        }

        [Fact]
        public void CanUpdateTrips_AsAdmin_ShouldReturnTrue()
        {
            SetCurrentPrincipal("admin");
            var api = GetApiInstance();
            api.CanUpdateTrips().Should().BeTrue();
        }

        [Fact]
        public void CanUpdateTrips_AsNonAdmin_ShouldReturnFalse()
        {
            SetCurrentPrincipal();
            var api = GetApiInstance();
            api.CanUpdateTrips().Should().BeFalse();
        }

        [Fact]
        public void CanExecuteResetDataSource_ShouldReturnFalse()
        {
            var api = GetApiInstance();
            api.CanExecuteResetDataSource().Should().BeFalse();
        }

        private static void SetCurrentPrincipal(params string[] roles)
        {
            var claims = new List<Claim>();
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "Test");
            Thread.CurrentPrincipal = new ClaimsPrincipal(identity);
        }

        // In a real test, use RestierTestHelpers or NSubstitute to create the API instance.
        // This is simplified for illustration.
        private static TrippinApi GetApiInstance() => throw new NotImplementedException(
            "Use RestierTestHelpers.GetTestableApiInstance<TrippinApi>() for real tests");
    }

}
```
```

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/server/method-authorization.md
git commit -m "docs: update method-authorization.md with current API and xUnit patterns"
```

---

### Task 7: Update `server/interceptors.md`

**Files:**
- Modify: `docs/msdocs/server/interceptors.md`

- [ ] **Step 1: Rewrite `interceptors.md` fixing incorrect descriptions and code examples**

Key changes:
- Fix the intro paragraph (lines 10-11) that incorrectly says interceptors return boolean — that's authorization, not interception. Interceptors perform pre/post processing logic and may throw exceptions to reject.
- Replace `EntityFrameworkApi<TrippinModel>` parameterless class with constructor-DI
- Remove `WebApiConfig.cs` references
- Fix the Centralized Interception section: it should use `IChangeSetItemFilter` (not `IChangeSetItemAuthorizer`)
- Remove TODO/NEEDS CLARIFICATION markers
- Replace `ConfigureApi()` override with `AddChainedService<>()` in route config
- Update unit test examples from MSTest to xUnit
- Add async interceptor examples (OnInsertingAsync, etc.)

```markdown
# Interceptors

Interceptors allow you to process validation and business logic before *and after* Entities hit the database. For
example, you may need to validate some external business rules before the object is saved, but then after it's saved,
you may need to send a notification or queue further processing.

## Convention-Based Interception

Users can add pre- and post-processing logic for submit operations by putting `protected internal` methods into the
API class. The method name must conform to the convention `On{Operation}{TargetName}`.

<table style="width: 100%;">
    <tr>
        <td>The possible values for pre-submit <code>{Operation}</code> are:</td>
        <td>The possible values for post-submit <code>{Operation}</code> are:</td>
        <td>The possible values for <code>{TargetName}</code> are:</td>
    </tr>
    <tr>
        <td>
            <ul style="margin-bottom: 0;">
                <li>Inserting</li>
                <li>Updating</li>
                <li>Deleting</li>
                <li>Executing</li>
            </ul>
        </td>
        <td>
            <ul style="margin-bottom: 0;">
                <li>Inserted</li>
                <li>Updated</li>
                <li>Deleted</li>
                <li>Executed</li>
            </ul>
        </td>
        <td style="vertical-align: text-top;">
            <ul style="margin-bottom: 0;">
                <li><i>EntitySetName</i></li>
                <li><i>ActionName</i></li>
            </ul>
        </td>
    </tr>
</table>

Interceptor methods receive the entity being processed. Pre-submit interceptors (`Inserting`, `Updating`, `Deleting`)
can modify the entity or throw an exception to reject the operation. Post-submit interceptors (`Inserted`, `Updated`,
`Deleted`) run after the database operation completes.

Both synchronous (`void`) and asynchronous (`Task`) return types are supported.

### Example

```cs
using System;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;

namespace MyApp.Api
{

    public class TrippinApi : EntityFrameworkApi<TrippinContext>
    {
        public TrippinApi(TrippinContext dbContext, IEdmModel model,
            IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(dbContext, model, queryHandler, submitHandler)
        {
        }

        /// <summary>
        /// Validates a Trip before it is inserted into the database.
        /// Throws an ODataException to reject the operation.
        /// </summary>
        protected internal void OnInsertingTrip(Trip trip)
        {
            if (string.IsNullOrWhiteSpace(trip.Description))
            {
                throw new ODataException("The Trip Description cannot be blank.");
            }
        }

        /// <summary>
        /// Runs after a Trip has been inserted. Use for notifications or side effects.
        /// </summary>
        protected internal void OnInsertedTrip(Trip trip)
        {
            Console.WriteLine($"Trip {trip.TripId} has been inserted.");
        }

        /// <summary>
        /// Async interceptors are also supported. Sets an audit timestamp before update.
        /// </summary>
        protected internal Task OnUpdatingTrip(Trip trip)
        {
            trip.LastModified = DateTimeOffset.UtcNow;
            return Task.CompletedTask;
        }
    }

}
```

## Centralized Interception

In addition to the convention-based approach, you can centralize pre- and post-processing into one location using
the `IChangeSetItemFilter` interface. This is useful when you have cross-cutting logic that applies to all entity
sets (e.g., audit logging).

There are two steps:

1. Create a class that implements `IChangeSetItemFilter`.
2. Register it as a chained service in your route configuration.

### Example

```cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Restier.Core.Submit;

namespace MyApp.Api
{

    public class AuditLogFilter : IChangeSetItemFilter
    {
        public IChangeSetItemFilter Inner { get; set; }

        public async Task OnChangeSetItemProcessingAsync(
            SubmitContext context,
            ChangeSetItem item,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"Processing: {item.GetType().Name}");

            // Delegate to the inner (convention-based) filter.
            if (Inner is not null)
            {
                await Inner.OnChangeSetItemProcessingAsync(context, item, cancellationToken);
            }
        }

        public async Task OnChangeSetItemProcessedAsync(
            SubmitContext context,
            ChangeSetItem item,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"Processed: {item.GetType().Name}");

            // Delegate to the inner (convention-based) filter.
            if (Inner is not null)
            {
                await Inner.OnChangeSetItemProcessedAsync(context, item, cancellationToken);
            }
        }
    }

}
```

Register the filter in your route configuration in `Program.cs`:

```cs
options.AddRestierRoute<TrippinApi>("api", routeServices =>
{
    routeServices.AddEFCoreProviderServices<TrippinContext>(dbOptions =>
        dbOptions.UseSqlServer(connectionString));

    routeServices.AddChainedService<IChangeSetItemFilter>((sp, inner) =>
        new AuditLogFilter { Inner = inner });
});
```

## Unit Testing Considerations

Because interceptor methods are decoupled from the database interaction layer, the logic is easily testable.

> **Note:** RESTier auto-configures `InternalsVisibleTo` from each source project to its matching test project,
> so the `protected internal` interceptor methods are accessible from your tests without additional setup.

### Example

```cs
using System;
using FluentAssertions;
using Microsoft.OData;
using Xunit;

namespace MyApp.Tests.Api
{

    public class TrippinApiInterceptorTests
    {
        [Fact]
        public void OnInsertingTrip_WithBlankDescription_ShouldThrow()
        {
            var api = GetApiInstance();
            var trip = new Trip { Description = "" };

            var act = () => api.OnInsertingTrip(trip);

            act.Should().Throw<ODataException>()
                .WithMessage("The Trip Description cannot be blank.");
        }

        [Fact]
        public void OnInsertingTrip_WithValidDescription_ShouldNotThrow()
        {
            var api = GetApiInstance();
            var trip = new Trip { Description = "A great trip" };

            var act = () => api.OnInsertingTrip(trip);

            act.Should().NotThrow();
        }

        // In a real test, use RestierTestHelpers or NSubstitute to create the API instance.
        private static TrippinApi GetApiInstance() => throw new NotImplementedException(
            "Use RestierTestHelpers.GetTestableApiInstance<TrippinApi>() for real tests");
    }

}
```
```

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/server/interceptors.md
git commit -m "docs: rewrite interceptors.md — fix incorrect descriptions, update to current API"
```

---

### Task 8: Update `server/model-building.md`

**Files:**
- Modify: `docs/msdocs/server/model-building.md`

- [ ] **Step 1: Rewrite `model-building.md` with current patterns**

Key changes:
- Replace all `ConfigureApi()` override patterns with route-level `AddChainedService<IModelBuilder>()`
- Replace `Microsoft.Restier.Provider.EntityFramework` with `Microsoft.Restier.EntityFrameworkCore`
- Replace `System.Web.OData.Builder` with `Microsoft.OData.ModelBuilder`
- Replace `ApiConfiguratorAttribute` usage (no longer exists) with route configuration
- Update `IModelBuilder.GetModelAsync()` to `IModelBuilder.GetEdmModel()` (current signature)
- Remove `InvocationContext`/`ModelContext` from model builder — current API uses parameterless `GetEdmModel()`
- Replace `Context` property with `DbContext` property
- Update Operation attribute examples to use `[BoundOperation]`/`[UnboundOperation]` instead of `[Operation]`
- Add `[Resource]` attribute for entity sets and singletons

```markdown
# Customizing the Entity Model

OData and the Entity Framework are based on the same underlying concept for mapping the idea of an Entity with
its representation in the database. That "mapping" layer is called the Entity Data Model, or EDM for short.

Part of the beauty of RESTier is that, for the majority of API builders, it can construct your EDM for you
*automagically*. But there are times where you have to take charge of the process. And as with many things in RESTier,
there are two ways to do so.

The first method allows you to completely replace the automagic model construction with your own.

The second method lets RESTier do the initial work for you, and then you manipulate the resulting EDM metadata.

## ModelBuilder Takeover

There are several situations where you may want to use this approach. For example, if you're migrating from an
existing Web API OData implementation and needed to customize that model, you can reuse your existing model builder
code. Or if you're using Entity Framework Model First with SQL Views, you may need to define a primary key or
omit the View from your service.

To take over model building, implement `IModelBuilder` and register it as a chained service.

### Example

```cs
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core.Model;

namespace MyApp.Api
{

    internal class CustomizedModelBuilder : IModelBuilder
    {
        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntityType<Person>();
            return builder.GetEdmModel();
        }
    }

}
```

Register it in your route configuration:

```cs
options.AddRestierRoute<TrippinApi>("api", routeServices =>
{
    routeServices.AddEFCoreProviderServices<TrippinContext>(dbOptions =>
        dbOptions.UseSqlServer(connectionString));

    routeServices.AddChainedService<IModelBuilder>((sp, inner) =>
        new CustomizedModelBuilder { Inner = inner });
});
```

If the RESTier Entity Framework provider is used and you have no additional types beyond those in the database schema,
no custom model builder is required — the provider will build the model automatically.

## Extend a model from the API class

The `RestierWebApiModelExtender` will further extend the EDM model using public properties and methods declared
in your API class. Properties and methods declared in parent classes are **NOT** considered.

### Entity Sets

If a property declared in the API class meets these conditions, an entity set will be added to the model:

 - Public with a getter
 - Either static or instance
 - No existing entity set with the same name
 - Return type is `IQueryable<T>` where `T` is a class type
 - Decorated with the `[Resource]` attribute

Example:

```cs
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;

namespace MyApp.Api
{

    public class TrippinApi : EntityFrameworkApi<TrippinContext>
    {
        public TrippinApi(TrippinContext dbContext, IEdmModel model,
            IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(dbContext, model, queryHandler, submitHandler)
        {
        }

        [Resource]
        public IQueryable<Person> PeopleWithFriends =>
            DbContext.People.Include(p => p.Friends);
    }

}
```

### Singletons

If a property declared in the API class meets these conditions, a singleton will be added to the model:

 - Public with a getter
 - Either static or instance
 - No existing singleton with the same name
 - Return type is a non-generic class type
 - Decorated with the `[Resource]` attribute

Example:

```cs
[Resource]
public Person Me => DbContext.People.Find(1);
```

> **Note:** Due to limitations from Entity Framework and the OData spec, CUD (create, update, delete) operations
> on singleton entities are **NOT** supported directly by RESTier. Users need to define their own routes for these
> operations.

### Navigation Property Binding

The `RestierWebApiModelExtender` follows these rules to add navigation property bindings after entity sets and
singletons have been built:

 - Bindings are **ONLY** added for entity sets and singletons built inside `RestierWebApiModelExtender`.
   Entity sets built by the EF provider are assumed to have their bindings already.
 - Only navigation sources of the same entity type as the source navigation property are searched.
 - Singleton navigation properties can be bound to either entity sets or singletons.
 - Collection navigation properties can **ONLY** be bound to entity sets.
 - If there is any ambiguity among entity sets or singletons, no binding will be added.

### Operations

Methods declared in the API class can be exposed as OData actions or functions using the `[BoundOperation]`
or `[UnboundOperation]` attributes.

Example:

```cs
using System.Collections.Generic;
using System.Linq;
using Microsoft.Restier.AspNetCore.Model;

namespace MyApp.Api
{

    public class TrippinApi : EntityFrameworkApi<TrippinContext>
    {
        // ... constructor omitted for brevity ...

        // Unbound action (action import)
        [UnboundOperation(OperationType = OperationType.Action)]
        public void CleanUpExpiredTrips() { }

        // Bound action
        [BoundOperation(OperationType = OperationType.Action)]
        public Trip EndTrip(Trip bindingParameter) { ... }

        // Unbound function (function import)
        [UnboundOperation(EntitySet = "People")]
        public IEnumerable<Person> GetPeopleWithFriendsAtLeast(int n) { ... }

        // Bound composable function
        [BoundOperation(IsComposable = true)]
        public IQueryable<Person> GetPersonWithMostFriends(IEnumerable<Person> bindingParameter) { ... }
    }

}
```

> **Note:** The default `OperationType` is `Function`. Set `OperationType = OperationType.Action` for actions.

## Custom Model Extension

If you need to extend the model after RESTier's conventions have been applied, register an additional
`IModelBuilder` as a chained service. By calling `Inner.GetEdmModel()` first, you get the model built by
RESTier and can then modify it.

```cs
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;

namespace MyApp.Api
{

    internal class CustomModelExtender : IModelBuilder
    {
        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel()
        {
            IEdmModel model = null;

            // Call inner model builder to get the base model.
            if (Inner is not null)
            {
                model = Inner.GetEdmModel();
            }

            // Extend the model here (e.g., add custom navigation property bindings).

            return model;
        }
    }

}
```

Register it in your route configuration:

```cs
routeServices.AddChainedService<IModelBuilder>((sp, inner) =>
    new CustomModelExtender { Inner = inner });
```

The model building order is:

1. EF provider model builder (creates EDM from DbContext)
2. `RestierWebApiModelExtender` (adds entity sets, singletons, and operations from the API class)
3. Your custom model builder (if registered)
```

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/server/model-building.md
git commit -m "docs: rewrite model-building.md with current DI and attribute patterns"
```

---

### Task 9: Create `server/operations.md`

**Files:**
- Create: `docs/msdocs/server/operations.md`

- [ ] **Step 1: Write the operations documentation**

This replaces the old `extending-restier/additional-operations.md` with current patterns.

```markdown
# Operations (Actions & Functions)

RESTier supports OData operations — both actions and functions — as methods on your API class.
Operations are declared using attributes and are automatically added to the OData EDM model.

## Operation Types

| Type | Attribute | Description |
|------|-----------|-------------|
| Unbound Function | `[UnboundOperation]` | A function import — callable without an entity binding |
| Unbound Action | `[UnboundOperation(OperationType = OperationType.Action)]` | An action import — callable without an entity binding |
| Bound Function | `[BoundOperation]` | A function bound to an entity or collection |
| Bound Action | `[BoundOperation(OperationType = OperationType.Action)]` | An action bound to an entity or collection |

The default `OperationType` is `Function`. Set `OperationType = OperationType.Action` to declare an action.

> **Note:** RESTier disables qualified operation calls by default, so you do not need to include the
> namespace in the URL when calling operations.

## Examples

```cs
using System;
using System.Linq;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;

namespace MyApp.Api
{

    public class LibraryApi : EntityFrameworkApi<LibraryContext>
    {
        public LibraryApi(LibraryContext dbContext, IEdmModel model,
            IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(dbContext, model, queryHandler, submitHandler)
        {
        }

        /// <summary>
        /// Unbound action: checks out a book and returns the updated entity.
        /// </summary>
        [UnboundOperation(OperationType = OperationType.Action, EntitySet = "Books")]
        public Book CheckoutBook(Book book)
        {
            book.Title += " | Checked Out";
            return book;
        }

        /// <summary>
        /// Unbound function: returns a queryable collection of favorite books.
        /// </summary>
        [UnboundOperation]
        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All)]
        public IQueryable<Book> FavoriteBooks()
        {
            return DbContext.Books.Where(b => b.IsFavorite);
        }

        /// <summary>
        /// Bound composable function: returns books for a given publisher.
        /// </summary>
        [BoundOperation(IsComposable = true, EntitySetPath = "publisher/Books")]
        public IQueryable<Book> PublishedBooks(Publisher publisher)
        {
            return DbContext.Books.Where(b => b.PublisherId == publisher.Id);
        }

        /// <summary>
        /// Bound action on a collection: discontinues all books in the binding set.
        /// </summary>
        [BoundOperation(OperationType = OperationType.Action)]
        public IQueryable<Book> DiscontinueBooks(IQueryable<Book> books)
        {
            foreach (var book in books.ToList())
            {
                book.IsActive = false;
            }

            return books;
        }
    }

}
```

## Operation Interception

You can intercept operations using the same convention-based pattern as entity set interception:

- `OnExecuting{OperationName}` — runs before the operation executes
- `OnExecuted{OperationName}` — runs after the operation executes
- `CanExecute{OperationName}` — controls whether the operation can be called

```cs
protected internal void OnExecutingCheckoutBook(Book book)
{
    if (book is null)
    {
        throw new ArgumentNullException(nameof(book));
    }
}

protected internal bool CanExecuteCheckoutBook()
{
    return ClaimsPrincipal.Current.IsInRole("librarian");
}
```

## Batch Support

RESTier supports OData batch requests for operations. Batch support is enabled by default when using
`AddRestierRoute<TApi>()`. You can disable it by passing `useRestierBatching: false`:

```cs
options.AddRestierRoute<LibraryApi>("api", routeServices => { ... }, useRestierBatching: false);
```
```

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/server/operations.md
git commit -m "docs: add operations.md documenting OData actions and functions"
```

---

### Task 10: Create `server/swagger.md`

**Files:**
- Create: `docs/msdocs/server/swagger.md`

- [ ] **Step 1: Write the OpenAPI/Swagger documentation**

```markdown
# OpenAPI / Swagger

RESTier can automatically generate OpenAPI (Swagger) documentation for your OData API using the
`Microsoft.Restier.AspNetCore.Swagger` package.

## Setup

### 1. Install the NuGet package

```bash
dotnet add package Microsoft.Restier.AspNetCore.Swagger
```

### 2. Register Swagger services

In your `Program.cs`, add the Swagger services:

```cs
builder.Services.AddRestierSwagger();
```

You can optionally configure the OpenAPI output:

```cs
builder.Services.AddRestierSwagger(settings =>
{
    settings.ServiceRoot = new Uri("https://api.example.com");
});
```

### 3. Add the Swagger middleware

After building the app, add the Swagger UI middleware:

```cs
var app = builder.Build();

app.UseRouting();
app.UseRestierSwaggerUI();
app.MapControllers();

app.Run();
```

## Usage

Once configured, the following endpoints are available:

- **Swagger UI:** `/swagger` — interactive API documentation
- **OpenAPI JSON:** `/swagger/{routePrefix}/swagger.json` — the raw OpenAPI document

If your API is registered with an empty route prefix, the document name defaults to `restier`.

## Multiple APIs

If you have multiple RESTier APIs registered with different route prefixes, Swagger UI will automatically
show a dropdown to switch between them.
```

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/server/swagger.md
git commit -m "docs: add swagger.md documenting OpenAPI support"
```

---

### Task 11: Create `server/testing.md`

**Files:**
- Create: `docs/msdocs/server/testing.md`

- [ ] **Step 1: Write the Breakdance testing documentation**

```markdown
# Testing with Breakdance

The `Microsoft.Restier.Breakdance` package provides an in-memory integration testing framework for RESTier APIs.
It lets you test your complete OData pipeline — including convention-based interceptors, model building, and
query execution — without deploying to a web server.

## Setup

### Install the NuGet package

```bash
dotnet add package Microsoft.Restier.Breakdance
```

## Using RestierTestHelpers (static methods)

The `RestierTestHelpers` class provides static methods for one-off test requests:

```cs
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.EntityFrameworkCore;
using Xunit;

namespace MyApp.Tests
{

    public class BookstoreApiTests
    {
        [Fact]
        public async Task GetBooks_ShouldReturnOk()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<BookstoreApi>(
                HttpMethod.Get,
                resource: "/Books",
                serviceCollection: services =>
                {
                    services.AddEFCoreProviderServices<BookstoreContext>(options =>
                        options.UseInMemoryDatabase("TestDb"));
                });

            response.IsSuccessStatusCode.Should().BeTrue();
        }
    }

}
```

## Using RestierBreakdanceTestBase (base class)

For test classes that share common setup, inherit from `RestierBreakdanceTestBase<TApi>`:

```cs
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Breakdance;
using Microsoft.Restier.EntityFrameworkCore;
using Xunit;

namespace MyApp.Tests
{

    public class BookstoreApiIntegrationTests : RestierBreakdanceTestBase<BookstoreApi>
    {
        [Fact]
        public async Task GetBooks_ShouldReturnOk()
        {
            var response = await ExecuteTestRequest(HttpMethod.Get, resource: "/Books");
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Fact]
        public async Task GetMetadata_ShouldReturnValidEdm()
        {
            var metadata = await GetApiMetadataAsync();
            metadata.Should().NotBeNull();
        }
    }

}
```

## Available Test Methods

### RestierTestHelpers (static)

| Method | Description |
|--------|-------------|
| `ExecuteTestRequest<TApi>(...)` | Sends an HTTP request through the full OData pipeline |
| `GetTestableApiInstance<TApi>(...)` | Gets an API instance for direct method testing |
| `GetTestableModelAsync<TApi>(...)` | Gets the EDM model for inspection |
| `GetApiMetadataAsync<TApi>(...)` | Gets the `$metadata` document as `XDocument` |
| `GetTestableHttpClient<TApi>(...)` | Gets an `HttpClient` configured for the test API |
| `GetTestableInjectedService<TApi, TService>(...)` | Resolves a service from the test DI container |

### RestierBreakdanceTestBase<TApi> (instance)

| Method | Description |
|--------|-------------|
| `ExecuteTestRequest(...)` | Sends an HTTP request through the full OData pipeline |
| `GetApiMetadataAsync(...)` | Gets the `$metadata` document as `XDocument` |
| `GetApiInstance(...)` | Gets an API instance for direct method testing |
| `GetModel(...)` | Gets the EDM model for inspection |
| `GetScopedRequestContainer(...)` | Gets the scoped service provider |
```

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/server/testing.md
git commit -m "docs: add testing.md documenting Breakdance test framework"
```

---

### Task 12: Update `extending-restier/in-memory-provider.md`

**Files:**
- Modify: `docs/msdocs/extending-restier/in-memory-provider.md`

- [ ] **Step 1: Rewrite `in-memory-provider.md` with ASP.NET Core patterns**

Key changes:
- Replace `System.Web.OData.Builder` with `Microsoft.OData.ModelBuilder`
- Replace `ConfigureApi()` override with route-level registration
- Replace `WebApiConfig` / `MapRestierRoute` / `GlobalConfiguration` with ASP.NET Core `Program.cs` setup
- Replace `GetModelAsync(InvocationContext, CancellationToken)` with `GetEdmModel()`
- Use constructor DI for ApiBase

```markdown
## In-Memory Data Provider

RESTier supports building an OData service with all-in-memory resources. There is no dedicated in-memory provider
module — you write a custom model builder and provide data from in-memory collections.

### 1. Create the API class

Create a simple data type and expose it as an entity set on your API class:

```cs
using System.Collections.Generic;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace MyApp.Api
{

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TrippinApi : ApiBase
    {
        private static readonly List<Person> people = new()
        {
            new Person { Id = 1, Name = "Alice" },
            new Person { Id = 2, Name = "Bob" },
        };

        public TrippinApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }

        [Resource]
        public IQueryable<Person> People => people.AsQueryable();
    }

}
```

### 2. Create an initial model builder

Since there is no database context to derive the model from, you need a custom model builder:

```cs
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core.Model;

namespace MyApp.Api
{

    internal class InMemoryModelBuilder : IModelBuilder
    {
        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntityType<Person>();
            return builder.GetEdmModel();
        }
    }

}
```

### 3. Configure the OData endpoint

In `Program.cs`:

```cs
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core.Model;
using MyApp.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddRestier(options =>
    {
        options.Select().Expand().Filter().OrderBy().SetMaxTop(100).Count();

        options.AddRestierRoute<TrippinApi>("api/Trippin", routeServices =>
        {
            routeServices.AddChainedService<IModelBuilder>((sp, inner) =>
                new InMemoryModelBuilder { Inner = inner });
        });
    });

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
```
```

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/extending-restier/in-memory-provider.md
git commit -m "docs: update in-memory-provider.md to ASP.NET Core patterns"
```

---

### Task 13: Update `extending-restier/temporal-types.md`

**Files:**
- Modify: `docs/msdocs/extending-restier/temporal-types.md`

- [ ] **Step 1: Update the namespace reference in temporal-types.md**

The only change needed is updating the first line to reference both EF providers:

Replace:
```
When using the Microsoft.Restier.Providers.EntityFramework provider, temporal types are now supported.
```

With:
```
When using the Entity Framework providers (`Microsoft.Restier.EntityFrameworkCore` or `Microsoft.Restier.EntityFramework`), temporal types are supported.
```

The rest of the content (type mapping table, code examples) is about Entity Framework data annotations and EDM types, which is still accurate.

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/extending-restier/temporal-types.md
git commit -m "docs: update temporal-types.md namespace references"
```

---

### Task 14: Update `contribution-guidelines.md`

**Files:**
- Modify: `docs/msdocs/contribution-guidelines.md`

- [ ] **Step 1: Update tools and test references**

Key changes:
- Replace Visual Studio 2015 with Visual Studio 2022
- Remove Atom/MarkdownPad references
- Update test specification section: xUnit v3, FluentAssertions (AwesomeAssertions), NSubstitute
- Fix project naming convention (it's `X -> X.Tests`, not `X -> X.Tests`)
- Update rebase instructions to use `main` instead of `master`

```markdown
# How Can I Contribute?

There are many ways for you to contribute to RESTier. The easiest way is to participate in discussion of
features and issues. You can also contribute by sending pull requests of features or bug fixes to us.

## Discussion

You can participate in discussions and ask questions about RESTier at our
[GitHub issues](https://github.com/OData/RESTier/issues).

## Bug Reports

When reporting a bug at the issue tracker, fill the template of issue. Issues related to other libraries
should not be reported in the RESTier issue tracker but in the appropriate library's tracker.

## Pull Requests

**Pull request is the only way we accept code and document contributions.** Pull requests for features
and bug fixes are both welcomed. Refer to this [link](https://help.github.com/articles/using-pull-requests/)
to learn details about pull requests. Before you send a pull request, make sure you've followed the steps
listed below.

### Pick an issue to work on

You should either create or pick an issue on the [issue tracker](https://github.com/OData/RESTier/issues)
before you work on the pull request.

### Prepare Tools

Visual Studio 2022 or later is recommended for code contribution. VS Code and JetBrains Rider also work well.

### Steps to create a pull request

1. Create a forked repository of [https://github.com/OData/RESTier.git](https://github.com/OData/RESTier.git)
2. Clone the forked repository into your local environment
3. Add a git remote to upstream: `git remote add upstream https://github.com/OData/RESTier.git`
4. Make code changes and add test cases (refer to the Test specification section)
5. Build and test: `dotnet build RESTier.slnx && dotnet test RESTier.slnx`
6. Commit changed code to local repository with a clear message
7. Rebase on upstream: `git pull --rebase upstream main` and resolve conflicts if any
8. Push local commits to the forked repository
9. Create a pull request from the forked repository comparing with upstream

### Test specification

All tests must be written with **xUnit v3** and use **FluentAssertions** for assertions. **NSubstitute** is
used for mocking. Here are the rules for organizing test code:

- **Project name correspondence** (`X` -> `X.Tests`). For instance, all test code for the `Microsoft.Restier.Core` project should be in `Microsoft.Restier.Tests.Core`.
- **Path and file name correspondence** (`X/Y/Z/A.cs` -> `X.Tests/Y/Z/ATests.cs`).
- **Namespace correspondence** — the namespace must follow the folder path (e.g., `Microsoft.Restier.Tests.Core.Convention`).
- **Utility classes** can be placed at the same level as their consumer. File names must **NOT** end with `Tests`.
- **Integration and scenario tests** go in `X.Tests/IntegrationTests` and `X.Tests/ScenarioTests`.
```

- [ ] **Step 2: Commit**

```bash
git add docs/msdocs/contribution-guidelines.md
git commit -m "docs: update contribution-guidelines.md with current tools and test conventions"
```

---

### Task 15: Final review pass

- [ ] **Step 1: Verify all internal links work**

Check that all cross-references between docs are valid:
- `getting-started.md` links to server/*.md pages
- `index.md` links to getting-started.md and contribution-guidelines.md
- `method-authorization.md` internal anchor links
- No remaining links to deleted files (clients/*, license.md, extending-restier/additional-operations.md)

Grep for broken references:

```bash
grep -r "additional-operations" docs/msdocs/
grep -r "clients/" docs/msdocs/
grep -r "license.md" docs/msdocs/
grep -r "ConfigureApi" docs/msdocs/
grep -r "WebApiConfig" docs/msdocs/
grep -r "MapRestierRoute" docs/msdocs/
grep -r "Providers.EntityFramework" docs/msdocs/
grep -r "Provider.EntityFramework" docs/msdocs/
grep -r "GlobalConfiguration" docs/msdocs/
grep -r "TODO" docs/msdocs/
grep -r "NEEDS CLARIFICATION" docs/msdocs/
grep -r "Coming Soon" docs/msdocs/
```

All of these should return zero results. If any are found, fix them in the appropriate file.

- [ ] **Step 2: Build the docs locally to verify**

```bash
cd docs/msdocs && bash build.sh
```

Verify no build errors.

- [ ] **Step 3: Final commit if any fixes were needed**

```bash
git add docs/msdocs/
git commit -m "docs: fix broken links and remaining outdated references"
```
