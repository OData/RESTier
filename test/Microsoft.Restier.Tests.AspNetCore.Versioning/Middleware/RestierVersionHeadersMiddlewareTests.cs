// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Asp.Versioning;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.AspNetCore.Versioning.Middleware;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Middleware
{

    /// <summary>
    /// Unit-level coverage for the path-matching logic. Header-emission behavior (group isolation,
    /// sunset, "do not overwrite") is exercised by integration tests in
    /// <c>VersionHeadersIntegrationTests</c> because it depends on <c>HttpResponse.OnStarting</c>
    /// callbacks firing, which only happens through a real <c>TestServer</c>.
    /// </summary>
    [TestClass]
    public class RestierVersionHeadersMiddlewareTests
    {

        [TestMethod]
        public void TryMatch_NoDescriptors_ReturnsNull()
        {
            var registry = new RestierApiVersionRegistry();
            RestierVersionHeadersMiddleware.TryMatch(registry, new PathString("/api/v1/x"))
                .Should().BeNull();
        }

        [TestMethod]
        public void TryMatch_NoPrefixMatch_ReturnsNull()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            RestierVersionHeadersMiddleware.TryMatch(registry, new PathString("/unrelated/path"))
                .Should().BeNull();
        }

        [TestMethod]
        public void TryMatch_ExactPrefix_Matches()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            RestierVersionHeadersMiddleware.TryMatch(registry, new PathString("/api/v1"))
                .Should().NotBeNull();
        }

        [TestMethod]
        public void TryMatch_PrefixWithTrailing_Matches()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            RestierVersionHeadersMiddleware.TryMatch(registry, new PathString("/api/v1/Customers"))
                .Should().NotBeNull();
        }

        [TestMethod]
        public void TryMatch_LookalikePrefix_DoesNotMatch()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            RestierVersionHeadersMiddleware.TryMatch(registry, new PathString("/api/v10/anything"))
                .Should().BeNull();
        }

        [TestMethod]
        public void TryMatch_LongestPrefixWins()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api", typeof(SampleApi), false, "default", null);
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);

            var match = RestierVersionHeadersMiddleware.TryMatch(registry, new PathString("/api/v1/x"));
            match.Should().NotBeNull();
            match.RoutePrefix.Should().Be("api/v1");
        }

        private class SampleApi { }

    }

}
