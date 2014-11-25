// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Data.Domain.Samples.Northwind.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Order Details Extended")]
    public partial class Order_Details_Extended
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int OrderID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ProductID { get; set; }

        [Key]
        [Column(Order = 2)]
        [StringLength(40)]
        public string ProductName { get; set; }

        [Key]
        [Column(Order = 3, TypeName = "money")]
        public decimal UnitPrice { get; set; }

        [Key]
        [Column(Order = 4)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short Quantity { get; set; }

        [Key]
        [Column(Order = 5)]
        public float Discount { get; set; }

        [Column(TypeName = "money")]
        public decimal? ExtendedPrice { get; set; }
    }
}
