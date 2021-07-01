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
            var sourceData = new LibraryTestDataFactory();

            if (sourceData.Addresses is not null)
            {
                modelBuilder.Entity<Address>().HasData(sourceData.Addresses.ToArray());
            }

            if (sourceData.Universes is not null)
            {
                modelBuilder.Entity<Universe>().HasData(sourceData.Universes.ToArray());
            }

            if (sourceData.Books is not null)
            {
                modelBuilder.Entity<Book>().HasData(sourceData.Books.ToArray());
            }

            if (sourceData.LibraryCards is not null)
            {
                modelBuilder.Entity<LibraryCard>().HasData(sourceData.LibraryCards.ToArray());
            }

            if (sourceData.Publishers is not null)
            {
                modelBuilder.Entity<Publisher>().HasData(sourceData.Publishers.ToArray());
            }

            if (sourceData.Readers is not null)
            {
                modelBuilder.Entity<Employee>().HasData(sourceData.Readers.ToArray());
            }
        }

    }
}
