using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.OData.Service.Sample.Trippin.Models
{
    public class Staff
    {
        public virtual ICollection<Staff> PeerStaffs { get; set; }

        public virtual ICollection<Conference> Conferences { get; set; }

        public int StaffId { get; set; }

        public string UserName { get; set; }

        [Required]
        public string FirstName { get; set; }

        public int YearOfService { get; set; }
    }
}