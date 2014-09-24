using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace System.Web.OData.Domain.Test.Services.Trippin.Models
{
    public class Flight
    {
        public int FlightId { get; set; }

        public string ConfirmationCode { get; set; }

        public DateTimeOffset StartsAt { get; set; }

        public DateTimeOffset EndsAt { get; set; }

        public TimeSpan Duration { get; set; }

        public string SeatNumber { get; set; }

        public string FlightNumber { get; set; }

        public string FromId { get; set; }

        public string ToId { get; set; }

        public string AirlineId { get; set; }

        [ForeignKey("FromId")]
        public virtual Airport From { get; set; }

        [ForeignKey("ToId")]
        public virtual Airport To { get; set; }

        [ForeignKey("AirlineId")]
        public virtual Airline Airline { get; set; }
    }
}