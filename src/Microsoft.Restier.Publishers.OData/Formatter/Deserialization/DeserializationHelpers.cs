// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Web.OData;
using System.Web.OData.Formatter.Deserialization;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Publishers.OData.Formatter.Deserialization
{
    /// <summary>
    /// Get clr type from payload.
    /// </summary>
    internal static class DeserializationHelpers
    {
        internal static object ConvertValue(object oDataValue, Type expectedReturnType, ref IEdmTypeReference propertyType, ApiBase api)
        {
            var model = api.GetModelAsync().Result;
            ODataDeserializerContext readContext = new ODataDeserializerContext
            {
                Model = model
            };

            ODataDeserializerProvider deserializerProvider = api.Context.GetApiService<ODataDeserializerProvider>();

            if (oDataValue == null)
            {
                return null;
            }

            ODataNullValue nullValue = oDataValue as ODataNullValue;
            if (nullValue != null)
            {
                return null;
            }

            ODataComplexValue complexValue = oDataValue as ODataComplexValue;
            if (complexValue != null)
            {
                ODataEdmTypeDeserializer deserializer = deserializerProvider.GetEdmTypeDeserializer(propertyType.AsComplex());
                return deserializer.ReadInline(complexValue, propertyType, readContext);
            }

            ODataEnumValue enumValue = oDataValue as ODataEnumValue;
            if (enumValue != null)
            {
                ODataEdmTypeDeserializer deserializer = deserializerProvider.GetEdmTypeDeserializer(propertyType.AsEnum());
                return deserializer.ReadInline(enumValue, propertyType, readContext);
            }

            ODataCollectionValue collection = oDataValue as ODataCollectionValue;
            if (collection != null)
            {
                ODataEdmTypeDeserializer deserializer = deserializerProvider.GetEdmTypeDeserializer(propertyType as IEdmCollectionTypeReference);
                var collectionResult = deserializer.ReadInline(collection, propertyType, readContext);

                var genericType = expectedReturnType.FindGenericType(typeof (ICollection<>));
                if (genericType != null || expectedReturnType.IsArray)
                {
                    var elementClrType = expectedReturnType.GetElementType() ??
                                         expectedReturnType.GenericTypeArguments[0];
                    var castMethodInfo = typeof(Enumerable).GetMethod("Cast").MakeGenericMethod(elementClrType);
                    var castedResult = castMethodInfo.Invoke(null, new object[] {collectionResult});

                    if (expectedReturnType.IsArray)
                    {
                        var toArrayMethodInfo = typeof(Enumerable).GetMethod("ToArray");
                        var arrayResult = toArrayMethodInfo.MakeGenericMethod(elementClrType).Invoke(null, new object[] { castedResult });
                        return arrayResult;
                    }
                    else if (genericType != null)
                    {
                        var toListMethodInfo = typeof(Enumerable).GetMethod("ToList");
                        var listResult = toListMethodInfo.MakeGenericMethod(elementClrType).Invoke(null, new object[] { castedResult });
                        return listResult;
                    }
                }

                // It means return type is IEnumerable<>
                return collectionResult;
            }

            return oDataValue;
        }
    }
}
