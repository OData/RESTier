// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
            ApiContext apiContext)
        {
            ODataDeserializerContext readContext = new ODataDeserializerContext
            {
                Model = model
            };

            ODataDeserializerProvider deserializerProvider = apiContext.GetApiService<ODataDeserializerProvider>();

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

                return ConvertCollectionType(collectionResult, expectedReturnType);
            }

            return odataValue;
        }

        internal static object ConvertCollectionType(object collectionResult, Type expectedReturnType)
        {
            if (collectionResult == null)
            {
                return null;
            }

            var genericType = expectedReturnType.FindGenericType(typeof(ICollection<>));
            if (genericType != null || expectedReturnType.IsArray)
            {
                var elementClrType = expectedReturnType.GetElementType() ??
                                     expectedReturnType.GenericTypeArguments[0];
                var castMethodInfo = ExpressionHelperMethods.EnumerableCastGeneric.MakeGenericMethod(elementClrType);
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

            // There is case where expected type is IEnumerable<Type> but actual type is IEnumerable<Type?>,
            // need some convert
            genericType = collectionResult.GetType().FindGenericType(typeof(IEnumerable<>));
            var returnGenericType = expectedReturnType.FindGenericType(typeof(IEnumerable<>));
            if (genericType != null && returnGenericType != null)
            {
                var actualElementType = genericType.GenericTypeArguments[0];
                var expectElementType = returnGenericType.GenericTypeArguments[0];
                if (actualElementType != expectedReturnType)
                {
                    var castMethodInfo = ExpressionHelperMethods
                        .EnumerableCastGeneric.MakeGenericMethod(expectElementType);
                    var castedResult = castMethodInfo.Invoke(null, new object[] { collectionResult });
                    return castedResult;
                }
            }

            // It means return type is IEnumerable<> or raw type is passed in value is single value
            return collectionResult;
        }
    }
}
