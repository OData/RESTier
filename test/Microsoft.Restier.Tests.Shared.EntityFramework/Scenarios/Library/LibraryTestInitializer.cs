// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using System;
using System.Collections.ObjectModel;
#if EF6
using System.Data.Entity;
using System.Linq;
#endif
#if EFCore
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Tests.Shared.EntityFrameworkCore;
#endif


using Microsoft.Restier.Tests.Shared.Scenarios.Library;

#if EF6
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EF6
#elif EFCore
namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore
#endif
{
    /// <summary>
    /// An initializer to populate data into the context.
    /// </summary>
    public class LibraryTestInitializer
#if EF6
        : DropCreateDatabaseIfModelChanges<LibraryContext>
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
#pragma warning disable CS0618 // TimeOfDay is obsolete but still used by OData
                    TimeOfDayProperty = TimeOfDay.Now
#pragma warning restore CS0618
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
#pragma warning disable CS0618 // TimeOfDay is obsolete but still used by OData
                    TimeOfDayProperty = TimeOfDay.Now
#pragma warning restore CS0618
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
                         IsActive = true,
                         Category = BookCategory.Fiction,
                         PublishDate = new DateTime(1962, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    },
                    new Book
                    {
                        Id = new Guid("c2081e58-21a5-4a15-b0bd-fff03ebadd30"),
                        Isbn = "7273389962644",
                        Title = "Jungle Book, The",
                        IsActive = true,
                        Category = BookCategory.Fiction,
                        PublishDate = new DateTime(1894, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    },
                    new Book
                    {
                        Id = new Guid("2A139A64-B7D9-4F9F-B7F4-E93C1678EB0F"),
                        Isbn = "1122334455668",
                        Title = "Sea of Rustoleum",
                        IsActive = false,
                        PublishDate = new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                    },
                    new AudioBook
                    {
                        Id = new Guid("E6916E98-8427-4F7B-92DA-890F68BFD039"),
                        Isbn = "9780141370354",
                        Title = "Matilda",
                        IsActive = true,
                        Duration = TimeSpan.FromHours(4.5),
                        Narrator = "Kate Winslet",
                        PublishDate = new DateTime(1988, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                    },
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
                        IsActive = true,
                        PublishDate = new DateTime(1982, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    }
                }
            });

            libraryContext.Books.Add(new Book
            {
                Id = new Guid("2D760F15-974D-4556-8CDF-D610128B537E"),
                Isbn = "1122334455667",
                Title = "Sea of Rust",
                IsActive = true,
                PublishDate = new DateTime(2017, 9, 5, 0, 0, 0, DateTimeKind.Utc),
            });

            libraryContext.LibraryCards.Add(new LibraryCard
            {
                Id = new Guid("A1111111-1111-1111-1111-111111111111"),
                DateRegistered = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero),
            });

            libraryContext.Reviews.Add(new Review
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000101"),
                Content = "Great book!",
                Rating = 5,
                BookId = new Guid("19d68c75-1313-4369-b2bf-521f2b260a59"),
            });

            libraryContext.Reviews.Add(new Review
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000102"),
                Content = "Decent read.",
                Rating = 3,
                BookId = new Guid("19d68c75-1313-4369-b2bf-521f2b260a59"),
            });

