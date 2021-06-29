using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    public static class LibraryModelBuilderExtensions
    {
        public static void Seed(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>().HasData(
                new Book
                {
                    Id = new Guid("2D760F15-974D-4556-8CDF-D610128B537E"),
                    Isbn = "1122334455667",
                    Title = "Sea of Rust"
                },
                new Book
                {
                    Id = new Guid("19d68c75-1313-4369-b2bf-521f2b260a59"),
                    Isbn = "9476324472648",
                    Title = "A Clockwork Orange"
                },
                new Book
                {
                    Id = new Guid("c2081e58-21a5-4a15-b0bd-fff03ebadd30"),
                    Isbn = "7273389962644",
                    Title = "Jungle Book, The"
                },
                new Book
                {
                    Id = new Guid("0697576b-d616-4057-9d28-ed359775129e"),
                    Isbn = "1315290642409",
                    Title = "Color Purple, The"
                }
                );

        }

    }
}
