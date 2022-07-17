using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.IncorrectLibrary
{

    /// <summary>
    /// The data context for the Library scenario.
    /// </summary>
    public class IncorrectLibraryContext : DbContext
    {

        #region EntitySet Properties

        public DbSet<Book> Books { get; set; }

        public DbSet<LibraryCard> LibraryCards { get; set; }

        public DbSet<Publisher> Publishers { get; set; }

        public DbSet<Employee> Readers { get; set; }

        public DbSet<Address> Addresses { get; set; }

        public DbSet<Universe> Universes { get; set; }

        #endregion

        public IncorrectLibraryContext(DbContextOptions<IncorrectLibraryContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(nameof(IncorrectLibraryContext));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>().OwnsOne(c => c.Addr);
            modelBuilder.Entity<Employee>().OwnsOne(c => c.Universe);
            modelBuilder.Entity<Publisher>().OwnsOne(c => c.Addr);

        }

    }

}
