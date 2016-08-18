// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OData.Client;
using Microsoft.OData.Service.Sample.Trippin.Models;
using Xunit;

namespace Microsoft.OData.Service.Sample.Tests
{
    public class TrippinE2EEtagTestCases : TrippinE2ETestBase
    {
        [Fact]
        public void EtagAnnotationTesting()
        {
            this.TestGetPayloadContains("Flights", "@odata.etag");
            this.TestGetPayloadContains("Flights?$select=FlightId", "@odata.etag");
            this.TestGetPayloadContains("Flights(1)", "@odata.etag");
        }

        [Fact]
        public void IfMatchCRUDTest()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            Flight flight = new Flight()
            {
                ConfirmationCode = "JH44444",
                StartsAt = DateTimeOffset.Parse("2016-01-04T17:55:00+08:00"),
                EndsAt = DateTimeOffset.Parse("2016-01-04T20:45:00+08:00"),
                Duration = TimeSpan.Parse("2:50"),
                FlightNumber = "AA4035",
                FromId = "KJFK",
                ToId = "KORD",
                AirlineId = "AA"
            };

            this.TestClientContext.AddToFlights(flight);
            this.TestClientContext.SaveChanges();
            int flightId = flight.FlightId;
            var code = flight.ConfirmationCode;

            string etag = null;
            int statusCode = -1;
            EventHandler<ReceivingResponseEventArgs> statusCodeHandler = (sender, eventArgs) =>
            {
                etag = eventArgs.ResponseMessage.GetHeader("ETag");
                statusCode = eventArgs.ResponseMessage.StatusCode;
            };
            this.TestClientContext.ReceivingResponse += statusCodeHandler;

            // Retrieve a none match etag
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", 1 } }).GetValue();
            var nonMatchEtag = etag;

            // Request single entity, the header should return Etag
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();
            Assert.Equal(code, flight.ConfirmationCode);
            Assert.NotNull(etag);
            Assert.Equal(200, statusCode);

            // Test If-Match header and the header is not matched, should return 412.
            EventHandler<SendingRequest2EventArgs> sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-Match", nonMatchEtag);
            };
            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            try
            {
                flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();
            }
            catch (DataServiceQueryException)
            {
            }
            Assert.Equal(412, statusCode);

