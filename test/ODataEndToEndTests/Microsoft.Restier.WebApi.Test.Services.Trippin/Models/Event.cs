using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Models
{
    public class Event
    {
        public int Id { get; set; }
        public Location OccursAt { get; set; }
        public string Description { get; set; }
    }
}