// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using System;
using System.Collections.ObjectModel;
#if EF6
using System.Data.Entity;
#endif
#if EFCore
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.EntityFrameworkCore;
#endif


namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    /// <summary>
    /// An initializer to populate data into the context.
    /// </summary>
    public class LibraryTestInitializer
#if EF6
        : DropCreateDatabaseAlways<LibraryContext>
    {

        protected override void Seed(LibraryContext libraryContext)
        {

#else
        : IDatabaseInitializer

    {

        public void Seed(DbContext context)
        {
            var libraryContext = context as LibraryContext;
#endif

            libraryContext.Readers.Add(new Employee
            {
                Addr = new Address { Street = "street1" },
                FullName = "p1",
                Id = new Guid("53162782-EA1B-4712-AF26-8AA1D2AC0461"),
                Universe = new Universe
                {
                    BinaryProperty = new byte[] { 0x1, 0x2 },
                    BooleanProperty = true,
                    ByteProperty = 0x3,
                    //DateProperty = Date.Now,
                    DateTimeOffsetProperty = DateTimeOffset.Now,
                    DecimalProperty = decimal.One,
                    DoubleProperty = 123.45,
                    DurationProperty = TimeSpan.FromHours(1.0),
                    GuidProperty = new Guid("53162782-EA1B-4712-AF26-8AA1D2AC0461"),
                    Int16Property = 12345,
                    Int32Property = 1234567,
                    Int64Property = 9876543210,
                    // SByteProperty = -1,
                    SingleProperty = (float)123.45,
                    // StreamProperty = new FileStream("temp.txt", FileMode.OpenOrCreate),
                    StringProperty = "Hello",
                    TimeOfDayProperty = TimeOfDay.Now
                }
            });
            libraryContext.Readers.Add(new Employee
            {
                Addr = new Address { Street = "street2" },
                FullName = "p2",
                Id = new Guid("8B04EA8B-37B1-4211-81CB-6196C9A1FE36"),
                Universe = new Universe
                {
                    BinaryProperty = new byte[] { 0x1, 0x2 },
                    BooleanProperty = true,
                    ByteProperty = 0x3,
                    //DateProperty = Date.Now,
                    DateTimeOffsetProperty = DateTimeOffset.Now,
                    DecimalProperty = decimal.One,
                    DoubleProperty = 123.45,
                    DurationProperty = TimeSpan.FromHours(1.0),
                    GuidProperty = new Guid("8B04EA8B-37B1-4211-81CB-6196C9A1FE36"),
                    Int16Property = 12345,
                    Int32Property = 1234567,
                    Int64Property = 9876543210,
                    // SByteProperty = -1,
                    SingleProperty = (float)123.45,
                    // StreamProperty = new FileStream("temp.txt", FileMode.OpenOrCreate),
                    StringProperty = "Hello",
                    TimeOfDayProperty = TimeOfDay.Now
                }
            });

            libraryContext.Publishers.Add(new Publisher
            {
                Id = "Publisher1",
                Addr = new Address
                {
                    Street = "123 Sesame St.",
                    Zip = "00010"
                },
                LastUpdated = DateTimeOffset.MinValue,
                Books = new ObservableCollection<Book>
                {
                    new Book
                    {
                         Id = new Guid("19d68c75-1313-4369-b2bf-521f2b260a59"),
                         Isbn = "9476324472648",
                         Title = "A Clockwork Orange",
                         IsActive = true
                    },
                    new Book
                    {
                        Id = new Guid("c2081e58-21a5-4a15-b0bd-fff03ebadd30"),
                        Isbn = "7273389962644",
                        Title = "Jungle Book, The",
                        IsActive = true
                    },
                    new Book
                    {
                        Id = new Guid("2A139A64-B7D9-4F9F-B7F4-E93C1678EB0F"),
                        Isbn = "1122334455668",
                        Title = "Sea of Rustoleum",
                        IsActive = false
                    }
                }
            });

            libraryContext.Publishers.Add(new Publisher
            {
                Id = "Publisher2",
                Addr = new Address
                {
                    Street = "234 Anystreet St.",
                    Zip = "10010"
                },
                LastUpdated = DateTimeOffset.MinValue,
                Books = new ObservableCollection<Book>
                {
                    new Book
                    {
                        Id = new Guid("0697576b-d616-4057-9d28-ed359775129e"),
                        Isbn = "1315290642409",
                        Title = "Color Purple, The",
                        IsActive = true                        
                    }
                }
            });

            libraryContext.Books.Add(new Book
            {
                Id = new Guid("2D760F15-974D-4556-8CDF-D610128B537E"),
                Isbn = "1122334455667",
                Title = "Sea of Rust",
                IsActive = true
            });

            libraryContext.SaveChanges();

        }

    }

}
