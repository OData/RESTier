#if NET5_0_OR_GREATER

using CloudNimble.Breakdance.AspNetCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Restier.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Builder;
using System;
using Microsoft.AspNet.OData.Extensions;

namespace Microsoft.Restier.Breakdance
{

    /// <summary>
    /// A test class that comes configured with everything you need to get a <see cref="TestServer"/> configured for use with Restier.
    /// </summary>
    public class RestierBreakdanceTests<TApi, TDbContext> : AspNetCoreBreakdanceTestBase
        where TApi : ApiBase
        where TDbContext : DbContext
    {

        /// <summary>
        /// 
        /// </summary>
        public string RouteName { get; init; }

        /// <summary>
        /// 
        /// </summary>
        public string RoutePrefix { get; init; }

        /// <summary>
        /// 
        /// </summary>
        public RestierBreakdanceTests(string routeName, string routePrefix)
        {
            RouteName = routeName;
            RoutePrefix = routePrefix;

            TestHostBuilder.ConfigureServices(services =>
            {
                services.AddRestier(apiBuilder =>
                {
                    // This delegate is executed after OData is added to the container.
                    // Add replacement services here.
                    apiBuilder.AddRestierApi<TApi>(restierServices =>
                    {
                        restierServices
                            .AddEFCoreProviderServices<TDbContext>()
                            .AddSingleton(new ODataValidationSettings
                            {
                                MaxTop = 5,
                                MaxAnyAllExpressionDepth = 3,
                                MaxExpansionDepth = 3,
                            });
                    });

                })
                    .AddApplicationPart(typeof(RestierController).Assembly);        // this may be establishing a route that is causing problems
            });

            AddMinimalMvc(app: builder =>
            {
                /* JHC Note: the code below shows some code from the OData project
                 *           we want to change how this class starts up the server so that it doesn't need to call UseMvc() because that loads
                 *           way more services than we need.
                 *           Part of this process will be to add some additional service extensions (Endpoint extensions specifically) to do some of
                 *           the OData and Restier configuration automatically.
                 *           */
                //app.UseODataBatching();
                //app.UseRouting();
                //app.UseEndpoints(endpoints =>
                //{

                //    /* JHC: we want to get here soon
                //    endpoints.EnableAllODataFeatures(100);  // << optional MaxTop() value
                //    endpoints.MapRestier(routes =>
                //    { 
                //        routes.MapApiRoute<TApi>(routeName, routePrefix, true);
                //    });
                //    */
                //});

                builder.UseMvc(routeBuilder =>
                {
                    routeBuilder.Select().Expand().Filter().OrderBy().MaxTop(100).Count().SetTimeZoneInfo(TimeZoneInfo.Utc);

                    routeBuilder.MapRestier(restierRouteBuilder =>
                    {
                        restierRouteBuilder.MapApiRoute<TApi>(routeName, routePrefix, true);
                    });

                });
            });
        }

    }
}

#endif