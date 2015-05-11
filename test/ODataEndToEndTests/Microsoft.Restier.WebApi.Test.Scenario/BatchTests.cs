// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.OData.Client;
using Microsoft.OData.Core;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Models;
using Xunit;

namespace Microsoft.Restier.WebApi.Test.Scenario
{
    public class BatchTests : E2ETestBase<TrippinModel>
    {
        public BatchTests()
            : base(new Uri("http://localhost:18384/api/Trippin/"))
        {
        }

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
                AirlineCode = "DL"
            };

            Airline airline1 = new Airline()
            {
                Name = "American Delta",
                AirlineCode = "DL"
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

                if (option == SaveChangesOptions.BatchWithIndependentOperations)
                {
                    DataServiceResponse response1 = this.TestClientContext.SaveChanges(option);
                    Assert.Equal(200, response1.BatchStatusCode);
                    Assert.True(response1.IsBatchResponse);
                    Assert.Equal(3, response1.Count());
                    var result = response1.ToList();
                    Assert.Equal(201, result[0].StatusCode);
                    Assert.Equal(201, result[1].StatusCode);
                    Assert.Equal(500, result[2].StatusCode);
                    var newFlight =
                        this.TestClientContext.Flights.Where(f => f.FlightNumber == flight.FlightNumber).ToList();
                    Assert.Equal(1, newFlight.Count);
                }
                else
                {
                    bool exc = false;
                    try
                    {
                        this.TestClientContext.SaveChanges(option);
                    }
                    catch (Exception)
                    {
                        exc = true;
                    }

                    Assert.True(exc);
                    var newFlight =
                        this.TestClientContext.Flights.Where(f => f.FlightNumber == flight.FlightNumber).ToList();
                    Assert.Equal(0, newFlight.Count);
                }

                this.TestClientContext.Detach(airline1);
                this.TestClientContext.Detach(airline);
                this.TestClientContext.Detach(flight);
            }
        }
    }
}
