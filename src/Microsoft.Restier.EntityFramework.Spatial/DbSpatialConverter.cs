// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Data.Entity.Spatial;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Spatial;

namespace Microsoft.Restier.EntityFramework.Spatial
{
    /// <summary>
    /// Round-trips between Microsoft.Spatial values and EF6 <see cref="DbGeography"/> / <see cref="DbGeometry"/>
    /// via the SQL Server extended WKT dialect (with <c>SRID=N;</c> prefix).
    /// </summary>
    public class DbSpatialConverter : ISpatialTypeConverter
    {
        private static readonly WellKnownTextSqlFormatter Formatter
            = WellKnownTextSqlFormatter.Create(allowOnlyTwoDimensions: false);

        /// <inheritdoc />
        public bool CanConvert(Type storageType)
        {
            if (storageType is null)
            {
                return false;
            }

            return typeof(DbGeography).IsAssignableFrom(storageType)
                || typeof(DbGeometry).IsAssignableFrom(storageType);
        }

        /// <inheritdoc />
        public object ToEdm(object storageValue, Type targetEdmType)
        {
            if (storageValue is null)
            {
                return null;
            }

            string bareWkt;
            int srid;

            if (storageValue is DbGeography geography)
            {
                bareWkt = DbSpatialServices.Default.AsTextIncludingElevationAndMeasure(geography);
                srid = geography.CoordinateSystemId;
            }
            else if (storageValue is DbGeometry geometry)
            {
                bareWkt = DbSpatialServices.Default.AsTextIncludingElevationAndMeasure(geometry);
                srid = geometry.CoordinateSystemId;
            }
            else
            {
                throw new NotSupportedException(
                    $"DbSpatialConverter does not handle storage type '{storageValue.GetType().FullName}'.");
            }

            var sridPrefixed = SridPrefixHelpers.FormatWithSridPrefix(srid, bareWkt);

            using var reader = new StringReader(sridPrefixed);
            var readMethod = typeof(WellKnownTextSqlFormatter)
                .GetMethod(nameof(WellKnownTextSqlFormatter.Read), BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(TextReader) }, null)
                .MakeGenericMethod(targetEdmType);
            return readMethod.Invoke(Formatter, new object[] { reader });
        }

        /// <inheritdoc />
        public object ToStorage(Type targetStorageType, object edmValue)
        {
            if (edmValue is null)
            {
                return null;
            }

            int srid;
            if (edmValue is Geography g)
            {
                srid = g.CoordinateSystem.EpsgId
                    ?? throw new InvalidOperationException(
                        $"Cannot convert Microsoft.Spatial value with non-EPSG CoordinateSystem '{g.CoordinateSystem.Id}'.");
            }
            else if (edmValue is Geometry m)
            {
                srid = m.CoordinateSystem.EpsgId
                    ?? throw new InvalidOperationException(
                        $"Cannot convert Microsoft.Spatial value with non-EPSG CoordinateSystem '{m.CoordinateSystem.Id}'.");
            }
            else
            {
                throw new NotSupportedException(
                    $"DbSpatialConverter does not handle EDM type '{edmValue.GetType().FullName}'.");
            }

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                Formatter.Write((ISpatial)edmValue, writer);
            }

            var (_, body) = SridPrefixHelpers.ParseSridPrefix(sb.ToString());

            if (typeof(DbGeography).IsAssignableFrom(targetStorageType))
            {
                return DbGeography.FromText(body, srid);
            }

            if (typeof(DbGeometry).IsAssignableFrom(targetStorageType))
            {
                return DbGeometry.FromText(body, srid);
            }

            throw new NotSupportedException(
                $"DbSpatialConverter does not produce values of type '{targetStorageType.FullName}'.");
        }
    }
}
