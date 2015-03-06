---
layout: post
title: "Getting started with RESTier -- Part 1"
description: ""
category: Getting Started
---


RESTier is a new RESTful API development framework for building standardized OData V4 services with rich domain logic. This tutorial shows how to create a basic OData V4 endpoint using RESTier in a few minutes. Then in [part 2](http://odata.github.io/RESTier/Getting-Started-Part-2/), we will show how to add advanced OData features and rich domain logic on top of this sample service. We will be using [Northwind](http://msdn.microsoft.com/en-us/library/8b6y4c7s.aspx) as the sample database and [Entity Framework](http://msdn.microsoft.com/en-us/data/ef.aspx) as the data proxy.

#### Create the Visual Studio Project
In [Visual Studio](http://msdn.microsoft.com/en-us/vstudio/aa718325.aspx), from the File menu, select _New > Project_.
Expand _Installed > Templates > Visual C# > Web_, and select the _ASP.NET Web Application_ template. Name the project “NorthwindSample”.

<img src="https://raw.githubusercontent.com/wiki/OData/restier/images/Nothwind1.png" width="600px" height="350px" align="center"  />

In the _New Project_ dialog, select _Empty_ template. Under “Add folders and core references…” click _Web API_. Click _OK_.

<img src="https://raw.githubusercontent.com/wiki/OData/restier/images/Nothwind2.png" width="600px" height="400px" align="center"  />

#### Install the RESTier Packages

To install [RESTier 0.1.0-pre](http://www.nuget.org/packages/Microsoft.Restier/0.1.0-pre), run the following command in the Package Manager Console
```
PM> Install-Package Microsoft.Restier -Pre 
```

#### Generate the model class
The current version of RESTier only supports [Entity Framework](http://msdn.microsoft.com/en-us/data/ef.aspx) (EF) as the Data Provider. You can choose various ways to generate the model classes, including Code First and others supported by Entity Framework.

For this sample, we will use [Entity Framework 6 Tools for Visual Studio](http://www.microsoft.com/en-in/download/details.aspx?id=40762) to automatically generate the model class from the database file. To get started, [download and install the tools](http://www.microsoft.com/en-in/download/details.aspx?id=40762).

First you need to [import the Northwind database file](http://msdn.microsoft.com/en-us/library/8b6y4c7s.aspx). In case that sometimes it's hard to import the Northwind, you can alternatively just add the [Northwind.mdf](https://github.com/OData/RESTier/blob/master/assets/data/Northwind.mdf) to the `App_Data` folder.

Next, in Solution Explorer, right-click the `App_Data` folder. From the context menu, click _Add > Existing Item…_ and navigate to the Northwind database file you downloaded.

<img src="https://raw.githubusercontent.com/wiki/OData/restier/images/Nothwind3.png" width="400px" height="300px" align="center"  />

In Solution Explorer, right-click the `Models` folder. From the context menu, click _Add > ADO.NET Entity Data Model_, then input the Item name “NothwindContext” and click _OK_.
 
Then in the Entity Data Model Wizard, click _Code First from database_ and click _Next_. Then choose `Northwind.mdf` in _Which data connection should your application to connect to the database_ and click _Next_. 

<img src="https://raw.githubusercontent.com/wiki/OData/restier/images/Nothwind4.png" width="500px" height="400px" align="center"  />

Choose "Tables" in the Entity Data Model Wizard and then click _Finish_. 

<img src="https://raw.githubusercontent.com/wiki/OData/restier/images/Nothwind5.png" width="400px" height="320px" align="center"  />

<img src="https://raw.githubusercontent.com/wiki/OData/restier/images/Nothwind6.png" width="400px" height="350px" align="center"  />

Now, when you check the Models folder in Solution Explorer, you can see all model classes are automatically generated.

<img src="https://raw.githubusercontent.com/wiki/OData/restier/images/Nothwind7.png" width="400px" height="500px" align="center"  />

#### Create the Controller
In Solution Explorer, right-click the Models folder. From the context menu, click _Add > Class_, name the class `NorthwindDomain`. In the `NorthwindDomain.cs`, replace the code with the following.

```csharp
using Microsoft.Restier.EntityFramework;

public class NorthwindDomain : DbDomain<NorthwindContext>
{
    public NorthwindContext Context 
    { 
	    get { return DbContext; } 
	}
}
```
Then in Solution Explorer, right-click the Controllers folder. From the context menu, click _Add > Controller_, name the controller `NorthwindController`. In the `NorthwindController.cs`, replace the boilerplate code with the following.

```csharp
using Microsoft.Restier.WebApi;

public class NorthwindController : ODataDomainController<NorthwindDomain>
{
    private NorthwindContext DbContext
    {
        get { return Domain.Context;}
    }
}

```

#### Configure the OData Endpoint
Open the file `App_Start/WebApiConfig.cs`. Replace the boilerplate code with the following. 

```csharp
using Microsoft.Restier.WebApi;
using Microsoft.Restier.WebApi.Batch;

public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        config.MapHttpAttributeRoutes();
        RegisterNorthwind(config, GlobalConfiguration.DefaultServer);
        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional }
        );
    }
 
    public static async void RegisterNorthwind(HttpConfiguration config, HttpServer server)
    {
        await config.MapODataDomainRoute<NorthwindController>(
           "NorthwindApi", "api/Northwind",
            new ODataDomainBatchHandler(server));
    }
}

```

After these steps, you will have finished bootstrapping an OData service endpoint. You can then *Run* the project and an OData service is started. Then you can start by accessing the URL `http://localhost:<ISS Express port>/api/Northwind` to view all available entity sets, and try with other basic OData CRUD operations. For instance, you may try querying any of the entity sets using the `$select`, `$filter`, `$orderby`, `$top` or `$skip` query string parameters.
