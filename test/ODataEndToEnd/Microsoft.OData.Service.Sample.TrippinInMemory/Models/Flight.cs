// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Models
{
    public class Flight : PublicTransportation
    {
        public string FlightNumber { get; set; }

        public Airline Airline { get; set; }

        public virtual Airport From { get; set; }

        public virtual Airport To { get; set; }

        public override object Clone()
        {
            var newPlan = new Flight()
            {
                ConfirmationCode = this.ConfirmationCode,
                Duration = this.Duration,
                EndsAt = this.EndsAt,
                PlanItemId = this.PlanItemId,
                StartsAt = this.StartsAt,
                FlightNumber = this.FlightNumber,
                Airline = this.Airline,
                From = this.From,
                SeatNumber = this.SeatNumber,
                To = this.To,
            };

            return newPlan;
        }
    }
}