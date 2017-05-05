## Additional WebAPI Operations

RESTier is built on top of ASP.NET Web API, so like our regular OData support, augmenting your service
with additional actions is very simple.

First, you must add the action to the EDM Model Builder.

Currently RESTier can not route an operation request to a method defined in API class for operation model 
building, user need to define its own controller with ODataRoute attribute for operation route.

Operation includes function (bounded), function import (unbounded), action (bounded), and action(unbounded). 

For function and action, the ODataRoute attribute must include namespace information. There is a way to simplify 
the URL to omit the namespace, user can enable this via call "config.EnableUnqualifiedNameCall(true);" during registering.

For function import and action import, the ODataRoute attribute must NOT include namespace information.
 
RESTier also supports operation request in batch request, as long as user defines its own controller for operation route.
 
This is an example on how to define customized controller with ODataRoute attribute for operation.

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Service.Sample.Trippin.Api;
using Microsoft.OData.Service.Sample.Trippin.Models;

namespace Microsoft.OData.Service.Sample.Trippin.Controllers
{
    public class TrippinController : ODataController
    {        
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
        // Unbounded action does not need namespace in route attribute
        [ODataRoute("ResetDataSource")]
        public IHttpActionResult ResetDataSource()
        {
            // reset the data source;
            return StatusCode(HttpStatusCode.NoContent);
        }

        [ODataRoute("Trips({key})/Microsoft.OData.Service.Sample.Trippin.Models.EndTrip")]
        public IHttpActionResult EndTrip(int key)
        {
            var trip = DbContext.Trips.SingleOrDefault(t => t.TripId == key);
            return Ok(Api.EndTrip(trip));
        }
        ...
    }
}
```