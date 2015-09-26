// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.Restier.WebApi.Test.Services.TrippinInMemory
{
    public class Person
    {
        public virtual ICollection<Person> Friends { get; set; }

        public int PersonId { get; set; }

        public string UserName { get; set; }

        [Required]
        public string FirstName { get; set; }

        [MaxLength(26), MinLength(1)]
        public string LastName { get; set; }

        public long Concurrency { get; set; }

        [Column("BirthDate", TypeName = "Date")]
        public DateTime BirthDate { get; set; }

        public virtual ICollection<string> Emails { get; set; }

        public virtual ICollection<Location> Locations { get; set; } 
    }
}
