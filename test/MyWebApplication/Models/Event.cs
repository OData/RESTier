namespace MyWebApplication.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Event
    {
        public int Id { get; set; }

        public string OccursAt_Address { get; set; }

        public string Description { get; set; }

        public int? Trip_TripId { get; set; }

        public virtual TripsTable TripsTable { get; set; }
    }
}
