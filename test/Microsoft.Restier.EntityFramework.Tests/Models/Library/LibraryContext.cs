// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Data.Entity;

namespace Microsoft.Restier.EntityFramework.Tests.Models.Library
{
    class LibraryContext : DbContext
    {
        public LibraryContext()
            : base("LibraryContext")
        {
            Database.SetInitializer(new TestInitializer());
        }

        public DbSet<Book> Books { get; set; }

        public DbSet<Publisher> Publishers { get; set; }

        public DbSet<Person> Readers { get; set; }
    }

    class TestInitializer : DropCreateDatabaseAlways<LibraryContext>
    {
        protected override void Seed(LibraryContext context)
        {
            context.Readers.Add(new Person
            {
                Addr = new Address { Street = "street1" }, 
                FullName = "p1",
                Id = new Guid("53162782-EA1B-4712-AF26-8AA1D2AC0461")
            });
            context.SaveChanges();
        }
    }
}
