// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.OData.Service.Sample.Northwind.Models
{
    public partial class Order
    {
        public Order()
        {
            Order_Details = new HashSet<Order_Detail>();
        }

        public int OrderID { get; set; }

        [StringLength(5)]
        public string CustomerID { get; set; }

        public int? EmployeeID { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? OrderDate { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? RequiredDate { get; set; }

        [Column(TypeName = "Date")]
        public DateTime? ShippedDate { get; set; }

        [Range(0, 3)]
        public int? ShipVia { get; set; }

        [Column(TypeName = "money")]
        public decimal? Freight { get; set; }

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

        public virtual Customer Customer { get; set; }

        public virtual Employee Employee { get; set; }

        public virtual ICollection<Order_Detail> Order_Details { get; set; }

        public virtual Shipper Shipper { get; set; }
    }
}
