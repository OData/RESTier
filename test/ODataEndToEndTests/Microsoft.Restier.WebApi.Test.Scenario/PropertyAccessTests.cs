using System;
using System.Collections.Generic;
using System.IO;
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

        private void TestPayloadString(string uriAfterServiceRoot, Action<string> testMethod)
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "GET",
                    new Uri(this.ServiceBaseUri.OriginalString + uriAfterServiceRoot, UriKind.Absolute),
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
