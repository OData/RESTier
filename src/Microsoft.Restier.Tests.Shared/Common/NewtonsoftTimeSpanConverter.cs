// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if !NET6_0_OR_GREATER
using System;
using System.Xml;
using Newtonsoft.Json;

namespace Microsoft.Restier.Tests.Shared.Common
{

    /// <summary>
    /// 
    /// </summary>
    public class NewtonsoftTimeSpanConverter : JsonConverter
    {

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeSpan);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType != typeof(TimeSpan))
            {
                throw new ArgumentException("Object passed in was not a TimeSpan.", nameof(objectType));
            }

            if (!(reader.Value is string spanString))
            {
                return null;
            }

            if (spanString.Contains("-") && spanString.IndexOf("-", StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                spanString = $"-{spanString.Replace("-", "")}";
            }
            return XmlConvert.ToTimeSpan(spanString);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var duration = (TimeSpan)value;
            writer.WriteValue(XmlConvert.ToString(duration));
        }

    }

}
#endif