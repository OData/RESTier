// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.OData.Builder;

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Models
{
    public enum PersonGender
    {
        Male,
        Female,
        Unknow
    }

    public enum Feature
    {
        Feature1,
        Feature2,
        Feature3,
        Feature4
    }

    public class Person
    {
        [Key]
        public string UserName { get; set; }

        [Required]
        public string FirstName { get; set; }

        [MaxLength(26), MinLength(1)]
        public string LastName { get; set; }

        public string MiddleName { get; set; }

        public PersonGender Gender { get; set; }

        public long? Age { get; set; }

        public ICollection<string> Emails { get; set; }

        public ICollection<Location> AddressInfo { get; set; }

        public Location HomeAddress { get; set; }

        public virtual ICollection<Person> Friends { get; set; }

        public Person BestFriend { get; set; }

        public virtual ICollection<Trip> Trips { get; set; }

        public Feature FavoriteFeature { get; set; }

        public virtual ICollection<Feature> Features { get; set; }
    }
}