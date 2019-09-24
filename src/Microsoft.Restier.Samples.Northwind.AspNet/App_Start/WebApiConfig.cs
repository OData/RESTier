using System;
using System.Web.Http;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNet;
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

            config.UseRestier<NorthwindApi>(
            (services) =>
            {
                // This delegate is executed before OData is added to the container.
                // This delegate is optional. 
                services.AddEF6ProviderServices<NorthwindEntities>();
            },
            (services) =>
            {
                // This delegate is executed after OData is added to the container.
                // Add you replacement services here.

                // or even add the RestierServices explicitly yourself to be able to register services after
                // Restier registration.

                // services.AddRestierServices<NorthwindApi>();

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