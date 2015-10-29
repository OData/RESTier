---
layout: post
title: "2.4 Submit Logic [>=0.4.0-beta]"
description: ""
category: "2. Conventions"
---

Submit logic convention allows users to authorize a submit operation or plug in user logic (such as logging) before and after a submit operation. Usually a submit operation can be inserting an entity, deleting an entity, updating an entity or executing an OData action.

### Authorization
Users can control if one of the four submit operations is allowed on some entity set or action by putting some **protected** methods into the `Api` class. The method signatures must exactly match the following examples. The method name must conform to `Can<Insert|Update|Delete|Execute><EntitySetName|ActionName>`.

{% highlight csharp %}
using System.Collections.Generic;
using System.Linq;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Models;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Api
{
    public class TrippinApi : DbApi<TrippinModel>
    {
        ...
        // Can delete an entity from the entity set Trips?
        protected bool CanDeleteTrips()
        {
            return false;
        }
        
        // Can execute an action named ResetDataSource?
        protected bool CanExecuteResetDataSource()
        {
            return false;
        }
    }
}
{% endhighlight %}

### Plug in user logic
Users can plug in user logic before and after executing one of the four submit operations by putting similar **protected** methods into the `Api` class. The method signatures must also exactly match the following examples. The method name must conform to `Can<Insert|Updat|Delet|Execut><ed|ing><EntitySetName|ActionName>` where `ing` for **before submit** and `ed` for **after submit**.

{% highlight csharp %}
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFramework;

namespace Microsoft.Restier.Samples.Northwind.Models
{
    public class NorthwindApi : DbApi<NorthwindContext>
    {
        ...
        // Gets called before updating an entity from the entity set Products.
        protected void OnUpdatingProducts(Product product)
        {
            WriteLog(DateTime.Now.ToString() + product.ProductID + " is being updated");
        }

        // Gets called after inserting an entity to the entity set Products.
        protected void OnInsertedProducts(Product product)
        {
            WriteLog(DateTime.Now.ToString() + product.ProductID + " has been inserted");
        }
    }
}
{% endhighlight %}