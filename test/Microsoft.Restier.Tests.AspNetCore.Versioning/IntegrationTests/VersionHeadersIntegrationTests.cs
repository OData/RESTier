// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.IntegrationTests
{

    [TestClass]
    public class VersionHeadersIntegrationTests
    {

        public TestContext TestContext { get; set; }


        [TestMethod]
        public async Task V1Response_CarriesSupportedAndDeprecatedVersionsHeaders()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/api/v1/Items", cancellationToken);

            response.Headers.GetValues("api-supported-versions").Single().Should().Be("1.0, 2.0");
            response.Headers.GetValues("api-deprecated-versions").Single().Should().Be("1.0");
        }

        [TestMethod]
        public async Task V2Response_CarriesSupportedHeader_AndDeprecatedHeader()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/api/v2/Items", cancellationToken);

            response.Headers.GetValues("api-supported-versions").Single().Should().Be("1.0, 2.0");
            response.Headers.GetValues("api-deprecated-versions").Single().Should().Be("1.0");
        }

        [TestMethod]
        public async Task UnrelatedPath_DoesNotCarryHeaders()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var response = await client.GetAsync("/some/unrelated/path", cancellationToken);

            response.Headers.Contains("api-supported-versions").Should().BeFalse();
        }

        [TestMethod]
        public async Task GroupIsolation_OrdersHeadersDoNotIncludeInventoryVersions()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await MultiGroupApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var ordersResponse = await client.GetAsync("/orders/v1/Orders", cancellationToken);
            ordersResponse.Headers.GetValues("api-supported-versions").Single().Should().Be("1.0, 2.0");

            var inventoryResponse = await client.GetAsync("/inventory/v1/Stock", cancellationToken);
            inventoryResponse.Headers.GetValues("api-supported-versions").Single().Should().Be("1.0");
        }

        [TestMethod]
        public async Task SunsetHeader_OnlyEmittedForVersionWithSunsetConfigured()
        {
            var sunset = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await MultiGroupApiFixture.BuildHostAsync(cancellationToken, ordersV2Sunset: sunset);
            var client = host.GetTestClient();

            var v1Response = await client.GetAsync("/orders/v1/Orders", cancellationToken);
            v1Response.Headers.Contains("Sunset").Should().BeFalse();

            var v2Response = await client.GetAsync("/orders/v2/Orders", cancellationToken);
            v2Response.Headers.GetValues("Sunset").Single()
                .Should().Be("Fri, 01 Jan 2027 00:00:00 GMT");
        }

    }

}
