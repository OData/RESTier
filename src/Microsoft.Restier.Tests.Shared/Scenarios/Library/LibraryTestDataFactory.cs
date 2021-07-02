using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// A class representing a generalized object container for populating both EF and EFCore data contexts.
    /// </summary>
    public class LibraryTestDataFactory
    {

        #region Entity Properities

        public List<Book> Books { get; set; }

        public List<LibraryCard> LibraryCards { get; set; }

        public List<Publisher> Publishers { get; set; }

        public List<Employee> Readers { get; set; }

        public List<Address> Addresses { get; set; }

        public List<Universe> Universes { get; set; }

        #endregion

        /// <summary>
        /// A contstructor that seeds the class instance with data.
        /// </summary>
        public LibraryTestDataFactory()
        {
            var context = this;

            context.Addresses = new List<Address>
            {
                new Address { Id = new Guid("B692094E-5930-4E59-A985-55EB59E13F14"), Street = "street1" },
                new Address { Id = new Guid("F59C41D5-877B-4BB3-8D43-447610A80156"), Street = "street2" },
                new Address { Id = new Guid("0483ECE5-64A9-460C-889E-F1085BD2093E"), Street = "123 Publisher Way", Zip = "00010" },
                new Address { Id = new Guid("F784BCDE-C203-4C0B-B718-B8D74FE56CF6"), Street = "234 Anystreet St.", Zip = "10010" },
            };

            context.Universes = new List<Universe>
            {
                new Universe
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
                },
                new Universe
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
                }
            };
            
            context.Readers = new List<Employee>
            {
                new Employee
                {
                    Id = new Guid("53162782-EA1B-4712-AF26-8AA1D2AC0461"),
                    AddrId = new Guid("B692094E-5930-4E59-A985-55EB59E13F14"),
                    FullName = "p1",
                    UniverseId = new Guid("22A07C43-7B6A-4089-AF20-840EB24E4443"),
                },
                new Employee
                {
                    Id = new Guid("8B04EA8B-37B1-4211-81CB-6196C9A1FE36"),
                    AddrId = new Guid("F59C41D5-877B-4BB3-8D43-447610A80156"),
                    FullName = "p2",
                    UniverseId = new Guid("E8858E23-E6E4-4BE2-8463-10CDB6A8D480"),
                }
            };

            context.Publishers = new List<Publisher>
            {
                new Publisher
                {
                    Id = "Publisher1",
                    Name = "Tor Books",
                    AddrId = new Guid("0483ECE5-64A9-460C-889E-F1085BD2093E"),
                },

                new Publisher
                {
                    Id = "Publisher2",
                    Name = "DelRay Books",
                    AddrId = new Guid("F784BCDE-C203-4C0B-B718-B8D74FE56CF6"),
                }
            };

            context.Books = new List<Book> {
                new Book
                {
                    Id = new Guid("2D760F15-974D-4556-8CDF-D610128B537E"),
                    Isbn = "1122334455667",
                    Title = "Sea of Rust"
                },
                new Book
                {
                    Id = new Guid("19d68c75-1313-4369-b2bf-521f2b260a59"),
                    PublisherId = "Publisher1",
                    Isbn = "9476324472648",
                    Title = "A Clockwork Orange"
                },
                new Book
                {
                    Id = new Guid("c2081e58-21a5-4a15-b0bd-fff03ebadd30"),
                    PublisherId = "Publisher1",
                    Isbn = "7273389962644",
                    Title = "Jungle Book, The"
                },
                new Book
                {
                    Id = new Guid("0697576b-d616-4057-9d28-ed359775129e"),
                    PublisherId = "Publisher2",
                    Isbn = "1315290642409",
                    Title = "Color Purple, The"
                }
            };

        }

    }
}
