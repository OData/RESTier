// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.AspNetCore.Swagger.IntegrationTests
{
    /// <summary>
    /// Regression for issue #766: an UnboundOperation with a facet-bearing primitive
    /// parameter (string, decimal, byte[], DateTimeOffset) caused an InvalidCastException
    /// in Microsoft.OpenApi.OData's schema generator, because RESTier emitted a bare
    /// <c>EdmPrimitiveTypeReference</c> instead of the specific <c>EdmStringTypeReference</c>,
    /// <c>EdmDecimalTypeReference</c>, etc. The OpenAPI generator hard-casts to those
    /// interfaces and failed at request time, breaking the entire Swagger document.
    /// </summary>
    public class Issue766_PrimitiveParamOperationTests
    {

        public class PrimitiveParamApi : ApiBase
        {
            public PrimitiveParamApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }

            [UnboundOperation]
            public string EchoString(string code) => code;

            [UnboundOperation]
            public decimal EchoDecimal(decimal amount) => amount;

            [UnboundOperation]
            public byte[] EchoBinary(byte[] payload) => payload;

            [UnboundOperation]
            public DateTimeOffset EchoTimestamp(DateTimeOffset at) => at;
        }

        [Fact]
        public async Task SwaggerDoc_WithFacetBearingPrimitiveParams_GeneratesSuccessfully()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(web => web
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services
                            .AddControllers()
                            .AddRestier(options =>
                            {
                                options.AddRestierRoute<PrimitiveParamApi>("", _ => { });
                            });
                        services.AddRestierSwagger();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints => endpoints.MapRestier());
                        app.UseRestierSwaggerUI();
                    }));

            using var host = await builder.StartAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/swagger/default/swagger.json", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            response.IsSuccessStatusCode.Should().BeTrue(
                $"Swagger doc generation must not throw InvalidCastException for facet-bearing primitive parameters. Body: {body}");
        }

    }
}
