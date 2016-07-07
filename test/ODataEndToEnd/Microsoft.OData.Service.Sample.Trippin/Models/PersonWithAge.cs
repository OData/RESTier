// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.OData.Service.Sample.Trippin.Models
{
    public class PersonWithAge
    {

        public PersonWithAge()
        {
            
        }
        public PersonWithAge(Person p)
        {
            this.Id = p.PersonId;
            this.UserName = p.UserName;
            this.FirstName = p.FirstName;
            this.LastName = p.LastName;
        }
        
        public long Id { get; set; }

        public string UserName { get; set; }

        [Required]
        public string FirstName { get; set; }

        [MaxLength(26), MinLength(1)]
        public string LastName { get; set; }
    }
}