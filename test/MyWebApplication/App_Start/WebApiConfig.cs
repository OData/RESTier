using Microsoft.AspNet.OData.Extensions;
using Microsoft.Restier.Providers.EntityFramework;
using Microsoft.Restier.Publishers.OData;
using Microsoft.Restier.Publishers.OData.Batch;
using MyWebApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace MyWebApplication
{
    public static class WebApiConfig
    {
        public async static void Register(HttpConfiguration config)
        {
            config.Filter().Expand().Select().OrderBy().MaxTop(null).Count();
            await config.MapRestierRoute<EntityFrameworkApi<TrippinModel>>(
                "odata", "odata", new RestierBatchHandler(GlobalConfiguration.DefaultServer));
        }
    }
}
