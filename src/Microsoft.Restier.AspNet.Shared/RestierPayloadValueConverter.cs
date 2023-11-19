// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData;
using Microsoft.OData.Edm;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.AspNetCore
#else
namespace Microsoft.Restier.AspNet
#endif
{
    /// <summary>
    /// The default payload value converter in RESTier.
    /// </summary>
    public class RestierPayloadValueConverter : ODataPayloadValueConverter
    {
        /// <summary>
        /// Converts the given primitive value defined in a type definition from the payload object.
        /// </summary>
        /// <param name="value">The given CLR value.</param>
        /// <param name="edmTypeReference">The expected type reference from model.</param>
        /// <returns>The converted payload value of the underlying type.</returns>
        public override object ConvertToPayloadValue(object value, IEdmTypeReference edmTypeReference)
        {
            if (edmTypeReference is not null)
            {
                // System.DateTime is shared by *Edm.Date and Edm.DateTimeOffset.
                if (value is DateTime)
                {
                    var dateTimeValue = (DateTime)value;

                    // System.DateTime[SqlType = Date] => Edm.Library.Date
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
            }

            return base.ConvertToPayloadValue(value, edmTypeReference);
        }
    }
}
