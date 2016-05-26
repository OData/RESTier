// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.OData.Client;
using Microsoft.OData.Service.Sample.Trippin.Models;
using Xunit;

namespace Microsoft.OData.Service.Sample.Tests
{
    public class BatchTests : TrippinE2ETestBase
    {
        [Fact]
        public void SingleChangesetShouldBeAtomic()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;
            SaveChangesOptions[] options = new SaveChangesOptions[]
            {
                // All modifications share one changeset
                SaveChangesOptions.BatchWithSingleChangeset,

                // Each modification uses seperate changeset
                SaveChangesOptions.BatchWithIndependentOperations
            };

            Airline airline = new Airline()
            {
                Name = "American Delta",
                AirlineCode = "DL",
                TimeStampValue = new byte[] { 0 }
            };

            Airline airline1 = new Airline()
            {
                Name = "American Delta",
                AirlineCode = "DL",
                TimeStampValue = new byte[] { 0 }
            };

            Flight flight = new Flight()
            {
                ConfirmationCode = "JH58496",
                FlightNumber = "DL589",
                StartsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 15, 00, 0)),
                EndsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 16, 30, 0)),
                AirlineId = "DL",
                SeatNumber = "C32",
                FromId = "KSEA",
                ToId = "ZSSS",
                Airline = airline
            };

            foreach (var option in options)
            {
                this.TestClientContext.ResetDataSource().Execute();

                this.TestClientContext.AddToAirlines(airline);
                this.TestClientContext.AddToFlights(flight);
                // Post an entity with same ID, this would cause creation failture.
                this.TestClientContext.AddToAirlines(airline1);

                switch (option)
                {
                    case SaveChangesOptions.BatchWithIndependentOperations:
                        DataServiceResponse response1 = this.TestClientContext.SaveChanges(option);
                        Assert.Equal(200, response1.BatchStatusCode);
                        Assert.True(response1.IsBatchResponse);
                        Assert.Equal(3, response1.Count());
                        var result = response1.ToList();

                        // 3rd operation would fail, but new flight entry would be inserted.
                        Assert.Equal(201, result[0].StatusCode);
                        Assert.Equal(201, result[1].StatusCode);
                        Assert.Equal(500, result[2].StatusCode);
                        Assert.Equal(1,
                            this.TestClientContext
                                .Flights.Where(f => f.FlightNumber == flight.FlightNumber).ToList().Count);
                        break;
                    case SaveChangesOptions.BatchWithSingleChangeset:
                        bool exc = false;
                        try
                        {
                            // The single changeset would fail.
                            this.TestClientContext.SaveChanges(option);
                        }
                        catch (Exception)
                        {
                            exc = true;
                        }

                        Assert.True(exc);

                        Assert.Equal(0,
                            this.TestClientContext
                                .Flights.Where(f => f.FlightNumber == flight.FlightNumber).ToList().Count);
                        break;
                }


                this.TestClientContext.Detach(airline1);
                this.TestClientContext.Detach(airline);
                this.TestClientContext.Detach(flight);
            }
        }

        [Fact]
        public void CreateRelatedEntitesWithDifferentChangesetOptions()
        {
            this.TestClientContext.MergeOption = MergeOption.OverwriteChanges;
            SaveChangesOptions[] options = new SaveChangesOptions[]
            {
                // All modifications share one changeset
                SaveChangesOptions.BatchWithSingleChangeset,

                // Each modification uses seperate changeset
                SaveChangesOptions.BatchWithIndependentOperations
            };

            Airline airline = new Airline()
            {
                Name = "American Delta",
                AirlineCode = "DL",
                TimeStampValue = new byte[] { 0 }
            };

            Flight flight = new Flight()
            {
                ConfirmationCode = "JH58496",
                FlightNumber = "DL589",
                StartsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 15, 00, 0)),
                EndsAt = new DateTimeOffset(new DateTime(2014, 2, 10, 16, 30, 0)),
                AirlineId = "DL",
                SeatNumber = "C32",
                FromId = "KSEA",
                ToId = "ZSSS",
                Airline = airline
            };

            foreach (var option in options)
            {
                this.TestClientContext.ResetDataSource().Execute();

                // This should fail for BatchWithIndependentOperations, as the foreign key restriction breaks.
                this.TestClientContext.AddToFlights(flight);
                this.TestClientContext.AddToAirlines(airline);
                this.TestClientContext.SendingRequest2 += (sender, e) =>
                {
                    e.RequestMessage.SetHeader("Prefer", "odata.continue-on-error");
                };

                DataServiceResponse response1 = this.TestClientContext.SaveChanges(option);

                switch (option)
                {
                    case SaveChangesOptions.BatchWithIndependentOperations:
                        
                        Assert.Equal(200, response1.BatchStatusCode);
                        Assert.True(response1.IsBatchResponse);
                        Assert.Equal(2, response1.Count());
                        var result1 = response1.ToList();
                        // fail for adding flight, but succeed for adding airlire
                        Assert.Equal(500, result1[0].StatusCode);
                        Assert.Equal(201, result1[1].StatusCode);
                        Assert.Equal(0,
                            this.TestClientContext
                                .Flights.Where(f => f.FlightNumber == flight.FlightNumber).ToList().Count);
                        break;
                    case SaveChangesOptions.BatchWithSingleChangeset:
                        Assert.Equal(200, response1.BatchStatusCode);
                        Assert.True(response1.IsBatchResponse);
                        Assert.Equal(2, response1.Count());
                        var result2 = response1.ToList();

                        // Both would succeed
                        Assert.Equal(201, result2[0].StatusCode);
                        Assert.Equal(201, result2[1].StatusCode);

                        Assert.Equal(1,
                            this.TestClientContext
                                .Flights.Where(f => f.FlightNumber == flight.FlightNumber).ToList().Count);
                        break;

                }

                this.TestClientContext.Detach(airline);
                this.TestClientContext.Detach(flight);
            }
        }
    }
}
