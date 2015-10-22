using System.Web.Http;
using Microsoft.Restier.WebApi.Batch;

namespace Microsoft.Restier.WebApi.Test.Services.TrippinInMemory
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
