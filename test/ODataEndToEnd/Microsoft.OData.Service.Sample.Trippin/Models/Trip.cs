// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.OData.Service.Sample.Trippin.Models
{
    [Table("TripsTable")]
    public class Trip
    {
        public int TripId { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public Guid? TrackGuid { get; set; }

        public long? PersonId { get; set; }

        public Guid ShareId { get; set; }

        public string Name { get; set; }

        [Column("BudgetCol", Order = 1)]
        public float Budget { get; set; }

        public string Description { get; set; }

        public DateTimeOffset StartsAt { get; set; }

        public DateTimeOffset EndsAt { get; set; }

        [NotMapped]
        public TimeSpan TotalTimespan { get { return this.EndsAt - this.StartsAt; } }

        public virtual ICollection<Flight> Flights { get; set; }

        [ConcurrencyCheck]
        public DateTimeOffset LastUpdated { get; set; }

        public virtual ICollection<Event> Events { get; set; }
    }
}
