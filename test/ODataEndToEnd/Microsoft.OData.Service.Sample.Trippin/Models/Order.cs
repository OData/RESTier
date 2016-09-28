// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.OData.Service.Sample.Trippin.Models
{
    public class Order
    {
        public Order()
        {
            // Set the computed value in constructor, it could be done by database layer
            ComputedProperty = "OrderComputedValue";
            ComputedOrderDetail = new OrderDetail()
            {
                NormalProperty = "OrderDetailNormalProperty",
                ImmutableProperty = "OrderDetailImmutableProperty"
            };
        }

        [Key]
        [Column(Order = 0)]
        public int PersonId { get; set; }

        [Key]
        [Column(Order = 1)]
        public int OrderId { get; set; }

        public double Price { get; set; }

        public string ComputedProperty { get; set; }

        public string ImmutableProperty { get; set; }

        public OrderDetail NormalOrderDetail { get; set; }

        public OrderDetail ComputedOrderDetail { get; set; }

        public OrderDetail ImmutableOrderDetail { get; set; }

        public string Description { get; set; }
    }
}