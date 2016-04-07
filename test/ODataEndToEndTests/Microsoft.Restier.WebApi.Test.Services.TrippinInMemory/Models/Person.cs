// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.OData.Builder;

namespace Microsoft.Restier.WebApi.Test.Services.TrippinInMemory
{
    public class Person
    {
        public Person BestFriend { get; set; }

        // Way 1: enable auto-expand through attribute.
        [AutoExpand]
        public virtual ICollection<Person> Friends { get; set; }

        public virtual ICollection<Trip> Trips { get; set; }

        public int PersonId { get; set; }

        public string UserName { get; set; }

        [Required]
        public string FirstName { get; set; }

        [MaxLength(26), MinLength(1)]
        public string LastName { get; set; }
        
        public string MiddleName { get; set; }

        public long Concurrency { get; set; }

        [Column("BirthDate", TypeName = "Date")]
        public DateTime BirthDate { get; set; }

        public Feature FavoriteFeature { get; set; }

        public virtual ICollection<string> Emails { get; set; }
        
        public Location HomeAddress { get; set; }

        public virtual ICollection<Location> Locations { get; set; } 

        public virtual ICollection<Feature> Features { get; set; } 
    }
}
