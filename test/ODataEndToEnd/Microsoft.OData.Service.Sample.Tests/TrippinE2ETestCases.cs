// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.OData.Client;
using Microsoft.OData.Core;
using Microsoft.OData.Service.Sample.Trippin.Models;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.OData.Service.Sample.Tests
{
    public class TrippinE2ETestCases : TrippinE2ETestBase
    {
        [Fact]
        public void AnnotationComputedOptimisticConcurrency()
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "GET",
                    new Uri(this.ServiceBaseUri.OriginalString + "$metadata", UriKind.Absolute),
                    true,
                    false,
                    new Dictionary<string, string>()));
            using (var r = new StreamReader(requestMessage.GetResponse().GetStream()))
            {
                var modelStr = r.ReadToEnd();
                Assert.Contains("<Annotation Term=\"Org.OData.Core.V1.Computed\" Bool=\"true\" />", modelStr, StringComparison.Ordinal);
                Assert.Contains("<Annotation Term=\"Org.OData.Core.V1.OptimisticConcurrency\">", modelStr, StringComparison.Ordinal);
            }
        }


        // With Web Api OData model build, max/min length is not set
        // Keep this test case as Web Api is working on add max/min length support.
        // [Fact]
        public void AnnotationRequiredMaxMinLengthTimestamp()
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "GET",
                    new Uri(this.ServiceBaseUri.OriginalString + "$metadata", UriKind.Absolute),
                    useDefaultCredentials: true,
                    usePostTunneling: false,
                    headers: new Dictionary<string, string>()));
            using (var r = new StreamReader(requestMessage.GetResponse().GetStream()))
            {
                var modelStr = r.ReadToEnd();

                // [Required] ==> Nullable="false"
                Assert.Contains(
                    "<Property Name=\"FirstName\" Type=\"Edm.String\" Nullable=\"false\" MaxLength=\"max\" />",
                    modelStr,
                    StringComparison.Ordinal);

                // [MaxLength] [MinLength] --> only MaxLength=".."
                Assert.Contains(
                    "<Property Name=\"LastName\" Type=\"Edm.String\" MaxLength=\"26\" />",
                    modelStr,
                    StringComparison.Ordinal);

                // [Timestamp] ==> Computed
                Assert.Contains(
                    "<Property Name=\"TimeStampValue\" Type=\"Edm.Binary\" Nullable=\"false\" MaxLength=\"8\">\r\n"
                        + "          <Annotation Term=\"Org.OData.Core.V1.Computed\" Bool=\"true\" />",
                    modelStr,
                    StringComparison.Ordinal);
            }
        }

        [Fact]
        public void MetadataShouldContainEnumProperty()
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "GET",
                    new Uri(this.ServiceBaseUri.OriginalString + "$metadata", UriKind.Absolute),
                    true,
                    false,
                    new Dictionary<string, string>()));
            using (var r = new StreamReader(requestMessage.GetResponse().GetStream()))
            {
                var modelStr = r.ReadToEnd();

                Assert.Contains("<EnumType Name=\"Feature\">", modelStr, StringComparison.Ordinal);
                Assert.Contains("<Member Name=\"Feature1\" Value=\"0\" />", modelStr, StringComparison.Ordinal);
                Assert.Contains("<Member Name=\"Feature2\" Value=\"1\" />", modelStr, StringComparison.Ordinal);
                Assert.Contains("<Member Name=\"Feature3\" Value=\"2\" />", modelStr, StringComparison.Ordinal);
                Assert.Contains("<Member Name=\"Feature4\" Value=\"3\" />", modelStr, StringComparison.Ordinal);
                Assert.Contains("<Property Name=\"FavoriteFeature\"", modelStr, StringComparison.Ordinal);
                Assert.Contains("<Property Name=\"FavoriteFeature2\"", modelStr, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void MetadataShouldContainDerivedType()
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "GET",
                    new Uri(this.ServiceBaseUri.OriginalString + "$metadata", UriKind.Absolute),
                    true,
                    false,
                    new Dictionary<string, string>()));
            using (var r = new StreamReader(requestMessage.GetResponse().GetStream()))
            {
                var modelStr = r.ReadToEnd();
                Assert.Contains("<EntityType Name=\"Manager\" BaseType=\"Microsoft.OData.Service.Sample.Trippin.Models.Person\">", modelStr, StringComparison.Ordinal);
                Assert.Contains("<EntityType Name=\"Employee\" BaseType=\"Microsoft.OData.Service.Sample.Trippin.Models.Person\">", modelStr, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void CURDEntity()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            Person person = new Person()
            {
                FirstName = "Sheldon",
                UserName = "SheldonCooper",
                Age = 12
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long? personId = person.PersonId;

            // Count this entity
            var count = this.TestClientContext.People.Count();
            Assert.Equal(personId, count);

            // Update an entity
            person.LastName = "Lee";
            person.Age = null;
            this.TestClientContext.UpdateObject(person);
            this.TestClientContext.SaveChanges();

            // Query an entity
            this.TestClientContext.Detach(person);
            person = this.TestClientContext.People
                .ByKey(new Dictionary<string, object>() { { "PersonId", personId } }).GetValue();
            Assert.Equal("Lee", person.LastName);

            // Delete an entity
            this.TestClientContext.DeleteObject(person);
            this.TestClientContext.SaveChanges();

            // Filter this entity
            var persons = this.TestClientContext.People
                .AddQueryOption("$filter", string.Format("PersonId eq {0}", personId)).ToList();
            Assert.Equal(0, persons.Count);
        }

        [Fact]
        public void CURDDerivedEntity()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            Employee employee = new Employee()
            {
                FirstName = "Vincent",
                UserName = "SheldonCooper",
                Age = 12,
                Cost = 10000
            };

            this.TestClientContext.AddToPeople(employee);
            this.TestClientContext.SaveChanges();
            long personId = employee.PersonId;

            // Count this entity
            var count = this.TestClientContext.People.Count();
            Assert.Equal(personId, count);

            // Query an entity, generate URI will like /entity(key)/cast
            this.TestClientContext.Detach(employee);
            employee = this.TestClientContext.People.Where(e => e.PersonId == personId).OfType<Employee>().First();
            Assert.Equal("SheldonCooper", employee.UserName);

            // Update an entity
            employee.LastName = "Lee";
            employee.Cost = 20000;
            employee.Age = null;
            this.TestClientContext.UpdateObject(employee);
            this.TestClientContext.SaveChanges();

            // Query an entity, generate URI will like /entity/cast(key)
            this.TestClientContext.Detach(employee);
            employee = this.TestClientContext.People.OfType<Employee>().Where(e =>e.PersonId ==personId).First();
            Assert.Equal("Lee", employee.LastName);
            Assert.Equal(20000, employee.Cost);

            // Delete an entity
            this.TestClientContext.DeleteObject(employee);
            this.TestClientContext.SaveChanges();

            // Filter this entity
            var persons = this.TestClientContext.People
                .AddQueryOption("$filter", string.Format("PersonId eq {0}", personId)).ToList();
            Assert.Equal(0, persons.Count);
        }

        [Fact]
        public void CURDComputedImmutableProperty()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            Order employee = new Order()
            {
                PersonId = 1,
                OrderId = 11,
                Price = 300,
                ComputedProperty = "ShouldBeIgnored",
                ImmutableProperty = "ShouldNotBeIgnored",
                NormalOrderDetail = new OrderDetail()
                {
                    NormalProperty = "ShouldNotBeIgnored",
                    ComputedProperty = "ShouldBeIgnored",
                    ImmutableProperty = "ShouldNotBeIgnored"
                },
                ComputedOrderDetail = new OrderDetail()
                {
                    NormalProperty = "IgnoredAsParentIgnored",
                    ComputedProperty = "IgnoredAsParentIgnored",
                    ImmutableProperty = "IgnoredAsParentIgnored"
                },
                ImmutableOrderDetail = new OrderDetail()
                {
                    NormalProperty = "ShouldNotBeIgnored",
                    ComputedProperty = "ShouldBeIgnored",
                    ImmutableProperty = "ShouldNotBeIgnored"
                },
            };

            this.TestClientContext.AddToOrders(employee);
            this.TestClientContext.SaveChanges();
            long personId = employee.PersonId;
            long orderId = employee.OrderId;

            // Query the new added entity
            this.TestClientContext.Detach(employee);
            employee = this.TestClientContext.Orders.Where(e => e.PersonId == personId && e.OrderId == orderId).First();
            // computed property should not have new value but Immutable property should have new value
            Assert.Equal(300, employee.Price);
            Assert.NotEqual("ShouldBeIgnored", employee.ComputedProperty);
            Assert.Equal("ShouldNotBeIgnored", employee.ImmutableProperty);

            Assert.Equal("ShouldNotBeIgnored", employee.NormalOrderDetail.NormalProperty);
            Assert.NotEqual("ShouldBeIgnored", employee.NormalOrderDetail.ComputedProperty);
            Assert.Equal("ShouldNotBeIgnored", employee.NormalOrderDetail.ImmutableProperty);

            Assert.NotEqual("IgnoredAsParentIgnored", employee.ComputedOrderDetail.NormalProperty);
            Assert.NotEqual("IgnoredAsParentIgnored", employee.ComputedOrderDetail.ComputedProperty);
            Assert.NotEqual("IgnoredAsParentIgnored", employee.ComputedOrderDetail.ImmutableProperty);

            Assert.Equal("ShouldNotBeIgnored", employee.ImmutableOrderDetail.NormalProperty);
            Assert.NotEqual("ShouldBeIgnored", employee.ImmutableOrderDetail.ComputedProperty);
            Assert.Equal("ShouldNotBeIgnored", employee.ImmutableOrderDetail.ImmutableProperty);

            // Update an entity
            employee.Price = 400;
            employee.ComputedProperty = "ShouldBeIgnored2";
            employee.ImmutableProperty = "ShouldBeIgnored2";

            employee.NormalOrderDetail = new OrderDetail()
            {
                NormalProperty = "ShouldNotBeIgnored2",
                ComputedProperty = "ShouldBeIgnored2",
                ImmutableProperty = "ShouldBeIgnored2"
            };

            employee.ComputedOrderDetail = new OrderDetail()
            {
                NormalProperty = "IgnoredAsParentIgnored2",
                ComputedProperty = "IgnoredAsParentIgnored2",
                ImmutableProperty = "IgnoredAsParentIgnored2"
            };

            employee.ImmutableOrderDetail = new OrderDetail()
            {
                NormalProperty = "ShouldBeIgnored2",
                ComputedProperty = "ShouldBeIgnored2",
                ImmutableProperty = "ShouldBeIgnored2"
            };

            this.TestClientContext.UpdateObject(employee);
            this.TestClientContext.SaveChanges();

            // Query the updated entity
            this.TestClientContext.Detach(employee);
            employee = this.TestClientContext.Orders.Where(e => e.PersonId == personId && e.OrderId == orderId).First();

            // both computed property and immutable property should not have new value
            Assert.Equal(400, employee.Price);
            Assert.NotEqual("ShouldBeIgnored2", employee.ComputedProperty);

            // Immutable property has value set during insert.
            Assert.NotEqual("ShouldBeIgnored2", employee.ImmutableProperty);
            Assert.Equal("ShouldNotBeIgnored", employee.ImmutableProperty);

            Assert.Equal("ShouldNotBeIgnored2", employee.NormalOrderDetail.NormalProperty);
            Assert.NotEqual("ShouldBeIgnored2", employee.NormalOrderDetail.ComputedProperty);
            Assert.NotEqual("ShouldBeIgnored2", employee.NormalOrderDetail.ImmutableProperty);

            Assert.NotEqual("IgnoredAsParentIgnored2", employee.ComputedOrderDetail.NormalProperty);
            Assert.NotEqual("IgnoredAsParentIgnored2", employee.ComputedOrderDetail.ComputedProperty);
            Assert.NotEqual("IgnoredAsParentIgnored2", employee.ComputedOrderDetail.ImmutableProperty);

            Assert.NotEqual("ShouldBeIgnored2", employee.ImmutableOrderDetail.NormalProperty);
            Assert.NotEqual("ShouldBeIgnored2", employee.ImmutableOrderDetail.ComputedProperty);
            Assert.NotEqual("ShouldBeIgnored2", employee.ImmutableOrderDetail.ImmutableProperty);

            // Delete an entity
            this.TestClientContext.DeleteObject(employee);
            this.TestClientContext.SaveChanges();
        }

        [Fact]
        public void CURDEntityWithComplexType()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            var entry = new Event
            {
                Description = "event1",
                OccursAt = new Location { Address = "address1" }
            };

            this.TestClientContext.AddToEvents(entry);
            this.TestClientContext.SaveChanges();
            int eventId = entry.Id;
            this.TestClientContext.Detach(entry);

            // Query this entity
            entry =
                this.TestClientContext.Events.ByKey(new Dictionary<string, object> { { "Id", eventId } }).GetValue();
            Assert.Equal("event1", entry.Description);
            Assert.Equal("address1", entry.OccursAt.Address);
            this.TestClientContext.Detach(entry);

            // PUT this entity
            entry = new Event
            {
                Id = eventId,
                Description = "event2",
                OccursAt = new Location { Address = "address2" }
            };
            this.TestClientContext.AttachTo("Events", entry);
            this.TestClientContext.UpdateObject(entry);
            this.TestClientContext.SaveChanges(SaveChangesOptions.ReplaceOnUpdate);
            this.TestClientContext.Detach(entry);

            // Query this entity
            entry =
                this.TestClientContext.Events.ByKey(new Dictionary<string, object> { { "Id", eventId } }).GetValue();
            Assert.Equal("event2", entry.Description);
            Assert.Equal("address2", entry.OccursAt.Address);

            // Update an entity
            entry.OccursAt.Address = "address3";
            this.TestClientContext.UpdateObject(entry);
            this.TestClientContext.SaveChanges();
            this.TestClientContext.Detach(entry);

            // Query this entity
            entry =
                this.TestClientContext.Events.ByKey(new Dictionary<string, object> { { "Id", eventId } }).GetValue();
            Assert.Equal("event2", entry.Description);
            Assert.Equal("address3", entry.OccursAt.Address);

            // Delete an entity
            var count = this.TestClientContext.Events
                .AddQueryOption("$filter", string.Format("Id eq {0}", eventId)).ToList().Count;
            Assert.Equal(1, count);
            this.TestClientContext.DeleteObject(entry);
            this.TestClientContext.SaveChanges();
            count = this.TestClientContext.Events
                .AddQueryOption("$filter", string.Format("Id eq {0}", eventId)).ToList().Count;
            Assert.Equal(0, count);
        }

        [Fact]
        public void UQProperty()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;
            // Post an entity
            Person person = new Person()
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper"
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long personId = person.PersonId;

            // Query a property
            var lastName = this.TestClientContext.People
                .Where(p => p.PersonId == personId).Select(p => p.LastName)
                .SingleOrDefault();
            Assert.Equal("Cooper", lastName);

            // Update a property
            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { "Content-Type", "application/json" }
            };

            HttpWebRequestMessage request = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "Put",
                    new Uri(string.Format(this.TestClientContext.BaseUri + "/People({0})/LastName", personId),
                        UriKind.Absolute),
                    false,
                    false,
                    headers));
            using (var stream = request.GetStream())
            using (StreamWriter writer = new StreamWriter(stream))
            {
                var Payload = @"{""value"":""Lee""}";
                writer.Write(Payload);
            }

            using (var response = request.GetResponse() as HttpWebResponseMessage)
            {
                Assert.Equal(200, response.StatusCode);
            }

            // Query a property's value : ~/$value
            headers.Clear();
            ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings { BaseUri = this.TestClientContext.BaseUri };

            request = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "Get",
                    new Uri(string.Format(this.TestClientContext.BaseUri + "/People({0})/LastName/$value", personId),
                        UriKind.Absolute),
                    false,
                    false,
                    headers));
            using (var response = request.GetResponse() as HttpWebResponseMessage)
            {
                Assert.Equal(200, response.StatusCode);
                using (var stream = response.GetStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    var expectedPayload = "Lee";
                    var content = reader.ReadToEnd();
                    Assert.Equal(expectedPayload, content);
                }
            }

            // Delete the entity to make data unchanged
            this.TestClientContext.DeleteObject(person);
            this.TestClientContext.SaveChanges();
        }


        [Fact]
        public void CRUDCompositeKey()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            Order order = new Order()
            {
                PersonId = 3,
                OrderId = 1,
                Description = "Person 3 order 1",
                Price = 1000,
                NormalOrderDetail = new OrderDetail(),
                ImmutableOrderDetail = new OrderDetail()
            };

            this.TestClientContext.AddToOrders(order);
            this.TestClientContext.SaveChanges();

            // Update an entity
            order.Description = "Order31";
            this.TestClientContext.UpdateObject(order);
            this.TestClientContext.SaveChanges();

            // Query an entity
            this.TestClientContext.Detach(order);
            order = this.TestClientContext.Orders
                .ByKey(new Dictionary<string, object>() { { "PersonId", 3 } , { "OrderId", 1 } }).GetValue();
            Assert.Equal("Order31", order.Description);

            // Filter this entity
            var orders = this.TestClientContext.Orders
                .AddQueryOption("$filter", "PersonId eq 3 and OrderId eq 1").ToList();
            Assert.Equal(1, orders.Count);

            // Delete an entity
            this.TestClientContext.DeleteObject(order);
            this.TestClientContext.SaveChanges();

            // Filter this entity
            orders = this.TestClientContext.Orders
                .AddQueryOption("$filter", "PersonId eq 3 and OrderId eq 1").ToList();
            Assert.Equal(0, orders.Count);
        }

        [Fact]
        public void QueryOptions()
        {
            this.TestClientContext.MergeOption = Microsoft.OData.Client.MergeOption.OverwriteChanges;
            // Post an entity
            Person person = new Person()
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper"
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long? personId = person.PersonId;

            // Filter this entity
            var persons = this.TestClientContext.People
                .AddQueryOption("$filter", string.Format("PersonId eq {0}", personId))
                .ToList();
            Assert.Equal(1, persons.Count);

            // Filter with Parameter alias
            this.TestClientContext.People
                .AddQueryOption("$filter", string.Format("FirstName eq @p1 and PersonId gt {0}&@p1='Sheldon'", personId - 1))
                .ToList();
            Assert.Equal(1, persons.Count);

            // Projection
            this.TestClientContext.Detach(person);
            person = this.TestClientContext.People.Where(p => p.PersonId == personId)
                .Select(p =>
                    new Person()
                    {
                        PersonId = p.PersonId,
                        UserName = p.UserName
                    }).FirstOrDefault();
            Assert.Equal(personId, person.PersonId);
            Assert.NotNull(person.UserName);
            Assert.Null(person.FirstName);
            Assert.Null(person.LastName);

            // Order by PersonId desc
            var people2 = this.TestClientContext.People.OrderByDescending(p => p.PersonId).ToList();
            Assert.Equal(personId, people2.First().PersonId);

            // Order by PersonId
            people2 = this.TestClientContext.People.OrderBy(p => p.PersonId).ToList();
            Assert.Equal(personId, people2.Last().PersonId);

            // top
            people2 = this.TestClientContext.People.OrderBy(p => p.PersonId).Take(3).ToList();
            Assert.Equal(3, people2.Count);

            // skip
            people2 = this.TestClientContext.People.Skip((int)(personId - 1)).ToList();
            Assert.Equal(personId, people2.First().PersonId);

            // count
            var countQuery = this.TestClientContext.People.IncludeTotalCount().Skip(1).Take(2) as DataServiceQuery<Person>;
            var response = countQuery.Execute() as QueryOperationResponse<Person>;
            Assert.Equal(response.TotalCount, 14);

            // count with expand
            countQuery = this.TestClientContext.People.IncludeTotalCount().Expand("Friends").Skip(1).Take(2) as DataServiceQuery<Person>;
            response = countQuery.Execute() as QueryOperationResponse<Person>;
            Assert.Equal(response.TotalCount, 14);

            // Delete the entity to make data unchanged
            this.TestClientContext.DeleteObject(person);
            this.TestClientContext.SaveChanges();
        }

        [Fact]
        public void FilterBuiltInDateFunctions()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;
            DateTime startDate = DateTime.Now.AddYears(1);
            DateTime endDate = startDate.AddHours(5);
            TimeSpan duration = endDate - startDate;
            // Post an entity
            Flight flight = new Flight()
            {
                ConfirmationCode = "MU58496",
                FlightNumber = "MU589",
                StartsAt = startDate,
                EndsAt = endDate,
                // TODO GitHubIssue#47 : SQL issue when duration length greater than 1 day
                // How to describe a timespan equal to or greater than 1 day.
                Duration = duration,
                AirlineId = "MU",
                SeatNumber = "C32",
                FromId = "KSEA",
                ToId = "ZSSS"
            };
            this.TestClientContext.AddToFlights(flight);
            this.TestClientContext.SaveChanges();

            var flightId = flight.FlightId;

            // year(DateTimeOffset) && month(DateTimeOffset)
            var flight1 = this.TestClientContext.Flights
                .Where(f => f.StartsAt.Year == startDate.Year
                    && f.StartsAt.Month == startDate.Month).ToList();
            Assert.True(flight1.Any(f => f.FlightId == flightId));
            Assert.True(flight1.All(f => f.StartsAt.Year == startDate.Year));
            Assert.True(flight1.All(f => f.StartsAt.Month == startDate.Month));

            // day(DateTimeOffset)
            flight1 = this.TestClientContext.Flights.Where(f => f.StartsAt.Day == startDate.Day).ToList();
            Assert.True(flight1.Any(f => f.FlightId == flightId));
            Assert.True(flight1.All(f => f.StartsAt.Day == startDate.Day));

            // hour(DateTimeOffset)
            flight1 = this.TestClientContext.Flights.Where(f => f.StartsAt.Hour == startDate.Hour).ToList();
            Assert.True(flight1.Any(f => f.FlightId == flightId));
            Assert.True(flight1.All(f => f.StartsAt.Hour == startDate.Hour));

            // minute(DateTimeOffset)
            flight1 = this.TestClientContext.Flights.Where(f => f.StartsAt.Minute == startDate.Minute).ToList();
            Assert.True(flight1.Any(f => f.FlightId == flightId));
            Assert.True(flight1.All(f => f.StartsAt.Minute == startDate.Minute));

            // second(DateTimeOffset)
            flight1 = this.TestClientContext.Flights.Where(f => f.StartsAt.Second == startDate.Second).ToList();
            Assert.True(flight1.Any(f => f.FlightId == flightId));
            Assert.True(flight1.All(f => f.StartsAt.Second == startDate.Second));

            // Following built-in functions are not supported now.
            // fractionalseconds
            // date
            // time
            // totaloffsetminutes
            // now
            // mindatetime
            // maxdatetime
            // totalseconds
        }

        [Fact]
        public void CURDSingleNavigationPropertyAndRef()
        {
            this.TestClientContext.MergeOption = Microsoft.OData.Client.MergeOption.OverwriteChanges;

            Airline airline = new Airline()
            {
                Name = "American Delta",
                AirlineCode = "DL",
                TimeStampValue = new byte[] { 0 }
            };

            this.TestClientContext.AddToAirlines(airline);
            this.TestClientContext.SaveChanges();

            // Post an entity
            Flight flight = new Flight()
            {
                ConfirmationCode = "JH58496",
                FlightNumber = "DL589",
                StartsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 15, 00, 0)),
                EndsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 16, 30, 0)),
                AirlineId = null,
                SeatNumber = "C32",
                FromId = "KSEA",
                ToId = "ZSSS"
            };

            this.TestClientContext.AddToFlights(flight);
            this.TestClientContext.SaveChanges();

            // Set $ref
            this.TestClientContext.SetLink(flight, "Airline", airline);
            this.TestClientContext.SaveChanges();

            this.TestClientContext.Detach(airline);
            // Query an Navigation Property
            var airline2 = this.TestClientContext.Flights
                .ByKey(new Dictionary<string, object>() { { "FlightId", flight.FlightId } })
                .Airline.GetValue();
            Assert.Equal(airline.AirlineCode, airline2.AirlineCode);

            // Expand an Navigation Property
            var flight2 = this.TestClientContext.Flights
                .Expand(f => f.From)
                .Expand(f => f.To)
                .Where(f => f.FlightId == flight.FlightId)
                .SingleOrDefault();
            Assert.Equal(flight.FromId, flight2.From.IcaoCode);
            Assert.Equal(flight.ToId, flight2.To.IcaoCode);

            // Expand with select
            this.TestClientContext.Detach(flight2.From);
            var flight3 = this.TestClientContext.Flights
                .AddQueryOption("$expand", "From($select=IcaoCode)")
                .Where(f => f.FlightId == flight.FlightId)
                .SingleOrDefault();
            Assert.Equal(flight.FromId, flight3.From.IcaoCode);
            Assert.Null(flight3.From.IataCode);

            // Get $ref
            Dictionary<string, string> headers = new Dictionary<string, string>();
            ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings { BaseUri = this.TestClientContext.BaseUri };

            HttpWebRequestMessage request = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "Get",
                    new Uri(string.Format(this.TestClientContext.BaseUri + "/Flights({0})/Airline/$ref", flight.FlightId),
                        UriKind.Absolute),
                    false,
                    false,
                    headers));

            using (HttpWebResponseMessage response = request.GetResponse() as HttpWebResponseMessage)
            {
                Assert.Equal(200, response.StatusCode);
                using (var stream = response.GetStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    var expectedPayload = "{"
                        + @"  ""@odata.context"":""http://localhost:18384/api/Trippin/$metadata#$ref"","
                        + string.Format(@"""@odata.id"":""http://localhost:18384/api/Trippin/Airlines('{0}')""", airline2.AirlineCode)
                        + "}";
                    var content = reader.ReadToEnd();
                    Assert.Equal(expectedPayload.Replace(" ", ""), content);
                }
            }

            // Delete $ref
            this.TestClientContext.SetLink(flight, "Airline", null);
            this.TestClientContext.SaveChanges();

            this.TestClientContext.Detach(airline);

            HttpWebRequestMessage request2 = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "Get",
                    new Uri(string.Format(this.TestClientContext.BaseUri + "/Flights({0})/Airline/$ref", flight.FlightId)
                        , UriKind.Absolute),
                    false,
                    false,
                    headers));

            DataServiceTransportException exception = null;

            try
            {
                request2.GetResponse();
            }
            catch (DataServiceTransportException e)
            {
                exception = e;
            }
            Assert.NotNull(exception);
            Assert.Equal(404, exception.Response.StatusCode);

            // TODO GitHubIssue#288 : 204 is expected.
            // Query an Navigation Property
            try
            {
                airline2 = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "FlightId", flight.FlightId } }).Airline.GetValue();
            }
            catch (DataServiceQueryException e)
            {
                Assert.Equal(404, e.Response.StatusCode);
            }
        }

        [Fact]
        public void PostNonContainedEntityToNavigationProperty()
        {
            // Note that this scenario DOES NOT conform to OData spec because the client
            // should post a non-contained entity directly to the entity set rather than
            // the navigation property. This case is just to repro a customer scenario
            // and test if the action TrippinController.PostToTripsFromPeople works.
            var personId = 2;
            var person = this.TestClientContext.People.ByKey(
                new Dictionary<string, object> { { "PersonId", personId } }).GetValue();

            var startDate = DateTime.Now;
            var trip = new Trip()
            {
                PersonId = personId,
                TrackGuid = Guid.NewGuid(),
                ShareId = new Guid("32a7ce27-7092-4754-a694-3ebf90278d0b"),
                Name = "Mars",
                Budget = 2000.0f,
                Description = "Happy Mars trip",
                StartsAt = startDate,
                EndsAt = startDate.AddYears(14),
                LastUpdated = DateTime.UtcNow,
            };

            // By default, this line of code would issue a POST request to the entity set
            // for non-contained navigation property.
            this.TestClientContext.AddRelatedObject(person, "Trips", trip);
            this.TestClientContext.Configurations.RequestPipeline.OnMessageCreating = args =>
                new HttpWebRequestMessage(
                    new DataServiceClientRequestMessageArgs(
                        args.Method,
                        new Uri(string.Format(this.TestClientContext.BaseUri + "/People({0})/Trips", personId),
                            UriKind.Absolute), // Force POST to navigation property instead of entity set.
                        args.UseDefaultCredentials,
                        args.UsePostTunneling,
                        args.Headers));
            var response = this.TestClientContext.SaveChanges();
            Assert.Equal(201, response.Single().StatusCode);
        }

        [Fact]
        public void CURDCollectionNavigationPropertyAndRef()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            Person person = new Person()
            {
                FirstName = "Sheldon",
                LastName = "Cooper",
                UserName = "SheldonCooper"
            };

            this.TestClientContext.AddToPeople(person);
            this.TestClientContext.SaveChanges();
            long? personId = person.PersonId;

            var startDate = DateTime.Now;
            Trip trip1 = new Trip()
            {
                PersonId = personId,
                TrackGuid = Guid.NewGuid(),
                ShareId = new Guid("c95d15e8-582b-44cf-9e97-ff63a5f26091"),
                Name = "Honeymoon",
                Budget = 2000.0f,
                Description = "Happy honeymoon trip",
                StartsAt = startDate,
                EndsAt = startDate.AddDays(3),
                LastUpdated = DateTime.UtcNow,
            };

            Trip trip2 = new Trip()
            {
                PersonId = personId,
                TrackGuid = Guid.NewGuid(),
                ShareId = new Guid("56947cf5-2133-43b8-81f0-b6c3f1e5e51a"),
                Name = "Honeymoon",
                Budget = 3000.0f,
                Description = "Happy honeymoon trip",
                StartsAt = startDate.AddDays(1),
                EndsAt = startDate.AddDays(5),
                LastUpdated = DateTime.UtcNow,
            };

            this.TestClientContext.AddToTrips(trip1);
            this.TestClientContext.SaveChanges();

            // Create a related entity by Navigation link
            this.TestClientContext.AddRelatedObject(person, "Trips", trip2);
            this.TestClientContext.SaveChanges();

            // Query Navigation properties.
            var trips = this.TestClientContext.People
                .ByKey(new Dictionary<string, object> { { "PersonId", personId } })
                .Trips.ToList();
            Assert.Equal(2, trips.Count());
            Assert.True(trips.Any(t => t.TripId == trip1.TripId));
            Assert.True(trips.Any(t => t.TripId == trip2.TripId));

            // Get $ref
            HttpWebRequestMessage request = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "Get",
                    new Uri(string.Format(this.TestClientContext.BaseUri + "/People({0})/Trips/$ref", personId),
                        UriKind.Absolute),
                    false,
                    false,
                    new Dictionary<string, string>()));

            using (var response = request.GetResponse() as HttpWebResponseMessage)
            {
                Assert.Equal(200, response.StatusCode);
                using (var stream = response.GetStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    var expectedPayload = "{"
                        + @"  ""@odata.context"":""http://localhost:18384/api/Trippin/$metadata#Collection($ref)"",""value"":["
                        + @"    {"
                        + string.Format(@"      ""@odata.id"":""http://localhost:18384/api/Trippin/Trips({0})""", trip1.TripId)
                        + "    },{"
                        + string.Format(@"      ""@odata.id"":""http://localhost:18384/api/Trippin/Trips({0})""", trip2.TripId)
                        + "    }"
                        + "  ]"
                        + "}";
                    var content = reader.ReadToEnd();
                    Assert.Equal(expectedPayload.Replace(" ", ""), content);
                }
            }

            // Delete $ref
            this.TestClientContext.DeleteLink(person, "Trips", trip2);
            this.TestClientContext.SaveChanges();

            // Expand Navigation properties
            this.TestClientContext.Detach(trip1);
            person = this.TestClientContext.People
                .ByKey(new Dictionary<string, object> { { "PersonId", personId } })
                .Expand(t => t.Trips)
                .GetValue();
            Assert.Equal(1, person.Trips.Count);
            Assert.True(person.Trips.Any(t => t.TripId == trip1.TripId));

            Person person2 = new Person()
            {
                FirstName = "Sheldon2",
                LastName = "Cooper2",
                UserName = "SheldonCooper2"
            };

            this.TestClientContext.AddToPeople(person2);
            this.TestClientContext.SaveChanges();
            personId = person2.PersonId;

            // Add $ref
            this.TestClientContext.AddLink(person2, "Trips", trip2);
            this.TestClientContext.SaveChanges();

            // Expand Navigation properties
            this.TestClientContext.Detach(trip1);
            this.TestClientContext.Detach(trip2);
            person = this.TestClientContext.People
                .ByKey(new Dictionary<string, object> { { "PersonId", personId } })
                .Expand(t => t.Trips)
                .GetValue();
            Assert.Equal(1, person.Trips.Count);
            Assert.True(person.Trips.Any(t => t.TripId == trip2.TripId));
        }

        [Fact]
        public void QueryWithFormat()
        {
            Dictionary<string, string> testCases = new Dictionary<string, string>()
            {
                {"People?$format=application/json", "application/json"},
                // ODL Bug: https://github.com/OData/odata.net/issues/313
                ////{"People?$format=application/json;odata.metadata=full", "application/json; odata.metadata=full"},
                {"People?$format=json", "application/json"},
            };

            ODataMessageReaderSettings readerSettings = new ODataMessageReaderSettings()
            {
                BaseUri = ServiceBaseUri,
                UndeclaredPropertyBehaviorKinds = ODataUndeclaredPropertyBehaviorKinds.IgnoreUndeclaredValueProperty
            };

            foreach (var testCase in testCases)
            {
                DataServiceClientRequestMessageArgs args = new DataServiceClientRequestMessageArgs(
                    "GET",
                    new Uri(ServiceBaseUri.AbsoluteUri + testCase.Key, UriKind.Absolute),
                    false, false, new Dictionary<string, string>() { });

                var requestMessage = new HttpWebRequestMessage(args);
                using (var responseMessage = requestMessage.GetResponse() as HttpWebResponseMessage)
                {
                    Assert.Equal(200, responseMessage.StatusCode);

                    string contentType = responseMessage.Headers.FirstOrDefault(x => x.Key.Equals("Content-Type")).Value;
                    Assert.True(contentType.StartsWith(testCase.Value));

                    using (var messageReader = new ODataMessageReader(
                        responseMessage,
                        readerSettings,
                        this.TestClientContext.Format.LoadServiceModel()))
                    {
                        var reader = messageReader.CreateODataFeedReader();

                        while (reader.Read())
                        {
                            if (reader.State == ODataReaderState.EntryEnd)
                            {
                                ODataEntry entry = reader.Item as ODataEntry;
                                Assert.NotNull(entry.Properties.Single(p => p.Name == "PersonId").Value);
                            }
                            else if (reader.State == ODataReaderState.FeedEnd)
                            {
                                Assert.NotNull(reader.Item as ODataFeed);
                            }
                        }

                        Assert.Equal(ODataReaderState.Completed, reader.State);
                    }
                }
            }
        }

        [Fact]
        public void BatchRequest()
        {
            this.TestClientContext.MergeOption = Microsoft.OData.Client.MergeOption.OverwriteChanges;
            SaveChangesOptions[] options = new SaveChangesOptions[]
            {
                SaveChangesOptions.BatchWithSingleChangeset,
                SaveChangesOptions.BatchWithIndependentOperations
            };

            Airline airline = new Airline()
            {
                Name = "American Delta",
                AirlineCode = "DL",
                TimeStampValue = new byte[] { 0 }
            };

            // Post an entity
            Flight flight = new Flight()
            {
                ConfirmationCode = "JH58496",
                FlightNumber = "DL589",
                StartsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 15, 00, 0)),
                EndsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 16, 30, 0)),
                AirlineId = "DL",
                SeatNumber = "C32",
                FromId = "KSEA",
                ToId = "ZSSS"
            };

            foreach (var option in options)
            {
                this.TestClientContext.ResetDataSource().Execute();
                this.TestClientContext.AddToAirlines(airline);
                this.TestClientContext.AddToFlights(flight);
                DataServiceResponse response1 = this.TestClientContext.SaveChanges(option);
                if (response1.BatchStatusCode != 200)
                {
                    Assert.NotNull(response1);
                }
                Assert.True(response1.IsBatchResponse);
                foreach (OperationResponse item in response1)
                {
                    Assert.Equal(201, item.StatusCode);
                }
                Assert.Equal(2, response1.Count());

                this.TestClientContext.Detach(airline);
                this.TestClientContext.Detach(flight);

                var request1 = this.TestClientContext.Airlines.Where(al => al.AirlineCode == airline.AirlineCode) as DataServiceQuery<Airline>;
                var request2 = this.TestClientContext.Flights.Where(f => f.FlightId == flight.FlightId) as DataServiceQuery<Flight>;
                var response2 = this.TestClientContext.ExecuteBatch(new DataServiceRequest[] { request1, request2 });
                Assert.NotNull(response2);
                Assert.True(response2.IsBatchResponse);
                var resp2 = response2.ToList();
                Assert.Equal(2, resp2.Count());
                foreach (QueryOperationResponse item in resp2)
                {
                    Assert.Equal(200, item.StatusCode);
                }
                this.TestClientContext.Detach(airline);
                this.TestClientContext.Detach(flight);
            }
        }

        [Fact]
        public void PostReturnBadRequestStatusWithSeviceValidationMessage()
        {
            Person person = new Person() { FirstName = "Fn", LastName = "" };
            this.TestClientContext.AddToPeople(person);
            var ex = Assert.Throws<DataServiceRequestException>(() => this.TestClientContext.SaveChanges());
            var clientException = Assert.IsAssignableFrom<DataServiceClientException>(ex.InnerException);
            Assert.Equal(400, clientException.StatusCode);
            Assert.Contains(
                "The field LastName must be a string or array type with a minimum length of '1'",
                clientException.Message);
        }

        [Fact]
        public void RequestNonExistingEntityShouldReturnNotFound()
        {
            var requestMessage = new HttpWebRequestMessage(
                new DataServiceClientRequestMessageArgs(
                    "GET",
                    new Uri(this.ServiceBaseUri.OriginalString + "Airlines('NonExisting')", UriKind.Absolute),
                    useDefaultCredentials: true,
                    usePostTunneling: false,
                    headers: new Dictionary<string, string>()));

            DataServiceTransportException exception = null;

            try
            {
                requestMessage.GetResponse();
            }
            catch (DataServiceTransportException e)
            {
                exception = e;
            }

            Assert.NotNull(exception);
            Assert.Equal(404, exception.Response.StatusCode);
        }

        [Fact]
        public void ConventionBasedChangeSetAuthorizerTest()
        {
            var trip = this.TestClientContext.Trips.First();
            this.TestClientContext.DeleteObject(trip);
            var ex = Assert.Throws<DataServiceRequestException>(() => this.TestClientContext.SaveChanges());
            var clientException = Assert.IsAssignableFrom<DataServiceClientException>(ex.InnerException);
            Assert.Equal(403, clientException.StatusCode);
            Assert.Contains(
                "The current user does not have permission to delete entities from the EntitySet 'Trips'.",
                clientException.Message);
        }

        [Fact]
        public void TestPatchSuccessfully()
        {
            // Get origin content.
            var uriStringAfterServiceRoot = "Orders(PersonId=1, OrderId=1)";
            var originContent = default(string);
            Action<string> getContent = p => originContent = p;
            TestGetPayload(uriStringAfterServiceRoot, getContent);

            // Patch it.
            var changedDescription = "TestDescription";
            var changedNormalProperty = "TestNormalProperty";
            string patchContent =
                string.Format(
                    "{{\n    \"Description\": \"{0}\",\n    \"NormalOrderDetail\": {{\n        \"NormalProperty\": \"{1}\"\n    }}\n}}",
                    changedDescription,
                    changedNormalProperty);
            bool result = TestPatchStatusCodeIs(uriStringAfterServiceRoot, patchContent, HttpStatusCode.NoContent).Wait(1000);
            Assert.Equal(true, result);

            // Test patch results.
            dynamic content = JsonConvert.DeserializeObject(originContent);
            content.Description = changedDescription;
            content.NormalOrderDetail.NormalProperty = changedNormalProperty;
            string changedContent = JsonConvert.SerializeObject(content);
            TestGetPayloadContains(uriStringAfterServiceRoot, changedContent);
        }
    }
}
