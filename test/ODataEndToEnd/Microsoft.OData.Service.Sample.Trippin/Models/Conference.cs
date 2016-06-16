using System.Collections.Generic;

namespace Microsoft.OData.Service.Sample.Trippin.Models
{
    public class Conference
    {
        public virtual ICollection<Sponsor> Sponsors { get; set; }

        public int ConferenceId { get; set; }

        public string Name { get; set; }

        public int NumberOfAttendees { get; set; }
    }
}