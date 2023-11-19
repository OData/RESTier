// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET6_0_OR_GREATER
using Microsoft.OData.Edm;
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Restier.Tests.Shared.Common
{

    /// <summary>
    /// 
    /// </summary>
    public class SystemTextJsonTimeOfDayConverter : JsonConverter<TimeOfDay>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override TimeOfDay Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert != typeof(TimeOfDay))
            {
                throw new ArgumentException("Object passed in was not a TimeSpan.", nameof(typeToConvert));
            }

            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value)) return default;

            return TimeOfDay.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, TimeOfDay value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

    }

}
#endif