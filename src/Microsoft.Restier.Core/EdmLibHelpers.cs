// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Core.Properties;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Offers a collection of helper methods for <see cref="IEdmType"/>.
    /// </summary>
    public static class EdmLibHelpers
    {
        /// <summary>
        /// Gets the EDM type reference for the EDM type definition.
        /// </summary>
        /// <param name="edmType">The EDM type definition.</param>
        /// <param name="isNullable">Indicates whether the type is nullable.</param>
        /// <returns>The created EDM type reference.</returns>
        public static IEdmTypeReference GetEdmTypeReference(this IEdmType edmType, bool isNullable)
        {
            Ensure.NotNull(edmType, "edmType");

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
                    throw Error.NotSupported(Resources.EdmTypeNotSupported, edmType.ToTraceString());
            }
        }
    }
}
