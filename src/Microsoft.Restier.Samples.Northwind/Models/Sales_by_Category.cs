// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.Restier.Samples.Northwind.Models
{
    [Table("Sales by Category")]
    public partial class Sales_by_Category
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CategoryID { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(15)]
        public string CategoryName { get; set; }

        [Key]
        [Column(Order = 2)]
        [StringLength(40)]
        public string ProductName { get; set; }

        [Column(TypeName = "money")]
        public decimal? ProductSales { get; set; }
    }
}
