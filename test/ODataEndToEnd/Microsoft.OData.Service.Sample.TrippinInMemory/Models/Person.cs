// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Models
{
    public enum PersonGender
    {
        Male,
        Female,
        Unknow
    }

    public class Person
    {
        [Key]
        [ConcurrencyCheck]
        public string UserName { get; set; }

        [Required]
        public string FirstName { get; set; }

        [MaxLength(26), MinLength(1)]
        public string LastName { get; set; }

        public PersonGender Gender { get; set; }

        public long? Age { get; set; }

        public ICollection<string> Emails { get; set; }

        public ICollection<Location> AddressInfo { get; set; }

        public virtual ICollection<Person> Friends { get; set; }

        public virtual ICollection<Trip> Trips { get; set; }

        [ConcurrencyCheck]
        public long Concurrency { get; set; }
    }
}