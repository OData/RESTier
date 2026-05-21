// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Spatial;
using NetTopologySuite.Geometries;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Spatial
{
    [TestClass]
    public class EFChangeSetInitializerSpatialTests
    {
        [TestMethod]
        public void EFCore_ConvertToEfValue_dispatches_to_registered_spatial_converter()
        {
            var ntsPoint = new NetTopologySuite.Geometries.GeometryFactory(new PrecisionModel(), 4326)
                .CreatePoint(new Coordinate(1, 2));
            var fakeEdm = GeographyPoint.Create(
                CoordinateSystem.Geography(4326), 2, 1, null, null);

            var converter = Substitute.For<ISpatialTypeConverter>();
            converter.CanConvert(typeof(Point)).Returns(true);
            converter.ToStorage(typeof(Point), fakeEdm).Returns(ntsPoint);

            var initializer = new EFChangeSetInitializer(new[] { converter });
            var result = initializer.ConvertToEfValue(typeof(Point), fakeEdm);

            result.Should().BeSameAs(ntsPoint);
        }

        [TestMethod]
        public void EFCore_ConvertToEfValue_passes_through_when_no_converter_registered()
        {
            var initializer = new EFChangeSetInitializer();
            var result = initializer.ConvertToEfValue(typeof(int), 42);
            result.Should().Be(42);
        }
    }
}
