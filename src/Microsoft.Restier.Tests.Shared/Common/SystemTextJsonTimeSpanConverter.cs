// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET6_0_OR_GREATER
using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace Microsoft.Restier.Tests.Shared.Common
{

    /// <summary>
    /// 
    /// </summary>
    public class SystemTextJsonTimeSpanConverter : JsonConverter<TimeSpan>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert != typeof(TimeSpan))
            {
                throw new ArgumentException("Object passed in was not a TimeSpan.", nameof(typeToConvert));
            }

            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value)) return default;

  
            if (value.Contains("-") && value.IndexOf("-", StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                value = $"-{value.Replace("-", "")}";
            }
            return XmlConvert.ToTimeSpan(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(XmlConvert.ToString(value));
        }

    }

}
#endif