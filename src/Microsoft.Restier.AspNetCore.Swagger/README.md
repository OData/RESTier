# Microsoft Restier - OData Made Simple

[Releases](https://github.com/OData/RESTier/releases)&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;Documentation&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;[OData v4.01 Documentation](https://www.odata.org/documentation/)

<!--[![Build Status][devops-build-img]][devops-build] [![Release Status][devops-release-img]][devops-release] <br />
[![Code of Conduct][code-of-conduct-img]][code-of-conduct] [![Twitter][twitter-img]][twitter-intent]-->

## Swagger for Restier ASP.NET Core

This package helps you quickly implement OpenAPI's Swagger.json and Swagger UI in your Restier service in just a few lines of code.

## Supported Platforms
[Microsoft.Restier.AspNetCore.Swagger](https://www.nuget.org/packages/Microsoft.Restier.AspNetCore.Swagger) 1.1 currently supports the following platforms:
- ASP.NET Core 6.0, 7.0, and 8.0 via Endpoint Routing

## Getting Started
Building OpenAPI into your Restier project is easy!

### Step 1: Install [Microsoft.Restier.AspNetCore.Swagger](https://www.nuget.org/packages/Microsoft.Restier.AspNetCore.Swagger/1.1.0-CI-20231125-225528)
- Add the package above to your API project.
- **[Optional]** Change the version of your Restier packages to `1.*-*` so you always get the latest version.

### Step 2: Convert to Endpoint Routing (Part 1)
- Add the new parameter to the end of your `services.AddRestier()` call:
```cs
    services.AddRestier((builder) =>
    {
        ...
    }, true); // <-- @robertmclaws: This parameter adds Endpoint Routing support.
```

### Step 3: Register Swagger Services
- Add the line below immediately after the `services.AddRestier()` call:
```cs
services.AddRestierSwagger();
```
- There is an overload to this method that takes an `Action<OpenApiConvertSettings>` that will let you change the configuration of the generated Swagger definition.

### Step 4: Convert to Endpoint Routing & Use Swagger (Part 2)
- Replace your existing `Configure` code with the following (don't forget your customizations):
```cs
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRestierBatching();
        app.UseRouting();

        app.UseAuthorization();
        // @robertmclaws: This line is optional but helps with leveraging ClaimsPrincipal.Current for cross-platform business logic.
        app.UseClaimsPrincipals();

        app.UseEndpoints(endpoints =>
        {
            endpoints.Select().Expand().Filter().OrderBy().MaxTop(100).Count().SetTimeZoneInfo(TimeZoneInfo.Utc);
            endpoints.MapRestier(builder =>
            {
                builder.MapApiRoute<YOURAPITYPEHERE>("ROUTENAME", "ROUTEPREFIX", true);
            });
        });

        app.UseRestierSwagger(true);
    }
```
- On the last line, the boolean specifies whether or not to use SwaggerUI. If you want to control more of the UI configuration, use `services.Configure<SwaggerUIOptions>();` in your `ConfigureServices()` method.

### Step 5: Browse Swagger Resources
- Browse to `/swagger/ROUTENAME/swagger.json` to see the generated Swagger definition.
- Browse to `/swagger` to see the Swagger UI.

## Reporting Security Issues

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) <secure@microsoft.com>. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://www.microsoft.com/msrc/faqs-report-an-issue). You can also find these instructions in this repo's [SECURITY.md](./SECURITY.md).

## Contributors

Special thanks to everyone involved in making Restier the best API development platform for .NET. The following people have made various contributions to this package:

| External         |
|------------------|
| Cengiz Ilerler   |
| Robert McLaws    |
| Micah Rairdon    |

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
