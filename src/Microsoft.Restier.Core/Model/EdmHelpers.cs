// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Model
{
    /// <summary>
    /// An utility class to operate with Edm model.
    /// </summary>
    internal static class EdmHelpers
    {
        /// <summary>
        /// Get the type reference based on Edm type
        /// </summary>
        /// <param name="edmType">The edm type to retrieve Edm type reference</param>
        /// <returns>The edm type reference</returns>
        public static IEdmTypeReference GetTypeReference(this IEdmType edmType)
        {
            Ensure.NotNull(edmType, "edmType");

            var isNullable = false;
            switch (edmType.TypeKind)
            {
                case EdmTypeKind.Collection:
                    return new EdmCollectionTypeReference(edmType as IEdmCollectionType);
                case EdmTypeKind.Complex:
                    return new EdmComplexTypeReference(edmType as IEdmComplexType, isNullable);
                case EdmTypeKind.Entity:
                    return new EdmEntityTypeReference(edmType as IEdmEntityType, isNullable);
                case EdmTypeKind.EntityReference:
                    return new EdmEntityReferenceTypeReference(edmType as IEdmEntityReferenceType, isNullable);
                case EdmTypeKind.Enum:
                    return new EdmEnumTypeReference(edmType as IEdmEnumType, isNullable);
                case EdmTypeKind.Primitive:
                    return new EdmPrimitiveTypeReference(edmType as IEdmPrimitiveType, isNullable);
                default:
                    string message = string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.EdmTypeNotSupported,
                        edmType.ToTraceString());
                    throw new NotSupportedException(message);
            }
        }
    }
}
