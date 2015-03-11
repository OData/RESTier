---
layout: post
title: "Add OData functions and actions in RESTier preview"
description: ""
category: Extentions
---

In the current version, RESTier doesn't natively support OData operations (functions and actions) because [Entity Framework](http://msdn.microsoft.com/en-us/data/ef.aspx) has no related concept. To mitigate this, we provide a way to extend the model to make it possible for users to add OData operations to their OData services.

The basic idea is to allow user to extend the `Edm Model` to add actions and functions. 

### Code Sample
Here we assume that you are familiar with the basic use of RESTier, if not, please refer to the [basic tutorial](https://github.com/OData/RESTier/wiki/Samples-1:-Getting-started---basic).

First, add the convention method `OnModelExtending` in `NorthwindDomain`:

```csharp
    using Microsoft.OData.Edm;
    using Microsoft.OData.Edm.Library;

    public class NorthwindDomain : DbDomain<NorthwindContext>
    {
        public NorthwindContext Context { get { return DbContext; } }

        protected EdmModel OnModelExtending(EdmModel model)
        {
            var ns = model.DeclaredNamespaces.First();
            var product = model.FindDeclaredType(ns + "." + "Product");
            var products = EdmCoreModel.GetCollection(product.GetEdmTypeReference(isNullable: false));
            var mostExpensive = new EdmFunction(ns, "MostExpensive",
                EdmCoreModel.Instance.GetPrimitive(EdmPrimitiveTypeKind.Double, isNullable: false), isBound: true,
                entitySetPathExpression: null, isComposable: false);
            mostExpensive.AddParameter("bindingParameter", products);
            model.AddElement(mostExpensive);
            return model;
        }
     }
```

Then, in the `NorthwindController`, add the following code:

```csharp
    public class NorthwindController : ODataDomainController<NorthwindDomain>
    {
        // other code

        [HttpGet]
        [ODataRoute("Products/Microsoft.Restier.Samples.Northwind.Models.MostExpensive")]
        public IHttpActionResult MostExpensive()
        {
            var product = DbContext.Products.Max(p => p.UnitPrice);
            return Ok(product);
        }
     }
```

Now, you are successfully supporting a `MostExpensive` function bound to the Products entity set in your OData service.

### In the future
Working directly on `Edm Model` can be a bit tricky, even though it adds quite a bit of flexibility. In the coming release, we are planning on improving the support for OData operations with a more user-friendly approach. 
