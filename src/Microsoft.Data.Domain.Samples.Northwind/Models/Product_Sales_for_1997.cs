// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Data.Domain.Samples.Northwind.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Product Sales for 1997")]
    public partial class Product_Sales_for_1997
    {
        [Key]
        [Column(Order = 0)]
        [StringLength(15)]
        public string CategoryName { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(40)]
        public string ProductName { get; set; }

        [Column(TypeName = "money")]
        public decimal? ProductSales { get; set; }
    }
}
