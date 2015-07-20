// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.OData.Client;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Models;
using Xunit;

namespace Microsoft.Restier.WebApi.Test.Scenario
{
    public class UrlConventionsTests : E2ETestBase<TrippinModel>
    {
        public UrlConventionsTests()
            : base(new Uri("http://localhost:18384/api/Trippin/"))
        {
        }

        [Fact]
        public void ServiceRoot()
        {
            TestGetPayloadContains(string.Empty, "http://localhost:18384/api/Trippin/$metadata");
        }

        [Fact]
        public void Metadata()
        {
            TestGetPayloadContains("$metadata", "<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\">");
        }

        [Fact]
        public void AddressingEntitySet()
        {
            TestGetPayloadContains("People", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void AddressingSingleton()
        {
            TestGetPayloadContains("Me", "http://localhost:18384/api/Trippin/$metadata#Me");
        }

        [Fact]
        public void AddressingSingleEntity()
        {
            TestGetPayloadContains("People(1)", "http://localhost:18384/api/Trippin/$metadata#People/$entity");
        }

        [Fact]
        public void AddressingSingleEntityWithKeyName()
        {
            TestGetPayloadContains("People(PersonId=1)", "http://localhost:18384/api/Trippin/$metadata#People/$entity");
        }

        [Fact]
        public void AddressingNavigationProperty()
        {
            TestGetPayloadContains("People(1)/Trips", "http://localhost:18384/api/Trippin/$metadata#Trips");
        }

        [Fact]
        public void AddressingEntityReference()
        {
            TestGetPayloadContains("People(1)/Trips/$ref", "http://localhost:18384/api/Trippin/$metadata#Collection($ref)");
        }

        [Fact]
        public void AddressingBoundFunction()
        {
            TestGetPayloadContains("People(1)/Microsoft.Restier.WebApi.Test.Services.Trippin.Models.GetNumberOfFriends", "http://localhost:18384/api/Trippin/$metadata#Edm.Int32");
        }

        [Fact]
        public void AddressingFunctionImport()
        {
            TestGetPayloadContains("GetPersonWithMostFriends", "http://localhost:18384/api/Trippin/$metadata#People/$entity");
        }

        [Fact]
        public void AddressingFunctionImportWithArgument()
        {
            TestGetPayloadContains("GetPeopleWithFriendsAtLeast(1)", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact(Skip = "GitHub#125")]
        public void AddressingFunctionImportWithNamedArgument()
        {
            TestGetPayloadContains("GetPeopleWithFriendsAtLeast(n=1)", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact(Skip = "GitHub#125")]
        public void AddressingFunctionImportUsingParameterAlias()
        {
            TestGetPayloadContains("GetPeopleWithFriendsAtLeast(n=@n)?@n=1", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void AddressingBoundAction()
        {
            TestPostPayloadContains("Trips(1)/Microsoft.Restier.WebApi.Test.Services.Trippin.Models.EndTrip", "http://localhost:18384/api/Trippin/$metadata#Trips/$entity");
        }

        [Fact]
        public void AddressingBoundActionWithParenthesis()
        {
            TestPostPayloadContains("Trips(1)/Microsoft.Restier.WebApi.Test.Services.Trippin.Models.EndTrip()", "http://localhost:18384/api/Trippin/$metadata#Trips/$entity");
        }

        [Fact]
        public void AddressingActionImport()
        {
            TestPostStatusCodeIs("CleanUpExpiredTrips", 204);
        }

        [Fact]
        public void AddressingActionImportWithParenthesis()
        {
            TestPostStatusCodeIs("CleanUpExpiredTrips()", 204);
        }

        private void TestGetPayloadContains(string uriStringAfterServiceRoot, string expectedSubString)
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "GET",
                    new Uri(this.ServiceBaseUri.OriginalString + uriStringAfterServiceRoot, UriKind.Absolute),
                    useDefaultCredentials: true,
                    usePostTunneling: false,
                    headers: new Dictionary<string, string>()));
            using (var r = new StreamReader(requestMessage.GetResponse().GetStream()))
            {
                var payloadString = r.ReadToEnd();
                Assert.Contains(expectedSubString, payloadString);
            }
        }

        private void TestPostPayloadContains(string uriStringAfterServiceRoot, string expectedSubString)
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "POST",
                    new Uri(this.ServiceBaseUri.OriginalString + uriStringAfterServiceRoot, UriKind.Absolute),
                    useDefaultCredentials: true,
                    usePostTunneling: false,
                    headers: new Dictionary<string, string>() { { "Content-Length", "0" } }));
            using (var r = new StreamReader(requestMessage.GetResponse().GetStream()))
            {
                var payloadString = r.ReadToEnd();
                Assert.Contains(expectedSubString, payloadString);
            }
        }

        private void TestPostStatusCodeIs(string uriStringAfterServiceRoot, int statusCode)
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "POST",
                    new Uri(this.ServiceBaseUri.OriginalString + uriStringAfterServiceRoot, UriKind.Absolute),
                    useDefaultCredentials: true,
                    usePostTunneling: false,
                    headers: new Dictionary<string, string>() { { "Content-Length", "0" } }));
            Assert.Equal(204, requestMessage.GetResponse().StatusCode);
        }

    }
}
