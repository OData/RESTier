// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if !NET6_0_OR_GREATER
using System;
using System.Globalization;
using Microsoft.OData.Edm;
using Newtonsoft.Json;

namespace Microsoft.Restier.Tests.Shared.Common
{

    /// <summary>
    /// 
    /// </summary>
    public class NewtonsoftTimeOfDayConverter : JsonConverter
    {

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeOfDay);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType != typeof(TimeOfDay))
            {
                throw new ArgumentException("Object passed in was not a TimeOfDay.", nameof(objectType));
            }

            if (!(reader.Value is string spanString))
            {
                return null;
            }

            return TimeOfDay.Parse(spanString, CultureInfo.InvariantCulture);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var duration = (TimeOfDay)value;
            writer.WriteValue(duration.ToString());
        }

    }

}
#endif