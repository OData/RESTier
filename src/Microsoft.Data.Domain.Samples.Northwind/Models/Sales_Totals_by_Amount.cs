namespace Microsoft.Data.Domain.Samples.Northwind.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Sales Totals by Amount")]
    public partial class Sales_Totals_by_Amount
    {
        [Column(TypeName = "money")]
        public decimal? SaleAmount { get; set; }

        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int OrderID { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(40)]
        public string CompanyName { get; set; }

        public DateTime? ShippedDate { get; set; }
    }
}
