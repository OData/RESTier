// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Data.Domain.Samples.Northwind.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Customer and Suppliers by City")]
    public partial class Customer_and_Suppliers_by_City
    {
        [StringLength(15)]
        public string City { get; set; }

        [Key]
        [Column(Order = 0)]
        [StringLength(40)]
        public string CompanyName { get; set; }

        [StringLength(30)]
        public string ContactName { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(9)]
        public string Relationship { get; set; }
    }
}
