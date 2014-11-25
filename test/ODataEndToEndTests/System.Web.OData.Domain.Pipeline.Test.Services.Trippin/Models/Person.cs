// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace System.Web.OData.Domain.Test.Services.Trippin.Models
{
    public class Person
    {
        public virtual ICollection<Person> Friends { get; set; }

        public virtual ICollection<Trip> Trips { get; set; }

        public int PersonId { get; set; }

        public string UserName { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public long Concurrency { get; set; }
    }
}
