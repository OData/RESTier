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

        [Fact]
        public void TestEntitySet()
        {
            TestGetPayloadContains("People", "FirstName");
        }

        [Fact]
        public void TestImperativeViewEntitySet()
        {
            TestGetPayloadContains("NewComePeople", "FirstName");
        }

        [Fact]
        public void TestCollectionOfPrimitivePropertyAccess()
        {
            TestGetPayloadContains("People(1)/Emails",
                "\"@odata.context\":\"http://localhost:21248/api/Trippin/$metadata#Collection(Edm.String)\"");
        }

        [Fact]
        public void TestCollectionOfComplexPropertyAccess()
        {
            TestGetPayloadContains("People(1)/Locations",
                "\"@odata.context\":\"http://localhost:21248/api/Trippin/$metadata#Collection(" +
                "Microsoft.Restier.WebApi.Test.Services.TrippinInMemory.Location)\"");
        }

        [Fact]
        public void TestCollectionOfEnumPropertyAccess()
        {
            TestGetPayloadContains("People(1)/Colors",
                "\"@odata.context\":\"http://localhost:21248/api/Trippin/$metadata#Collection(" +
                "Microsoft.Restier.WebApi.Test.Services.TrippinInMemory.Color)\"");
        }

        [Fact]
        public void TestEnumPropertyAccess()
        {
            TestGetPayloadContains("People(1)/FavoriteColor",
                "\"@odata.context\":\"http://localhost:21248/api/Trippin/$metadata#People(1)/FavoriteColor");
        }

        [Fact]
        public void TestRawValuedEnumPropertyAccess()
        {
            TestGetPayloadIs("People(1)/FavoriteColor/$value", "Red");
        }
    }
}
