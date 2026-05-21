// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Model
{
    [TestClass]
    public class EdmHelpersSpatialTests
    {
        [TestMethod]
        [DataRow(typeof(GeographyPoint), EdmPrimitiveTypeKind.GeographyPoint)]
        [DataRow(typeof(GeographyLineString), EdmPrimitiveTypeKind.GeographyLineString)]
        [DataRow(typeof(GeographyPolygon), EdmPrimitiveTypeKind.GeographyPolygon)]
        [DataRow(typeof(GeographyMultiPoint), EdmPrimitiveTypeKind.GeographyMultiPoint)]
        [DataRow(typeof(GeographyMultiLineString), EdmPrimitiveTypeKind.GeographyMultiLineString)]
        [DataRow(typeof(GeographyMultiPolygon), EdmPrimitiveTypeKind.GeographyMultiPolygon)]
        [DataRow(typeof(GeographyCollection), EdmPrimitiveTypeKind.GeographyCollection)]
        [DataRow(typeof(Geography), EdmPrimitiveTypeKind.Geography)]
        [DataRow(typeof(GeometryPoint), EdmPrimitiveTypeKind.GeometryPoint)]
        [DataRow(typeof(GeometryLineString), EdmPrimitiveTypeKind.GeometryLineString)]
        [DataRow(typeof(GeometryPolygon), EdmPrimitiveTypeKind.GeometryPolygon)]
        [DataRow(typeof(GeometryMultiPoint), EdmPrimitiveTypeKind.GeometryMultiPoint)]
        [DataRow(typeof(GeometryMultiLineString), EdmPrimitiveTypeKind.GeometryMultiLineString)]
        [DataRow(typeof(GeometryMultiPolygon), EdmPrimitiveTypeKind.GeometryMultiPolygon)]
        [DataRow(typeof(GeometryCollection), EdmPrimitiveTypeKind.GeometryCollection)]
        [DataRow(typeof(Geometry), EdmPrimitiveTypeKind.Geometry)]
        public void GetPrimitiveTypeReference_recognizes_Microsoft_Spatial_types(Type clrType, EdmPrimitiveTypeKind expected)
        {
            var reference = clrType.GetPrimitiveTypeReference();
            reference.Should().NotBeNull();
            reference.PrimitiveKind().Should().Be(expected);
        }
    }
}
