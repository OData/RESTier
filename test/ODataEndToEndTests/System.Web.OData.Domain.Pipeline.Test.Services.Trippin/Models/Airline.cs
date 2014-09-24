using System.ComponentModel.DataAnnotations;

namespace System.Web.OData.Domain.Test.Services.Trippin.Models
{
    public class Airline
    {
        [Key]
        public string AirlineCode { get; set; }

        public string Name { get; set; }
    }
}
