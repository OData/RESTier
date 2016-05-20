// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.OData.Service.Sample.Trippin.Models
{
    public class Person
    {
        public virtual Person BestFriend { get; set; }

        public virtual ICollection<Person> Friends { get; set; }

        public virtual ICollection<Trip> Trips { get; set; }

        public long PersonId { get; set; }

        public string UserName { get; set; }

        [Required]
        public string FirstName { get; set; }

        [MaxLength(26), MinLength(1)]
        public string LastName { get; set; }

        public long? Age { get; set; }

        public long Concurrency { get; set; }

        [Column(TypeName = "Date")]
        public DateTime BirthDate { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? BirthDate2 { get; set; }

        [Column(TypeName = "Time")]
        public TimeSpan BirthTime { get; set; }

        [Column(TypeName = "Time")]
        public TimeSpan? BirthTime2 { get; set; }

        // Notes:
        //   1) System.DateTime is mapped to Edm.DateTimeOffset by default;
        //   2) The range of SqlDateTime is limited (1753-01-01 ~ 9999-12-31);
        //      so use SqlDateTime2 for wider range (0001-01-01 ~ 9999-12-31).
        [Column(TypeName = "DateTime2")]
        public DateTime BirthDateTime { get; set; }

        [Column(TypeName = "DateTime2")]
        public DateTime? BirthDateTime2 { get; set; }

        public Feature FavoriteFeature { get; set; }

        public Feature? FavoriteFeature2 { get; set; }
    }
}
