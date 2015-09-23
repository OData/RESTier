// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.OData.Client;
using Xunit;

namespace Microsoft.Restier.WebApi.Test.Scenario
{
    public class PropertyAccessTests : TrippinE2ETestBase
    {
        [Fact]
        public void QueryIntPropertyOfSingleEntity()
        {
            TestPayloadString("People(1)/PersonId", payloadStr =>
            {
                Assert.Contains(
                    "\"@odata.context\":\"http://localhost:18384/api/Trippin/$metadata#People(1)/PersonId\"," +
                    "\"value\":1", payloadStr, StringComparison.Ordinal);
            });
        }

        [Fact]
        public void QueryRawIntPropertyOfSingleEntity()
        {
            TestPayloadString("People(1)/PersonId/$value", payloadStr =>
            {
                Assert.Equal("1", payloadStr, StringComparer.Ordinal);
            });
        }

        [Fact]
        public void QueryStringPropertyOfSingleEntity()
        {
            TestPayloadString("People(1)/UserName", payloadStr =>
            {
                Assert.Contains(
                    "\"@odata.context\":\"http://localhost:18384/api/Trippin/$metadata#People(1)/UserName\"," +
                    "\"value\":\"russellwhyte\"", payloadStr, StringComparison.Ordinal);
            });
        }

        [Fact]
        public void QueryRawStringPropertyOfSingleEntity()
        {
            TestPayloadString("People(1)/UserName/$value", payloadStr =>
            {
                Assert.Equal("russellwhyte", payloadStr, StringComparer.Ordinal);
            });
        }

        [Fact]
        public void QueryDatePropertyOfSingleEntity()
        {
            TestPayloadString("People(1)/BirthDate", payloadStr =>
            {
                Assert.Contains(
                    "\"@odata.context\":\"http://localhost:18384/api/Trippin/$metadata#People(1)/BirthDate\"," +
                    "\"value\":\"1980-10-15\"", payloadStr, StringComparison.Ordinal);
            });
        }

        [Fact]
        public void QueryRawDatePropertyOfSingleEntity()
        {
            TestPayloadString("People(1)/BirthDate/$value", payloadStr =>
            {
                Assert.Equal("1980-10-15", payloadStr, StringComparer.Ordinal);
            });
        }

        [Fact]
        public void QueryComplexPropertyOfSingleEntity()
        {
            var firstEvent = this.TestClientContext.Events.First();
            TestPayloadString("Events(" + firstEvent.Id + ")/OccursAt", payloadStr =>
            {
                Assert.Contains(
                    "\"@odata.context\":\"http://localhost:18384/api/Trippin/$metadata#Events(" + firstEvent.Id + ")/OccursAt\"," +
                    "\"Address\":\"Address1\"", payloadStr, StringComparison.Ordinal);
            });
        }

        [Fact]
        public void QueryPrimitivePropertyUnderComplexPropertyOfSingleEntity()
        {
            var firstEvent = this.TestClientContext.Events.First();
            TestPayloadString("Events(" + firstEvent.Id + ")/OccursAt/Address", payloadStr =>
            {
                Assert.Contains(
                    "\"@odata.context\":\"http://localhost:18384/api/Trippin/$metadata#Events(" + firstEvent.Id + ")/OccursAt/Address\"," +
                    "\"value\":\"Address1\"", payloadStr, StringComparison.Ordinal);
            });
        }

        [Fact]
        public void QueryRawPrimitivePropertyUnderComplexPropertyOfSingleEntity()
        {
            var firstEvent = this.TestClientContext.Events.First();
            TestPayloadString("Events(" + firstEvent.Id + ")/OccursAt/Address/$value", payloadStr =>
            {
                Assert.Equal("Address1", payloadStr, StringComparer.Ordinal);
            });
        }

        private void TestPayloadString(string uriAfterServiceRoot, Action<string> testMethod)
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "GET",
                    new Uri(this.ServiceBaseUri, uriAfterServiceRoot),
                    true,
                    false,
                    new Dictionary<string, string>()));
            using (var r = new StreamReader(requestMessage.GetResponse().GetStream()))
            {
                var payloadStr = r.ReadToEnd();
                testMethod(payloadStr);
            }
        }
    }
}
