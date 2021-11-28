using System;
using System.Web.Http;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Samples.Northwind.AspNet.Controllers;
using Microsoft.Restier.Samples.Northwind.AspNet.Data;

namespace Microsoft.Restier.Samples.Northwind.AspNet
{

    /// <summary>
    /// 
    /// </summary>
    public static class WebApiConfig
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        public static void Register(HttpConfiguration config)
        {

            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

#if !PROD
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
#endif

            config.Filter().Expand().Select().OrderBy().MaxTop(100).Count().SetTimeZoneInfo(TimeZoneInfo.Utc);

            config.UseRestier((builder) =>
            {
                // This delegate is executed after OData is added to the container.
                // Add you replacement services here.
                builder.AddRestierApi<NorthwindApi>(services =>
                {
                    services
                        .AddEF6ProviderServices<NorthwindEntities>()
                        .AddSingleton(new ODataValidationSettings
                        {
                            MaxTop = 5,
                            MaxAnyAllExpressionDepth = 3,
                            MaxExpansionDepth = 3,
                        });
                });
            });

            config.MapHttpAttributeRoutes();

            config.MapRestier((builder) =>
            {
                builder.MapApiRoute<NorthwindApi>("ApiV1", "", true);
            });

        }

    }

}