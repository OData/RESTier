using System;
using System.Web.Http;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.Restier.AspNet.Batch;
using Microsoft.Restier.Samples.Northwind.AspNet.Controllers;

namespace Microsoft.Restier.Samples.Northwind.AspNet
{
    public class WebApiConfig
    {

        public static async void Register(HttpConfiguration config)
        {

#if !PROD
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.SetUseVerboseErrors(true);
#endif

            config.Filter().Expand().Select().OrderBy().MaxTop(100).Count();
            config.SetTimeZoneInfo(TimeZoneInfo.Utc);

            config.MapHttpAttributeRoutes();

            var batchHandler = new RestierBatchHandler(GlobalConfiguration.DefaultServer);
            await config.MapRestierRoute<NorthwindApi>("ApiV1", "", batchHandler);

        }
    }
}