// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Tests.AspNetCore.Swagger.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.Swagger.IntegrationTests
{

    /// <summary>
    /// Verifies that a Restier keyless-view function import (unbound EDM function
    /// returning <c>Collection(ComplexType)</c>) surfaces in the Swagger-flavour
    /// OpenAPI document served at <c>/swagger/{name}/swagger.json</c>:
    ///   * the function-import path exists;
    ///   * the complex-type schema component exists.
    /// </summary>
    [TestClass]
    public class KeylessViewOpenApiTests
    {

        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task SwaggerDoc_ContainsKeylessViewFunctionImportPathAndComplexTypeSchema()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await BuildAsync(cancellationToken);
            var client = host.GetTestClient();

            var json = await client.GetStringAsync("/swagger/default/swagger.json", cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            root.GetProperty("paths").EnumerateObject()
                .Should().Contain(p => p.Name.Contains("/TestViews", StringComparison.OrdinalIgnoreCase),
                    "the OpenAPI doc must include the keyless-view function-import path");

            // Schemas live under "components.schemas" for OpenAPI 3.0 (which is what the
            // Restier middleware emits) and "definitions" for Swagger 2.0. Cover both
            // so the assertion still holds if the serializer version is ever changed.
            JsonElement schemasContainer;
            if (root.TryGetProperty("components", out var components)
                && components.TryGetProperty("schemas", out var schemas))
            {
                schemasContainer = schemas;
            }
            else
            {
                schemasContainer = root.GetProperty("definitions");
            }

            schemasContainer.EnumerateObject()
                .Should().Contain(p => p.Name.Contains("KeylessViewTestRow", StringComparison.Ordinal),
                    "the OpenAPI doc must include the complex-type schema");
        }

        private static async Task<IHost> BuildAsync(CancellationToken cancellationToken)
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
                                options.AddRestierRoute<KeylessViewTestApi>("", restierServices =>
                                {
                                    restierServices.AddSingleton<IChainedService<IModelBuilder>, KeylessViewTestApiModelBuilder>();
                                });
                            });

                        services.AddRestierSwagger();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapRestier());
                        app.UseRestierSwaggerUI();
                    }));

            return await builder.StartAsync(cancellationToken);
        }

    }

}
