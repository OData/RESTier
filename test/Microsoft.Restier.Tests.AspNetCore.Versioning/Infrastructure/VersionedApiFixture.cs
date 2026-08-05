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
using Microsoft.AspNetCore.OData.Query.Validator;
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

    [ApiVersion("1.0", Deprecated = true)]
    public class SampleApiV1 : ApiBase
    {
        public SampleApiV1(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }

        [Resource]
        public IQueryable<SampleEntity> Items => Enumerable.Empty<SampleEntity>().AsQueryable();
    }

    [ApiVersion("2.0")]
    public class SampleApiV2 : ApiBase
    {
        public SampleApiV2(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }

        [Resource]
        public IQueryable<SampleEntity> Items => Enumerable.Empty<SampleEntity>().AsQueryable();

        // V2-only entity set
        [Resource]
        public IQueryable<SampleAuditLog> AuditLogs => Enumerable.Empty<SampleAuditLog>().AsQueryable();
    }

    public class SampleEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SampleAuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; }
    }

    public class SampleV1ModelBuilder : IModelBuilder
    {
        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel()
        {
            var b = new ODataConventionModelBuilder();
            b.EntitySet<SampleEntity>(nameof(SampleApiV1.Items));
            return b.GetEdmModel();
        }
    }

    public class SampleV2ModelBuilder : IModelBuilder
    {
        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel()
        {
            var b = new ODataConventionModelBuilder();
            b.EntitySet<SampleEntity>(nameof(SampleApiV2.Items));
            b.EntitySet<SampleAuditLog>(nameof(SampleApiV2.AuditLogs));
            return b.GetEdmModel();
        }
    }

    public static class VersionedApiFixture
    {

        public static async Task<IHost> BuildHostAsync(CancellationToken cancellationToken)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddApiVersioning(o =>
                        {
                            o.DefaultApiVersion = new ApiVersion(2, 0);
                            o.ReportApiVersions = true;
                            o.ApiVersionReader = new UrlSegmentApiVersionReader();
                        }).AddApiExplorer();

                        services.AddControllers()
                            .AddRestier(options =>
                            {
                                options.Select().Expand().Filter().OrderBy().Count();
                            })
                            .AddApplicationPart(typeof(RestierController).Assembly);

                        services.AddRestierApiVersioning(b => b
                            .AddVersion<SampleApiV1>("api", svc =>
                            {
                                svc.AddSingleton<IChainedService<IModelBuilder>, SampleV1ModelBuilder>();
                                svc.AddSingleton<IChangeSetInitializer, DefaultChangeSetInitializer>();
                                svc.AddSingleton<ISubmitExecutor, DefaultSubmitExecutor>();
                            })
                            .AddVersion<SampleApiV2>("api", svc =>
                            {
                                svc.AddSingleton<IChainedService<IModelBuilder>, SampleV2ModelBuilder>();
                                svc.AddSingleton<IChangeSetInitializer, DefaultChangeSetInitializer>();
                                svc.AddSingleton<ISubmitExecutor, DefaultSubmitExecutor>();
                            }));
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
