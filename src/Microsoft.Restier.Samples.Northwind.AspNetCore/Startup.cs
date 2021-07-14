using System;
using System.Linq;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core;
using Microsoft.Restier.Samples.Northwind.AspNet.Controllers;

namespace Microsoft.Restier.Samples.Northwind.AspNetCore
{
    /// <summary>
    /// Startup class. Configures the container and the application.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Configures the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRestier((builder) =>
            {
                // This delegate is executed after OData is added to the container.
                // Add you replacement services here.
                builder.AddRestierApi<NorthwindApi>(routeServices =>
                {

                    routeServices
                        .AddEFCoreProviderServices<NorthwindContext>((services, options) => options.UseSqlServer(Configuration.GetConnectionString("NorthwindEntities")))
                        .AddSingleton(new ODataValidationSettings
                        {
                            MaxTop = 5,
                            MaxAnyAllExpressionDepth = 3,
                            MaxExpansionDepth = 3,
                        });

                });
            });
            services.AddControllers(options => options.EnableEndpointRouting = false);
        }

        /// <summary>
        /// Configures the application and the HTTP Request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc(builder =>
            {
                builder.Select().Expand().Filter().OrderBy().MaxTop(100).Count().SetTimeZoneInfo(TimeZoneInfo.Utc);

                builder.MapRestier(builder =>
                {
                    builder.MapApiRoute<NorthwindApi>("ApiV1", "/v1", true);
                });
            });
        }

        /// <summary>
        /// The application configuration
        /// </summary>
        public IConfiguration Configuration { get; }
    }
}
