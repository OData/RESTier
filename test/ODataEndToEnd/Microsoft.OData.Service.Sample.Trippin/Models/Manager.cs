// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.OData.Service.Sample.Trippin.Models
{
    public class Manager : Person
    {
        public virtual ICollection<Person> DirectReports { get; set; }
        
        public long Budget { get; set; }

        public Location BossOffice { get; set; }
    }
}
