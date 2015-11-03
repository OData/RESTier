---
layout: post
title: "2.6 Model Building [>=0.4.0-beta]"
description: ""
category: "2. Conventions"
---

RESTier supports various ways to build EDM model. Users may first get an initial model from the EF provider. Then RESTier's `ConventionBasedApiModelBuilder` can further extend the model with additional entity sets, singletons and operations from the public properties and methods defined in the `Api` class. This subsection mainly talks about how to build an initial EDM model and then the convention RESTier adopts to extend an EDM model from an `Api` class.

### Build an initial EDM model
The `ConventionBasedApiModelBuilder` requires EDM types to be present in the initial model because it is only responsible for building entity sets, singletons and operations **NOT types**. So anyway users need to build an initial EDM model with adequate types added in advance. The typical way to do so is to write a custom model builder implementing `IModelBuilder` and register it to the `Api` class. Here is an example using the `ConventionModelBuilder` in OData Web API to build an initial model only containing the `Person` type.

{% highlight csharp %}
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.WebApi.Test.Services.TrippinInMemory
{
    public class TrippinApi : ApiBase
    {
        protected override ApiConfiguration CreateApiConfiguration()
        {
            return base.CreateApiConfiguration()
                .AddHookHandler<IModelBuilder>(new ModelBuilder());
        }

        private class ModelBuilder : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                var builder = new ODataConventionModelBuilder();
                builder.EntityType<Person>();
                return Task.FromResult(builder.GetEdmModel());
            }
        }
    }
}
{% endhighlight %}

If RESTier entity framework provider is used and user has no additional types other than those in the database schema, no custom model builder or even the `Api` class is required because the provider will take over to build the model instead. But what the provider does behind the scene is similar.

### Extend a model from Api class
The `ConventionBasedApiModelBuilder` will further extend the EDM model passed in using the public properties and methods defined in the `Api` class. Please note that all properties and methods declared in the parent classes are **NOT** considered.

**Entity set**
If a property declared in the `Api` class satisfies the following conditions, an entity set whose name is the property name will be added into the model.

 - Public
 - Has getter
 - Either static or instance
 - There is no existing entity set with the same name
 - Return type must be `IQueryable<T>` where `T` is class type

Example:

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
        public IQueryable<Person> PeopleWithFriends
        {
            get { return Context.People.Include("Friends"); }
        }
        ...
    }
}
{% endhighlight %}
<br/>
 
**Singleton**
If a property declared in the `Api` class satisfies the following conditions, a singleton whose name is the property name will be added into the model.

 - Public
 - Has getter
 - Either static or instance
 - There is no existing singleton with the same name
 - Return type must be non-generic class type

Example:

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
        public Person Me { get { return DbContext.People.Find(1); } }
        ...
    }
}
{% endhighlight %}

For versions under 0.4.0-beta, users must define an action with `ODataRouteAttribute` in their custom controller to access a singleton. **After version 0.4.0-rc, no custom route is required.** However due to some limitations from Entity Framework and OData spec, CUD (insertion, update and deletion) on the singleton entity are **NOT** supported directly by RESTier. Users need to define their own route to achieve these operations.

{% highlight csharp %}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Api;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Models;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Controllers
{
    public class TrippinController : ODataController
    {
        ...
        // Only needed <=0.4.0-beta
        [EnableQuery]
        [HttpGet]
        [ODataRoute("Me")]
        public IHttpActionResult Me()
        {
            return Ok(DbContext.People.Find(1));
        }
        ...
    }
}
{% endhighlight %}

**Operation**
If a method declared in the `Api` class satisfies the following conditions, an operation whose name is the method name will be added into the model.

 - Public
 - Either static or instance
 - There is no existing operation with the same name

Example (namespace should be specified if the namespace of the method does not match the model):

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
        // Action import
        [Action(Namespace = "Microsoft.Restier.WebApi.Test.Services.Trippin.Models")]
        public void CleanUpExpiredTrips() {}
        
        // Bound action
        [Action(Namespace = "Microsoft.Restier.WebApi.Test.Services.Trippin.Models")]
        public Trip EndTrip(Trip bindingParameter) { ... }
        
        // Function import
        [Function(Namespace = "Microsoft.Restier.WebApi.Test.Services.Trippin.Models")]
        public IEnumerable<Person> GetPeopleWithFriendsAtLeast(int n) { ... }
        
        // Bound function
        [Function(Namespace = "Microsoft.Restier.WebApi.Test.Services.Trippin.Models")]
        public Person GetPersonWithMostFriends(IEnumerable<Person> bindingParameter) { ... }
        ...
    }
}
{% endhighlight %}

Please note that in order to access an operation user must define an action with `ODataRouteAttribute` in his custom controller.

{% highlight csharp %}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Api;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Models;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Controllers
{
    public class TrippinController : ODataController
    {
        private TrippinApi api;
        
        private TrippinApi Api
        {
            get
            {
                if (api == null)
                {
                    api = new TrippinApi();
                }
                
                return api;
            }
        }
        ...
        [ODataRoute("Trips({key})/Microsoft.Restier.WebApi.Test.Services.Trippin.Models.EndTrip")]
        public IHttpActionResult EndTrip(int key)
        {
            var trip = DbContext.Trips.SingleOrDefault(t => t.TripId == key);
            return Ok(Api.EndTrip(trip));
        }
        ...
    }
}
{% endhighlight %}