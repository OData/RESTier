using System;

namespace Microsoft.Restier.WebApi.Test.Services.TrippinInMemory
{
    public class Trip
    {
        public int TripId { get; set; }

        public int? PersonId { get; set; }

        public Guid ShareId { get; set; }

        public string Name { get; set; }
        
        public float Budget { get; set; }

        public string Description { get; set; }

        public DateTimeOffset StartsAt { get; set; }

        public DateTimeOffset EndsAt { get; set; }
    }
}