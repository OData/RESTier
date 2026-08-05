// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Spatial;

namespace Microsoft.Restier.AspNetCore
{
    /// <summary>
    /// The default payload value converter in RESTier.
    /// </summary>
    public class RestierPayloadValueConverter : ODataPayloadValueConverter
    {
        private readonly ISpatialTypeConverter[] spatialConverters;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierPayloadValueConverter"/> class.
        /// </summary>
        public RestierPayloadValueConverter()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierPayloadValueConverter"/> class.
        /// </summary>
        /// <param name="spatialConverters">The spatial type converters to use, resolved via DI.</param>
        public RestierPayloadValueConverter(IEnumerable<ISpatialTypeConverter> spatialConverters)
        {
            this.spatialConverters = spatialConverters?.ToArray() ?? Array.Empty<ISpatialTypeConverter>();
        }

        /// <summary>
        /// Converts the given primitive value defined in a type definition from the payload object.
        /// </summary>
        /// <param name="value">The given CLR value.</param>
        /// <param name="edmTypeReference">The expected type reference from model.</param>
        /// <returns>The converted payload value of the underlying type.</returns>
        public override object ConvertToPayloadValue(object value, IEdmTypeReference edmTypeReference)
        {
            if (edmTypeReference is not null && IsSpatialEdmType(edmTypeReference) && value is not null)
            {
                var storageType = value.GetType();
                for (var i = 0; i < spatialConverters.Length; i++)
                {
                    if (spatialConverters[i].CanConvert(storageType))
                    {
                        var targetClrType = MapEdmSpatialKindToClr(edmTypeReference.PrimitiveKind());
                        if (targetClrType is not null)
                        {
                            return spatialConverters[i].ToEdm(value, targetClrType);
                        }
                    }
                }
            }

            if (edmTypeReference is not null)
            {
                // System.DateTime is shared by *Edm.Date and Edm.DateTimeOffset.
                if (value is DateTime)
                {
                    var dateTimeValue = (DateTime)value;

#pragma warning disable CS0618 // Date and TimeOfDay are obsolete but still used by OData
                    // System.DateTime[SqlType = Date] => Edm.Date
                    if (edmTypeReference.IsDate())
                    {
                        return new Date(dateTimeValue.Year, dateTimeValue.Month, dateTimeValue.Day);
                    }

                    // System.DateTime[SqlType = DateTime or DateTime2] => Edm.DateTimeOffset
                    // If DateTime.Kind equals Local, offset should equal the offset of the system's local time zone
                    if (dateTimeValue.Kind == DateTimeKind.Local)
                    {
                        return new DateTimeOffset(dateTimeValue, TimeZoneInfo.Local.GetUtcOffset(dateTimeValue));
                    }

                    return new DateTimeOffset(dateTimeValue, TimeSpan.Zero);
                }

                // System.TimeSpan is shared by *Edm.TimeOfDay and Edm.Duration:
                //   System.TimeSpan[SqlType = Time] => Edm.Library.TimeOfDay
                //   System.TimeSpan[SqlType = Time] => System.TimeSpan[EdmType = Duration]
                if (edmTypeReference.IsTimeOfDay() && value is TimeSpan)
                {
                    var timeSpanValue = (TimeSpan)value;
                    return (TimeOfDay)timeSpanValue;
                }

                // System.DateTime is converted to System.DateTimeOffset in OData Web API.
                // In order not to break ODL serialization when the EDM type is Edm.Date,
                // need to convert System.DateTimeOffset back to Edm.Date.
                if (edmTypeReference.IsDate() && value is DateTimeOffset)
                {
                    var dateTimeOffsetValue = (DateTimeOffset)value;
                    return new Date(dateTimeOffsetValue.Year, dateTimeOffsetValue.Month, dateTimeOffsetValue.Day);
                }

                // System.DateOnly => Edm.Date
                if (edmTypeReference.IsDate() && value is DateOnly dateOnlyValue)
                {
                    return new Date(dateOnlyValue.Year, dateOnlyValue.Month, dateOnlyValue.Day);
                }

                // System.TimeOnly => Edm.TimeOfDay
                if (edmTypeReference.IsTimeOfDay() && value is TimeOnly timeOnlyValue)
                {
                    return new TimeOfDay(timeOnlyValue.Hour, timeOnlyValue.Minute, timeOnlyValue.Second, timeOnlyValue.Millisecond);
                }
#pragma warning restore CS0618
            }

            return base.ConvertToPayloadValue(value, edmTypeReference);
        }

