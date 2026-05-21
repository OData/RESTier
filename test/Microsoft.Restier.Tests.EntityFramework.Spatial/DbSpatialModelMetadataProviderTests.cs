// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Data.Entity.Spatial;
using FluentAssertions;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFramework.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFramework.Spatial
{
    [TestClass]
    public class DbSpatialModelMetadataProviderTests
    {
        private class Probe
        {
            public DbGeography Geo { get; set; }
            public DbGeometry Geom { get; set; }
            public string NotSpatial { get; set; }
        }

        private readonly DbSpatialModelMetadataProvider _provider = new();

        [TestMethod]
        public void IsSpatialStorageType_recognizes_DbGeography_and_DbGeometry()
        {
            _provider.IsSpatialStorageType(typeof(DbGeography)).Should().BeTrue();
            _provider.IsSpatialStorageType(typeof(DbGeometry)).Should().BeTrue();
        }

        [TestMethod]
        public void IsSpatialStorageType_rejects_other_types()
        {
            _provider.IsSpatialStorageType(typeof(string)).Should().BeFalse();
            _provider.IsSpatialStorageType(typeof(int)).Should().BeFalse();
        }

        [TestMethod]
        public void IgnoredStorageTypes_lists_DbGeography_and_DbGeometry()
        {
            _provider.IgnoredStorageTypes
                .Should().BeEquivalentTo(new[] { typeof(DbGeography), typeof(DbGeometry) });
        }

        [TestMethod]
        public void InferGenus_returns_Geography_for_DbGeography_property()
        {
            var prop = typeof(Probe).GetProperty(nameof(Probe.Geo));
            _provider.InferGenus(typeof(Probe), prop, providerContext: null)
                .Should().Be(SpatialGenus.Geography);
        }

        [TestMethod]
        public void InferGenus_returns_Geometry_for_DbGeometry_property()
        {
            var prop = typeof(Probe).GetProperty(nameof(Probe.Geom));
            _provider.InferGenus(typeof(Probe), prop, providerContext: null)
                .Should().Be(SpatialGenus.Geometry);
        }

        [TestMethod]
        public void InferGenus_returns_null_for_non_spatial_property()
        {
            var prop = typeof(Probe).GetProperty(nameof(Probe.NotSpatial));
            _provider.InferGenus(typeof(Probe), prop, providerContext: null)
                .Should().BeNull();
        }
    }
}
