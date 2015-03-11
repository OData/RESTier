---
layout: post
title: "Enable Multiple Controllers in RESTier"
description: ""
category: Extentions
---


First of all, it's important to emphasize that **you only _need_ to use one `ODataDomainController<DbDomain>`** to bootstrap an entire OData service. 

Nonetheless, we received feedback from early customers that it may be organizationally preferable in some situations to have multiple controllers. For instance, this allows logic to be separated out for different entity sets. Given this, RESTier gives you the _option_ of putting your service in a single controller, or distributing it across multiple controllers. When multiple controllers are used, their behavior is still coordinated: the behavior defined in a specific controller will overwrite the default behavior of the the controller inheriting from `ODataDomainController<DbDomain>`.

### Code Samples
Here we assume that you are familiar with the basic implementation of RESTier; if not, please first refer to the [basic tutorials](https://github.com/OData/RESTier/wiki/Getting-started---Basic-Tutorial).

Below is the _default_ controller:

```csharp
    public class NorthwindController :ODataDomainController<NorthwindDomain>
    {
        private NorthwindContext DbContext
        {
            get
            {
                return Domain.Context;
            }
        }
    }
```

Below is a _specific_ controller for the entity set *Regions*. This is exactly the same with as it would be using just [Web API OData](http://www.asp.net/web-api/overview/odata-support-in-aspnet-web-api/odata-v4/create-an-odata-v4-endpoint) _except_ we've added the `NorthwindContext` to the beginning: `private readonly NorthwindContext _context = new NorthwindContext();`.

```csharp
    public class RegionsController : ODataController
    {
        private readonly NorthwindContext _context = new NorthwindContext();

        [EnableQuery]
        public IQueryable<Region> Get()
        {
            return _context.Regions;
        }

        [EnableQuery]
        public SingleResult<Region> Get(int key)
        {
            var result = customerFucntion(); // Fake user logic
            return result ;
        }
    }
```

Entity sets with no controllers specified will be routed to the corresponding method on the `NorthwindController`. For entity sets with controllers specified, however, the more specific controller will override the default behaviors. In this example, the `Get` and `Get(int key)` calls for the **Regions** entity are intercepted by the `RegionsController`. 
