
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core;
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

            // Add services to the container.

            builder.Services
                .AddRestier(
                    restierBuilder =>
                    {
                        // This delegate is executed after OData is added to the container.
                        // Add you replacement services here.
                        restierBuilder.AddRestierApi<RestierTestContextApi>(routeServices =>
                        {
                            routeServices
                                .AddEFCoreProviderServices<RestierTestContext>((services, options) =>
                                    options.UseNpgsql(builder.Configuration.GetConnectionString(nameof(RestierTestContext))))
                                .AddSingleton(new ODataValidationSettings
                                {
                                    MaxTop = 5,
                                    MaxAnyAllExpressionDepth = 3,
                                    MaxExpansionDepth = 3,
                                });
                        });

                    }, true);

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseRestierBatching();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();


#pragma warning disable ASP0014 // Suggest using top level route registrations
            app.UseEndpoints(endpoints =>
            {
                endpoints.Select().Expand().Filter().OrderBy().MaxTop(100).Count().SetTimeZoneInfo(TimeZoneInfo.Utc);

                endpoints.MapRestier(builder =>
                {
                    builder.MapApiRoute<RestierTestContextApi>("ApiV3", "/v3", true);
                });

            });
#pragma warning restore ASP0014 // Suggest using top level route registrations

            app.Run();
        }
    }
}
