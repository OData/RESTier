<div align="center">
<h1>Microsoft Restier - OData Made Simple</h1>

[Releases](https://github.com/OData/RESTier/releases)&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;Documentation&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;[OData v4.01 Documentation](https://www.odata.org/documentation/)

[![Build Status][devops-build-img]][devops-build] [![Release Status][devops-release-img]][devops-release] [![Nightly Feed][nightly-feed-img]][nightly-feed] <br />
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
Now that the project has restarted, we have a new location for our [Continuous Integration builds][nightly-feed]. We've simplified the NuGet
packages as well, so now you can just reference `Microsoft.Restier.AspNet` or `Microsoft.Restier.AspNetCore` (coming soon) packages, and we'll take care of
the rest. 

## Use Cases
Coming Soon!

## Supported Platforms
Restier 1.0 currently ships with support for Classic ASP.NET 5.2.3 and later. Support for ASP.NET Core 2.2 is coming in the first half of 2019. (More specifics will be provided in a few weeks.)

## Restier Components
The Classic ASP.NET flavor of Restier is made up of the following components:
- **Microsoft.Restier.AspNet:** Plugs into the OData/WebApi processing pipeline and provides query interception capabilities.
- **Microsoft.Restier.Core:** The base library that contains the core convention-based interception framework.
- **Microsoft.Restier.EntityFramework:** Translates intercepted queries down to the database level to be executed.

While the ASP.NET Core flavor of Restier (when is ships) will consist of the following:
- **Microsoft.Restier.AspNetCore:** Plugs into the OData/WebApi processing pipeline and provides query interception capabilities.
- **Microsoft.Restier.Core:** The base library that contains the core convention-based interception framework.
- **Microsoft.Restier.EntityFrameworkCore:** Translates intercepted queries down to the database level to be executed.

## Ecosystem
Restier is used in solutions from:
- [BurnRate.io](https://burnrate.io)
- [CloudNimble, Inc.](https://nimbleapps.cloud)
- [Florida Agency for Health Care Administration](https://ahca.myflorida.com)

There is also a growing set of tools to support Restier-based development
- [Breakdance.Restier](https://github.com/cloudnimble/breakdance): Convention-based name troubleshooting and integration test support.
## Community
After a couple years in statis, Restier is in active development once again. The project is lead by Robert McLaws and Chris Woodruff.

### Weekly Standups
The core development team meets once a week on Google Hangouts to discuss pressing items and work through the issues list. A history of
those meetings can be found in the Wiki.

### Contributing
If you'd like to help out with the project, our Contributor's Handbook is also located in the Wiki.

## Contributors

Special thanks to everyone involved in making RESTier the best API development platform for .NET. The following people
have made various contributions to the codebase:

| Microsoft     | External       |
|---------------|----------------|
| Lewis Cheng   | Cengiz Ilerler |
| Challenh      | Kemal M        |
| Eric Erhardt  | Robert McLaws  |
| Vincent He    |                |
| Dong Liu      |                |
| Layla Liu     |                |
| Fan Ouyang    |                |
| Congyong S    |                |
| Mark Stafford |                |
| Ray Yao       |                |

## 

<!--
Link References
-->

[devops-build]:https://dev.azure.com/dotnet/OData/_build?definitionId=89
[devops-release]:https://dev.azure.com/cloudnimble/Restier/_release?view=all&definitionId=1
[nightly-feed]:https://www.myget.org/F/restier-nightly/api/v3/index.json
[twitter-intent]:https://twitter.com/intent/tweet?url=https%3A%2F%2Fgithub.com%2FOData%2FRESTier&via=robertmclaws&text=Check%20out%20Restier%21%20It%27s%20the%20simple%2C%20queryable%20framework%20for%20building%20data-driven%20APIs%20in%20.NET%21&hashtags=odata
[code-of-conduct]:https://opensource.microsoft.com/codeofconduct/

[devops-build-img]:https://img.shields.io/azure-devops/build/dotnet/odata/89.svg?style=for-the-badge&logo=azuredevops
[devops-release-img]:https://img.shields.io/azure-devops/release/cloudnimble/d3aaa016-9aea-4903-b6a6-abda1d4c84f0/1/1.svg?style=for-the-badge&logo=azuredevops
[nightly-feed-img]:https://img.shields.io/badge/continuous%20integration-feed-0495dc.svg?style=for-the-badge&logo=nuget&logoColor=fff
[github-version-img]:https://img.shields.io/github/release/ryanoasis/nerd-fonts.svg?style=for-the-badge
[gitter-img]:https://img.shields.io/gitter/room/nwjs/nw.js.svg?style=for-the-badge
[code-climate-img]:https://img.shields.io/codeclimate/issues/github/ryanoasis/nerd-fonts.svg?style=for-the-badge
[code-of-conduct-img]: https://img.shields.io/badge/code%20of-conduct-00a1f1.svg?style=for-the-badge&logo=windows
[twitter-img]:https://img.shields.io/badge/share-on%20twitter-55acee.svg?style=for-the-badge&logo=twitter
