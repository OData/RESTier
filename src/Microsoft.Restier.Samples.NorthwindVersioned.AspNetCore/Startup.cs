// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.Data;

namespace Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore
{

    public class Startup
    {

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApiVersioning(o =>
            {
                o.DefaultApiVersion = new ApiVersion(2, 0);
                o.ReportApiVersions = true;
                o.ApiVersionReader = new UrlSegmentApiVersionReader();
            }).AddApiExplorer();

            services.AddControllers().AddRestier(options =>
            {
                options.Select().Expand().Filter().OrderBy().SetMaxTop(5).Count();
                options.TimeZone = TimeZoneInfo.Utc;
            });

            services.AddRestierApiVersioning(b => b
                .AddVersion<NorthwindApiV1>("api", restierServices =>
                {
                    restierServices
                        .AddEFCoreProviderServices<NorthwindContextV1>((sp, dbOptions) =>
                            dbOptions.UseInMemoryDatabase("Northwind-V1"));
                },
                opts => opts.SunsetDate = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero),
                bag =>
                {
                    bag.Validation.MaxAnyAllExpressionDepth = 3;
                    bag.Validation.MaxExpansionDepth = 3;
                })
                .AddVersion<NorthwindApiV2>("api", restierServices =>
                {
                    restierServices
                        .AddEFCoreProviderServices<NorthwindContextV2>((sp, dbOptions) =>
                            dbOptions.UseInMemoryDatabase("Northwind-V2"));
                },
                configureVersioning: null,
                configureOptions: bag =>
                {
                    bag.Validation.MaxAnyAllExpressionDepth = 3;
                    bag.Validation.MaxExpansionDepth = 3;
                }));

            services.AddRestierNSwag();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<Restier.AspNetCore.Middleware.ODataBatchHttpContextFixerMiddleware>();
            app.UseODataBatching();
            app.UseRouting();
            app.UseRestierVersionHeaders();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRestier();
            });

            app.UseRestierOpenApi();
            app.UseRestierReDoc();
            app.UseRestierNSwagUI();
        }

    }

}
