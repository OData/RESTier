using System.Web.Http;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.Restier.AspNet.Batch;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.Samples.Chinook.Api.Models;

namespace Microsoft.Restier.Samples.Chinook.Api
{
    public static class WebApiConfig
    {
        public static async void Register(HttpConfiguration config)
        {
            config.Filter().Expand().Select().OrderBy().MaxTop(null).Count();
            await config.MapRestierRoute<EntityFrameworkApi<ChinookContext>>(
                "Chinook",
                "api/",
                new RestierBatchHandler(GlobalConfiguration.DefaultServer)).ConfigureAwait(false);
        }
    }
}
