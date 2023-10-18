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

## Getting Started
1. Create an ASP.NET Classic or ASP.NET Core web project.
2. Add the corresponding Restier package for the flavor of ASP.NET you're targeting: `Microsoft.Restier.AspNet` or `Microsoft.Restier.AspNetCore`.
3. Add thhe corresponding Restier package for the flavor of Entity Framework you are targering: `Microsoft.Restier.EntityFramework` or `Microsoft.Restier.EntityFrameworkCore`
4. Review the [ASP.NET Classic](https://github.com/OData/RESTier/tree/main/src/Microsoft.Restier.Samples.Northwind.AspNet) or [ASP.NET Core](https://github.com/OData/RESTier/tree/main/src/Microsoft.Restier.Samples.Northwind.AspNetCore) samples to help you get started.

## Use Cases
Coming Soon!

## Supported Platforms
Restier 1.0 currently supports the following platforms:
- Classic ASP.NET 5.2.7 and later
- ASP.NET Core 6.0, 7.0, and 8.0 RC2 (Binaries targeting deprecated versions of .NET are still available on NuGet.org)
- Entity Framework 6.4 and later
- Entity Framework Core 6.0 and later

## Restier Components
Restier is made up of the following components:
- **Microsoft.Restier.AspNet & Microsoft.Restier.AspNetCore:** Plugs into the OData/WebApi processing pipeline and provides query interception capabilities.
- **Microsoft.Restier.Core:** The base library that contains the core convention-based interception framework.
- **Microsoft.Restier.EntityFramework & Microsoft.Restier.EntityFramework:** Translates intercepted queries down to the database level to be executed.
- **Microsoft.Restier.Breakdance:** Unit test Restier services and components in-memory without spnning up a separate IIS instance, as well as verify the availability of your custom convention-based interceptors.

## Ecosystem
Restier is used in solutions from:
- [BurnRate.io](https://burnrate.io)
- [CloudNimble, Inc.](https://nimbleapps.cloud)
- [Florida Agency for Health Care Administration](https://ahca.myflorida.com)
- [Miller's Ale House](https://millersalehouse.com)

## Community
After a couple years in statis, Restier is in active development once again. The project is lead by Robert McLaws and Mike Pizzo.

### Weekly Standups
The core development team meets twice a month on Microsoft Teams to discuss pressing items and work through the issues list. A history of
those meetings can be found in the Wiki.

### Contributing
If you'd like to help out with the project, our Contributor's Handbook is also located in the Wiki.

### Reporting Security Issues

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) <secure@microsoft.com>. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://www.microsoft.com/msrc/faqs-report-an-issue). You can also find these instructions in this repo's [SECURITY.md](./SECURITY.md).

## Contributors

Special thanks to everyone involved in making RESTier the best API development platform for .NET. The following people
have made various contributions to the codebase:

| Microsoft     | External         |
|---------------|------------------|
| Lewis Cheng   | Cengiz Ilerler   |
| Challenh      | Kemal M          |
| Eric Erhardt  | Robert McLaws    |
| Vincent He    | Jan-Willem Spuij |
| Dong Liu      | Chris Woodruff   |
| Layla Liu     | James Caldwell   |
| Fan Ouyang    | Angel Garay      |
| Mike Pizzo    |                  |
| Congyong S    |                  |
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
