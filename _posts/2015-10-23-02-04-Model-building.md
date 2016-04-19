---
layout: post
title: "2.4 Model Building [>=0.4.0-beta]"
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
<br/>

**Navigation property binding**
Starting from version 0.4.0-rc, the `ConventionBasedApiModelBuilder` follows the rules below to add navigation property bindings after entity sets and singletons have been built.

 - Bindings will **ONLY** be added for those entity sets and singletons that have been built inside `ConventionBasedApiModelBuilder`.
   **Example:** Entity sets built by the RESTier's EF provider are assumed to have their navigation property bindings added already.
 - The `ConventionBasedApiModelBuilder` only searches navigation sources who have the same entity type as the source navigation property.
   **Example:** If the type of a navigation property is `Person` or `Collection(Person)`, only those entity sets and singletons of type `Person` are searched.
 - Singleton navigation properties can be bound to either entity sets or singletons. 
   **Example:** If `Person.BestFriend` is a singleton navigation property, bindings from `BestFriend` to an entity set `People` or to a singleton `Boss` are all allowed.
 - Collection navigation properties can **ONLY** be bound to entity sets.
   **Example:** If `Person.Friends` is a collection navigation property. **ONLY** binding from `Friends` to an entity set `People` is allowed. Binding from `Friends` to a singleton `Boss` is **NOT** allowed.
 - If there is any ambiguity among entity sets or singletons, no binding will be added.
   **Example:** For the singleton navigation property `Person.BestFriend`, no binding will be added if 1) there are at least two entity sets (or singletons) both of type `Person`; 2) there is at least one entity set and one singleton both of type `Person`. However for the collection navigation property `Person.Friends`, no binding will be added only if there are at least two entity sets both of type `Person`. One entity set and one singleton both of type `Person` will **NOT** lead to any ambiguity and one binding to the entity set will be added.

If any expected navigation property binding is not added by RESTier, users can always manually add it through custom model extension (mentioned below).
<br/>

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

### Custom model extension
If users have the need to extend the model even after RESTier's conventions have been applied, `ApiConfiguratorAttribute` can be used. First implement a custom `ApiConfiguratorAttribute` and register a model extender in it. The difference from the previous `TrippinApi.ModelBuilder` is that the previous one does **NOT** need to implement `IDelegateHookHandler<IModelBuilder>` which provides it with the capability to call an inner model builder. The previous one itself is responsible for producing an initial model. However `TrippinAttribute.TrippinModelExtender` **MUST** implement this interface and call the inner model builder to at least get a workable model to extend. Notably the built-in `ConventionBasedApiModelBuilder` and `ConventionBasedOperationProvider` also follow this pattern.

{% highlight csharp %}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Models;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Api
{
    public class TrippinAttribute : ApiConfiguratorAttribute
    {
        public override void Configure(ApiConfiguration configuration, Type type)
        {
            // Add your custom model extender here.
            configuration.AddHookHandler<IModelBuilder>(new TrippinModelExtender());
        }

        private class TrippinModelExtender : IModelBuilder, IDelegateHookHandler<IModelBuilder>
        {
            public IModelBuilder InnerHandler { get; set; }

            public async Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                IEdmModel model = null;
                
                // Call inner model builder to get a model to extend.
                if (this.InnerHandler != null)
                {
                    model = await this.InnerHandler.GetModelAsync(context, cancellationToken);
                }

                // Do sth to extend the model such as add custom navigation property binding.

                return model;
            }
        }
    }
}
{% endhighlight %}

Then apply it to the `Api` class.

{% highlight csharp %}
using System.Collections.Generic;
using System.Linq;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Models;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Api
{
    [Trippin]
    public class TrippinApi : ApiBase
    {
        ...
    }
}
{% endhighlight %}

After the above steps, the final process of building the model will be:

 - User's model builder or RESTier provider's model builder registered in `CreateApiConfiguration`: produce an initial model
   **In this case:** `TrippinApi.ModelBuilder` or `Microsoft.Restier.EntityFramework.Model.ModelProducer`.
 - `ConventionBasedApiModelBuilder`: extend the model with entity sets and singletons from `Api` class
 - `ConventionBasedOperationProvider`: extend the model with actions and functions from `Api` class
 - User's model extender registered in custom `ApiConfiguratorAttribute`: custom model extension
   **In this case:** `TrippinAttribute.TrippinModelExtender`.
 <br/>
 
Actually this order not only applies to the `IModelBuilder` but also all other hook handlers. The typical order for executing a hook handler will be:

 - Hook handlers registered in `CreateApiConfiguration`: provide an initial result
 - Hook handlers provided by RESTier conventions: apply RESTier conventions to the result
 - Hook handlers registered in custom `ApiConfiguratorAttribute`: user customizations