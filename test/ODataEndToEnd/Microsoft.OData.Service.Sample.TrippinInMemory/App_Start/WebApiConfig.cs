using System.Web.Http;
using Microsoft.OData.Service.Sample.TrippinInMemory.Models;
using Microsoft.Restier.Publishers.OData;
using Microsoft.Restier.Publishers.OData.Batch;

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
