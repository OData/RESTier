// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.Restier.AspNetCore.Formatter
{

    /// <summary>
    /// Get clr type from payload.
    /// </summary>
    internal static class DeserializationHelpers
    {
        private delegate object ConvertDelegate(object odataValue, IEdmTypeReference propertyType, Type expectedReturnType, string parameterName, ODataDeserializerContext readContext, IServiceProvider serviceProvider);
        private static readonly ConvertDelegate convert = Type
            .GetType("Microsoft.AspNetCore.OData.Formatter.ODataModelBinderConverter, Microsoft.AspNetCore.OData")
            .GetMethod("Convert", new Type[] { typeof(object), typeof(IEdmTypeReference), typeof(Type), typeof(string), typeof(ODataDeserializerContext), typeof(IServiceProvider) })
            .CreateDelegate<ConvertDelegate>(null);

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
            HttpRequest request,
            IServiceProvider serviceProvider)
        {
            // OData URL parser represents ?p=null as a ConstantNode whose Value is null.
            // ODataModelBinderConverter.Convert passes ConstantNode.Value (= null) to
            // EdmPrimitiveHelper.ConvertPrimitiveValue, which throws NullReferenceException.
            // Short-circuit here for nullable parameters (issue #656).
            if (odataValue is Microsoft.OData.UriParser.ConstantNode constNode && constNode.Value is null
                && (propertyType is null || propertyType.IsNullable))
            {
                return null;
            }

            var readContext = new ODataDeserializerContext
            {
                Model = model,
                Request = request,
            };

            var returnValue = convert(odataValue, propertyType, expectedReturnType, parameterName, readContext, serviceProvider);
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
