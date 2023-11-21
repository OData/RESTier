// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
#if !NET6_0_OR_GREATER
using System.Net.Http;
#endif
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNet.OData.Formatter.Deserialization;
#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Http;
#endif
using Microsoft.OData.Edm;

#if NET6_0_OR_GREATER
namespace Microsoft.Restier.AspNetCore.Formatter
#else
namespace Microsoft.Restier.AspNet.Formatter
#endif
{

    /// <summary>
    /// Get clr type from payload.
    /// </summary>
    internal static class DeserializationHelpers
    {
        /// <summary>
        /// Converts an OData value into a CLR object.
        /// </summary>
        /// <param name="odataValue">The value to convert.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="expectedReturnType">The expected return type.</param>
        /// <param name="propertyType">The property type.</param>
        /// <param name="model">The model.</param>
        /// <param name="request">The request.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>The converted value.</returns>
        internal static object ConvertValue(
            object odataValue,
            string parameterName,
            Type expectedReturnType,
            IEdmTypeReference propertyType,
            IEdmModel model,
#if NET6_0_OR_GREATER
            HttpRequest request,
#else
            HttpRequestMessage request,
#endif
            IServiceProvider serviceProvider)
        {
            var readContext = new ODataDeserializerContext
            {
                Model = model,
                Request = request,
            };

            var returnValue = ODataModelBinderConverter.Convert(odataValue, propertyType, expectedReturnType, parameterName, readContext, serviceProvider);

            if (!propertyType.IsCollection())
            {
                return returnValue;
            }

            return ConvertCollectionType(returnValue, expectedReturnType);
        }

        /// <summary>
        /// Converts a Collection type.
        /// </summary>
        /// <param name="collectionResult">The collection to convert.</param>
        /// <param name="expectedReturnType">The expected return type.</param>
        /// <returns>The converted collection.</returns>
        internal static object ConvertCollectionType(object collectionResult, Type expectedReturnType)
        {
            if (collectionResult is null)
            {
                return null;
            }

            var genericType = expectedReturnType.FindGenericType(typeof(ICollection<>));
            if (genericType is not null || expectedReturnType.IsArray)
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
                else if (genericType is not null)
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
            if (genericType is not null && returnGenericType is not null)
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
