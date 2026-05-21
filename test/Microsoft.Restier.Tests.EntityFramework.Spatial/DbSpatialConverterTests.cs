// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Data.Entity.Spatial;
using FluentAssertions;
using Microsoft.Restier.EntityFramework.Spatial;
using Microsoft.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFramework.Spatial
{
    [TestClass]
    public class DbSpatialConverterTests
    {
        /// <summary>
        /// True when the native <c>SqlServerSpatial160.dll</c> is loadable in the current process.
        /// EF6 + Microsoft.SqlServer.Types 160.x can construct <c>DbGeography</c> Points without
        /// the native library, but multi-point geographies (LineString, Polygon, …) go through
        /// <c>SqlGeography.IsValidExpensive</c> → <c>GeodeticIsValid</c>, which requires the
        /// Windows-only native binary. Tests that hit that path are gated by this probe.
        /// </summary>
        public static bool GeodeticNativeAvailable
        {
            get
            {
                try
                {
                    _ = DbGeography.FromText("LINESTRING(0 0, 1 1)", 4326);
                    return true;
                }
                catch (Exception)
                {
                    // Intentionally broad: EF6 surfaces the native-loader failure as
                    // PlatformNotSupportedException, but under xUnit v3's reflection-based
                    // test invocation that specific type doesn't reliably propagate. Treat
                    // any exception from FromText as "native binary not loadable".
                    return false;
                }
            }
        }

        private readonly DbSpatialConverter _converter = new();

        [TestMethod]
        public void CanConvert_returns_true_for_DbGeography()
        {
            _converter.CanConvert(typeof(DbGeography)).Should().BeTrue();
        }

        [TestMethod]
        public void ToEdm_returns_GeographyPoint_for_DbGeography_Point()
        {
            var dbg = DbGeography.FromText("POINT(4.9041 52.3676)", 4326);

            var point = (GeographyPoint)_converter.ToEdm(dbg, typeof(GeographyPoint));

            point.Latitude.Should().BeApproximately(52.3676, 0.0001);
            point.Longitude.Should().BeApproximately(4.9041, 0.0001);
            point.CoordinateSystem.EpsgId.Should().Be(4326);
        }

        [TestMethod]
        public void ToStorage_returns_DbGeography_for_GeographyPoint()
        {
            var p = GeographyPoint.Create(CoordinateSystem.Geography(4326), 52.3676, 4.9041, null, null);

            var result = _converter.ToStorage(typeof(DbGeography), p);

            var dbg = result.Should().BeOfType<DbGeography>().Subject;
            dbg.SpatialTypeName.Should().Be("Point");
            dbg.Latitude.Should().BeApproximately(52.3676, 0.0001);
            dbg.Longitude.Should().BeApproximately(4.9041, 0.0001);
            dbg.CoordinateSystemId.Should().Be(4326);
        }

        [TestMethod]
        public void Round_trip_preserves_value()
        {
            var original = DbGeography.FromText("POINT(4.9041 52.3676)", 4326);

            var edm = _converter.ToEdm(original, typeof(GeographyPoint));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.AsText().Should().Be(original.AsText());
            roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
        }

        [TestMethod, Ignore("Requires Windows-only SqlServerSpatial160.dll (geodesic LineString/Polygon validity check). Install Microsoft.SqlServer.Types and call SqlServerTypes.Utilities.LoadNativeAssemblies(...) at startup to enable.")]
        public void Round_trips_LineString()
        {
            var original = DbGeography.FromText("LINESTRING(0 0, 1 1, 2 2)", 4326);

            var edm = (GeographyLineString)_converter.ToEdm(original, typeof(GeographyLineString));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.AsText().Should().Be(original.AsText());
            roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
        }

        [TestMethod, Ignore("Requires Windows-only SqlServerSpatial160.dll (geodesic LineString/Polygon validity check). Install Microsoft.SqlServer.Types and call SqlServerTypes.Utilities.LoadNativeAssemblies(...) at startup to enable.")]
        public void Round_trips_Polygon()
        {
            var original = DbGeography.FromText("POLYGON((0 0, 0 1, 1 1, 1 0, 0 0))", 4326);

            var edm = (GeographyPolygon)_converter.ToEdm(original, typeof(GeographyPolygon));
            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);

            roundTrip.AsText().Should().Be(original.AsText());
            roundTrip.CoordinateSystemId.Should().Be(original.CoordinateSystemId);
        }

        [TestMethod]
        [DataRow(4326)]
        [DataRow(4269)]
        public void Preserves_Geography_SRID(int srid)
        {
            var original = DbGeography.FromText("POINT(1 2)", srid);

            var edm = (GeographyPoint)_converter.ToEdm(original, typeof(GeographyPoint));
            edm.CoordinateSystem.EpsgId.Should().Be(srid);

            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);
            roundTrip.CoordinateSystemId.Should().Be(srid);
        }

        [TestMethod]
        public void Preserves_Z_coordinate()
        {
            var original = DbGeography.FromText("POINT(1 2 3)", 4326);

            var edm = (GeographyPoint)_converter.ToEdm(original, typeof(GeographyPoint));
            edm.Z.Should().BeApproximately(3.0, 0.0001);

            var roundTrip = (DbGeography)_converter.ToStorage(typeof(DbGeography), edm);
            roundTrip.Elevation.Should().BeApproximately(3.0, 0.0001);
        }

        [TestMethod]
        public void Round_trips_DbGeometry_Point_with_planar_SRID()
        {
            var original = DbGeometry.FromText("POINT(123456.78 654321.09)", 3857);

            var edm = (GeometryPoint)_converter.ToEdm(original, typeof(GeometryPoint));
            edm.X.Should().BeApproximately(123456.78, 0.01);
            edm.CoordinateSystem.EpsgId.Should().Be(3857);

            var roundTrip = (DbGeometry)_converter.ToStorage(typeof(DbGeometry), edm);
            roundTrip.CoordinateSystemId.Should().Be(3857);
        }

        [TestMethod]
        public void Null_storage_value_returns_null()
        {
            _converter.ToEdm(null, typeof(GeographyPoint)).Should().BeNull();
            _converter.ToStorage(typeof(DbGeography), null).Should().BeNull();
        }

        [TestMethod]
        public void ToStorage_with_unsupported_storage_type_throws()
        {
            var p = GeographyPoint.Create(CoordinateSystem.Geography(4326), 0, 0, null, null);

            var act = () => _converter.ToStorage(typeof(string), p);

            act.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void ToEdm_with_unsupported_storage_value_throws()
        {
            var act = () => _converter.ToEdm("not a spatial value", typeof(GeographyPoint));

            act.Should().Throw<NotSupportedException>();
        }
    }
}
