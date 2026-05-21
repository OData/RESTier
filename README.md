<div align="center">
<h1>Microsoft Restier - OData Made Simple</h1>

[Releases](https://github.com/OData/RESTier/releases)&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;Documentation&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;[OData v4.01 Documentation](https://www.odata.org/documentation/)

[![Build Status][devops-build-img]][devops-build] [![Release Status][devops-release-img]][devops-release] <br />
[![Code of Conduct][code-of-conduct-img]][code-of-conduct] [![Twitter][twitter-img]][twitter-intent]

</div>

## What is Restier?

Restier is an API development framework for building standardized, OData V4 based RESTful services on .NET. 

Restier is the spiritual successor to [WCF Data Services](https://en.wikipedia.org/wiki/WCF_Data_Services). Instead of 
generating endless boilerplate code with the current Web API + OData toolchain, RESTier helps you boostrap a standardized, 
queryable HTTP-based REST interface in literally minutes. And that's just the beginning.

Like WCF Data Services before it, Restier provides simple and straightforward ways to shape queries and intercept submissions
_before_ and _after_ they hit the database. And like Web API + OData, you still have the flexibility to add your own
custom queries and actions with techniques you're already familiar with.

## What is OData?

OData stands for the Open Data Protocol. OData enables the creation and consumption of RESTful APIs, which allow 
resources, defined in a data model and identified by using URLs, to be published and edited by Web clients using 
simple HTTP requests.

OData was originally designed by Microsoft to be a framework for exposing Entity Framework objects over REST services.
The first concepts shipped as "Project Astoria" in 2007. By 2009, the concept had evolved enough for Microsoft to
announce OData, along with a [larger effort](https://blogs.msdn.microsoft.com/odatateam/2009/11/17/breaking-down-data-silos-the-open-data-protocol-odata/)
to push the format as an industry standard.

Work on the current version of the protocol (V4) began in April 2012, and was ratified by OASIS as an industry standard in Feb 2014.

## What's New in 2.0 (Beta)

Restier 2.0 is a ground-up modernization of the framework — keeping the convention-based simplicity that made
the original Restier a joy to use, while embracing the current .NET stack. Highlights:

- **Modern targets:** ASP.NET Core on .NET 8, 9, and 10, on top of Microsoft.AspNetCore.OData 9.x.
- **Endpoint routing:** Routes register against the standard endpoint-routing pipeline via `AddRestierRoute` / `MapRestier`.
- **API versioning:** First-class support via `Microsoft.Restier.AspNetCore.Versioning`, with per-version EDM models.
- **OpenAPI / Swagger:** Both Swashbuckle and NSwag integrations, including combined Restier + plain-controller documents.
- **Deep operations:** Cascade insert, update, and delete across navigation properties in a single request.
- **Keyless views:** Map read-only DbContext views (`HasNoKey()`) as queryable EDM entity sets.
- **Spatial types:** Round-trip `Edm.Geography*` between Microsoft.Spatial and NetTopologySuite (EF Core) or SQL Server (EF6).
- **Multi-tenancy:** Per-tenant API instances resolved from the request, with isolated `DbContext` factories.
- **Authorization attributes:** Declarative `[RestierAuthorize]` for entity-set and operation policies.
- **AsNoTracking by default:** Read queries no longer pollute the change tracker, with opt-in `TrackingBehavior` for write paths.
- **Magical operations:** Function and action bindings receive the typed entity directly — no `(int key, ...)` boilerplate.
- **Dynamic routing:** Register Restier routes at runtime without restarting the host.
- **Deferred query materialization:** Large result sets stream without buffering the full collection in memory.
- **Conformance options:** Per-API `RestierConformanceOptions` toggles behaviors that diverge from strict OData v4.01.
- **Validation options bag:** Centralized `RestierValidationOptions` for tweaking validator behavior per API.
- **MSTest test framework:** The internal test suite uses MSTest 3.x with cross-process coverage collection on Windows CI.

Restier 2.0 ships as a beta while the new surface stabilizes — please file issues against any rough edges.

## Getting Started
1. Create an ASP.NET Classic or ASP.NET Core web project.
2. Add the corresponding Restier package for the flavor of ASP.NET you're targeting: `Microsoft.Restier.AspNet` or `Microsoft.Restier.AspNetCore`.
3. Add thhe corresponding Restier package for the flavor of Entity Framework you are targering: `Microsoft.Restier.EntityFramework` or `Microsoft.Restier.EntityFrameworkCore`
4. Review the [ASP.NET Classic](https://github.com/OData/RESTier/tree/main/src/Microsoft.Restier.Samples.Northwind.AspNet) or [ASP.NET Core](https://github.com/OData/RESTier/tree/main/src/Microsoft.Restier.Samples.Northwind.AspNetCore) samples to help you get started.

## Use Cases
Coming Soon!

## Supported Platforms
Restier 2.0 (Beta) targets modern .NET only and runs on the current OData v4.01 stack:
- ASP.NET Core 8.0, 9.0, and 10.0 (on Microsoft.AspNetCore.OData 9.x)
- Entity Framework 6.5 and later
- Entity Framework Core 8.0, 9.0, and 10.0
- Microsoft.OData.Core / Microsoft.OData.Edm 8.x

> The Restier 1.x line on Classic ASP.NET 5.2.7+ and earlier .NET versions remains available on
> NuGet, but is no longer actively developed. New projects should target 2.0.

## Restier Components
Restier is made up of the following components:
- **Microsoft.Restier.Core:** Convention-based interception framework — chain-of-responsibility query and submit pipelines, DI, and the `ApiBase` programming model.
- **Microsoft.Restier.AspNetCore:** ASP.NET Core integration — endpoint routing, the `RestierController`, batching, multi-tenancy, and HTTP-context plumbing.
- **Microsoft.Restier.EntityFramework:** Entity Framework 6.x provider — translates intercepted queries to a `DbContext`.
- **Microsoft.Restier.EntityFrameworkCore:** Entity Framework Core 8.x+ provider — same surface, modern stack.
- **Microsoft.Restier.AspNetCore.Swagger:** Swagger / OpenAPI generation via Swashbuckle.
- **Microsoft.Restier.AspNetCore.NSwag:** NSwag-based OpenAPI generation, including combined Restier + plain-controller documents.
- **Microsoft.Restier.AspNetCore.Versioning:** API versioning via Asp.Versioning, with per-version EDM models.
- **Microsoft.Restier.EntityFramework.Spatial / Microsoft.Restier.EntityFrameworkCore.Spatial:** Spatial-type conversion between Microsoft.Spatial and NetTopologySuite or SQL Server geography.
- **Microsoft.Restier.Breakdance:** In-memory test framework — exercise Restier APIs end-to-end without spinning up a real host.

## Ecosystem
Restier is used in solutions from:
- [BurnRate.io](https://burnrate.io)
- [CloudNimble](https://nimbleapps.cloud)
- [Florida Agency for Health Care Administration](https://ahca.myflorida.com)
- [Microsoft](https://graph.microsoft.com)
- [Miller's Ale House](https://millersalehouse.com)
- [NoCore](https://nocore.nl)

## Community
After a couple years in statis, Restier is in active development once again. The project is lead by Robert McLaws and Mike Pizzo.

### Contributing
If you'd like to help out with the project, our Contributor's Handbook is also located in the Wiki.

### Reporting Security Issues

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) <secure@microsoft.com>. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://www.microsoft.com/msrc/faqs-report-an-issue). You can also find these instructions in this repo's [SECURITY.md](./SECURITY.md).

## Contributors

Special thanks to everyone involved in making Restier the best API development platform for .NET. The following people
have made various contributions to the codebase:

| Microsoft     | External         |
|---------------|------------------|
| Lewis Cheng   | James Caldwell   |
| Challen H     | Angel Garay      |
| Eric Erhardt  | Cengiz Ilerler   |
| Vincent He    | Kemal M          |
| Dong Liu      | Mateusz Malicki  |
| Layla Liu     | Robert McLaws    |
| Fan Ouyang    | Micah Rairdon    |
| Mike Pizzo    | Jan-Willem Spuij |
| Congyong S    | Chris Woodruff   |
| Mark Stafford |                  |
| Ray Yao       |                  |

## 

<!--
Link References
-->

[devops-build]:https://dev.azure.com/dotnet/OData/_build?definitionId=89
[devops-release]:https://dev.azure.com/dotnet/odata/_release?view=all&definitionId=2
[twitter-intent]:https://twitter.com/intent/tweet?url=https%3A%2F%2Fgithub.com%2FOData%2FRESTier&via=robertmclaws&text=Check%20out%20Restier%21%20It%27s%20the%20simple%2C%20queryable%20framework%20for%20building%20data-driven%20APIs%20in%20.NET%21&hashtags=odata
[code-of-conduct]:https://opensource.microsoft.com/codeofconduct/

[devops-build-img]:https://img.shields.io/azure-devops/build/dotnet/odata/89.svg?style=for-the-badge&logo=azuredevops
[devops-release-img]:https://img.shields.io/azure-devops/release/dotnet/f69f4a5b-2486-494e-ad83-7ba2b889f752/2/2.svg?style=for-the-badge&logo=azuredevops
[nightly-feed-img]:https://img.shields.io/badge/continuous%20integration-feed-0495dc.svg?style=for-the-badge&logo=nuget&logoColor=fff
[github-version-img]:https://img.shields.io/github/release/ryanoasis/nerd-fonts.svg?style=for-the-badge
[gitter-img]:https://img.shields.io/gitter/room/nwjs/nw.js.svg?style=for-the-badge
[code-climate-img]:https://img.shields.io/codeclimate/issues/github/ryanoasis/nerd-fonts.svg?style=for-the-badge
[code-of-conduct-img]: https://img.shields.io/badge/code%20of-conduct-00a1f1.svg?style=for-the-badge&logo=windows
[twitter-img]:https://img.shields.io/badge/share-on%20twitter-55acee.svg?style=for-the-badge&logo=twitter
