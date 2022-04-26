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
    /// The Entity Framework <see cref="DbContext"/> for the Library scenario.
    /// </summary>
    public class LibraryContext : DbContext
    {

#if EF6

        #region Properties

        public IDbSet<Book> Books { get; set; }

        public IDbSet<LibraryCard> LibraryCards { get; set; }

        public IDbSet<Publisher> Publishers { get; set; }

        public IDbSet<Employee> Readers { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        public LibraryContext() : base("LibraryContext") 
            => Database.SetInitializer(new LibraryTestInitializer());

        #endregion

#endif

#if EFCore

        #region Properties

        public DbSet<Book> Books { get; set; }

        public DbSet<LibraryCard> LibraryCards { get; set; }

        public DbSet<Publisher> Publishers { get; set; }

        public DbSet<Employee> Readers { get; set; }

        #endregion

        #region Constructors

        ///
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options)
        {
        }

        #endregion

        #region Overrides

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

        #endregion

#endif

    }

}
