// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.OData.Service.Sample.Tests
{
    public class TrippinE2EQueryTestCases : TrippinE2ETestBase
    {
        [Theory]
        [InlineData("People", "#Microsoft.OData.Service.Sample.Trippin.Models.Employee")]
        [InlineData("People/Microsoft.OData.Service.Sample.Trippin.Models.Employee", "$metadata#People/Microsoft.OData.Service.Sample.Trippin.Models.Employee")]
        [InlineData("People(12)/Microsoft.OData.Service.Sample.Trippin.Models.Employee", "$metadata#People/Microsoft.OData.Service.Sample.Trippin.Models.Employee")]
        [InlineData("People/Microsoft.OData.Service.Sample.Trippin.Models.Employee(12)", "$metadata#People/Microsoft.OData.Service.Sample.Trippin.Models.Employee")]
        [InlineData("People(11)/Friends/Microsoft.OData.Service.Sample.Trippin.Models.Employee", "\"UserName\":\"salliesampson\"")]
        // Next call returns empty collection
        [InlineData("People(1)/Friends/Microsoft.OData.Service.Sample.Trippin.Models.Employee", "$metadata#People/Microsoft.OData.Service.Sample.Trippin.Models.Employee\",\"value\":[")]
        [InlineData("People(12)/Microsoft.OData.Service.Sample.Trippin.Models.Employee/UserName", "salliesampson")]
        [InlineData("People(12)/Microsoft.OData.Service.Sample.Trippin.Models.Employee/Cost", "1000000")]
        public void DerivedTypeQueryResponse(string uriStringAfterServiceRoot, string expectedSubString)
        {
            this.TestGetPayloadContains(uriStringAfterServiceRoot, expectedSubString);
        }

        [Theory]
        [InlineData("People(1)/Microsoft.OData.Service.Sample.Trippin.Models.Employee", 204)]
        [InlineData("People/Microsoft.OData.Service.Sample.Trippin.Models.Employee/UserName", 500)]
        [InlineData("People/Microsoft.OData.Service.Sample.Trippin.Models.Employee/Cost", 500)]
        public void DerivedTypeQueryStatus(string url, int expectedCode)
        {
            TestGetStatusCodeIs(url, expectedCode);
        }

        [Theory]
        [InlineData("People?$filter=Microsoft.OData.Service.Sample.Trippin.Models.Manager/Budget gt 1000", "\"UserName\":\"jonirosales\"")]
        [InlineData("People?$filter=Microsoft.OData.Service.Sample.Trippin.Models.Manager/BossOffice/Address eq 'ROOM 1001'", "\"UserName\":\"jonirosales\"")]
        [InlineData("People/Microsoft.OData.Service.Sample.Trippin.Models.Manager?$filter=Budget gt 1000", "\"UserName\":\"jonirosales\"")]
        [InlineData("People/Microsoft.OData.Service.Sample.Trippin.Models.Manager?$filter=BossOffice/Address eq 'ROOM 1001'", "\"UserName\":\"jonirosales\"")]
        public void DerivedTypeQueryOptions(string uriStringAfterServiceRoot, string expectedSubString)
        {
            this.TestGetPayloadContains(uriStringAfterServiceRoot, expectedSubString);
        }

        [Theory]
        [InlineData("People/$count", "13")]
        [InlineData("People(1)/Friends/$count", "1")]
        [InlineData("Flights/$count", "4")]
        [InlineData("People/$count?$filter=indexof(FirstName,'R') eq 0", "3")]
        [InlineData("People/$count?$filter=indexof(FirstName,'R') eq 0&$top(1)", "3")]
        [InlineData("People/$count?$filter=indexof(FirstName,'R') eq 0&$skip(1)&$top(1)", "3")]
        public void TestCountEntities(string uriStringAfterServiceRoot, string expectedString)
        {
            this.TestGetPayloadIs(uriStringAfterServiceRoot, expectedString);
        }

        [Theory]
        [InlineData("Me", "http://localhost:18384/api/Trippin/$metadata#Me")]
        public void TestSingleton(string uriStringAfterServiceRoot, string expectedSubString)
        {
            this.TestGetPayloadContains(uriStringAfterServiceRoot, expectedSubString);
        }

        [Theory]
        [InlineData("Me/UserName", "http://localhost:18384/api/Trippin/$metadata#Me/UserName")]
        [InlineData("Me/FavoriteFeature", "http://localhost:18384/api/Trippin/$metadata#Me/FavoriteFeature")]
        [InlineData("Me/Friends", "http://localhost:18384/api/Trippin/$metadata#People")]
        [InlineData("Me/Trips", "http://localhost:18384/api/Trippin/$metadata#Trips")]
        public void TestSingletonPropertyAccess(string uriStringAfterServiceRoot, string expectedSubString)
        {
            this.TestGetPayloadContains(uriStringAfterServiceRoot, expectedSubString);
        }

        [Theory]
        [InlineData("Me/FavoriteFeature2", 204)]
        public void TestSingletonPropertyAccessStatus(string uriStringAfterServiceRoot, int statusCode)
        {
            this.TestGetStatusCodeIs(uriStringAfterServiceRoot, statusCode);
        }

        [Theory]
        [InlineData("Me/PersonId/$value", "1")]
        public void TestSingletonPropertyRawValueAccess(string uriStringAfterServiceRoot, string expectedString)
        {
            this.TestGetPayloadIs(uriStringAfterServiceRoot, expectedString);
        }

        [Theory]
        [InlineData("Me?$expand=Friends", ",\"Friends\":[")]
        [InlineData("Me?$select=UserName,PersonId", "http://localhost:18384/api/Trippin/$metadata#Me(UserName,PersonId)")]
        public void TestSingletonWithQueryOptions(string uriStringAfterServiceRoot, string expectedSubString)
        {
            this.TestGetPayloadContains(uriStringAfterServiceRoot, expectedSubString);
        }

        [Theory]
        [InlineData("People?$count=true")]
        [InlineData("People(1)/Friends?$count=true")]
        [InlineData("People?$filter=PersonId gt 5&$count=true")]
        [InlineData("Me/Friends?$count=true")]
        [InlineData("GetPeopleWithFriendsAtLeast(n=1)?$count=true")]
        public void TestCountQueryOptionIsTrue(string uriStringAfterServiceRoot)
        {
            this.TestGetPayloadContains(uriStringAfterServiceRoot, "@odata.count");
        }

        [Theory]
        [InlineData("People?$count=false")]
        [InlineData("People(1)/Friends?$count=false")]
        [InlineData("GetPeopleWithFriendsAtLeast(n=1)?$count=false")]
        [InlineData("People")]
        [InlineData("People(1)/Friends")]
        [InlineData("GetPeopleWithFriendsAtLeast(n=1)")]
        [InlineData("Me?$count=true")]
        public void TestCountQueryOptionIsFalse(string uriStringAfterServiceRoot)
        {
            this.TestGetPayloadDoesNotContain(uriStringAfterServiceRoot, "@odata.count");
        }

        [Theory]
        [InlineData("People?$apply=aggregate(Concurrency with sum as Result)", "Result")]
        [InlineData("People?$apply=aggregate(PersonId with sum as Total)/filter(Total eq 78)", "Total")]
        [InlineData("People?$apply=groupby((FirstName), aggregate(PersonId with sum as Total))", "Total")]
        public void TestApplyQueryOption(string uriStringAfterServiceRoot, string expectedSubString)
        {
            this.TestGetPayloadContains(uriStringAfterServiceRoot, expectedSubString);
        }

        [Theory]
        [InlineData("Me", "http://localhost:18384/api/Trippin/$metadata#Me")]
        [InlineData("Flights1", "http://localhost:18384/api/Trippin/$metadata#Flights1")]
        [InlineData("Flights2", "http://localhost:18384/api/Trippin/$metadata#Flights2")]
        [InlineData("PeopleWithAge", "http://localhost:18384/api/Trippin/$metadata#PeopleWithAge")]
        [InlineData("PeopleWithAge1", "http://localhost:18384/api/Trippin/$metadata#PeopleWithAge1")]
        [InlineData("PeopleWithAgeMe", "http://localhost:18384/api/Trippin/$metadata#PeopleWithAgeMe")]
        public void TestCustomizedEntitySetSingleton(string uriStringAfterServiceRoot, string expectedString)
        {
            this.TestGetPayloadContains(uriStringAfterServiceRoot, expectedString);
        }
    }
}
