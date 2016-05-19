// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.OData.Client;
using Microsoft.OData.Service.Sample.Tests;
using Xunit;

namespace Microsoft.Restier.WebApi.Test.Scenario
{
    public class UrlConventionsTests : TrippinE2ETestBase
    {
        [Fact]
        public void ServiceRoot()
        {
            TestGetPayloadContains(string.Empty, "http://localhost:18384/api/Trippin/$metadata");
        }

        [Fact]
        public void Metadata()
        {
            TestGetPayloadContains(
                "$metadata", "<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\">");
        }

        [Fact]
        public void AddressingEntitySet()
        {
            TestGetPayloadContains("People", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void AddressingSingleton()
        {
            TestGetPayloadContains(
                "Me", "http://localhost:18384/api/Trippin/$metadata#Me");
        }

        [Fact]
        public void AddressingSingleEntity()
        {
            TestGetPayloadContains(
                "People(1)", "http://localhost:18384/api/Trippin/$metadata#People/$entity");
        }

        [Fact]
        public void AddressingSingleEntityWithKeyName()
        {
            TestGetPayloadContains(
                "People(PersonId=1)", "http://localhost:18384/api/Trippin/$metadata#People/$entity");
        }

        [Fact]
        public void AddressingNavigationProperty()
        {
            TestGetPayloadContains(
                "People(1)/Trips", "http://localhost:18384/api/Trippin/$metadata#Trips");
        }

        [Fact]
        public void AddressingEntityReference()
        {
            TestGetPayloadContains(
                "People(1)/Trips/$ref", "http://localhost:18384/api/Trippin/$metadata#Collection($ref)");
        }

        [Fact]
        public void AddressingBoundFunction()
        {
            TestGetPayloadContains(
                "People(1)/Microsoft.Restier.WebApi.Test.Services.Trippin.Models.GetNumberOfFriends",
                "http://localhost:18384/api/Trippin/$metadata#Edm.Int32");
        }

        [Fact]
        public void AddressingFunctionImport()
        {
            TestGetPayloadContains(
                "GetPersonWithMostFriends", "http://localhost:18384/api/Trippin/$metadata#People/$entity");
        }

        [Fact]
        public void AddressingFunctionImportWithNamedArgument()
        {
            TestGetPayloadContains(
                "GetPeopleWithFriendsAtLeast(n=1)", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void AddressingFunctionImportUsingParameterAlias()
        {
            TestGetPayloadContains(
                "GetPeopleWithFriendsAtLeast(n=@n)?@n=1", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void AddressingBoundAction()
        {
            TestPostPayloadContains(
                "Trips(1)/Microsoft.Restier.WebApi.Test.Services.Trippin.Models.EndTrip",
                "http://localhost:18384/api/Trippin/$metadata#Trips/$entity");
        }

        [Fact]
        public void AddressingBoundActionWithParenthesis()
        {
            TestPostPayloadContains(
                "Trips(1)/Microsoft.Restier.WebApi.Test.Services.Trippin.Models.EndTrip()",
                "http://localhost:18384/api/Trippin/$metadata#Trips/$entity");
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

        [Fact]
        public void AddressingProperty()
        {
            TestGetPayloadContains(
                "People(1)/LastName", "http://localhost:18384/api/Trippin/$metadata#People(1)/LastName");
        }

        [Fact]
        public void AddressingPropertyValue()
        {
            TestGetStatusCodeIs("People(1)/LastName/$value", 200);
        }

        [Fact]
        public void AddressingCollectionCount()
        {
            TestGetStatusCodeIs("People/$count", 200);
        }

        [Fact]
        public void ApplyingFilterFunctions_1()
        {
            TestGetPayloadContains(
                "People?$filter=PersonId gt 2 and not ((PersonId add 1) eq 4 or startswith(UserName, 'mar'))",
                "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingFilterFunctions_2()
        {
            TestGetPayloadContains(
                "People?$filter=contains(UserName, 'a') or endswith(LastName, 't') and length(FirstName) lt 5",
                "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingFilterFunctions_3()
        {
            TestGetPayloadContains(
                "People?$filter=toupper(substring(UserName,indexof(trim(UserName),'abc'),3))" +
                "eq concat(tolower('ABC'),'d')",
                "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingFilterFunctions_4()
        {
            TestGetPayloadContains(
                "Trips?$filter=year(StartsAt) eq 2015 and month(StartsAt) eq 5 and day(StartsAt) eq 3 and " +
                "hour(StartsAt) eq 13 and minute(StartsAt) eq 59 and second(StartsAt) eq 59 and " +
                "fractionalseconds(StartsAt) lt 0.1",
                "http://localhost:18384/api/Trippin/$metadata#Trips");
        }

        [Fact]
        public void ApplyingFilterFunctions_5()
        {
            TestGetPayloadContains(
                "Trips?$filter=date(StartsAt) ne date(EndsAt) and time(StartsAt) eq time(EndsAt)",
                "http://localhost:18384/api/Trippin/$metadata#Trips");
        }

        [Fact]
        public void ApplyingFilterFunctions_6()
        {
            TestGetPayloadContains(
                "Trips?$filter=round(10.5) eq 11.0 and floor(10.5) eq 10.0 and ceiling(10.1) eq 11.0 and " +
                "cast(10,Edm.Double) eq 10.0",
                "http://localhost:18384/api/Trippin/$metadata#Trips");
        }

        [Fact]
        public void ApplyingFilterPrimitiveLiterals()
        {
            TestGetPayloadContains(
                "Trips?$filter=true eq false or 0.2 gt 0.1e2 or -3 lt -1 or StartsAt eq 2012-12-03T07:16:23Z or " +
                "TrackGuid eq 01234567-89ab-cdef-0123-456789abcdef or " +
                "duration'P00DT23H59M59.999999999999S' ne duration'P00DT23H59M58.999999999999S' or " +
                "['red','green'] ne ['yellow','blue']",
                "http://localhost:18384/api/Trippin/$metadata#Trips");
        }

        [Fact]
        public void ApplyingFilterPathExpression()
        {
            TestGetPayloadContains(
                "Trips(1)/Events?$filter=OccursAt/Address eq 'abc'",
                "http://localhost:18384/api/Trippin/$metadata#Events");
        }

        [Fact]
        public void ApplyingFilterLambdaOperatorsAny()
        {
            TestGetPayloadContains(
                "People?$filter=Trips/any(d:d/TripId gt 1)", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingFilterLambdaOperatorsAll()
        {
            TestGetPayloadContains(
                "People?$filter=Trips/all(d:d/TripId gt 1)", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingSingleLevelExpand()
        {
            TestGetPayloadContains(
                "People?$expand=Trips", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingMutiLevelExpand()
        {
            TestGetPayloadContains(
                "People?$expand=Friends($levels=3)", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingLevelThanMaxExpand()
        {
            Action test = () => TestGetPayloadContains(
                "People?$expand=Friends($levels=4)", "The request includes a $expand path which is too deep. The maximum depth allowed is 2. To increase the limit, set the 'MaxExpansionDepth' property on EnableQueryAttribute or ODataValidationSettings.");
            // OData .NET client has wrapped the expcetion with new message.
            test.ShouldThrow<DataServiceTransportException>();
        }

        [Fact]
        public void ApplyingExpandWithNestedQueryOptions()
        {
            TestGetPayloadContains(
                "People?$expand=Trips($filter=StartsAt lt EndsAt)", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingSelect()
        {
            TestGetPayloadContains(
                "People?$select=FirstName,LastName", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingSelectWithExpand()
        {
            TestGetPayloadContains(
                "People?$select=FirstName&$expand=Trips", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingSelectStar()
        {
            TestGetPayloadContains(
                "People?$select=*", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingOrderBy()
        {
            TestGetPayloadContains(
                "People?$orderby=FirstName", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingTopAndSkip()
        {
            TestGetPayloadContains(
                "People?$top=3&$skip=3", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void ApplyingSearch()
        {
            TestGetPayloadContains(
                "People?$search=abc", "http://localhost:18384/api/Trippin/$metadata#People");
        }
    }
}
