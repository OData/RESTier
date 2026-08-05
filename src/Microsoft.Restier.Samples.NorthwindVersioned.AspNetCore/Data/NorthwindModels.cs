// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.Data
{

    public class Customer
    {
        public string CustomerId { get; set; }
        public string CompanyName { get; set; }

        // Email exists on the entity but is hidden by V1's DbContext via Ignore().
        public string Email { get; set; }
    }

    public class Order
    {
        public int OrderId { get; set; }
        public string CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
    }

    // V2-only entity set
    public class OrderShipment
    {
        public int OrderShipmentId { get; set; }
        public int OrderId { get; set; }
        public string Carrier { get; set; }
        public string TrackingNumber { get; set; }
    }

}
