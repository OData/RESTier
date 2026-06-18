// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.EntityFrameworkCore.Spatial;
using Microsoft.Restier.Samples.Postgres.AspNetCore.Controllers;
using Microsoft.Restier.Samples.Postgres.AspNetCore.Models;
using System;

namespace Microsoft.Restier.Samples.Postgres.AspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddControllers()
                .AddRestier(options =>
                {
                    options.Select().Expand().Filter().OrderBy().SetMaxTop(5).Count();
                    options.TimeZone = TimeZoneInfo.Utc;

                    options.AddRestierRoute<RestierTestContextApi>("v3", restierServices =>
                    {
                        var connectionString = builder.Configuration.GetConnectionString(nameof(RestierTestContext));
                        restierServices
                            .AddEFCoreProviderServices<RestierTestContext>(dbOptions =>
                                dbOptions.UseNpgsql(connectionString, o => o.UseNetTopologySuite()))
                            .AddRestierSpatial();
                    },
                    bag =>
                    {
                        bag.Validation.MaxAnyAllExpressionDepth = 3;
                        bag.Validation.MaxExpansionDepth = 3;
                    });
                })
                .AddApplicationPart(typeof(RestierTestContextApi).Assembly)
                .AddApplicationPart(typeof(RestierController).Assembly);

            builder.Services.AddRestierNSwag();

            var app = builder.Build();

            // Apply pending migrations and seed data on startup.
            var optionsBuilder = new DbContextOptionsBuilder<RestierTestContext>();
            optionsBuilder.UseNpgsql(app.Configuration.GetConnectionString(nameof(RestierTestContext)), o => o.UseNetTopologySuite());
            using (var db = new RestierTestContext(optionsBuilder.Options))
            {
                db.Database.Migrate();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseODataRouteDebug();
            app.UseRouting();
            app.UseAuthorization();

#pragma warning disable ASP0014 // Suggest using top level route registrations
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRestier();
            });
#pragma warning restore ASP0014 // Suggest using top level route registrations

            app.UseRestierOpenApi();
            app.UseRestierReDoc();

            app.Run();
        }
    }
}
