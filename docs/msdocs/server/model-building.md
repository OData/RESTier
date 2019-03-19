# Customizing the Entity Model

OData and the Entity Framework are based on the same underlying concept for mapping the idea of an Entity with
its representation in the database. That "mapping" layer is called the Entity Data Model, or EDM for short.

Part of the beautiy of RESTier is that, for the majority of API builders, it can construct your EDM for you
*automagically*. But there are times where you have to take charge of the process. And as with many things in RESTier,
the intrepid developers at Microsoft provide you with two ways to do so.

The first method allows you to completely relpace the automagic model construction with your own, in a manner
very similar to Web API OData.

The second method lets RESTier do the initial work for you, and then you manipulate the resulting EDM metadata.

Let's take a look at how each of these methods work.

## ModelBuilder Takeover

There are several situations where you are likely going to want to use this approach to create your Model.
For example, if you're migrating from an existing Web API OData v3 or v4 implementation, and needed to
customize that model, you will be able to copy/paste your existing code over, with just a few small changes.
If you're building a new model, but you're using Entity Framework Model First + SQL Views, then you'll
likely need to define a primary key, or omit the View from your service.

With the Entity Framework provider, the model is built with the
[**ODataConventionModelBuilder**](http://odata.github.io/WebApi/#02-04-convention-model-builder). To 
understand how this ModelBuilder works, please take a few minutes and review that documentation.

# Example

```cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;

namespace Microsoft.OData.Service.Sample.TrippinInMemory
{

    internal class CustomizedModelBuilder : IModelBuilder
    {
        public Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntityType<Person>();
            return Task.FromResult(builder.GetEdmModel());
        }
    }

    ///<summary>
    ///
    ///</summary>
    public class TrippinApi : ApiBase
    {

        ///<summary>
        ///
        ///</summary>
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            return base.ConfigureApi(services)
                .AddService<IModelBuilder, CustomizedModelBuilder>();
        }

    }

}
```

If RESTier entity framework provider is used and user has no additional types other than those in the database schema, no 
custom model builder or even the `Api` class is required because the provider will take over to build the model instead. 
But what the provider does behind the scene is similar.



## Extend a model from Api class
The `RestierModelExtender` will further extend the EDM model passed in using the public properties and methods defined in the 
`Api` class. Please note that all properties and methods declared in the parent classes are **NOT** considered.

**Entity set**
If a property declared in the `Api` class satisfies the following conditions, an entity set whose name is the property name 
will be added into the model.

 - Public
 - Has getter
 - Either static or instance
 - There is no existing entity set with the same name
 - Return type must be `IQueryable<T>` where `T` is class type

Example:

```cs
using System.Collections.Generic;
using System.Linq;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Provider.EntityFramework;
using Microsoft.OData.Service.Sample.Trippin.Models;

namespace Microsoft.OData.Service.Sample.Trippin.Api
{
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {
        public IQueryable<Person> PeopleWithFriends
        {
            get { return Context.People.Include("Friends"); }
        }
        ...
    }
}
```
 
**Singleton**
If a property declared in the `Api` class satisfies the following conditions, a singleton whose name is the property name 
will be added into the model.

 - Public
 - Has getter
 - Either static or instance
 - There is no existing singleton with the same name
 - Return type must be non-generic class type

Example:

```cs
using System.Collections.Generic;
using System.Linq;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Provider.EntityFramework;
using Microsoft.OData.Service.Sample.Trippin.Models;

namespace Microsoft.OData.Service.Sample.Trippin.Api
{
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {
        ...
        public Person Me { get { return DbContext.People.Find(1); } }
        ...
    }
}
```

Due to some limitations from Entity Framework and OData spec, CUD (insertion, update and deletion) on the singleton entity are 
**NOT** supported directly by RESTier. Users need to define their own route to achieve these operations.

**Navigation property binding**
Starting from version 0.5.0, the `RestierModelExtender` follows the rules below to add navigation property bindings after entity 
    sets and singletons have been built.

 - Bindings will **ONLY** be added for those entity sets and singletons that have been built inside `RestierModelExtender`.
   **Example:** Entity sets built by the RESTier's EF provider are assumed to have their navigation property bindings added already.
 - The `RestierModelExtender` only searches navigation sources who have the same entity type as the source navigation property.
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

```cs
using System.Collections.Generic;
using System.Linq;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Provider.EntityFramework;
using Microsoft.OData.Service.Sample.Trippin.Models;

namespace Microsoft.OData.Service.Sample.Trippin.Api
{
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {
        ...
        // Action import
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public void CleanUpExpiredTrips() {}
        
        // Bound action
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", HasSideEffects = true)]
        public Trip EndTrip(Trip bindingParameter) { ... }
        
        // Function import
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", EntitySet = "People")]
        public IEnumerable<Person> GetPeopleWithFriendsAtLeast(int n) { ... }
        
        // Bound function
        [Operation(Namespace = "Microsoft.OData.Service.Sample.Trippin.Models", EntitySet = "People")]
        public Person GetPersonWithMostFriends(IEnumerable<Person> bindingParameter) { ... }
        ...
    }
}
```

Note:

1. Operation attribute's EntitySet property is needed if there are more than one entity set of the entity type that is type of result defined. Take an example if two EntitySet People and AllPersons are defined whose entity type is Person, and the function returns Person or List of Person, then the Operation attribute for function must have EntitySet defined, or EntitySet property is optional. 

2. Function and Action uses the same attribute, and if the method is an action, must specify property HasSideEffects with value of true whose default value is false.
    
3. In order to access an operation user must define an action with `ODataRouteAttribute` in his custom controller.
Refer to [section 3.3](http://odata.github.io/RESTier/#03-03-Operation) for more information.

## Custom model extension
If users have the need to extend the model even after RESTier's conventions have been applied, user can use IServiceCollection AddService to add a ModelBuilder after calling base.ConfigureApi(services).

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Provider.EntityFramework;
using Microsoft.OData.Service.Sample.Trippin.Models;

namespace Microsoft.OData.Service.Sample.Trippin.Api
{
    public class TrippinAttribute : ApiConfiguratorAttribute
    {
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            services = base.ConfigureApi(services);
            // Add your custom model extender here.
            services.AddService<IModelBuilder, CustomizedModelBuilder>();
            return services;
        }

        private class CustomizedModelBuilder : IModelBuilder
        {
            public IModelBuilder InnerModelBuilder { get; set; }

            public async Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                IEdmModel model = null;
                
                // Call inner model builder to get a model to extend.
                if (this.InnerModelBuilder != null)
                {
                    model = await this.InnerModelBuilder.GetModelAsync(context, cancellationToken);
                }

                // Do sth to extend the model such as add custom navigation property binding.

                return model;
            }
        }
    }
}
```
    
After the above steps, the final process of building the model will be:

 - User's model builder registered before base.ConfigureApi(services) is called first.
 - RESTier's model builder includes EF model builder and RestierModelExtender will be called.
 - User's model builder registered after base.ConfigureApi(services) is called.
 <br/>
 
If InnerModelBuilder method is not called first, then the calling sequence will be different.
Actually this order not only applies to the `IModelBuilder` but also all other services.

Refer to [section 4.3](http://odata.github.io/RESTier/#04-03-Api-Service) for more details of RESTier API Service.