namespace MyWebApplication.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Flight
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Flight()
        {
            TripsTables = new HashSet<TripsTable>();
        }

        public int FlightId { get; set; }

        public string ConfirmationCode { get; set; }

        public DateTimeOffset StartsAt { get; set; }

        public DateTimeOffset EndsAt { get; set; }

        public TimeSpan Duration { get; set; }

        public string SeatNumber { get; set; }

        public string FlightNumber { get; set; }

        [StringLength(128)]
        public string FromId { get; set; }

        [StringLength(128)]
        public string ToId { get; set; }

        [StringLength(128)]
        public string AirlineId { get; set; }

        public virtual Airline Airline { get; set; }

        public virtual Airport Airport { get; set; }

        public virtual Airport Airport1 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TripsTable> TripsTables { get; set; }
    }
}
