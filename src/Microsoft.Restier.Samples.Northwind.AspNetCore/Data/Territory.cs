using System;
using System.Collections.Generic;

#nullable disable

namespace Microsoft.Restier.Samples.Northwind.AspNetCore
{
    public partial class Territory
    {
        public Territory()
        {
        }

        public string TerritoryId { get; set; }
        public string TerritoryDescription { get; set; }
        public int RegionId { get; set; }

        public virtual Region Region { get; set; }
    }
}