#if EF6
            // Commit the non-spatial seed first so it survives even if the spatial save throws.
            // EF6's UpdateTranslator eagerly initializes the spatial type default map for any
            // entity that targets a table with geography/geometry columns, which requires
            // Microsoft.SqlServer.Types — an assembly that's only loadable on .NET Framework or
            // with a third-party shim. On .NET 5+ without the shim, saving SpatialPlace throws.
            libraryContext.SaveChanges();

            try
            {
                libraryContext.SpatialPlaces.Add(new SpatialPlace
                {
                    Id = 1,
                    Name = "Spatial Place 1",
                    HeadquartersLocation = System.Data.Entity.Spatial.DbGeography.FromText("POINT(4.9041 52.3676)", 4326),
                    ServiceArea = System.Data.Entity.Spatial.DbGeography.FromText("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))", 4326),
                    FloorPlan = System.Data.Entity.Spatial.DbGeometry.FromText("POINT(100 200)", 0),
                    RouteLine = System.Data.Entity.Spatial.DbGeography.FromText("LINESTRING(0 0, 1 1, 2 2)", 4326),
                });
                libraryContext.SaveChanges();
            }
            catch (System.Exception)
            {
                // Spatial unavailable on this runtime (no Microsoft.SqlServer.Types). Detach the
                // failed entry so subsequent context use isn't poisoned, and leave SpatialPlaces
                // empty — the EF6 spatial tests in Microsoft.Restier.Tests.EntityFramework.Spatial
                // are already [Skip]-marked for the same reason.
                foreach (var entry in libraryContext.ChangeTracker.Entries<SpatialPlace>().ToList())
                {
                    entry.State = System.Data.Entity.EntityState.Detached;
                }
            }
#endif

#if EFCore
            libraryContext.SaveChanges();

            // Issue #741 — create the BooksByPublisher SQL view on top of the seeded
            // Publishers/Books. Guarded by IsRelational() because some tests use
            // UseInMemoryDatabase (no ExecuteSqlRaw support); the keyless-view tests run
            // against real SQL Server via AddEntityFrameworkServices<LibraryContext>().
            if (libraryContext.Database.IsRelational())
            {
                libraryContext.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('BooksByPublisher', 'V') IS NOT NULL DROP VIEW BooksByPublisher;
                    EXEC('CREATE VIEW BooksByPublisher AS
                           SELECT p.Id AS PublisherId,
                                  b.Title AS BookName,
                                  CAST(COUNT(b.Id) OVER(PARTITION BY p.Id) AS INT) AS BookCount
                           FROM Publishers p
                           INNER JOIN Books b ON b.PublisherId = p.Id;');
                ");
            }

            // Spatial seeding requires CLR to be enabled on SQL Server (sp_configure 'clr enabled', 1).
            // If the instance doesn't have CLR enabled (e.g., bare Docker SQL Server), skip spatial values.
            try
            {
                var geographyFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

                var hq = geographyFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(4.9041, 52.3676));

                // SQL Server geography requires a counterclockwise (positive signed area) exterior ring.
                // Counterclockwise in 2D (lon,lat): (0,0) -> (1,0) -> (1,1) -> (0,1) -> (0,0).
                var area = geographyFactory.CreatePolygon(new[]
                {
                    new NetTopologySuite.Geometries.Coordinate(0, 0),
                    new NetTopologySuite.Geometries.Coordinate(1, 0),
                    new NetTopologySuite.Geometries.Coordinate(1, 1),
                    new NetTopologySuite.Geometries.Coordinate(0, 1),
                    new NetTopologySuite.Geometries.Coordinate(0, 0),
                });

                // IndoorOrigin uses HasColumnType("geography"); use a representative geographic point.
                var indoor = geographyFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(10, 20));

                // RouteLine: simple LineString for geo.length filter tests.
                var route = geographyFactory.CreateLineString(new[]
                {
                    new NetTopologySuite.Geometries.Coordinate(0, 0),
                    new NetTopologySuite.Geometries.Coordinate(1, 1),
                    new NetTopologySuite.Geometries.Coordinate(2, 2),
                });

                libraryContext.SpatialPlaces.Add(new SpatialPlace
                {
                    Name = "Spatial Place 1",
                    HeadquartersLocation = hq,
                    ServiceArea = area,
                    IndoorOrigin = indoor,
                    RouteLine = route,
                });

                libraryContext.SaveChanges();
            }
            catch (System.Exception)
            {
                // Spatial insert failed (e.g., CLR not enabled on SQL Server). Seed without spatial values.
                libraryContext.ChangeTracker.Clear();
                libraryContext.SpatialPlaces.Add(new SpatialPlace { Name = "Spatial Place 1" });
                libraryContext.SaveChanges();
            }
#endif

        }

    }

}
