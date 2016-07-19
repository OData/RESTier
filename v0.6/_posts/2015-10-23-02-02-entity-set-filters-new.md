---
layout: post
title: "2.2 Entity Set Filters [>=0.4.0]"
description: ""
category: "2. Features"
---

Entity set filter convention helps plug in a piece of filtering logic for entity set. It is done via adding an `OnFilter[entity type name](IQueryable<T> entityset)` method to the `Api` class.

	1. The filter method name must be OnFilter[entity type name], ending with the target entity type name.
	2. It must be a **protected** method on the `Api` class.
	3. It should accept an IQueryable<T> parameter and return an IQueryable<T> result where T is the entity type. 

Supposed that ~/AdventureWorksLT/Products can get all the Product entities, the below OnFilterProduct method will filter some Product entities by checking the ProductID.

{% highlight csharp %}
using Microsoft.Restier.Core;
using Microsoft.Restier.Provider.EntityFramework;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace AdventureWorksLTSample.Models
{
    public class AdventureWorksApi : EntityFrameworkApi<AdventureWorksContext>
    {
        protected IQueryable<Product> OnFilterProduct(IQueryable<Product> entitySet)
        {
            return entitySet.Where(s => s.ProductID % 3 == 0).AsQueryable();
        }
    }
}
{% endhighlight %}

Now some testings will show that:

	1. ~/AdventureWorksLT/Products will only get the Product entities whose ProductID is  3,6,9,12,15,... 
	2. ~/AdventureWorksLT/Products([product id]) will only be able to get a Product entity whose ProductID mod 3 results a zero. 

Note: 
1. Starting from version 0.6, the conversion name is changed to OnFilter[entity type name], and before version 0.6, the name is OnFilter[entity set name]

2. Starting from version 0.6, the filter is applied to all places besides the top level entity set which includes navigation properties, collection of entity in $expand, collection in filter and so on. Refer to end to end test case [TrippinE2EOnFilterTestCases](https://github.com/OData/RESTier/blob/master/test/ODataEndToEnd/Microsoft.OData.Service.Sample.Tests/TrippinE2EOnFilterTestCases.cs) for all the scenarios supported.

3. More meaningful filter can be adopted like filter entity by the owner and the entity owner is current request user.