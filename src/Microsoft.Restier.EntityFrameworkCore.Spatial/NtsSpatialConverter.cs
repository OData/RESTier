// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Spatial;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Microsoft.Restier.EntityFrameworkCore.Spatial
{
    /// <summary>
    /// Round-trips between Microsoft.Spatial values and NetTopologySuite <see cref="NetTopologySuite.Geometries.Geometry"/> values
    /// via the SQL Server extended WKT dialect (with <c>SRID=N;</c> prefix).
    /// </summary>
    public class NtsSpatialConverter : ISpatialTypeConverter
    {
        private static readonly WellKnownTextSqlFormatter Formatter
            = WellKnownTextSqlFormatter.Create(allowOnlyTwoDimensions: false);

        private static readonly WKTWriter NtsWriter = new(4) { OutputOrdinates = Ordinates.XYZM };

        /// <inheritdoc />
        public bool CanConvert(Type storageType)
        {
            if (storageType is null)
            {
                return false;
            }

            return typeof(NetTopologySuite.Geometries.Geometry).IsAssignableFrom(storageType);
        }

        /// <inheritdoc />
        public object ToEdm(object storageValue, Type targetEdmType)
        {
            if (storageValue is null)
            {
                return null;
            }

            if (storageValue is not NetTopologySuite.Geometries.Geometry geometry)
            {
                throw new NotSupportedException(
                    $"NtsSpatialConverter does not handle storage type '{storageValue.GetType().FullName}'.");
            }

            var bareWkt = NtsWriter.Write(geometry);
            var sridPrefixed = SridPrefixHelpers.FormatWithSridPrefix(geometry.SRID, bareWkt);

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
            if (edmValue is Microsoft.Spatial.Geography g)
            {
                srid = g.CoordinateSystem.EpsgId
                    ?? throw new InvalidOperationException(
                        $"Cannot convert Microsoft.Spatial value with non-EPSG CoordinateSystem '{g.CoordinateSystem.Id}'.");
            }
            else if (edmValue is Microsoft.Spatial.Geometry m)
            {
                srid = m.CoordinateSystem.EpsgId
                    ?? throw new InvalidOperationException(
                        $"Cannot convert Microsoft.Spatial value with non-EPSG CoordinateSystem '{m.CoordinateSystem.Id}'.");
            }
            else
            {
                throw new NotSupportedException(
                    $"NtsSpatialConverter does not handle EDM type '{edmValue.GetType().FullName}'.");
            }

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                Formatter.Write((ISpatial)edmValue, writer);
            }

            var (_, body) = SridPrefixHelpers.ParseSridPrefix(sb.ToString());

            var ntsReader = new WKTReader();
            var result = ntsReader.Read(body);
            result.SRID = srid;

            if (!targetStorageType.IsAssignableFrom(result.GetType()))
            {
                throw new NotSupportedException(
                    $"Parsed NTS geometry of type '{result.GetType().Name}' is not assignable to target type '{targetStorageType.FullName}'.");
            }

            return result;
        }
    }
}