        internal static bool IsSpatialEdmType(IEdmTypeReference reference)
        {
            var kind = reference.PrimitiveKind();
            return kind == EdmPrimitiveTypeKind.Geography
                || kind == EdmPrimitiveTypeKind.GeographyPoint
                || kind == EdmPrimitiveTypeKind.GeographyLineString
                || kind == EdmPrimitiveTypeKind.GeographyPolygon
                || kind == EdmPrimitiveTypeKind.GeographyMultiPoint
                || kind == EdmPrimitiveTypeKind.GeographyMultiLineString
                || kind == EdmPrimitiveTypeKind.GeographyMultiPolygon
                || kind == EdmPrimitiveTypeKind.GeographyCollection
                || kind == EdmPrimitiveTypeKind.Geometry
                || kind == EdmPrimitiveTypeKind.GeometryPoint
                || kind == EdmPrimitiveTypeKind.GeometryLineString
                || kind == EdmPrimitiveTypeKind.GeometryPolygon
                || kind == EdmPrimitiveTypeKind.GeometryMultiPoint
                || kind == EdmPrimitiveTypeKind.GeometryMultiLineString
                || kind == EdmPrimitiveTypeKind.GeometryMultiPolygon
                || kind == EdmPrimitiveTypeKind.GeometryCollection;
        }

        private static Type MapEdmSpatialKindToClr(EdmPrimitiveTypeKind kind) => kind switch
        {
            EdmPrimitiveTypeKind.Geography => typeof(Microsoft.Spatial.Geography),
            EdmPrimitiveTypeKind.GeographyPoint => typeof(Microsoft.Spatial.GeographyPoint),
            EdmPrimitiveTypeKind.GeographyLineString => typeof(Microsoft.Spatial.GeographyLineString),
            EdmPrimitiveTypeKind.GeographyPolygon => typeof(Microsoft.Spatial.GeographyPolygon),
            EdmPrimitiveTypeKind.GeographyMultiPoint => typeof(Microsoft.Spatial.GeographyMultiPoint),
            EdmPrimitiveTypeKind.GeographyMultiLineString => typeof(Microsoft.Spatial.GeographyMultiLineString),
            EdmPrimitiveTypeKind.GeographyMultiPolygon => typeof(Microsoft.Spatial.GeographyMultiPolygon),
            EdmPrimitiveTypeKind.GeographyCollection => typeof(Microsoft.Spatial.GeographyCollection),
            EdmPrimitiveTypeKind.Geometry => typeof(Microsoft.Spatial.Geometry),
            EdmPrimitiveTypeKind.GeometryPoint => typeof(Microsoft.Spatial.GeometryPoint),
            EdmPrimitiveTypeKind.GeometryLineString => typeof(Microsoft.Spatial.GeometryLineString),
            EdmPrimitiveTypeKind.GeometryPolygon => typeof(Microsoft.Spatial.GeometryPolygon),
            EdmPrimitiveTypeKind.GeometryMultiPoint => typeof(Microsoft.Spatial.GeometryMultiPoint),
            EdmPrimitiveTypeKind.GeometryMultiLineString => typeof(Microsoft.Spatial.GeometryMultiLineString),
            EdmPrimitiveTypeKind.GeometryMultiPolygon => typeof(Microsoft.Spatial.GeometryMultiPolygon),
            EdmPrimitiveTypeKind.GeometryCollection => typeof(Microsoft.Spatial.GeometryCollection),
            _ => null,
        };
    }
}
