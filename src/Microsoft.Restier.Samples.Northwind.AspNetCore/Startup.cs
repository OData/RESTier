// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core;
using Microsoft.Restier.Samples.Northwind.AspNet.Controllers;
using System;
using System.Linq;

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
            services.AddRestier((builder) =>
            {
                // This delegate is executed after OData is added to the container.
                // Add your replacement services here.
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
            }, true);

            services.AddRestierSwagger();

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

            app.UseRestierBatching();
            app.UseRouting();

            app.UseAuthorization();
            app.UseClaimsPrincipals();

            app.UseEndpoints(endpoints =>
            {
                endpoints.Select().Expand().Filter().OrderBy().MaxTop(100).Count().SetTimeZoneInfo(TimeZoneInfo.Utc);
                endpoints.MapRestier(builder =>
                {
                    //builder.MapApiRoute<NorthwindApi>("ApiV1", "test", true);
                    builder.MapApiRoute<NorthwindApi>("ApiV1", "", true);
                });
            });

            app.UseRestierSwagger(true);
        }

    }

}
