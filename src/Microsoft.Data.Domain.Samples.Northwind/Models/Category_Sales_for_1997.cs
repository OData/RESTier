// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Data.Domain.Samples.Northwind.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Category Sales for 1997")]
    public partial class Category_Sales_for_1997
    {
        [Key]
        [StringLength(15)]
        public string CategoryName { get; set; }

        [Column(TypeName = "money")]
        public decimal? CategorySales { get; set; }
    }
}
