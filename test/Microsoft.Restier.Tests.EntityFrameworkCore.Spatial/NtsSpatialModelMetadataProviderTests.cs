// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFrameworkCore.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTopologySuite.Geometries;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Spatial
{
    [TestClass]
    public class NtsSpatialModelMetadataProviderTests
    {
        private class Probe
        {
            public int Id { get; set; }
            public NetTopologySuite.Geometries.Point Geo { get; set; }
            public NetTopologySuite.Geometries.Point Geom { get; set; }
            public NetTopologySuite.Geometries.Point Unspecified { get; set; }
            public string NotSpatial { get; set; }
        }

        private class ProbeContext : DbContext
        {
            public DbSet<Probe> Probes { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseInMemoryDatabase("nts-provider-tests");
            }

            protected override void OnModelCreating(ModelBuilder b)
            {
                b.Entity<Probe>(e =>
                {
                    e.Property(x => x.Geo).HasColumnType("geography");
                    e.Property(x => x.Geom).HasColumnType("geometry(Point,4326)");
                    // Unspecified intentionally has no HasColumnType to exercise the null-genus path.
                });
            }
        }

        private readonly NtsSpatialModelMetadataProvider _provider = new();

        [TestMethod]
        public void IsSpatialStorageType_recognizes_NTS_subclasses()
        {
            _provider.IsSpatialStorageType(typeof(NetTopologySuite.Geometries.Point)).Should().BeTrue();
            _provider.IsSpatialStorageType(typeof(NetTopologySuite.Geometries.Geometry)).Should().BeTrue();
        }

        [TestMethod]
        public void IsSpatialStorageType_rejects_other_types()
        {
            _provider.IsSpatialStorageType(typeof(string)).Should().BeFalse();
        }

        [TestMethod]
        public void IgnoredStorageTypes_lists_Geometry_and_concrete_subclasses()
        {
            _provider.IgnoredStorageTypes.Should().Contain(typeof(Geometry));
            _provider.IgnoredStorageTypes.Should().Contain(typeof(NetTopologySuite.Geometries.Point));
            _provider.IgnoredStorageTypes.Should().Contain(typeof(LineString));
            _provider.IgnoredStorageTypes.Should().Contain(typeof(NetTopologySuite.Geometries.Polygon));
            _provider.IgnoredStorageTypes.Should().Contain(typeof(MultiPoint));
            _provider.IgnoredStorageTypes.Should().Contain(typeof(MultiLineString));
            _provider.IgnoredStorageTypes.Should().Contain(typeof(MultiPolygon));
            _provider.IgnoredStorageTypes.Should().Contain(typeof(GeometryCollection));
        }

        [TestMethod]
        public void InferGenus_returns_Geography_for_geography_column_type()
        {
            using var ctx = new ProbeContext();
            var prop = typeof(Probe).GetProperty(nameof(Probe.Geo));

            _provider.InferGenus(typeof(Probe), prop, ctx)
                .Should().Be(SpatialGenus.Geography);
        }

        [TestMethod]
        public void InferGenus_returns_Geometry_for_geometry_prefixed_column_type()
        {
            using var ctx = new ProbeContext();
            var prop = typeof(Probe).GetProperty(nameof(Probe.Geom));

            _provider.InferGenus(typeof(Probe), prop, ctx)
                .Should().Be(SpatialGenus.Geometry);
        }

        [TestMethod]
        public void InferGenus_returns_null_when_column_type_is_unspecified()
        {
            using var ctx = new ProbeContext();
            var prop = typeof(Probe).GetProperty(nameof(Probe.Unspecified));

            _provider.InferGenus(typeof(Probe), prop, ctx)
                .Should().BeNull();
        }

        [TestMethod]
        public void InferGenus_returns_null_when_providerContext_is_null()
        {
            var prop = typeof(Probe).GetProperty(nameof(Probe.Geo));

            _provider.InferGenus(typeof(Probe), prop, providerContext: null)
                .Should().BeNull();
        }
    }
}
