// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Restier.Tests.AspNetCore.Versioning.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.IntegrationTests
{

    [TestClass]
    public class VersionedBatchTests
    {

        public TestContext TestContext { get; set; }


        [TestMethod]
        public async Task BatchToV1_RoutesV1InnerRequest()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var batch = BuildBatch("GET /api/v1/Items HTTP/1.1");
            var response = await client.SendAsync(batch, cancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            body.Should().NotContain("AuditLogs", "V1 batch must not see V2-only entity set");
        }

        [TestMethod]
        public async Task BatchToV2_RoutesV2InnerRequest()
        {
            var cancellationToken = TestContext.CancellationTokenSource.Token;
            using var host = await VersionedApiFixture.BuildHostAsync(cancellationToken);
            var client = host.GetTestClient();

            var batch = BuildBatch("GET /api/v2/AuditLogs HTTP/1.1");
            var response = await client.SendAsync(batch, cancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private static HttpRequestMessage BuildBatch(string innerRequestLine)
        {
            const string boundary = "batch_test";
            var body = new StringBuilder();
            body.Append($"--{boundary}\r\n");
            body.Append("Content-Type: application/http\r\n");
            body.Append("Content-Transfer-Encoding: binary\r\n\r\n");
            body.Append($"{innerRequestLine}\r\n");
            body.Append("Host: localhost\r\n\r\n");
            body.Append($"--{boundary}--\r\n");

            // The $batch endpoint is at the per-route prefix, not at the version.
            // Decide which version to target based on the inner path; v1 → /api/v1/$batch.
            var batchUrl = innerRequestLine.Contains("/api/v1/") ? "/api/v1/$batch" : "/api/v2/$batch";

            var content = new StringContent(body.ToString(), Encoding.UTF8);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse($"multipart/mixed; boundary={boundary}");

            return new HttpRequestMessage(HttpMethod.Post, batchUrl) { Content = content };
        }

    }

}
