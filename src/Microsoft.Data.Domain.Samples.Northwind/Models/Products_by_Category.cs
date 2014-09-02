namespace Microsoft.Data.Domain.Samples.Northwind.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Products by Category")]
    public partial class Products_by_Category
    {
        [Key]
        [Column(Order = 0)]
        [StringLength(15)]
        public string CategoryName { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(40)]
        public string ProductName { get; set; }

        [StringLength(20)]
        public string QuantityPerUnit { get; set; }

        public short? UnitsInStock { get; set; }

        [Key]
        [Column(Order = 2)]
        public bool Discontinued { get; set; }
    }
}
