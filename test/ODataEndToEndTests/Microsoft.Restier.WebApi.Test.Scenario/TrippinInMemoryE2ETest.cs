// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Models;
using Xunit;
namespace Microsoft.Restier.WebApi.Test.Scenario
{
    public class TrippinInMemoryE2ETest : E2ETestBase<TrippinModel>, IClassFixture<TrippinServiceFixture>
    {
        private const string baseUri = "http://localhost:21248/api/Trippin/";

        public TrippinInMemoryE2ETest()
            : base(new Uri(baseUri))
        {
        }

        [Fact]
        public void TestMetadata()
        {
            TestGetPayloadContains("$metadata", "<EntitySet Name=\"People\"");
        }

        [Fact(Skip = "source issue")]
        public void TestEntitySet()
        {
            TestGetPayloadContains("People", "FirstName");
        }
    }
}
