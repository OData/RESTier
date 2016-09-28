// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Data.Entity;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Providers.EntityFramework.Tests.Models.Library
{
    class LibraryContext : DbContext
    {
        public LibraryContext()
            : base("LibraryContext")
        {
            Database.SetInitializer(new TestInitializer());
        }
        
        public IDbSet<Person> Readers { get; set; }
    }

    class TestInitializer : DropCreateDatabaseAlways<LibraryContext>
    {
        protected override void Seed(LibraryContext context)
        {
            context.Readers.Add(new Person
            {
                Addr = new Address { Street = "street1" }, 
                FullName = "p1",
                Id = new Guid("53162782-EA1B-4712-AF26-8AA1D2AC0461"),
                Universe = new Universe
                {
                    BinaryProperty = new byte[] { 0x1, 0x2 },
                    BooleanProperty = true,
                    ByteProperty = 0x3,
                    DateProperty = Date.Now,
                    DateTimeOffsetProperty = DateTimeOffset.Now,
                    DecimalProperty = Decimal.One,
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
            context.SaveChanges();
        }
    }
}
