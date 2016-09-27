using System.Collections.Generic;

namespace Microsoft.OData.Service.Sample.Trippin.Models
{
    public class HighEndConference : Conference
    {
        public virtual ICollection<GlodSponsor> GlodSponsors { get; set; }

        public int NumberofVips { get; set; }
    }
}