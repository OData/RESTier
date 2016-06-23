// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web.OData.Formatter.Deserialization;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Publishers.OData.Formatter.Deserialization
{
    /// <summary>
    /// Get clr type from payload.
    /// </summary>
    internal static class DeserializationHelpers
    {
        internal static object ConvertValue(
            object odataValue,
            Type expectedReturnType,
            IEdmTypeReference propertyType,
            IEdmModel model,
            ApiBase api)
        {
            ODataDeserializerContext readContext = new ODataDeserializerContext
            {
                Model = model
            };

            ODataDeserializerProvider deserializerProvider = api.Context.GetApiService<ODataDeserializerProvider>();

            if (odataValue == null)
            {
                return null;
            }

            ODataNullValue nullValue = odataValue as ODataNullValue;
            if (nullValue != null)
            {
                return null;
            }

            ODataComplexValue complexValue = odataValue as ODataComplexValue;
            if (complexValue != null)
            {
                ODataEdmTypeDeserializer deserializer
                    = deserializerProvider.GetEdmTypeDeserializer(propertyType.AsComplex());
                return deserializer.ReadInline(complexValue, propertyType, readContext);
            }

            ODataEnumValue enumValue = odataValue as ODataEnumValue;
            if (enumValue != null)
            {
                ODataEdmTypeDeserializer deserializer
                    = deserializerProvider.GetEdmTypeDeserializer(propertyType.AsEnum());
                return deserializer.ReadInline(enumValue, propertyType, readContext);
            }

            ODataCollectionValue collection = odataValue as ODataCollectionValue;
            if (collection != null)
            {
                ODataEdmTypeDeserializer deserializer
                    = deserializerProvider.GetEdmTypeDeserializer(propertyType as IEdmCollectionTypeReference);
                var collectionResult = deserializer.ReadInline(collection, propertyType, readContext);

                var genericType = expectedReturnType.FindGenericType(typeof(ICollection<>));
                if (genericType != null || expectedReturnType.IsArray)
                {
                    var elementClrType = expectedReturnType.GetElementType() ??
                                         expectedReturnType.GenericTypeArguments[0];
                    var castMethodInfo = ExpressionHelperMethods.EnumerableCastGeneric
                        .MakeGenericMethod(elementClrType);
                    var castedResult = castMethodInfo.Invoke(null, new object[] { collectionResult });

                    if (expectedReturnType.IsArray)
                    {
                        var toArrayMethodInfo = ExpressionHelperMethods.EnumerableToArrayGeneric
                            .MakeGenericMethod(elementClrType);
                        var arrayResult = toArrayMethodInfo.Invoke(null, new object[] { castedResult });
                        return arrayResult;
                    }
                    else if (genericType != null)
                    {
                        var toListMethodInfo = ExpressionHelperMethods.EnumerableToListGeneric
                            .MakeGenericMethod(elementClrType);
                        var listResult = toListMethodInfo.Invoke(null, new object[] { castedResult });
                        return listResult;
                    }
                }

                // It means return type is IEnumerable<>
                return collectionResult;
            }

            return odataValue;
        }
    }
}
