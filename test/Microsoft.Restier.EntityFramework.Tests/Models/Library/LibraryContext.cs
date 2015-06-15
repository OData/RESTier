using System.Data.Entity;

namespace Microsoft.Restier.EntityFramework.Tests.Models.Library
{
    class LibraryContext : DbContext
    {
        public LibraryContext()
            : base()//"ComplexTypeTest")
        {
            if (Database.Exists())
            {
              Database.Delete();
            }
        }

        public DbSet<Book> Books { get; set; }

        public DbSet<Publisher> Publishers { get; set; }

        public DbSet<Person> Readers { get; set; }
   }
}
