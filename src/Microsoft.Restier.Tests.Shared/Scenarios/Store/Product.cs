using System.ComponentModel.DataAnnotations;

namespace Microsoft.Restier.Tests.Shared
{
    internal class Product
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [Required]
        public Address Addr { get; set; }

        public Address Addr2 { get; set; }

        public Address Addr3 { get; set; }
    }
}
