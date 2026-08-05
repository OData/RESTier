// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.AspNetCore.NSwag.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag.Extensions
{

    [TestClass]
    public class IApplicationBuilderExtensionsTests
    {

        public TestContext TestContext { get; set; }


        [TestMethod]
        public async Task UseRestierOpenApi_ServesEachRegisteredRouteUnderItsName()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await BuildHostAsync(routes: new[] { ("", typeof(TestApi)), ("v3", typeof(TestApi)) }, cancellationToken);
            var client = host.GetTestClient();

            foreach (var (urlPath, expectedServerSuffix) in new[]
            {
                ("/openapi/default/openapi.json", string.Empty),
                ("/openapi/v3/openapi.json", "/v3"),
            })
            {
                var response = await client.GetAsync(urlPath, cancellationToken);
                response.StatusCode.Should().Be(HttpStatusCode.OK, $"path {urlPath} must serve OpenAPI");
                response.Content.Headers.ContentType?.MediaType.Should().Be("application/json", $"path {urlPath} must declare JSON content type");

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                root.GetProperty("openapi").GetString().Should().StartWith("3.", $"path {urlPath} must serve OpenAPI 3.x");
                root.GetProperty("paths").EnumerateObject()
                    .Should().Contain(p => p.Name.Contains("/Items", StringComparison.OrdinalIgnoreCase),
                        $"path {urlPath} must include the Items entity-set paths discovered from TestApi");

                var serverUrl = root.GetProperty("servers")[0].GetProperty("url").GetString();
                serverUrl.Should().EndWith(expectedServerSuffix, $"path {urlPath} server URL must reflect the route prefix");
            }
        }

        [TestMethod]
        public async Task UseRestierOpenApi_ReturnsNotFound_ForUnknownDocumentName()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await BuildHostAsync(routes: new[] { ("", typeof(TestApi)) }, cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/openapi/nonexistent/openapi.json", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task UseRestierOpenApi_ReflectsInboundHostAndPathBase_InServiceRoot()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await BuildHostAsync(routes: new[] { ("v3", typeof(TestApi)) }, cancellationToken);
            var client = host.GetTestClient();
            client.DefaultRequestHeaders.Host = "example.com:8443";

            var response = await client.GetAsync("/openapi/v3/openapi.json", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var serverUrl = doc.RootElement.GetProperty("servers")[0].GetProperty("url").GetString();
            serverUrl.Should().Contain("example.com:8443", "ServiceRoot host must reflect the inbound Host header");
            serverUrl.Should().EndWith("/v3", "ServiceRoot must include the route prefix");
        }

        [TestMethod]
        public async Task AddRestierNSwag_InvokesOpenApiConvertSettingsCallback_OnEachRequest()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            var callbackInvocations = 0;
            using var host = await BuildHostAsync(
                routes: new[] { ("", typeof(TestApi)) },
                cancellationToken,
                configureServices: services =>
                {
                    services.AddRestierNSwag(settings =>
                    {
                        settings.TopExample = 42;
                        System.Threading.Interlocked.Increment(ref callbackInvocations);
                    });
                });
            var client = host.GetTestClient();

            var response = await client.GetAsync("/openapi/default/openapi.json", cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            callbackInvocations.Should().BeGreaterThan(0,
                "the OpenApiConvertSettings configurator must be invoked when generating the document");
        }

        [TestMethod]
        public async Task UseRestierReDoc_ServesOnePagePerRoutePrefix_PointingAtRestierMiddlewareUrl()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await BuildHostAsync(
                routes: new[] { ("", typeof(TestApi)), ("v3", typeof(TestApi)) },
                cancellationToken,
                configurePipeline: app =>
                {
                    app.UseRestierOpenApi();
                    app.UseRestierReDoc();
                });
            var client = host.GetTestClient();

            // NSwag's ReDoc middleware redirects /redoc/{name} -> /redoc/{name}/index.html?url={DocumentPath}.
            // The OpenAPI URL is conveyed via the redirect Location query string, not embedded in the HTML body
            // (the HTML extracts it from window.location.search at runtime).
            var defaultRedirect = await client.GetAsync("/redoc/default", cancellationToken);
            defaultRedirect.StatusCode.Should().Be(HttpStatusCode.Found,
                "ReDoc serves the page at /redoc/{name}/index.html and redirects /redoc/{name} to it");
            defaultRedirect.Headers.Location.Should().NotBeNull();
            defaultRedirect.Headers.Location!.OriginalString.Should().Contain("/openapi/default/openapi.json",
                "ReDoc must load Restier doc from the middleware URL");

            var defaultPage = await client.GetAsync(defaultRedirect.Headers.Location.OriginalString, cancellationToken);
            defaultPage.StatusCode.Should().Be(HttpStatusCode.OK);

            var v3Redirect = await client.GetAsync("/redoc/v3", cancellationToken);
            v3Redirect.StatusCode.Should().Be(HttpStatusCode.Found);
            v3Redirect.Headers.Location.Should().NotBeNull();
            v3Redirect.Headers.Location!.OriginalString.Should().Contain("/openapi/v3/openapi.json");

            var v3Page = await client.GetAsync(v3Redirect.Headers.Location.OriginalString, cancellationToken);
            v3Page.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [TestMethod]
        public async Task UseRestierNSwagUI_ListsAllRestierRoutes_AsSwaggerUrls()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await BuildHostAsync(
                routes: new[] { ("", typeof(TestApi)), ("v3", typeof(TestApi)) },
                cancellationToken,
                configurePipeline: app =>
                {
                    app.UseRestierOpenApi();
                    app.UseRestierNSwagUI();
                });
            var client = host.GetTestClient();

            // NSwag's Swagger UI 3 exposes its config (including the urls array) via /swagger/index.html.
            // The default landing /swagger may redirect to /swagger/index.html (similar to ReDoc).
            var indexResponse = await client.GetAsync("/swagger/index.html", cancellationToken);
            if (indexResponse.StatusCode == HttpStatusCode.Found)
            {
                // Follow the redirect manually if NSwag redirects.
                indexResponse = await client.GetAsync(indexResponse.Headers.Location!.OriginalString, cancellationToken);
            }
            indexResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await indexResponse.Content.ReadAsStringAsync(cancellationToken);
            body.Should().Contain("/openapi/default/openapi.json", "Swagger UI must reference the default Restier doc URL");
            body.Should().Contain("/openapi/v3/openapi.json", "Swagger UI must reference the v3 Restier doc URL");
        }

        [TestMethod]
        public async Task UseRestierNSwagUI_IncludesUserRegisteredNSwagDocuments_InDropdown()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await BuildHostAsync(
                routes: new[] { ("", typeof(TestApi)) },
                cancellationToken,
                configureServices: services =>
                {
                    services.AddRestierNSwag();
                    services.AddOpenApiDocument(c => c.DocumentName = "controllers");
                },
                configurePipeline: app =>
                {
                    app.UseRestierOpenApi();
                    app.UseRestierNSwagUI();
                    app.UseOpenApi();
                });
            var client = host.GetTestClient();

            var indexResponse = await client.GetAsync("/swagger/index.html", cancellationToken);
            indexResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await indexResponse.Content.ReadAsStringAsync(cancellationToken);
            body.Should().Contain("/openapi/default/openapi.json", "Restier doc must be in the dropdown");
            body.Should().Contain("/swagger/controllers/swagger.json", "User-registered NSwag doc must also be in the dropdown");
        }

        private static async Task<IHost> BuildHostAsync(
            (string prefix, Type apiType)[] routes,
            CancellationToken cancellationToken,
            Action<IServiceCollection> configureServices = null,
            Action<IApplicationBuilder> configurePipeline = null)
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
                                foreach (var (prefix, apiType) in routes)
                                {
                                    if (apiType == typeof(TestApi))
                                    {
                                        options.AddRestierRoute<TestApi>(prefix, restierServices =>
                                        {
                                            restierServices.AddSingleton<IChainedService<IModelBuilder>, TestApiModelBuilder>();
                                        });
                                    }
                                }
                            });
                        if (configureServices is not null)
                        {
                            configureServices(services);
                        }
                        else
                        {
                            services.AddRestierNSwag();
                        }
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapRestier());
                        if (configurePipeline is not null)
                        {
                            configurePipeline(app);
                        }
                        else
                        {
                            app.UseRestierOpenApi();
                        }
                    }));

            return await builder.StartAsync(cancellationToken);
        }

    }

}
