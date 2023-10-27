// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET6_0_OR_GREATER

using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.Views
{

    /// <summary>
    /// The data context for the Library scenario.
    /// </summary>
    public class LibraryWithViewsContext : LibraryContext
    {

        public virtual DbSet<BooksByPublisher> BooksByPublisher { get; set; }

        public LibraryWithViewsContext(DbContextOptions<LibraryWithViewsContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(nameof(LibraryWithViewsContext));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BooksByPublisher>(entity =>
            {
                entity.ToView("Sales By Category");
                entity.Property(e => e.PublisherId).HasColumnName("PublisherID");
                entity.Property(e => e.PublisherName)
                    .IsRequired()
                    .HasMaxLength(15);
                entity.Property(e => e.BookName);
                entity.Property(e => e.BookCount);
            });
            base.OnModelCreating(modelBuilder);
        }

    }

}

#endif