// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using FluentAssertions;
using Microsoft.OData.Client;
using Xunit;

namespace Microsoft.OData.Service.Sample.Tests
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
        public void EntityBoundFunction()
        {
            TestGetPayloadContains(
                "People(1)/Microsoft.OData.Service.Sample.Trippin.Models.GetNumberOfFriends",
                "http://localhost:18384/api/Trippin/$metadata#Edm.Int32");
        }

        [Fact]
        public void EntityBoundFunctionNonExist()
        {
            TestGetStatusCodeIs("People(111)/Microsoft.OData.Service.Sample.Trippin.Models.GetNumberOfFriends", 404);
        }

        [Fact]
        public void EntitySetBoundFunctionIEnumerable()
        {
            TestGetPayloadContains(
                "People/Microsoft.OData.Service.Sample.Trippin.Models.GetBoundEntitySetIEnumerable(n=10)",
                "http://localhost:18384/api/Trippin/$metadata#Edm.Int32");
        }

        [Fact]
        public void EntitySetBoundFunctionICollection()
        {
            TestGetPayloadContains(
                "People/Microsoft.OData.Service.Sample.Trippin.Models.GetBoundEntitySetICollection(n=2,m=5)",
                "http://localhost:18384/api/Trippin/$metadata#Edm.Int32");
        }

        [Fact]
        public void EntitySetBoundFunctionArray()
        {
            TestGetPayloadContains(
                "People/Microsoft.OData.Service.Sample.Trippin.Models.GetBoundEntitySetArray(n=2,m=5)",
                "http://localhost:18384/api/Trippin/$metadata#Edm.Int32");
        }

        public void EntitySetBoundFunctionTypeCast()
        {
            TestGetPayloadContains(
                "People/Microsoft.OData.Service.Sample.Trippin.Models.Employee/Microsoft.OData.Service.Sample.Trippin.Models.GetBoundEntitySetIEnumerable(n=10)",
                "http://localhost:18384/api/Trippin/$metadata#Edm.Int32");
        }

        public void EntitySetBoundFunctionCollectionNavigation()
        {
            TestGetPayloadContains(
                "People(0)/Friends/Microsoft.OData.Service.Sample.Trippin.Models.GetBoundEntitySetIEnumerable(n=10)",
                "http://localhost:18384/api/Trippin/$metadata#Edm.Int32");
        }

        [Fact]
        public void FunctionImportPrimitive()
        {
            TestGetPayloadContains(
                "GetPrimitive","http://localhost:18384/api/Trippin/$metadata#Edm.Int32");
        }

        [Fact]
        public void FunctionImportNullPrimitive()
        {
            TestGetStatusCodeIs("GetNullPrimitive", 204);
        }

        [Fact]
        public void FunctionImportEnum()
        {
            TestGetPayloadContains(
                "GetEnum(f=Microsoft.OData.Service.Sample.Trippin.Models.Feature'Feature1')", "http://localhost:18384/api/Trippin/$metadata#Microsoft.OData.Service.Sample.Trippin.Models.Feature");
        }

        [Fact]
        public void FunctionImportEnumParameter()
        {
            // A default value is returned.
            TestGetPayloadContains(
                "GetEnum(f=null)", "http://localhost:18384/api/Trippin/$metadata#Microsoft.OData.Service.Sample.Trippin.Models.Feature");
        }

        [Fact]
        public void FunctionImportNullEnumParameter()
        {
            // A default value is returned.
            TestGetStatusCodeIs("GetNullEnum", 204);
        }

        [Fact]
        public void FunctionImportComplex()
        {
            TestGetPayloadContains(
                "GetComplex(l=@x)?@x={\"Address\":\"NE 24th St.\"}", "http://localhost:18384/api/Trippin/$metadata#Microsoft.OData.Service.Sample.Trippin.Models.Location");
        }

        [Fact]
        public void FunctionImportNullComplex()
        {
            TestGetStatusCodeIs("GetComplex(l=null)", 204);
        }

        [Fact]
        public void FunctionImportSingleEntity()
        {
            TestGetPayloadContains(
                "GetPersonWithMostFriends", "http://localhost:18384/api/Trippin/$metadata#People/$entity");
        }

        [Fact]
        public void FunctionImportNullSingleEntity()
        {
            TestGetStatusCodeIs("GetNullEntity", 204);
        }

        [Fact]
        public void FunctionImportWithNamedArgument()
        {
            TestGetPayloadContains(
                "GetPeopleWithFriendsAtLeast(n=1)", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void FunctionImportUsingParameterAlias()
        {
            TestGetPayloadContains(
                "GetPeopleWithFriendsAtLeast(n=@x)?@x=1", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void FunctionImportTwoNamedParametersInOrder()
        {
            TestGetPayloadContains(
                "GetPeopleWithFriendsAtLeastMost(n=1,m=4)", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void FunctionImportTwoNamedParametersNotInOrder()
        {
            TestGetPayloadContains(
                "GetPeopleWithFriendsAtLeastMost(m=4, n=1)", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void FunctionImportEmptyEntityCollection()
        {
            TestGetPayloadContains(
                "GetPeopleWithFriendsAtLeast(n=10)", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void FunctionImportNullEntityCollection()
        {
            TestGetPayloadContains(
                "GetNullEntityCollection", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void FunctionImportPrimitiveIEnumerable()
        {
            TestGetPayloadContains(
                "GetIEnumerable(intEnumerable=[1,2,3])", "http://localhost:18384/api/Trippin/$metadata#Collection(Edm.Int32)");
        }

        [Fact]
        public void FunctionImportPrimitiveIEnumerableEmpty()
        {
            TestGetPayloadContains(
                "GetIEnumerable(intEnumerable=[])", "http://localhost:18384/api/Trippin/$metadata#Collection(Edm.Int32)");
        }

        [Fact]
        public void FunctionImportNullPrimitiveIEnumerable()
        {
            // Primitive collection can not be null
            TestGetStatusCodeIs("GetIEnumerable(intEnumerable=null)", 400);
        }

        [Fact]
        public void FunctionImportPrimitiveICollection()
        {
            TestGetPayloadContains(
                "GetICollection(intColl=[1,2,3])", "http://localhost:18384/api/Trippin/$metadata#Collection(Edm.Int32)");
        }

        [Fact]
        public void FunctionImportPrimitiveICollectionEmpty()
        {
            TestGetPayloadContains(
                "GetICollection(intColl=[])", "http://localhost:18384/api/Trippin/$metadata#Collection(Edm.Int32)");
        }

        [Fact]
        public void FunctionImportNullPrimitiveICollection()
        {
            // Primitive collection can not be null
            TestGetStatusCodeIs("GetICollection(intColl=null)", 400);
        }

        [Fact]
        public void FunctionImportPrimitiveArray()
        {
            TestGetPayloadContains(
                "GetArray(intArray=[1,2,3])", "http://localhost:18384/api/Trippin/$metadata#Collection(Edm.Int32)");
        }

        [Fact]
        public void FunctionImportPrimitiveArrayEmpty()
        {
            TestGetPayloadContains(
                "GetArray(intArray=[])", "http://localhost:18384/api/Trippin/$metadata#Collection(Edm.Int32)");
        }

        [Fact]
        public void FunctionImportNullPrimitiveArray()
        {
            // Primitive collection can not be null
            TestGetStatusCodeIs("GetArray(intArray=null)", 400);
        }

        [Fact]
        public void FunctionImportEnumICollection()
        {
            TestGetPayloadContains(
                "GetEnumCollection(coll=['Feature1','Feature2'])", "http://localhost:18384/api/Trippin/$metadata#Collection(Microsoft.OData.Service.Sample.Trippin.Models.Feature)");
        }

        [Fact]
        public void FunctionImportEnumEmptyICollection()
        {
            TestGetPayloadContains(
                "GetEnumCollection(coll=[])", "http://localhost:18384/api/Trippin/$metadata#Collection(Microsoft.OData.Service.Sample.Trippin.Models.Feature)");
        }

        [Fact]
        public void FunctionImportNullEnumICollection()
        {
            TestGetPayloadContains(
                "GetEnumCollection(coll=null)", "http://localhost:18384/api/Trippin/$metadata#Collection(Microsoft.OData.Service.Sample.Trippin.Models.Feature)");
        }

        [Fact]
        public void FunctionImportComplexICollection()
        {
            TestGetPayloadContains(
                "GetComplexCollection(coll=@x)?@x=[{\"Address\":\"NE 24th St.\"},{\"Address\":\"NE 25th St.\"}]", 
                "http://localhost:18384/api/Trippin/$metadata#Collection(Microsoft.OData.Service.Sample.Trippin.Models.Location)");
        }

        [Fact]
        public void FunctionImportComplexEmptyICollection()
        {
            TestGetPayloadContains(
                "GetComplexCollection(coll=@x)?@x=[]",
                "http://localhost:18384/api/Trippin/$metadata#Collection(Microsoft.OData.Service.Sample.Trippin.Models.Location)");
        }

        [Fact]
        public void FunctionImportNullComplexICollection()
        {
            TestGetPayloadContains(
                "GetComplexCollection(coll=null)", 
                "http://localhost:18384/api/Trippin/$metadata#Collection(Microsoft.OData.Service.Sample.Trippin.Models.Location)");
        }

        [Fact]
        public void FunctionImportQueryOptionsFilter()
        {
            TestGetPayloadContains(
                "GetPeopleWithFriendsAtLeast(n=1)?$filter=FirstName eq 'Scott'", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void FunctionImportQueryOptionsCount()
        {
            TestGetPayloadContains(
                "GetPeopleWithFriendsAtLeast(n=1)?$count=true", "@odata.count");
        }

        [Fact]
        public void FunctionImportQueryOptionsFilterAndCount()
        {
            TestGetPayloadContains(
                "GetPeopleWithFriendsAtLeast(n=1)?$filter=FirstName eq 'Scott'&$count=true", "http://localhost:18384/api/Trippin/$metadata#People");
        }

        [Fact]
        public void FunctionImportException()
        {
            TestGetStatusCodeIs("GetWithException", 500);
        }

        [Fact]
        public void AddressingBoundAction()
        {
            TestPostPayloadContains(
                "Trips(1)/Microsoft.OData.Service.Sample.Trippin.Models.EndTrip",
                "http://localhost:18384/api/Trippin/$metadata#Trips/$entity");
        }

        [Fact]
        public void AddressingBoundActionNonExist()
        {
            TestPostStatusCodeIs("Trips(111)/Microsoft.OData.Service.Sample.Trippin.Models.EndTrip", 404);
        }

        [Fact]
        public void AddressingBoundActionWithParenthesis()
        {
            TestPostPayloadContains(
                "Trips(1)/Microsoft.OData.Service.Sample.Trippin.Models.EndTrip()",
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
        public void AddressingActionWithNonCollectionParameters()
        {
            string payload = @"{
                ""id"": 7,
                ""location"": { ""Address"":""NE 24th St.""},
                ""feature"": ""Feature1""
            }";

            TestPostStatusCodeIs("CleanUpTrip", payload, HttpStatusCode.NoContent);
        }

        [Fact]
        public void AddressingActionWithCollectionParameters()
        {
            string payload = @"{
                ""ids"": [7,9,10],
                ""locations"": [{""Address"":""NE 24th St.""},{""Address"":""NE 25th St.""}],
                ""features"": [""Feature1"",""Feature2""]
            }";

            TestPostStatusCodeIs("CleanUpTrips", payload, HttpStatusCode.NoContent);
        }

        [Fact]
        public void AddressingActionImportWithNonCollectionParameters()
        {
            string payload = @"{
                ""id"": 7,
                ""location"": { ""Address"":""NE 24th St.""},
                ""feature"": ""Feature1""
            }";

            TestPostPayloadContains("Trips(1)/Microsoft.OData.Service.Sample.Trippin.Models.EndTripWithPara", payload, "http://localhost:18384/api/Trippin/$metadata#Microsoft.OData.Service.Sample.Trippin.Models.Location");
        }

        [Fact]
        public void AddressingActionImportWithIEnumerableParameters()
        {
            string payload = @"{
                ""ids"": [7,9,10],
                ""locations"": [{""Address"":""NE 24th St.""},{""Address"":""NE 25th St.""}],
                ""features"": [""Feature1"",""Feature2""]
            }";

            TestPostPayloadContains("Trips/Microsoft.OData.Service.Sample.Trippin.Models.EndTripsIEnumerable", payload, "http://localhost:18384/api/Trippin/$metadata#Collection(Microsoft.OData.Service.Sample.Trippin.Models.Trip)");
        }

        [Fact]
        public void AddressingActionImportWithICollectionParameters()
        {
            string payload = @"{
                ""ids"": [7,9,10],
                ""locations"": [{""Address"":""NE 24th St.""},{""Address"":""NE 25th St.""}],
                ""features"": [""Feature1"",""Feature2""]
            }";

            TestPostPayloadContains("Trips/Microsoft.OData.Service.Sample.Trippin.Models.EndTripsICollection", payload, "http://localhost:18384/api/Trippin/$metadata#Collection(Edm.Int32)");
        }

        [Fact]
        public void AddressingActionImportWithArrayParameters()
        {
            string payload = @"{
                ""ids"": [7,9,10],
                ""locations"": [{""Address"":""NE 24th St.""},{""Address"":""NE 25th St.""}],
                ""features"": [""Feature1"",""Feature2""]
            }";

            TestPostPayloadContains("Trips/Microsoft.OData.Service.Sample.Trippin.Models.EndTripsArray", payload, "http://localhost:18384/api/Trippin/$metadata#Collection(Microsoft.OData.Service.Sample.Trippin.Models.Location)");
        }

        [Fact]
        public void AddressingActionImportPrimitive()
        {
            TestPostPayloadContains("ActionPrimitive", "http://localhost:18384/api/Trippin/$metadata#Edm.Int32");
        }

        [Fact]
        public void AddressingActionImportNullPrimitive()
        {
            TestPostStatusCodeIs("ActionNullPrimitive", 204);
        }

        [Fact]
        public void AddressingActionImportEnum()
        {
            string payload = @"{
                ""f"": ""Feature1""
            }";
            TestPostPayloadContains("ActionEnum", payload, "http://localhost:18384/api/Trippin/$metadata#Microsoft.OData.Service.Sample.Trippin.Models.Feature");
        }

        [Fact]
        public void AddressingActionImportNullEnum()
        {
            TestPostStatusCodeIs("ActionNullEnum", 204);
        }

        [Fact]
        public void AddressingActionImportComplex()
        {
            string payload = @"{
                ""l"": { ""Address"":""NE 24th St.""}
            }";

            TestPostPayloadContains("ActionComplex", payload, "http://localhost:18384/api/Trippin/$metadata#Microsoft.OData.Service.Sample.Trippin.Models.Location");
        }

        [Fact]
        public void AddressingActionImportNullComplex()
        {
            string payload = @"{
                ""l"": null
            }";

            TestPostStatusCodeIs("ActionComplex", payload,  HttpStatusCode.NoContent);
        }

        [Fact]
        public void AddressingActionImportNoComplex()
        {
            string payload = "";

            TestPostStatusCodeIs("ActionComplex", payload, HttpStatusCode.NoContent);
        }

        [Fact]
        public void AddressingActionImportWithPrimitiveCollection()
        {
            string payload = @"{
                ""intArray"": [7,9,10]
            }";

            TestPostPayloadContains("ActionPrimitiveArray", payload, "http://localhost:18384/api/Trippin/$metadata#Collection(Edm.Int32)");
        }

        [Fact]
        public void AddressingActionImportWithEnumCollection()
        {
            string payload = @"{
                ""coll"": [""Feature1"",""Feature2""]
            }";

            TestPostPayloadContains("ActionEnumCollection", payload, "http://localhost:18384/api/Trippin/$metadata#Collection(Microsoft.OData.Service.Sample.Trippin.Models.Feature)");
        }

        [Fact]
        public void AddressingActionImportWithNoEnumCollection()
        {
            string payload = "";

            TestPostPayloadContains("ActionEnumCollection", payload, "http://localhost:18384/api/Trippin/$metadata#Collection(Microsoft.OData.Service.Sample.Trippin.Models.Feature)");
        }

        [Fact]
        public void AddressingActionImportWithNullEnumCollection()
        {
            string payload = @"{
                ""coll"": null
            }";

            TestPostPayloadContains("ActionEnumCollection", payload, "http://localhost:18384/api/Trippin/$metadata#Collection(Microsoft.OData.Service.Sample.Trippin.Models.Feature)");
        }

        [Fact]
        public void AddressingActionImportWithComplexCollection()
        {
            string payload = @"{
                ""coll"": [{""Address"":""NE 24th St.""},{""Address"":""NE 25th St.""}]
            }";

            TestPostPayloadContains("ActionComplexCollection", payload, "http://localhost:18384/api/Trippin/$metadata#Collection(Microsoft.OData.Service.Sample.Trippin.Models.Location)");
        }

        [Fact]
        public void AddressingActionImportWithException()
        {
            TestPostStatusCodeIs("ActionWithException", 500);
        }

        [Fact]
        public void AddressingActionImportUnAuthorization()
        {
            TestPostStatusCodeIs("ActionForAuthorization", 403);
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
        public void AddressingKeyWithSlash()
        {
            TestGetStatusCodeIs("Airlines(AirlineCode='S%2F')", 200);
        }

        [Fact]
        public void AddressingKeyWithSlashDoubleEscape()
        {
            TestGetStatusCodeIs("Airlines(AirlineCode='S%252F')", 200);
        }

        [Fact]
        public void AddressingKeyWithBackSlash()
        {
            TestGetStatusCodeIs("Airlines(AirlineCode='BS%255C')", 200);
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
