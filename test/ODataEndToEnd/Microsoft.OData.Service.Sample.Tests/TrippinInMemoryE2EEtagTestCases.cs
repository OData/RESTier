// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.OData.Service.Sample.Tests
{
    public class TrippinInMemoryE2EEtagTestCases : TrippinInMemoryE2ETestBase
    {
        [Fact]
        public void EtagAnnotationTesting()
        {
            TestGetPayloadContains("Airlines", "@odata.etag");
            this.TestGetPayloadContains("Airlines('AA')", "@odata.etag");
        }

        // If-Match ann If-Not-Match need response with session Id.
        // TODO After get session id from client, add If-Match and If-Not-Match test cases here.
    }
}