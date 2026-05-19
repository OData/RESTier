// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if EF6
using System.Data.Entity;
#else
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.OData.Edm;
#endif


using Microsoft.Restier.Tests.Shared.Scenarios.Library;

#if EF6
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6
#elif EFCore
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore
#endif
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

        public IDbSet<Review> Reviews { get; set; }

        public IDbSet<SpatialPlace> SpatialPlaces { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        ///
        /// </summary>
        public LibraryContext() : base("LibraryContext")
            => Database.SetInitializer(new LibraryTestInitializer());

        /// <summary>
        /// Creates a new instance with an explicit connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to use.</param>
        public LibraryContext(string connectionString) : base(connectionString)
            => Database.SetInitializer(new LibraryTestInitializer());

        #endregion

#endif

#if EFCore

        #region Properties

        public DbSet<Book> Books { get; set; }

        public DbSet<LibraryCard> LibraryCards { get; set; }

        public DbSet<Publisher> Publishers { get; set; }

        public DbSet<Employee> Readers { get; set; }

        public DbSet<Review> Reviews { get; set; }

        public DbSet<SpatialPlace> SpatialPlaces { get; set; }

        // Keyless view (issue #741). Mapped via fluent HasNoKey().ToView(...) in OnModelCreating.
        // EF6 code-first does not support keyless entity types so this DbSet is EFCore-only;
        // EF6 keyless-view support requires EDMX-defined entity sets (covered by Task 11 in the
        // shared partial's source-factory pipe but not exercised by this code-first fixture).
        public DbSet<BooksByPublisher> BooksByPublisher { get; set; }

        #endregion

        #region Constructors

        ///
        public LibraryContext(DbContextOptions options) : base(options)
        {
        }

        #endregion

        #region Overrides

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
#pragma warning disable CS0618 // TimeOfDay is obsolete but still used by OData
            var timeOfDayConverter = new ValueConverter<TimeOfDay, TimeOnly>(
                v => new TimeOnly(v.Hours, v.Minutes, v.Seconds, (int)v.Milliseconds),
                v => new TimeOfDay(v.Hour, v.Minute, v.Second, v.Millisecond));
#pragma warning restore CS0618

            modelBuilder.Entity<Employee>().OwnsOne(c => c.Addr);
            modelBuilder.Entity<Employee>().OwnsOne(c => c.Universe, b =>
            {
                b.Property(u => u.TimeOfDayProperty).HasConversion(timeOfDayConverter);
            });
            modelBuilder.Entity<Publisher>().OwnsOne(c => c.Addr);

            modelBuilder.Entity<Book>()
                .HasOne(b => b.Publisher)
                .WithMany(p => p.Books)
                .HasForeignKey(b => b.PublisherId);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Book)
                .WithMany(b => b.Reviews)
                .HasForeignKey(r => r.BookId);

            modelBuilder.Entity<SpatialPlace>(e =>
            {
                e.Property(x => x.HeadquartersLocation).HasColumnType("geography");
                e.Property(x => x.ServiceArea).HasColumnType("geography");
                // IndoorOrigin uses [Spatial(typeof(GeographyPoint))] to exercise the attribute on a second geography Point.
                e.Property(x => x.IndoorOrigin).HasColumnType("geography");
                e.Property(x => x.RouteLine).HasColumnType("geography");
            });

            // Issue #741: keyless view mapped via HasNoKey + ToView. Restier picks this up
            // (null key list from FindPrimaryKey()) and demotes it to ComplexType + unbound
            // FunctionImport. The CREATE VIEW DDL runs in LibraryTestInitializer.Seed.
            modelBuilder.Entity<BooksByPublisher>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("BooksByPublisher");
            });
        }

        #endregion

#endif

    }

}
