using System.Web.Http;
using Microsoft.OData.Service.Sample.TrippinInMemory.Models;
using Microsoft.Restier.Publisher.OData.Batch;
using Microsoft.Restier.Publisher.OData.Routing;

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
