using System.Web.Http;
using System.Web.OData.Domain.Batch;
using System.Web.OData.Domain.Test.Services.Trippin.Controllers;

namespace System.Web.OData.Domain.Test.Services.Trippin
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            RegisterTrippin(config, GlobalConfiguration.DefaultServer);
        }

        public static async void RegisterTrippin(
            HttpConfiguration config, HttpServer server)
        {
            await config.MapODataDomainRoute<TrippinController>(
                "TrippinApi", "api/Trippin",
                new ODataDomainBatchHandler(server));
        }
    }
}
