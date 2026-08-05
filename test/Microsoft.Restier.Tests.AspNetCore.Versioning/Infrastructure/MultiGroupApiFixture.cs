// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure
{

    [ApiVersion("1.0")]
    [ApiVersion("2.0", Deprecated = true)]
    public class OrdersApi : ApiBase
    {
        public OrdersApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler) { }

        [Resource]
        public IQueryable<SampleEntity> Orders => Enumerable.Empty<SampleEntity>().AsQueryable();
    }

    [ApiVersion("1.0")]
    public class InventoryApi : ApiBase
    {
        public InventoryApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler) { }

        [Resource]
        public IQueryable<SampleEntity> Stock => Enumerable.Empty<SampleEntity>().AsQueryable();
    }

    public class OrdersModelBuilder : IModelBuilder
    {
        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel()
        {
            var b = new ODataConventionModelBuilder();
            b.EntitySet<SampleEntity>(nameof(OrdersApi.Orders));
            return b.GetEdmModel();
        }
    }

    public class InventoryModelBuilder : IModelBuilder
    {
        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel()
        {
            var b = new ODataConventionModelBuilder();
            b.EntitySet<SampleEntity>(nameof(InventoryApi.Stock));
            return b.GetEdmModel();
        }
    }

    public static class MultiGroupApiFixture
    {

        public static async Task<IHost> BuildHostAsync(CancellationToken cancellationToken, DateTimeOffset? ordersV2Sunset = null)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddApiVersioning().AddApiExplorer();

                        services.AddControllers()
                            .AddRestier(options =>
                            {
                                options.Select().Expand().Filter().OrderBy().Count();
                            })
                            .AddApplicationPart(typeof(RestierController).Assembly);

                        services.AddRestierApiVersioning(b =>
                        {
                            if (ordersV2Sunset is { } sunset)
                            {
                                // Imperative path: register V1 and V2 explicitly so we can
                                // attach the sunset date to V2 without a duplicate registration
                                // from the [ApiVersion("2.0")] attribute path.
                                b.AddVersion<OrdersApi>(
                                    new ApiVersion(1, 0), deprecated: false, "orders",
                                    svc =>
                                    {
                                        svc.AddSingleton<IChainedService<IModelBuilder>, OrdersModelBuilder>();
                                        svc.AddSingleton<IChangeSetInitializer, DefaultChangeSetInitializer>();
                                        svc.AddSingleton<ISubmitExecutor, DefaultSubmitExecutor>();
                                    },
                                    opts => opts.GroupNameFormatter = v => $"orders-v{v.MajorVersion}");

                                b.AddVersion<OrdersApi>(
                                    new ApiVersion(2, 0), deprecated: true, "orders",
                                    svc =>
                                    {
                                        svc.AddSingleton<IChainedService<IModelBuilder>, OrdersModelBuilder>();
                                        svc.AddSingleton<IChangeSetInitializer, DefaultChangeSetInitializer>();
                                        svc.AddSingleton<ISubmitExecutor, DefaultSubmitExecutor>();
                                    },
                                    opts =>
                                    {
                                        opts.GroupNameFormatter = v => $"orders-v{v.MajorVersion}";
                                        opts.SunsetDate = sunset;
                                    });
                            }
                            else
                            {
                                // Attribute-driven path: both V1 and V2 come from [ApiVersion] on OrdersApi.
                                b.AddVersion<OrdersApi>("orders", svc =>
                                {
                                    svc.AddSingleton<IChainedService<IModelBuilder>, OrdersModelBuilder>();
                                    svc.AddSingleton<IChangeSetInitializer, DefaultChangeSetInitializer>();
                                    svc.AddSingleton<ISubmitExecutor, DefaultSubmitExecutor>();
                                },
                                opts => opts.GroupNameFormatter = v => $"orders-v{v.MajorVersion}");
                            }

                            b.AddVersion<InventoryApi>("inventory", svc =>
                            {
                                svc.AddSingleton<IChainedService<IModelBuilder>, InventoryModelBuilder>();
                                svc.AddSingleton<IChangeSetInitializer, DefaultChangeSetInitializer>();
                                svc.AddSingleton<ISubmitExecutor, DefaultSubmitExecutor>();
                            },
                            opts => opts.GroupNameFormatter = v => $"inventory-v{v.MajorVersion}");
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseRestierVersionHeaders();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapRestier();
                        });
                    }));

            return await builder.StartAsync(cancellationToken);
        }

    }

}
