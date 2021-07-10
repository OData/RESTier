// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EF6
using System.Data.Entity;
#else
using Microsoft.EntityFrameworkCore;
#endif

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// The data context for the Library scenario.
    /// </summary>
    public class LibraryContext : DbContext
    {

#if EF6
        public LibraryContext()
           : base("LibraryContext") => Database.SetInitializer(new LibraryTestInitializer());

        public IDbSet<Book> Books { get; set; }

        public IDbSet<LibraryCard> LibraryCards { get; set; }

        public IDbSet<Publisher> Publishers { get; set; }

        public IDbSet<Employee> Readers { get; set; }

#endif

#if EFCore
        #region EntitySet Properties

        public DbSet<Book> Books { get; set; }

        public DbSet<LibraryCard> LibraryCards { get; set; }

        public DbSet<Publisher> Publishers { get; set; }

        public DbSet<Employee> Readers { get; set; }

        #endregion

        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(nameof(LibraryContext));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>().OwnsOne(c => c.Addr);
            modelBuilder.Entity<Employee>().OwnsOne(c => c.Universe);
            modelBuilder.Entity<Publisher>().OwnsOne(c => c.Addr);
        }
#endif

    }

}
