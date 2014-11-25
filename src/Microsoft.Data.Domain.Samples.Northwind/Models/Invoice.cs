// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Data.Domain.Samples.Northwind.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Invoice
    {
        [StringLength(40)]
        public string ShipName { get; set; }

        [StringLength(60)]
        public string ShipAddress { get; set; }

        [StringLength(15)]
        public string ShipCity { get; set; }

        [StringLength(15)]
        public string ShipRegion { get; set; }

        [StringLength(10)]
        public string ShipPostalCode { get; set; }

        [StringLength(15)]
        public string ShipCountry { get; set; }

        [StringLength(5)]
        public string CustomerID { get; set; }

        [Key]
        [Column(Order = 0)]
        [StringLength(40)]
        public string CustomerName { get; set; }

        [StringLength(60)]
        public string Address { get; set; }

        [StringLength(15)]
        public string City { get; set; }

        [StringLength(15)]
        public string Region { get; set; }

        [StringLength(10)]
        public string PostalCode { get; set; }

        [StringLength(15)]
        public string CountryRegion { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(31)]
        public string Salesperson { get; set; }

        [Key]
        [Column(Order = 2)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int OrderID { get; set; }

        public DateTime? OrderDate { get; set; }

        public DateTime? RequiredDate { get; set; }

        public DateTime? ShippedDate { get; set; }

        [Key]
        [Column(Order = 3)]
        [StringLength(40)]
        public string ShipperName { get; set; }

        [Key]
        [Column(Order = 4)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ProductID { get; set; }

        [Key]
        [Column(Order = 5)]
        [StringLength(40)]
        public string ProductName { get; set; }

        [Key]
        [Column(Order = 6, TypeName = "money")]
        public decimal UnitPrice { get; set; }

        [Key]
        [Column(Order = 7)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short Quantity { get; set; }

        [Key]
        [Column(Order = 8)]
        public float Discount { get; set; }

        [Column(TypeName = "money")]
        public decimal? ExtendedPrice { get; set; }

        [Column(TypeName = "money")]
        public decimal? Freight { get; set; }
    }
}
