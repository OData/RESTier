// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Restier.EntityFrameworkCore.Spatial;
using Microsoft.Spatial;
using NetTopologySuite.Geometries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Spatial
{
    [TestClass]
    public class NtsSpatialConverterTests
    {
        private readonly NtsSpatialConverter _converter = new();
        private readonly NetTopologySuite.Geometries.GeometryFactory _ntsFactory = new(new PrecisionModel(), 4326);

        [TestMethod]
        public void CanConvert_recognizes_NTS_Geometry_subclasses()
        {
            _converter.CanConvert(typeof(NetTopologySuite.Geometries.Point)).Should().BeTrue();
            _converter.CanConvert(typeof(NetTopologySuite.Geometries.Polygon)).Should().BeTrue();
            _converter.CanConvert(typeof(NetTopologySuite.Geometries.Geometry)).Should().BeTrue();
        }

        [TestMethod]
        public void CanConvert_rejects_non_NTS_types()
        {
            _converter.CanConvert(typeof(string)).Should().BeFalse();
        }

        [TestMethod]
        public void Round_trips_NTS_Point_to_GeographyPoint_with_SRID_4326()
        {
            var nts = _ntsFactory.CreatePoint(new Coordinate(4.9041, 52.3676));
            nts.SRID = 4326;

            var edm = (GeographyPoint)_converter.ToEdm(nts, typeof(GeographyPoint));
            edm.Latitude.Should().BeApproximately(52.3676, 0.0001);
            edm.Longitude.Should().BeApproximately(4.9041, 0.0001);
            edm.CoordinateSystem.EpsgId.Should().Be(4326);

            var roundTrip = (NetTopologySuite.Geometries.Point)_converter.ToStorage(typeof(NetTopologySuite.Geometries.Point), edm);
            roundTrip.X.Should().BeApproximately(4.9041, 0.0001);
            roundTrip.Y.Should().BeApproximately(52.3676, 0.0001);
            roundTrip.SRID.Should().Be(4326);
        }

        [TestMethod]
        public void Round_trips_NTS_Polygon()
        {
            var ring = _ntsFactory.CreateLinearRing(new[]
            {
                new Coordinate(0, 0),
                new Coordinate(1, 0),
                new Coordinate(1, 1),
                new Coordinate(0, 1),
                new Coordinate(0, 0),
            });
            var nts = _ntsFactory.CreatePolygon(ring);
            nts.SRID = 4326;

            var edm = (GeographyPolygon)_converter.ToEdm(nts, typeof(GeographyPolygon));
            var roundTrip = (NetTopologySuite.Geometries.Polygon)_converter.ToStorage(typeof(NetTopologySuite.Geometries.Polygon), edm);

            roundTrip.SRID.Should().Be(4326);
            roundTrip.Coordinates.Should().HaveCount(5);
        }

        [TestMethod]
        public void Preserves_planar_SRID_for_GeometryPoint()
        {
            var planarFactory = new NetTopologySuite.Geometries.GeometryFactory(new PrecisionModel(), 3857);
            var nts = planarFactory.CreatePoint(new Coordinate(123456.78, 654321.09));

            var edm = (GeometryPoint)_converter.ToEdm(nts, typeof(GeometryPoint));
            edm.X.Should().BeApproximately(123456.78, 0.01);
            edm.CoordinateSystem.EpsgId.Should().Be(3857);

            var roundTrip = (NetTopologySuite.Geometries.Point)_converter.ToStorage(typeof(NetTopologySuite.Geometries.Point), edm);
            roundTrip.SRID.Should().Be(3857);
        }

        [TestMethod]
        public void Null_storage_value_returns_null()
        {
            _converter.ToEdm(null, typeof(GeographyPoint)).Should().BeNull();
            _converter.ToStorage(typeof(NetTopologySuite.Geometries.Point), null).Should().BeNull();
        }
    }
}
