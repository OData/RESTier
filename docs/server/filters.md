# EntitySet Filters

Have you ever wanted to limit the results of a particular query based on the current user, or maybe you only want
to return results that are marked "active"? 

EntitySet Filters allow you to consistently control the shape of the results returned from particular EntitySets,
even across navigation properties. 

## Convention-Based Filtering

Like the rest of RESTier, this is accomplished through a simple convention that
meets the following criteria:

 1. The filter method name must be `OnFilter{EntitySetName}`, where `{EntitySetName}` is the name the target EntitySet.
 2. It must be a `protected internal` method on the implementing `EntityFrameworkApi` class.
 3. It should accept an IQueryable<T> parameter and return an IQueryable<T> result where T is the Entity type. 

### Example

```cs
using Microsoft.Restier.Core;
using Microsoft.Restier.Provider.EntityFramework;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.OData.Service.Sample.Trippin.Api
{

    ///<summary>
    /// Customizations to the EntityFrameworkApi for the TripPin service.
    ///</summary>
    ///<example>
    /// Add the following line in WebApiConfig.cs to register this code:
    /// await config.MapRestierRoute<TrippinApi>("Trippin", "api", new RestierBatchHandler(GlobalConfiguration.DefaultServer));
    ///</example>
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {

        ///<summary>
        /// Filters queries to the Trips EntitySet to only return Users that have Trips.
        ///</summary>
        protected internal IQueryable<Person> OnFilterPeople(IQueryable<Person> entitySet)
        {
            return entitySet.Where(c => c.Trips.Any()).AsQueryable();
        }

        ///<summary>
        /// Filters queries to the Trips EntitySet to only return the current user's Trips.
        ///</summary>
        protected internal IQueryable<trip> OnFilterTrips(IQueryable<Trip> entitySet)
        {
            return entitySet.Where(c => c.PersonId == ClaimsPrincipal.Current.FindFirst("currentUserId")).AsQueryable();
        }

    }

}
```

## Centralized Filtering

TODO: Pull content from Section 2.8.