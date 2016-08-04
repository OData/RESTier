// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Data.Entity.Spatial;
using System.Globalization;
using System.Text;
using Microsoft.Spatial;

namespace Microsoft.Restier.Providers.EntityFramework
{
    /// <summary>
    /// The class defined conversion between GeographyPoint and DbGeography,
    /// and between GeographyLineString and DbGeography.
    /// </summary>
    public static class GeographyConverter
    {
        private const string GeographyTypeNamePoint = "Point";
        private const string GeographyTypeNameLineString = "LineString";
        private static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo("En-Us");

        /// <summary>
        /// Convert a DbGeography to Edm GeographyPoint
        /// </summary>
        /// <param name="geography"> The DbGeography to be converted</param>
        /// <returns>A Edm GeographyPoint</returns>
        public static GeographyPoint ToGeographyPoint(this DbGeography geography)
        {
            if (geography == null)
            {
                return null;
            }

            if (geography.SpatialTypeName != GeographyTypeNamePoint)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.InvalidPointGeographyType,
                    geography.SpatialTypeName));
            }

            double lat = geography.Latitude ?? 0;
            double lon = geography.Longitude ?? 0;
            double? alt = geography.Elevation;
            double? m = geography.Measure;
            return GeographyPoint.Create(lat, lon, alt, m);
        }

        /// <summary>
        /// Convert a Edm GeographyPoint to DbGeography
        /// </summary>
        /// <param name="point">The Edm GeographyPoint to be converted</param>
        /// <returns>A DbGeography</returns>
        public static DbGeography ToDbGeography(this GeographyPoint point)
        {
            if (point == null)
            {
                return null;
            }

            string text = "POINT(" + point.Latitude.ToString(DefaultCulture) + " " +
                     point.Longitude.ToString(DefaultCulture);

            if (point.Z.HasValue)
            {
                text += " " + point.Z.Value;
            }

            if (point.M.HasValue)
            {
                text += " " + point.M.Value;
            }

            text += ")";

            return DbGeography.FromText(text);
        }

        /// <summary>
        /// Convert a DbGeography to Edm GeographyPoint
        /// </summary>
        /// <param name="geography"> The DbGeography to be converted</param>
        /// <returns>A Edm GeographyLineString</returns>
        public static GeographyLineString ToGeographyLineString(this DbGeography geography)
        {
            if (geography == null)
            {
                return null;
            }

            if (geography.SpatialTypeName != GeographyTypeNameLineString)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.InvalidLineStringGeographyType,
                    geography.SpatialTypeName));
            }

            SpatialBuilder builder = SpatialBuilder.Create();
            GeographyPipeline pipleLine = builder.GeographyPipeline;
            pipleLine.SetCoordinateSystem(CoordinateSystem.DefaultGeography);
            pipleLine.BeginGeography(SpatialType.LineString);

            int numPoints = geography.PointCount ?? 0;
            if (numPoints > 0)
            {
                DbGeography point = geography.PointAt(1);
                pipleLine.BeginFigure(new GeographyPosition(
                    point.Latitude ?? 0, point.Latitude ?? 0, point.Elevation, point.Measure));

                for (int n = 2; n <= numPoints; n++)
                {
                    point = geography.PointAt(n);
                    pipleLine.LineTo(new GeographyPosition(
                        point.Latitude ?? 0, point.Latitude ?? 0, point.Elevation, point.Measure));
                }

                pipleLine.EndFigure();
            }

            pipleLine.EndGeography();
            GeographyLineString lineString = (GeographyLineString)builder.ConstructedGeography;
            return lineString;
        }

        /// <summary>
        /// Convert a Edm GeographyLineString to DbGeography
        /// </summary>
        /// <param name="lineString">The Edm GeographyLineString to be converted</param>
        /// <returns>A DbGeography</returns>
        public static DbGeography ToDbGeography(this GeographyLineString lineString)
        {
            if (lineString == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder("LINESTRING(");
            int n = 0;
            foreach (var pt in lineString.Points)
            {
                double lat = pt.Latitude;
                double lon = pt.Longitude;
                double? alt = pt.Z;
                double? m = pt.M;

                string pointStr = lat.ToString(DefaultCulture) + " " + lon.ToString(DefaultCulture);

                if (alt != null)
                {
                    pointStr += " " + alt.Value;
                }

                if (m != null)
                {
                    pointStr += " " + m.Value;
                }

                sb.Append(pointStr);
                n++;
                if (n != lineString.Points.Count)
                {
                    sb.Append(",");
                }
            }

            sb.Append(")");

            return DbGeography.FromText(sb.ToString());
        }
    }
}
