using System.ComponentModel.DataAnnotations;

namespace System.Web.OData.Domain.Test.Services.Trippin.Models
{
    public class Airport
    {
        public string Name { get; set; }

        [Key]
        public string IcaoCode { get; set; }

        public string IataCode { get; set; }
    }
}
