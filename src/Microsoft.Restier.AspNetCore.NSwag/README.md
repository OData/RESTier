# Microsoft Restier - OData Made Simple

[Releases](https://github.com/OData/RESTier/releases)&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;Documentation&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;[OData v4.01 Documentation](https://www.odata.org/documentation/)

## NSwag for Restier ASP.NET Core

This package helps you quickly implement OpenAPI with [NSwag](https://github.com/RicoSuter/NSwag),
[ReDoc](https://redocly.com/redoc), and [Swagger UI 3](https://swagger.io/tools/swagger-ui/) in your
Restier service in just a few lines of code. It is the recommended OpenAPI integration for new
projects.

For the Swashbuckle-based alternative, see
[Microsoft.Restier.AspNetCore.Swagger](https://www.nuget.org/packages/Microsoft.Restier.AspNetCore.Swagger).

## Supported Platforms

ASP.NET Core 8.0, 9.0, and 10.0 via Endpoint Routing.

## Getting Started

### Step 1: Install [Microsoft.Restier.AspNetCore.NSwag](https://www.nuget.org/packages/Microsoft.Restier.AspNetCore.NSwag)

Add the package to your API project.

### Step 2: Register services

```csharp
builder.Services.AddRestierNSwag();
```

There is an overload that takes an `Action<OpenApiConvertSettings>` for customizing the generated
OpenAPI document.

### Step 3: Add middleware

```csharp
app.UseRestierOpenApi();   // /openapi/{name}/openapi.json
app.UseRestierReDoc();     // /redoc/{name}
app.UseRestierNSwagUI();   // /swagger (Swagger UI 3 with a route dropdown)
```

### Step 4: Combine with plain MVC controllers (optional)

```csharp
builder.Services.AddOpenApiDocument(c => c.DocumentName = "controllers");

// in the pipeline:
app.UseOpenApi();
app.UseReDoc(c =>
{
    c.Path = "/redoc/controllers";
    c.DocumentPath = "/swagger/controllers/swagger.json";
});
```

`AddRestierNSwag()` hides `RestierController` from ApiExplorer automatically, so it will not appear
in your controllers document.

### Step 5: Browse

- `/redoc/{routeName}` — ReDoc for one Restier route
- `/swagger` — Swagger UI 3 with all Restier routes
- `/openapi/{routeName}/openapi.json` — Raw OpenAPI 3.0 JSON

## Reporting Security Issues

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response
Center (MSRC) <secure@microsoft.com>. You should receive a response within 24 hours. If for some
reason you do not, please follow up via email to ensure we received your original message. Further
information, including the MSRC PGP key, can be found in the
[Security TechCenter](https://www.microsoft.com/msrc/faqs-report-an-issue). You can also find these
instructions in this repo's [SECURITY.md](https://github.com/OData/RESTier/blob/main/SECURITY.md).

## Contributors

Special thanks to everyone involved in making Restier the best API development platform for .NET.
The following people have made various contributions to this package:

| External         |
|------------------|
| Jan-Willem Spuij |
