// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using System;
using System.Collections.ObjectModel;
#if EF6
using System.Data.Entity;
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

        protected override void Seed(LibraryContext context)
#else
    {

        internal void Seed(LibraryContext context)
#endif
        {

            context.Readers.Add(new Employee
            {
                Addr = new Address { Id = new Guid("CAE49262-23D9-4F01-A0AB-EE3D0986B22D"), Street = "street1" },
                FullName = "p1",
                Id = new Guid("53162782-EA1B-4712-AF26-8AA1D2AC0461"),
                Universe = new Universe
                {
                    Id = new Guid("8B9F3463-B235-43BB-B995-E1EAB7A25299"),
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
            context.Readers.Add(new Employee
            {
                Addr = new Address { Id = new Guid("5F90A897-DF2D-4A66-9C62-D7EF8109C12C"), Street = "street2" },
                FullName = "p2",
                Id = new Guid("8B04EA8B-37B1-4211-81CB-6196C9A1FE36"),
                Universe = new Universe
                {
                    Id = new Guid("9F48BD42-0B80-402F-B299-5D04D3737255"),
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

            context.Publishers.Add(new Publisher
            {
                Id = "Publisher1",
                Addr = new Address
                {
                    Id = new Guid("8E37E9A3-0D0C-4495-AD06-2E78C9532835"),
                    Street = "123 Sesame St.",
                    Zip = "00010"
                },
                Books = new ObservableCollection<Book>
                {
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
                    }
                }
            });

            context.Publishers.Add(new Publisher
            {
                Id = "Publisher2",
                Addr = new Address
                {
                    Id = new Guid("64E96601-8BDB-499A-8BCF-3028A885F64D"),
                    Street = "234 Anystreet St.",
                    Zip = "10010"
                },
                Books = new ObservableCollection<Book>
                {
                    new Book
                    {
                        Id = new Guid("0697576b-d616-4057-9d28-ed359775129e"),
                        Isbn = "1315290642409",
                        Title = "Color Purple, The"
                    }
                }
            });

            context.Books.Add(new Book
            {
                Id = new Guid("2D760F15-974D-4556-8CDF-D610128B537E"),
                Isbn = "1122334455667",
                Title = "Sea of Rust"
            });

            context.SaveChanges();

        }

    }

}
