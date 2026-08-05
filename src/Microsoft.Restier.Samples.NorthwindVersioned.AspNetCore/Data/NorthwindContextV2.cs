// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.Data
{

    public class NorthwindContextV2 : DbContext
    {

        public NorthwindContextV2(DbContextOptions<NorthwindContextV2> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderShipment> OrderShipments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().HasKey(c => c.CustomerId);
            modelBuilder.Entity<Order>().HasKey(o => o.OrderId);
            modelBuilder.Entity<OrderShipment>().HasKey(s => s.OrderShipmentId);
        }

    }

}
