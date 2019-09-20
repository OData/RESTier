using System;
using System.Web.Http;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.Samples.Northwind.AspNet.Controllers;
using Microsoft.Restier.Samples.Northwind.AspNet.Data;

namespace Microsoft.Restier.Samples.Northwind.AspNet
{
    public static class WebApiConfig
    {

        public static void Register(HttpConfiguration config)
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

            config.UseRestier<NorthwindApi>((services) =>
            {
                services.AddEfProviderServices<NorthwindEntities>();

                // RWM: Add you replacement services here.
                services.AddSingleton(new ODataValidationSettings
                {
                    MaxAnyAllExpressionDepth = 3,
                    MaxExpansionDepth = 3,
                });
            });

            config.MapRestier<NorthwindApi>("ApiV1", "", true);

        }

    }

}