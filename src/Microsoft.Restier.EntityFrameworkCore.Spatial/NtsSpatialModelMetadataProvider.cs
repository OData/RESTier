// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Restier.Core.Spatial;
using NetTopologySuite.Geometries;

namespace Microsoft.Restier.EntityFrameworkCore.Spatial
{
    /// <summary>
    /// EF Core implementation of <see cref="ISpatialModelMetadataProvider"/>. Infers Geography vs Geometry
    /// genus by reading the EF Core mutable model's relational column type for the property
    /// (e.g. <c>"geography"</c>, <c>"geometry(Point,4326)"</c>).
    /// </summary>
    public class NtsSpatialModelMetadataProvider : ISpatialModelMetadataProvider
    {
        private static readonly Type[] StorageTypes =
        {
            typeof(Geometry),
            typeof(NetTopologySuite.Geometries.Point),
            typeof(LineString),
            typeof(NetTopologySuite.Geometries.Polygon),
            typeof(MultiPoint),
            typeof(MultiLineString),
            typeof(MultiPolygon),
            typeof(GeometryCollection),
        };

        /// <inheritdoc />
        public bool IsSpatialStorageType(Type clrType)
        {
            if (clrType is null)
            {
                return false;
            }

            return typeof(Geometry).IsAssignableFrom(clrType);
        }

        /// <inheritdoc />
        public SpatialGenus? InferGenus(Type entityClrType, PropertyInfo property, object providerContext)
        {
            if (providerContext is not DbContext dbContext)
            {
                return null;
            }

            var efEntityType = dbContext.Model.FindEntityType(entityClrType);
            var efProperty = efEntityType?.FindProperty(property.Name);
            var columnType = efProperty?.FindAnnotation("Relational:ColumnType")?.Value as string;

            if (string.IsNullOrEmpty(columnType))
            {
                return null;
            }

            if (columnType.StartsWith("geography", StringComparison.OrdinalIgnoreCase))
            {
                return SpatialGenus.Geography;
            }

            if (columnType.StartsWith("geometry", StringComparison.OrdinalIgnoreCase))
            {
                return SpatialGenus.Geometry;
            }

            return null;
        }

        /// <inheritdoc />
        public IReadOnlyList<Type> IgnoredStorageTypes => StorageTypes;
    }
}
