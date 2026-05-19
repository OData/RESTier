// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views
{
    /// <summary>
    /// LibraryContext + a single keyless view (BooksByPublisher) for keyless-view tests.
    /// </summary>
    /// <remarks>
    /// OnConfiguring falls back to in-memory ONLY when no provider has been configured —
    /// model-shape tests rely on this fallback so they can build the EDM without a real DB,
    /// while end-to-end tests (which supply a UseSqlServer options action via
    /// AddEntityFrameworkServices&lt;T&gt;) get the relational provider and the fallback skips.
    /// </remarks>
    public class LibraryWithViewsContext : LibraryContext
    {
        public virtual DbSet<BooksByPublisher> BooksByPublisher { get; set; }

        public LibraryWithViewsContext(DbContextOptions<LibraryWithViewsContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase(nameof(LibraryWithViewsContext));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<BooksByPublisher>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("BooksByPublisher");
            });
        }
    }
}
