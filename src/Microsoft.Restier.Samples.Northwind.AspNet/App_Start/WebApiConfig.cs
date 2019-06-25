using System;
using System.Web.Http;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.Restier.AspNet.Batch;
using Microsoft.Restier.Samples.Northwind.AspNet.Controllers;

namespace Microsoft.Restier.Samples.Northwind.AspNet
{
    public static class WebApiConfig
    {

        public static async void Register(HttpConfiguration config)
        {

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

#if !PROD
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
#endif

            config.Filter().Expand().Select().OrderBy().MaxTop(100).Count();
            config.SetTimeZoneInfo(TimeZoneInfo.Utc);

            config.MapHttpAttributeRoutes();

            var batchHandler = new RestierBatchHandler(GlobalConfiguration.DefaultServer);
#pragma warning disable CA2007 // Do not directly await a Task
            await config.MapRestierRoute<NorthwindApi>("ApiV1", "", batchHandler);
#pragma warning restore CA2007 // Do not directly await a Task

        }
    }
}