            // Test If-Match header and the header is matched, should return the entity.
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-Match", etag );
            };
            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();
            Assert.Equal(code, flight.ConfirmationCode);
            Assert.Equal(200, statusCode);
            var oldEtag = etag;

            // Update the Entity without If-Match header, should return 428
            // If this is not removed, client will auto add If-Match header
            var descriptor = this.TestClientContext.GetEntityDescriptor(flight);
            descriptor.ETag = null;
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            code = "JH33333";
            flight.ConfirmationCode = code;
            this.TestClientContext.UpdateObject(flight);
            try
            {
                this.TestClientContext.SaveChanges();
            }
            catch (DataServiceRequestException)
            {
            }
            this.TestClientContext.Detach(flight);
            Assert.Equal(428, statusCode);

            // Query to get etag updated
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();

            // Update the Entity with If-Match not match, should return 412
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-Match", nonMatchEtag);
            };
            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            code = "JH33333";
            flight.ConfirmationCode = code;
            this.TestClientContext.UpdateObject(flight);
            try
            {
                this.TestClientContext.SaveChanges();
            }
            catch (DataServiceRequestException)
            {
            }
            this.TestClientContext.Detach(flight);
            Assert.Equal(412, statusCode);

            // Query the flight again and etag should not be updated.
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();
            Assert.NotEqual(code, flight.ConfirmationCode);
            Assert.Equal(200, statusCode);
            Assert.Equal(oldEtag, etag);

            // Update the Entity with If-Match matches, should return 204
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-Match", etag);
            };
            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            code = "JH33333";
            flight.ConfirmationCode = code;
            this.TestClientContext.UpdateObject(flight);
            this.TestClientContext.SaveChanges();
            this.TestClientContext.Detach(flight);
            Assert.Equal(204, statusCode);

            // Query the flight again and etag should be updated.
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();
            Assert.Equal(code, flight.ConfirmationCode);
            Assert.Equal(200, statusCode);
            Assert.NotEqual(oldEtag, etag);

            // Delete the Entity with If-Match does not match, should return 412
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-Match", nonMatchEtag);
            };
            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            this.TestClientContext.DeleteObject(flight);
            try
            {
                this.TestClientContext.SaveChanges();
            }
            catch (DataServiceRequestException)
            {
            }
            Assert.Equal(412, statusCode);

            // Query to get etag updated
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();

            // Delete the Entity with If-Match matches, should return 204
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-Match", etag);
            };
            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            this.TestClientContext.DeleteObject(flight);
            this.TestClientContext.SaveChanges();
            Assert.Equal(204, statusCode);

            // Query the flight again and entity does not exist.
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            try
            {
                flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();
            }
            catch (DataServiceQueryException)
            {
            }
            Assert.Equal(404, statusCode);
        }

        [Fact]
        public void IfNoneMatchCRUDTest()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;

            // Post an entity
            Flight flight = new Flight()
            {
                ConfirmationCode = "JH44444",
                StartsAt = DateTimeOffset.Parse("2016-01-04T17:55:00+08:00"),
                EndsAt = DateTimeOffset.Parse("2016-01-04T20:45:00+08:00"),
                Duration = TimeSpan.Parse("2:50"),
                FlightNumber = "AA4035",
                FromId = "KJFK",
                ToId = "KORD",
                AirlineId = "AA"
            };

            this.TestClientContext.AddToFlights(flight);
            this.TestClientContext.SaveChanges();
            int flightId = flight.FlightId;
            var code = flight.ConfirmationCode;

            string etag = null;
            int statusCode = -1;
            EventHandler<ReceivingResponseEventArgs> statusCodeHandler = (sender, eventArgs) =>
            {
                etag = eventArgs.ResponseMessage.GetHeader("ETag");
                statusCode = eventArgs.ResponseMessage.StatusCode;
            };
            this.TestClientContext.ReceivingResponse += statusCodeHandler;

            // Retrieve a none match etag
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", 1 } }).GetValue();
            var nonMatchEtag = etag;

            // Request single entity, the header should return Etag
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();
            Assert.Equal(code, flight.ConfirmationCode);
            Assert.NotNull(etag);
            Assert.Equal(200, statusCode);

            // Test If-None-Match header and the header is matched, should return 304 (not modified).
            EventHandler<SendingRequest2EventArgs> sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-None-Match", etag);
            };
            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            try
            {
                flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() {{"flightId", flightId}}).GetValue();
            }
            catch (DataServiceQueryException)
            {
            }
            Assert.Equal(304, statusCode);

            // Test If-None-Match header and the header is not matched, should return the entity.
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-None-Match", nonMatchEtag);
            };
            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();
            Assert.Equal(code, flight.ConfirmationCode);
            Assert.Equal(200, statusCode);
            var oldEtag = etag;

            // Update the Entity without If-None-Match header, should return 428
            // If this is not removed, client will auto add If-Match header
            var descriptor = this.TestClientContext.GetEntityDescriptor(flight);
            descriptor.ETag = null;
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            code = "JH3333333";
            flight.ConfirmationCode = code;
            this.TestClientContext.UpdateObject(flight);

            try
            {
                this.TestClientContext.SaveChanges();
            }
            catch (DataServiceRequestException)
            {
            }
            this.TestClientContext.Detach(flight);
            Assert.Equal(428, statusCode);

            // Query to get etag updated
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();

            // Update the Entity with If-None-Match matches, should return 412
            // If this is not removed, client will auto add If-Match header
            descriptor = this.TestClientContext.GetEntityDescriptor(flight);
            descriptor.ETag = null;
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-None-Match", etag);
            };
            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            code = "JH3333333";
            flight.ConfirmationCode = code;
            this.TestClientContext.UpdateObject(flight);

            try
            {
                this.TestClientContext.SaveChanges();
            }
            catch (DataServiceRequestException)
            {
            }
            this.TestClientContext.Detach(flight);
            Assert.Equal(412, statusCode);

            // Query the flight again and etag should not be updated.
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();
            Assert.NotEqual(code, flight.ConfirmationCode);
            Assert.Equal(200, statusCode);
            Assert.Equal(oldEtag, etag);

            // Update the Entity with If-None-Match does not match, should return 204
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-None-Match", nonMatchEtag);
            };
            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            code = "JH3333333";
            flight.ConfirmationCode = code;
            this.TestClientContext.UpdateObject(flight);
            this.TestClientContext.SaveChanges();
            this.TestClientContext.Detach(flight);
            Assert.Equal(204, statusCode);

            // Query the flight again and etag should be updated.
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();
            Assert.Equal(code, flight.ConfirmationCode);
            Assert.Equal(200, statusCode);
            Assert.NotEqual(oldEtag, etag);

            // Delete the Entity with If-None-Match matches, should return 412
            // If this is not removed, client will auto add If-Match header
            descriptor = this.TestClientContext.GetEntityDescriptor(flight);
            descriptor.ETag = null;
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-None-Match", etag);
            };
            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            this.TestClientContext.DeleteObject(flight);
            try
            {
                this.TestClientContext.SaveChanges();
            }
            catch (DataServiceRequestException)
            {
            }
            Assert.Equal(412, statusCode);

            // Query to get etag updated
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();

            // Delete the Entity with If-None-Match does not match, should return 204
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-None-Match", nonMatchEtag);
            };
            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            this.TestClientContext.DeleteObject(flight);
            this.TestClientContext.SaveChanges();
            Assert.Equal(204, statusCode);

            // Query the flight again and entity does not exist.
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            try
            {
                flight = this.TestClientContext.Flights.ByKey(new Dictionary<string, object>() { { "flightId", flightId } }).GetValue();
            }
            catch (DataServiceQueryException)
            {
            }
            Assert.Equal(404, statusCode);
        }

        [Fact]
        public void ByteArrayIfMatchTest()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;
            var airline = new Airline()
            {
                AirlineCode = "TT",
                Name = "Test Airlines"
            };

            this.TestClientContext.AddToAirlines(airline);
            this.TestClientContext.SaveChanges();

            string etag = null;
            int statusCode = -1;
            EventHandler<ReceivingResponseEventArgs> statusCodeHandler = (sender, eventArgs) =>
            {
                etag = eventArgs.ResponseMessage.GetHeader("ETag");
                statusCode = eventArgs.ResponseMessage.StatusCode;
            };

            this.TestClientContext.ReceivingResponse += statusCodeHandler;

            // Retrieve the matched etag
            airline =
                this.TestClientContext.Airlines.ByKey(new Dictionary<string, object>()
                {
                    {"AirlineCode", airline.AirlineCode}
                }).GetValue();
            var matchEtag = etag;

            // Retrieve a none match etag
            var anotherAirline =
                this.TestClientContext.Airlines.ByKey(new Dictionary<string, object>() {{"AirlineCode", "AA"}})
                    .GetValue();
            var nonMatchEtag = etag;

            // Delete the Entity with If-Match does not match, should return 412
            EventHandler<SendingRequest2EventArgs> sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-Match", nonMatchEtag);
            };

            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            this.TestClientContext.DeleteObject(airline);
            Assert.Throws<DataServiceRequestException>(() => this.TestClientContext.SaveChanges());
            Assert.Equal(412, statusCode);

            // Delete the Entity with If-Match matches, should return 204
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            sendRequestEvent = (sender, eventArgs) =>
            {
                eventArgs.RequestMessage.SetHeader("If-Match", matchEtag);
            };

            this.TestClientContext.SendingRequest2 += sendRequestEvent;
            this.TestClientContext.DeleteObject(airline);
            this.TestClientContext.SaveChanges();
            Assert.Equal(204, statusCode);

            // Query the flight again and entity does not exist.
            this.TestClientContext.SendingRequest2 -= sendRequestEvent;
            Assert.Throws<DataServiceQueryException>(() =>
                airline =
                    this.TestClientContext.Airlines.ByKey(new Dictionary<string, object>()
                    {
                        {"AirlineCode", airline.AirlineCode}
                    }).GetValue()
                );

            Assert.Equal(404, statusCode);
        }
    }
}