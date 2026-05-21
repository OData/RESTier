// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.AspNetCore.NSwag.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSwag.AspNetCore;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag.IntegrationTests
{

    [TestClass]
    public class CombinedAppTests
    {

        public TestContext TestContext { get; set; }


        [TestMethod]
        public async Task RestierDocAndControllersDoc_AreIsolated()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await BuildAsync(cancellationToken);
            var client = host.GetTestClient();

            // Restier doc contains Restier paths (e.g., /Items), not the plain controller's path.
            var restierJson = await client.GetStringAsync("/openapi/default/openapi.json", cancellationToken);
            var restierRoot = JsonDocument.Parse(restierJson).RootElement;
            restierRoot.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/Items", System.StringComparison.OrdinalIgnoreCase),
                    "Restier doc must include the Items entity-set paths");
            restierJson.Should().NotContain("/health/live",
                "Restier doc must NOT contain plain MVC controller paths");

            // User's controllers doc contains the plain controller, not RestierController.
            var controllersJson = await client.GetStringAsync("/swagger/controllers/swagger.json", cancellationToken);
            controllersJson.Should().Contain("/health/live",
                "controllers doc must contain the plain HealthController path");
            controllersJson.Should().NotContain("RestierController",
                "RestierController must be filtered out of the controllers doc by the ApiExplorer convention");
        }

        [TestMethod]
        public async Task RestierDocs_AreNotInNSwagRegistry()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await BuildAsync(cancellationToken);
            var client = host.GetTestClient();

            // NSwag's default path for a doc named "default" would be /swagger/default/swagger.json.
            // Restier docs are not in NSwag's registry, so this must 404.
            var response = await client.GetAsync("/swagger/default/swagger.json", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "Restier docs must not be exposed via NSwag's default /swagger/{name}/swagger.json path");
        }

        private static async Task<IHost> BuildAsync(System.Threading.CancellationToken cancellationToken)
        {
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services
                            .AddControllers()
                            .AddRestier(options =>
                            {
                                options.AddRestierRoute<TestApi>("", restierServices =>
                                {
                                    restierServices.AddSingleton<IChainedService<IModelBuilder>, TestApiModelBuilder>();
                                });
                            })
                            .AddApplicationPart(typeof(HealthController).Assembly);

                        services.AddRestierNSwag();
                        services.AddOpenApiDocument(c => c.DocumentName = "controllers");
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                            endpoints.MapRestier();
                        });
                        app.UseRestierOpenApi();
                        app.UseRestierReDoc();
                        app.UseRestierNSwagUI();
                        app.UseOpenApi();
                    }));

            return await builder.StartAsync(cancellationToken);
        }

    }

    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {

        [HttpGet("live")]
        public IActionResult Live() => Ok(new { status = "ok" });

    }

}
