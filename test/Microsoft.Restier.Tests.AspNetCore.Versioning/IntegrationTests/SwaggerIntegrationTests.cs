// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.IntegrationTests
{

    [TestClass]
    public class SwaggerIntegrationTests
    {

        public TestContext TestContext { get; set; }


        [TestMethod]
        public async Task SwaggerJson_AtVersionGroupName_ReturnsCorrectVersionedDoc()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await BuildAsync(cancellationToken);
            var client = host.GetTestClient();

            var v1Json = await client.GetStringAsync("/swagger/v1/swagger.json", cancellationToken);
            JsonDocument.Parse(v1Json).RootElement.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/Items"));

            var v2Json = await client.GetStringAsync("/swagger/v2/swagger.json", cancellationToken);
            JsonDocument.Parse(v2Json).RootElement.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/AuditLogs"));
        }

        private static async Task<IHost> BuildAsync(CancellationToken cancellationToken)
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
                        services.AddRestierSwagger();
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
                        app.UseRestierSwaggerUI();
                    }));

            return await builder.StartAsync(cancellationToken);
        }

    }

}
