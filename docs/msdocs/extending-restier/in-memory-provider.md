## In-Memory Data Provider

RESTier supports building an OData service with **all-in-memory** resources. However currently RESTier 
has not provided a dedicated in-memory provider module so users have to write some service code to bootstrap 
the initial model with EDM types themselves. There is a sample service with in-memory provider [here](https://github.com/OData/RESTier/tree/apidev/test/ODataEndToEndTests/Microsoft.OData.Service.Sample.TrippinInMemory). 
This subsection mainly talks about how such a service is created.

First please create an **Empty ASP.NET Web API** project following the instructions in [Section 1.2](http://odata.github.io/RESTier/#01-02-Bootstrap). Stop **BEFORE** the **Generate the model classes** part.

### Create the Api class
Create a simple data type `Person` with some properties and "fabricate" some fake data. Then add the first entity set `People` to the `Api` class:

    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.OData.Builder;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Model;
    
    namespace Microsoft.OData.Service.Sample.TrippinInMemory
    {
        public class TrippinApi : ApiBase
        {
            private static readonly List<Person> people = new List<Person>
            {
                ...
            };
    
            public IQueryable<Person> People
            {
                get { return people.AsQueryable(); }
            }
        }
    }

### Create an initial model
Since the RESTier convention will not produce any EDM type, an initial model with at least the `Person` type needs to be created by service. Here the `ODataConventionModelBuilder` from OData Web API is used for quick model building.
Any model building methods supported by Web API OData can be used here, refer to **[Web API OData Model builder ](http://odata.github.io/WebApi/#02-01-model-builder-abstract)**document for more information.

    namespace Microsoft.OData.Service.Sample.TrippinInMemory
    {
        public class TrippinApi : ApiBase
        {
            protected override IServiceCollection ConfigureApi(IServiceCollection services)
            {
                services.AddService<IModelBuilder>(new ModelBuilder());
                return base.ConfigureApi(services);
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

### Configure the OData endpoint
Replace the `WebApiConfig` class with the following code. No need to create a custom controller if users don't have attribute routing.

    using System.Web.Http;
    using Microsoft.Restier.Publisher.OData.Batch;
    
    namespace Microsoft.OData.Service.Sample.TrippinInMemory
    {
        public static class WebApiConfig
        {
            public static void Register(HttpConfiguration config)
            {
                config.MapRestierRoute<TrippinApi>(
                    "TrippinApi",
                    "api/Trippin",
                    new RestierBatchHandler(GlobalConfiguration.DefaultServer)).Wait();
            }
        }
    }
