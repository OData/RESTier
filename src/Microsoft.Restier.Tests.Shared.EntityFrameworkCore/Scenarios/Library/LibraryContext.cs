// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;


namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// The EntityFrameworkCore data context for the Library scenario.
    /// </summary>
    public class LibraryContext : DbContext
    {

        #region EntitySet Properties

        public DbSet<Book> Books { get; set; }

        public DbSet<LibraryCard> LibraryCards { get; set; }

        public DbSet<Publisher> Publishers { get; set; }

        public DbSet<Employee> Readers { get; set; }

        public DbSet<Address> Addresses { get; set; }

        public DbSet<Universe> Universes { get; set; }

        #endregion

        public LibraryContext(DbContextOptions<LibraryContext> options)
        : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(nameof(LibraryContext));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // JHC Note: EF Core will cache the seeded model for reuse
            modelBuilder.Seed();
        }

    }
}
