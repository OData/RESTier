using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    public static class LibraryModelBuilderExtensions
    {
        public static void Seed(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>().HasData(
                new Book { Id = Guid.NewGuid(), Isbn = "1234", Title = "War and Peace" }
                );
        }

    }
}
