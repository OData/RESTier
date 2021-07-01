// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// The EntityFramework data context for the Library scenario.
    /// </summary>
    public class LibraryContext : DbContext
    {

        public LibraryContext()
            : base("LibraryContext") => Database.SetInitializer(new LibraryTestInitializer());

        public IDbSet<Book> Books { get; set; }

        public IDbSet<LibraryCard> LibraryCards { get; set; }

        public IDbSet<Publisher> Publishers { get; set; }

        public IDbSet<Employee> Readers { get; set; }

        public IDbSet<Address> Addresses { get; set; }

        public IDbSet<Universe> Universes { get; set; }

    }

    /// <summary>
    /// An initializer to populate data into the context.
    /// </summary>
    public class LibraryTestInitializer : DropCreateDatabaseAlways<LibraryContext>
    {
        protected override void Seed(LibraryContext context)
        {
            var sourceData = new LibraryTestDataFactory();

            if (sourceData.Addresses is not null)
            {
                sourceData.Addresses.ForEach(c => context.Addresses.Add(c));
            }

            if (sourceData.Universes is not null)
            {
                sourceData.Universes.ForEach(c => context.Universes.Add(c));
            }

            if (sourceData.Books is not null)
            {
                sourceData.Books.ForEach(c => context.Books.Add(c));
            }

            if (sourceData.LibraryCards is not null)
            {
                sourceData.LibraryCards.ForEach(c => context.LibraryCards.Add(c));
            }

            if (sourceData.Publishers is not null)
            {
                sourceData.Publishers.ForEach(c => context.Publishers.Add(c));
            }

            if (sourceData.Readers is not null)
            {
                sourceData.Readers.ForEach(c => context.Readers.Add(c));
            }

            context.SaveChanges();
        }

    }
}
