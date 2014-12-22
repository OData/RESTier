// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.Restier.Samples.Northwind.Models
{
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
