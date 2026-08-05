// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.Samples.NorthwindVersioned.AspNetCore.Data
{

    public class NorthwindContextV1 : DbContext
    {

        public NorthwindContextV1(DbContextOptions<NorthwindContextV1> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().HasKey(c => c.CustomerId);
            modelBuilder.Entity<Customer>().Ignore(c => c.Email);
            modelBuilder.Entity<Order>().HasKey(o => o.OrderId);
        }

    }

}
