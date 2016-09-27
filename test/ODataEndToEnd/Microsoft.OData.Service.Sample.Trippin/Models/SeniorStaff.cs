using System.Collections.Generic;

namespace Microsoft.OData.Service.Sample.Trippin.Models
{
    public class SeniorStaff : Staff
    {
        public virtual ICollection<SeniorStaff> PeerSeniorStaffs { get; set; }

        public virtual ICollection<HighEndConference> HighEndConferences { get; set; }

        public int SeniorLevel { get; set; }

        public string SeniorTitle { get; set; }
    }
}