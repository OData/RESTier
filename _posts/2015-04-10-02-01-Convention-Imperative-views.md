---
layout: post
title: "2.1 Imperative Views"
description: ""
category: "2. Conventions"
---

Imperative views enable adding new custom entity sets for existing entity type. For example, in order to add a new entity set called 'ColoredProducts' in addition to existing 'Products' entity set, the first thing is to choose the appropriate entity type `T` (e.g. `Proudct`), then define an `IQueryable<T>` (e.g. `IQueryable<Product>`) property on domain class. It requires:

	1. T must be an entity type already in the metadata model.
	2. IQueryable<T> property must be declared as protected property on the domain class that has [EnableConventions] attribute.
	3. IQueryable<T> property needs return an expression to be evaluated by RESTier provider later, e.g. return this.Source<Product>("[existing entity set name]").  IQueryable<T> property shouldn't directly return actually entity set data. 

Supposed that 'Products' is an existing entity set, the below 'ColoredProducts' property will add a new entity set called 'ColoredProducts' to the RESTier OData service.

{% highlight csharp %}
	
	using Microsoft.Restier.Conventions;
	using Microsoft.Restier.Core;
	using Microsoft.Restier.EntityFramework;
	using System.Data.Entity;
	using System.Linq;
	using System.Threading.Tasks;
	
	namespace AdventureWorksLTSample.Models
	{
	    [EnableConventions]
	    public class AdventureWorksDomain : DbDomain<AdventureWorksContext>
		{
			protected IQueryable<Product> ColoredProducts
		    {
		        get
		        {
		            return this.Source<Product>("Products").Where(s => !string.IsNullOrEmpty(s.Color));
		        }
			}
	    }
	}

{% endhighlight %}

Press F5 to run the AdventureWorks sample project, about 'ColoredProducts' entity set :

	1. Its entity set definition can be found in metadata document @ ~/AdventureWorksLT/$metadata.
	2. Its data can be viewed @ ~/AdventureWorksLT/ColoredProducts

