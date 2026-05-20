// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Samples.Northwind.AspNet.Controllers;
using NSwag.AspNetCore;
using System;

namespace Microsoft.Restier.Samples.Northwind.AspNetCore
{

    /// <summary>
    /// Startup class. Configures the container and the application.
    /// </summary>
    public class Startup
    {

        /// <summary>
        /// The application configuration
        /// </summary>
        public IConfiguration Configuration { get; }

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
            services
                .AddControllers()
                .AddRestier(options =>
                {
                    options.Select().Expand().Filter().OrderBy().SetMaxTop(5).Count();
                    options.TimeZone = TimeZoneInfo.Utc;

                    options.AddRestierRoute<NorthwindApi>(string.Empty, restierServices =>
                    {
                        restierServices
                            .AddEFCoreProviderServices<NorthwindContext>((services, dbOptions) =>
                                dbOptions.UseSqlServer(Configuration.GetConnectionString("NorthwindEntities")));
                    },
                    bag =>
                    {
                        bag.Validation.MaxAnyAllExpressionDepth = 3;
                        bag.Validation.MaxExpansionDepth = 3;
                    });
                })
                .AddApplicationPart(typeof(NorthwindApi).Assembly)
                .AddApplicationPart(typeof(RestierController).Assembly);

            services.AddRestierNSwag();
            services.AddOpenApiDocument(c => c.DocumentName = "controllers");

            //RWM: Since AddRestier calls .AddAuthorization(), you can uncomment the line below if you want every request to be authenticated.
            //services.Configure<AuthorizationOptions>(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
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

            app.UseMiddleware<Restier.AspNetCore.Middleware.ODataBatchHttpContextFixerMiddleware>();
            app.UseODataBatching();
            app.UseODataRouteDebug();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRestier();
            });

            app.UseRestierOpenApi();
            app.UseRestierReDoc();
            app.UseRestierNSwagUI();
            app.UseOpenApi();
            app.UseReDoc(c =>
            {
                c.Path = "/redoc/controllers";
                c.DocumentPath = "/swagger/controllers/swagger.json";
            });
        }

    }

}
