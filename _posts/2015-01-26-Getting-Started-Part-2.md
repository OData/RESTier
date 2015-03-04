---
layout: post
title: "Getting started with RESTier -- Part 2"
description: ""
category: Getting Started
---

This is the second part of a tutorial for a RESTier sample service based on the [Northwind database](http://msdn.microsoft.com/en-us/library/8b6y4c7s.aspx). Here we introduce rich domain logic while bridging the feature gap between [Web API OData](http://www.asp.net/web-api/overview/odata-support-in-aspnet-web-api/odata-v4/create-an-odata-v4-endpoint) and the [previous tutorial](http://odata.github.io/RESTier/Getting-Started-Part-1/).

## Fallback to Web API OData
RESTier is currently a preview release, so there remain features that are not yet natively supported. For example, CUD (Create, Update, Delete) operations are only supported for top-level entities. Given these limitations, we provide ways to fallback to Web API OData to fill these feature gaps. In doing so, we also demonstrate the core methods for extending RESTier in the future.

### Attribute Routing
Attribute Routing allows [an entity's property to be accessed directly via a unique URL](http://docs.oasis-open.org/odata/odata/v4.0/errata02/os/complete/part2-url-conventions/odata-v4.0-errata02-os-part2-url-conventions-complete.html#_Toc406398085), so that the value can be directly retrieved or updated. While Attribute Routing isn't yet supported by RESTier, you can fall back to Web API OData in order to add this functionality. To do so, add the code below to `NorthwindController.cs`. 

```csharp
using Microsoft.Restier.WebApi;
using RESTierDemo.Models;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;


// Attribute routing to enable query on property and property value
[ODataRoute("Customers({key})/CompanyName")]
[ODataRoute("Customers({key})/CompanyName/$value")]
public string GetCustomerCompanyName([FromODataUri]string key)
{
    return DbContext.Customers.Where(c => c.CustomerID == key).Select(c => c.CompanyName).FirstOrDefault();
}

// Attribute routing to enable UPDATE on property
[HttpPut]
[ODataRoute("Products({key})/UnitPrice")]
public IHttpActionResult UpdateProductUnitPrice(int key, [FromBody]decimal price)
{
    var entity = DbContext.Products.Find(key);
    if (entity == null)
    {
        return NotFound();
    }
    entity.UnitPrice = price;

    try
    {
        DbContext.SaveChanges();
    }
    catch (DbUpdateConcurrencyException)
    {
        if (!DbContext.Products.Any(p => p.ProductID == key))
        {
            return NotFound();
        }
        else
        {
            throw;
        }
    }
    return Ok(price);
}
```

**Enable Entity Counts**

Entity counts are not currently supported by the `ODataDomainController`, but you can easily implement them yourself. The example below asynchronously queries the database for the count.

```csharp
// Attribute routing to enable $count
[ODataRoute("Products/$count")]
public async Task<IHttpActionResult> GetProductsCount()
{
    return Ok(await DbContext.Products.CountAsync());
}
```

## Enable Rich Domain Logic

One important feature for RESTier is that it can easily enable rich domain logic based on conventions in RESTier. This allows, for instance, for custom business logic to be executed when an entity is being created, updated, or deleted. Be aware that the domain logic is based on predefined conventions; to enable these for imperative views, entity set filters, and submit logic, you will need to add the `[EnableConventions]` attribute to your `ODataDomainController` class.

### Imperative Views
Imperative Views allow custom views to be accessed which are not supported directly by existing entities. They may, for example, join multiple entities together, or provide a particular filter of a particular entity. With the convention-based module, you define these using `protected` properties that return `IQueryable<T>`. For the current iteration, `T` must be an existing entity type. Imperative views are also currently read-only. For example, by adding the code below to `NorthwindDomain.cs`, you can easily add an entity set named `CurrentOrders` in both the metadata and entity set containers.

```csharp
using System.Linq;
using Microsoft.Restier.Conventions;
using Microsoft.Restier.Core;
using Microsoft.Restier.EntityFramework;

[EnableConventions]
public class NorthwindDomain : DbDomain<NorthwindContext>
{
	// Other code
	
	// Imperative views
	protected IQueryable<Order> CurrentOrders
	{
	    get
	    {
	        return this.Source<Order>("Orders").Where(o => o.ShippedDate == null);
	    }
	}
	// Other code
}
```

After adding the `[EnableConventions]` attribute and the code for the `CurrentOrders` imperative view, you will notice that `http://localhost:<ISS Express port>/api/Northwind/$metadata` now contains:

```xml
<EntityContainer>
......
	<EntitySet Name="CurrentOrders" EntityType="Microsoft.Data.Domain.Samples.Northwind.Models.Order" />
</EntityContainer>
```

Further, you can now query `CurrentOrders` via `http://localhost:31181/api/Northwind/CurrentOrders` and get:

```json
{
    "@odata.context": "http://localhost:31181/api/Northwind/$metadata#CurrentOrders",
    "value": [
        {
            "OrderID": 11008,
            "CustomerID": "ERNSH",
            "EmployeeID": 7,
            "ShipVia": 3,
            "Freight": 79.46,
            "ShipName": "Ernst Handel",
            "ShipAddress": "Kirchgasse 6",
            "ShipCity": "Graz",
            "ShipRegion": null,
            "ShipPostalCode": "8010",
            "ShipCountry": "Austria"
        },
        ...
    ]
}
```

### Entity Set Filters
Entity Set Filters allow existing entity sets to be pre-filtered so only a subset of entities are returned by default. With the convention-based module, Entity Set Filters are specified by creating an `OnFilterEntitySet` method which accepts an `IQueryable<T>` argument and returns an `IQueryable<T>`, where `T` is the type of an entity in the entity set. By adding the code below to `NorthwindDomain.cs`, all queries to `Customers` will only return customers from France.

```csharp
private IQueryable<Customer> OnFilterCustomers(IQueryable<Customer> customers)
{
    return customers.Where(c => c.Country == "France");
}
```

_**Enabling Filtered Entity Counts**_

Because Entity Counts are not handled internally, the aforementioned `Count` method will not be passed through any filters you create. To correct this, you must modify your `Count` method to explicitly call the filter method. For example:

```csharp
// Attribute routing to enable $count
[ODataRoute("Customers/$count")]
public async Task<IHttpActionResult> GetCustomersCount()
{
    return Ok(await OnFilterCustomers(DbContext.Customers).CountAsync());
}
```

### Submit Logic
Using the same conventions, RESTier can provide custom business logic when entities are submitted. You can do this to all entity set with all operations (`Updated`, `Inserted`, `Deleted`; `Updating`, `Inserting`, `Deleting`) as long as you follow the naming convention `On[Operation][EntitySet]`. For example, by adding the code below to `NorthwindDomain.cs`, you can define what is to be done when updating a `Product`: 


```csharp
private void OnUpdatingProducts(Product product)
{
    // Logic when updating a product
}
 
private void OnInsertedProducts(Product product)
{
    // Logic after create a new product
}
```

### Role-based Security
By default, RESTier provides full read and write access to all entity sets. For many scenarios this won't be desirable. To address this, RESTier also allows role-based security. When you enable role-based security, the entire domain is locked down, and you need to grant _explicit permissions_ to provide access. For each grant, you must specify a permission; you can see what those are from the `DomainPermissionType` enumeration. You can optionally specify what securable and role (an arbitrary name) the permission applies to. By adding the code below to `NorthiwindDomain.cs`, you can grant several different permissions to different entity sets. This is just an example; there are quite a few possible compositions for permissions. We will have more detailed documentation for this later, but in the meanwhile you can first try them by yourself.
```csharp
using System.Linq;
using Microsoft.Restier.Conventions;
using Microsoft.Restier.Core;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.Security;

[EnableRoleBasedSecurity]
[Grant(DomainPermissionType.All, On = "Customers")]
[Grant(DomainPermissionType.All, On = "Products")]
[Grant(DomainPermissionType.All, On = "CurrentOrders")]
[Grant(DomainPermissionType.All, On = "Orders")]
[Grant(DomainPermissionType.All, On = "Employees")]
[Grant(DomainPermissionType.Inspect, On = "Suppliers")]
[Grant(DomainPermissionType.Read, On = "Suppliers")]

public class NorthwindDomain : DbDomain<NorthwindContext>
{
   // code 
}
```
By adding the security convention above, the security roles are as follows: 

 * You can do `All` things on *Customers*, *Products*, *CurrentOrders*, *Orders* and *Employees*. Please be aware:
   * You _must_ enable the permission on `Orders` to make `CurrentOrders` operable. 
   * The `DomainPermisionType.All` enumeration includes:
     * `Inspect` (allows inspecting the model definition), 
     * `Create` (allows creation of a new entity in an entity set), 
     * `Read` (allows reading entities from an entity set), 
     * `Update` (allows updating entities in an entity set), 
     * `Delete` (allows deleting entities in an entity set), and
     * `Invoke` (allows invoking a function or action)
 * You can `Inspect` and `Read` on *Suppliers*
 * You have _no_ permission for all other entity sets.
