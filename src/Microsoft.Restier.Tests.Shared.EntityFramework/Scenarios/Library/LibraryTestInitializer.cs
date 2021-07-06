// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using System;
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

        protected void Seed(LibraryContext context)
#endif
        {

            context.Addresses.Add(new Address { Id = new Guid("B692094E-5930-4E59-A985-55EB59E13F14"), Street = "street1" });
            context.Addresses.Add(new Address { Id = new Guid("F59C41D5-877B-4BB3-8D43-447610A80156"), Street = "street2" });
            context.Addresses.Add(new Address { Id = new Guid("0483ECE5-64A9-460C-889E-F1085BD2093E"), Street = "123 Publisher Way", Zip = "00010" });
            context.Addresses.Add(new Address { Id = new Guid("F784BCDE-C203-4C0B-B718-B8D74FE56CF6"), Street = "234 Anystreet St.", Zip = "10010" });

            context.Universes.Add(new Universe
            {
                Id = new Guid("22A07C43-7B6A-4089-AF20-840EB24E4443"),
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
            });
            context.Universes.Add(new Universe
            {
                Id = new Guid("E8858E23-E6E4-4BE2-8463-10CDB6A8D480"),
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
            });

            context.Readers.Add(new Employee
            {
                Id = new Guid("53162782-EA1B-4712-AF26-8AA1D2AC0461"),
                AddrId = new Guid("B692094E-5930-4E59-A985-55EB59E13F14"),
                FullName = "p1",
                UniverseId = new Guid("22A07C43-7B6A-4089-AF20-840EB24E4443"),
            });
            context.Readers.Add(new Employee
            {
                Id = new Guid("8B04EA8B-37B1-4211-81CB-6196C9A1FE36"),
                AddrId = new Guid("F59C41D5-877B-4BB3-8D43-447610A80156"),
                FullName = "p2",
                UniverseId = new Guid("E8858E23-E6E4-4BE2-8463-10CDB6A8D480"),
            });

            context.Publishers.Add(new Publisher
            {
                Id = "Publisher1",
                Name = "Tor Books",
                AddrId = new Guid("0483ECE5-64A9-460C-889E-F1085BD2093E"),
            });

            context.Publishers.Add(new Publisher
            {
                Id = "Publisher2",
                Name = "DelRay Books",
                AddrId = new Guid("F784BCDE-C203-4C0B-B718-B8D74FE56CF6"),
            });

            context.Books.Add(new Book
            {
                Id = new Guid("2D760F15-974D-4556-8CDF-D610128B537E"),
                Isbn = "1122334455667",
                Title = "Sea of Rust"
            });
            context.Books.Add(new Book
            {
                Id = new Guid("19d68c75-1313-4369-b2bf-521f2b260a59"),
                PublisherId = "Publisher1",
                Isbn = "9476324472648",
                Title = "A Clockwork Orange"
            });
            context.Books.Add(new Book
            {
                Id = new Guid("c2081e58-21a5-4a15-b0bd-fff03ebadd30"),
                PublisherId = "Publisher1",
                Isbn = "7273389962644",
                Title = "Jungle Book, The"
            });
            context.Books.Add(new Book
            {
                Id = new Guid("0697576b-d616-4057-9d28-ed359775129e"),
                PublisherId = "Publisher2",
                Isbn = "1315290642409",
                Title = "Color Purple, The"
            });

            context.SaveChanges();
        }

    }

}
