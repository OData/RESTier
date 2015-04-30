---
layout: post
title: "3.3 Fallback to Web API for OData"
description: ""
category: "3. Extensions"
---

The domain controller in RESTier currently only supports limited CRUD operations on top-level entities. But RESTier allows routing to fallback to Web API for OData once there is no way in the domain controller to handle a route. To do that, we just need to add controller actions with the `ODataRoute` attribute to the domain controller. This section shows how to support some advanced OData queries in the domain controller using attribute routing. We will use and extend the **AdventureWorksLT** sample service from [here](https://github.com/OData/ODataSamples/tree/master/RESTier/AdventureWorksLTSample).

### Access Property Values
In the **AdventureWorksController.cs**, add the following code into the `AdventureWorksController` class:

{% highlight csharp %}
using System.Linq;

namespace AdventureWorksLTSample.Controllers
{
    public class AdventureWorksController : ODataDomainController<AdventureWorksDomain>
    {
        ...
        [ODataRoute("Customers({id})/CompanyName")]
        [ODataRoute("Customers({id})/CompanyName/$value")]
        public IHttpActionResult GetCustomerCompanyName(int id)
        {
            var customer = DbContext.Customers.Find(id);
            if (customer == null)
            {
                return NotFound();
            }

            return Ok(customer.CompanyName);
        }
        ...
    }
}
{% endhighlight %}

This code adds the `GetCustomerCompanyName` action to the domain controller to enable query of the `CompanyName` property of a `Customer` entity.

### Enable Entity Counting
In the **AdventureWorksController.cs**, add the following code into the `AdventureWorksController` class:

{% highlight csharp %}
namespace AdventureWorksLTSample.Controllers
{
    public class AdventureWorksController : ODataDomainController<AdventureWorksDomain>
    {
        ...
        [ODataRoute("Customers/$count")]
        public IHttpActionResult GetCustomersCount()
        {
            return Ok(this.DbContext.Customers.Count());
        }
        ...
    }
}
{% endhighlight %}

This code adds the `GetCustomersCount` action to the domain controller to enable counting of the `Customers` entity set.

### Update Property Values
In the **AdventureWorksController.cs**, add the following code into the `AdventureWorksController` class:

{% highlight csharp %}
namespace AdventureWorksLTSample.Controllers
{
    public class AdventureWorksController : ODataDomainController<AdventureWorksDomain>
    {
        ...
        [HttpPut]
        [ODataRoute("Products({id})/Color")]
        public IHttpActionResult UpdateProductColor(int id, [FromBody] string color)
        {
            var product = DbContext.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Color = color;
            DbContext.SaveChanges();

            return Ok(color);
        }
        ...
    }
}
{% endhighlight %}

This code adds the `UpdateProductColor` action to the domain controller to enable updating the value of the `Color` property.

### Override Default Routes
In the **AdventureWorksController.cs**, add the following code into the `AdventureWorksController` class:

{% highlight csharp %}
namespace AdventureWorksLTSample.Controllers
{
    public class AdventureWorksController : ODataDomainController<AdventureWorksDomain>
    {
        ...
        [ODataRoute("Customers")]
        public IHttpActionResult GetCustomers()
        {
            return Ok(this.DbContext.Customers.Where(c => c.CustomerID % 3 == 0));
        }
        ...
    }
}
{% endhighlight %}

This code adds the `GetCustomers` action to the domain controller to override the default route to the `Customers` entity set. User-defined actions with **attribute routing** are **prioritized**.

There is no need to list more samples here because they are exactly the same as the ones defined in normal Web API projects. The only difference is that we put all the actions in the same domain controller.